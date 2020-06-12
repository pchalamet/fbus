module FBus.Transport
open System
open FBus
open RabbitMQ.Client
open RabbitMQ.Client.Events

let getExchangeName (t: System.Type) =
    let xchgname = sprintf "fbus:type:%s" t.FullName
    xchgname

let getTypeName (t: System.Type) =
    let typeName = t.FullName
    typeName

let generateQueueName() =
    let computerName = Environment.MachineName
    let pid = Diagnostics.Process.GetCurrentProcess().Id
    let rnd = Random().Next()
    sprintf "fbus:%s-%d-%d" computerName pid rnd


type RabbitMQ(conn: IConnection, channel: IModel) =
    let send context xchgName routingKey t body =
        let msgTypeProp = t |> getTypeName :> obj
        let headers = context |> Map.map (fun _ v -> v :> obj) |> Map.add "fbus:msgtype" msgTypeProp
        let props = channel.CreateBasicProperties(Headers = headers )
        channel.BasicPublish(exchange = xchgName,
                             routingKey = routingKey,
                             basicProperties = props,
                             body = body)

    interface IBusTransport with
        member _.Publish (context: Map<string, string>) (t: System.Type) (body: ReadOnlyMemory<byte>) =
            let xchgName = t |> getExchangeName
            send context xchgName "" t body

        member _.Send (context: Map<string, string>) (destination: string) (t: System.Type) (body: ReadOnlyMemory<byte>) =
            let routingKey = sprintf "fbus:%s" destination
            send context "" routingKey t body

    interface System.IDisposable with
        member _.Dispose() =
            channel.Dispose()
            conn.Dispose()

    static member Create (busBuilder: BusBuilder) msgCallback =
        let factory = ConnectionFactory(Uri = busBuilder.Uri)
        let conn = factory.CreateConnection()
        let channel = conn.CreateModel()

        let dispose () =
            if channel |> isNull |> not then channel.Dispose()
            if conn |> isNull |> not then conn.Dispose()

        let configureAck () =
            channel.BasicQos(prefetchSize = 0ul, prefetchCount = 1us, ``global`` = false)
            channel.ConfirmSelect()

        let queueName = busBuilder.Name |> Option.defaultWith generateQueueName |> sprintf "fbus:%s"
        let autoDelete = busBuilder.AutoDelete
        let xchgDeadLetter = "fbus:dead-letter"
        let deadLetterQueueName = queueName + ":dead-letter"

        let configureQueues () =
            // dead letter queues are bound to a single exchange (direct) - the routingKey is the target queue
            channel.ExchangeDeclare(exchange = xchgDeadLetter,
                                    ``type`` = ExchangeType.Direct,
                                    durable = true, autoDelete = false)
            channel.QueueDeclare(queue = deadLetterQueueName,
                                 durable = true, exclusive = false, autoDelete = false) |> ignore
            channel.QueueBind(queue = deadLetterQueueName, exchange = xchgDeadLetter, routingKey = deadLetterQueueName)

            // message queues are bound to an exchange (fanout) - all bound subscribers receive messages
            channel.QueueDeclare(queueName,
                                 durable = true, exclusive = false, autoDelete = autoDelete,
                                 arguments = dict [ "x-dead-letter-exchange", xchgDeadLetter :> obj
                                                    "x-dead-letter-routing-key", deadLetterQueueName :> obj ]) |> ignore

        let bindExchangeAndQueue xchgName =
            channel.ExchangeDeclare(exchange = xchgName, ``type`` = ExchangeType.Fanout, 
                                    durable = true, autoDelete = autoDelete)
            channel.QueueBind(queue = queueName, exchange = xchgName, routingKey = "")

        let subscribeMessages () =
            busBuilder.Handlers |> List.iter (fun x -> x.MessageType |> getExchangeName |> bindExchangeAndQueue)


        let listenMessages () =
            let msgType2HandlerInfo = busBuilder.Handlers |> List.map (fun x -> x.MessageType |> getTypeName, x)
                                                          |> Map
            let consumer = EventingBasicConsumer(channel)
            let consumerCallback msgCallback (ea: BasicDeliverEventArgs) =
                try
                    let msgType = match ea.BasicProperties.Headers.TryGetValue "fbus:msgtype" with
                                  | true, (:? (byte[]) as msgType) -> System.Text.Encoding.UTF8.GetString(msgType)
                                  | _ -> failwithf "Missing header fbus:msgtype"
                    let handlerInfo = match msgType2HandlerInfo |> Map.tryFind msgType with
                                      | Some handlerInfo -> handlerInfo
                                      | _ -> failwithf "Unknown message type [%s]" msgType

                    msgCallback handlerInfo ea.Body

                    channel.BasicAck(deliveryTag = ea.DeliveryTag, multiple = false)
                with
                    | exn -> // TODO: report exception to someone
                             printfn "Failed to process message %A" exn
                             channel.BasicNack(deliveryTag = ea.DeliveryTag, multiple = false, requeue = false)

            consumer.Received.Add (consumerCallback msgCallback)
            channel.BasicConsume(queue = queueName, autoAck = false, consumer = consumer) |> ignore

        try
            configureAck()
            configureQueues()
            subscribeMessages ()
            listenMessages()

            new RabbitMQ(conn, channel) :> IBusTransport
        with
            | _ -> dispose()
                   reraise()
