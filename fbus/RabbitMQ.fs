module FBus.Transport.RabbitMQ
open FBus
open RabbitMQ.Client
open RabbitMQ.Client.Events


type BusTransport(conn: IConnection, model: IModel) =
    interface IBusTransport with
        member this.Publish (t: System.Type) (m: string) =
            async { 
                ()
            }

        member this.Send (t: System.Type) (m: string) =
            async {
                ()
            }

    interface System.IDisposable with
        member this.Dispose() =
            model.Dispose()
            conn.Dispose()


let Create (busBuilder: BusBuilder) msgCallback =
    let factory = ConnectionFactory(Uri = busBuilder.Uri)
    let conn = factory.CreateConnection()
    let model = conn.CreateModel()

    let dispose () =
        if model |> isNull |> not then model.Dispose()
        if conn |> isNull |> not then conn.Dispose()

    let configureAck () =
        model.BasicQos(prefetchSize = 0ul, prefetchCount = 1us, ``global`` = false)
        model.ConfirmSelect()

    let generateQueueName() =
        let computerName = System.Environment.MachineName
        let pid = System.Diagnostics.Process.GetCurrentProcess().Id
        let rnd = System.Random().Next()
        sprintf "fbus:%s-%d-%d" computerName pid rnd

    let queueName = busBuilder.Name |> Option.defaultWith generateQueueName |> sprintf "fbus:%s"
    let autoDelete = busBuilder.AutoDelete
    let xchgDeadLetter = "fbus:dead-letter"
    let deadLetterQueueName = queueName + ":dead-letter"

    let configureQueues () =
        // dead letter queues are bound to a single exchange (direct) - the routingKey is the target queue
        model.ExchangeDeclare(exchange = xchgDeadLetter,
                              ``type`` = ExchangeType.Direct,
                              durable = true, autoDelete = false)
        model.QueueDeclare(queue = deadLetterQueueName,
                           durable = true, exclusive = false, autoDelete = false) |> ignore
        model.QueueBind(queue = deadLetterQueueName, exchange = xchgDeadLetter, routingKey = "")

        // message queues are bound to an exchange (fanout) - all bound subscribers receive messages
        model.QueueDeclare(queueName,
                           durable = false, exclusive = false, autoDelete = autoDelete,
                           arguments = dict [ "x-dead-letter-exchange", xchgDeadLetter :> obj
                                              "x-dead-letter-routing-key", queueName :> obj ]) |> ignore

    let bindExchangeAndQueue xchgName =
        model.ExchangeDeclare(exchange = xchgName, ``type`` = ExchangeType.Fanout, 
                              durable = true, autoDelete = autoDelete)
        model.QueueBind(queue = queueName, exchange = xchgName, routingKey = "")

    let getExchangeName (t: System.Type) =
        let xchgname = sprintf "fbus:type:%s" t.FullName
        xchgname

    let subscribeMessages () =
        busBuilder.Handlers |> List.iter (fun x -> x.MessageType |> getExchangeName |> bindExchangeAndQueue)


    let listenMessages () =
        let msgType2HandlerInfo = busBuilder.Handlers |> List.map (fun x -> x.MessageType |> getExchangeName, x)
                                                      |> Map
        let consumer = EventingBasicConsumer(model)
        let consumerCallback msgCallback (ea: BasicDeliverEventArgs) =
            try
                let msgType = match ea.BasicProperties.Headers.TryGetValue "fbus:msgtype" with
                              | true, (:? string as msgType) -> msgType
                              | _ -> failwithf "Missing header fbus:msgtype"
                let handlerInfo = match msgType2HandlerInfo |> Map.tryFind msgType with
                                  | Some handlerInfo -> handlerInfo
                                  | _ -> failwithf "Unknown message type [%s]" msgType
                let content = System.Text.Encoding.UTF8.GetString(ea.Body.ToArray(), 0, ea.Body.Length)

                msgCallback handlerInfo content

                model.BasicAck(deliveryTag = ea.DeliveryTag, multiple = false)
            with
                | exn -> model.BasicNack(deliveryTag = ea.DeliveryTag, multiple = false, requeue = false)

        consumer.Received.Add (consumerCallback msgCallback)
        model.BasicConsume(queue = queueName, autoAck = false, consumer = consumer) |> ignore

    try
        configureAck()
        configureQueues()
        subscribeMessages ()
        listenMessages()

        new BusTransport(conn, model) :> IBusTransport
    with
        | _ -> dispose()
               reraise()
