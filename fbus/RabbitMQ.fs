module FBus.RabbitMQ
open FBus
open RabbitMQ.Client
open RabbitMQ.Client.Events

let getClientQueue name = sprintf "fbus:client:%s" name

let getExchangeName (msgType: string) = msgType |> sprintf "fbus:type:%s"

type Transport(conn: IConnection, channel: IModel) =
    static let tryCreate (conn: IConnection) (channel: IModel) (busBuilder: BusBuilder) msgCallback =
        let configureAck () =
            channel.BasicQos(prefetchSize = 0ul, prefetchCount = 1us, ``global`` = false)
            channel.ConfirmSelect()

        let queueName = getClientQueue busBuilder.Name

        let configureDeadLettersQueues() =
           // ===============================================================================================
            // dead letter queues are bound to a single exchange (direct) - the routingKey is the target queue
            // ===============================================================================================
            let xchgDeadLetter = "fbus:dead-letter"
            let deadLetterQueueName = queueName + ":dead-letter"
            if busBuilder.IsEphemeral then Map.empty
            else
                channel.ExchangeDeclare(exchange = xchgDeadLetter,
                                        ``type`` = ExchangeType.Direct,
                                        durable = true, autoDelete = false)
                channel.QueueDeclare(queue = deadLetterQueueName,
                                     durable = true, exclusive = false, autoDelete = false) |> ignore
                channel.QueueBind(queue = deadLetterQueueName, exchange = xchgDeadLetter, routingKey = deadLetterQueueName)

                Map [ "x-dead-letter-exchange", xchgDeadLetter :> obj
                      "x-dead-letter-routing-key", deadLetterQueueName :> obj ]

        let configureQueues queueArgs = 
            // =========================================================================================
            // message queues are bound to an exchange (fanout) - all bound subscribers receive messages
            // =========================================================================================
            channel.QueueDeclare(queueName,
                                 durable = true, exclusive = false, autoDelete = busBuilder.IsEphemeral,
                                 arguments = queueArgs) |> ignore

        let bindExchangeAndQueue xchgName =
            channel.ExchangeDeclare(exchange = xchgName, ``type`` = ExchangeType.Fanout, 
                                    durable = true, autoDelete = false)
            channel.QueueBind(queue = queueName, exchange = xchgName, routingKey = "")

        let subscribeMessages () =
            busBuilder.Handlers |> Map.iter (fun msgType _ -> msgType |> bindExchangeAndQueue)


        let listenMessages () =
            let consumer = EventingBasicConsumer(channel)
            let consumerCallback msgCallback (ea: BasicDeliverEventArgs) =
                try
                    let msgType = match ea.BasicProperties.Headers.TryGetValue "fbus:msgtype" with
                                  | true, (:? (byte[]) as msgType) -> System.Text.Encoding.UTF8.GetString(msgType)
                                  | _ -> failwithf "Missing header fbus:msgtype"

                    let headers = ea.BasicProperties.Headers |> Seq.map (fun kvp -> kvp.Key, System.Text.Encoding.UTF8.GetString(kvp.Value :?> byte[]))
                                                             |> Map

                    msgCallback headers msgType ea.Body

                    channel.BasicAck(deliveryTag = ea.DeliveryTag, multiple = false)
                with
                    | exn -> // TODO: report exception to someone
                             printfn "Failed to process message %A" exn
                             channel.BasicNack(deliveryTag = ea.DeliveryTag, multiple = false, requeue = false)

            consumer.Received.Add (consumerCallback msgCallback)
            channel.BasicConsume(queue = queueName, autoAck = false, consumer = consumer) |> ignore

        configureAck()
        configureDeadLettersQueues() |> configureQueues
        subscribeMessages ()
        listenMessages()
        new Transport(conn, channel) :> IBusTransport

    let send headers xchgName routingKey msgType body =
        let headers = headers |> Map.map (fun _ v -> v :> obj) |> Map.add "fbus:msgtype" (msgType :> obj)
        let props = channel.CreateBasicProperties(Headers = headers )
        channel.BasicPublish(exchange = xchgName,
                             routingKey = routingKey,
                             basicProperties = props,
                             body = body)

    interface IBusTransport with
        member _.Publish headers msgType body =
            let xchgName = getExchangeName msgType
            send headers xchgName "" msgType body

        member _.Send headers client msgType body =
            let routingKey = getClientQueue client
            send headers "" routingKey msgType body

        member _.Dispose() =
            channel.Dispose()
            conn.Dispose()


    static member Create (busBuilder: BusBuilder) msgCallback =
        let factory = ConnectionFactory(Uri = busBuilder.Uri)
        let conn = factory.CreateConnection()
        try
            let channel = conn.CreateModel()
            try
                tryCreate conn channel busBuilder msgCallback
            with
                | _ -> channel.Dispose()
                       reraise()
        with
            | _ -> conn.Dispose()
                   reraise()
