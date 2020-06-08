module FBus.Core
open RabbitMQ.Client
open RabbitMQ.Client.Events
open System.Text.Json
open System.Text.Json.Serialization

type IBusSender =
    abstract Publish: 't -> Async<Unit>
    abstract Send: 't -> Async<Unit>

type IBusControl =
    abstract Start: obj -> unit
    abstract Stop: unit -> unit
    abstract Sender: IBusSender

type IConsumer<'t> =
    abstract Handle: 't -> unit

type HandlerInfo = {
    MessageType: System.Type
    InterfaceType: System.Type
    ImplementationType: System.Type
}

type BusBuilder =
    { Name: string option
      Uri : System.Uri
      AutoDelete: bool
      Registrant: HandlerInfo -> unit
      Activator: obj -> System.Type -> obj
      Handlers : HandlerInfo list }


let getExchangeName (t: System.Type) =
    let xchgname = sprintf "fbus-%s" t.FullName
    xchgname

let deserializeMessage (t: System.Type) (json: string) =
    let options = JsonSerializerOptions()
    JsonSerializer.Deserialize(json, t, options)

type BusControl(busBuilder: BusBuilder) =

    let mutable busSender = None

    interface IBusControl with
        member _.Sender: IBusSender =
            match busSender with
            | Some busSender -> busSender
            | None -> failwith "Bus is not started"
 
        member this.Start (context: obj) =
            match busSender with
            | Some _ -> ()
            | None ->
                let factory = ConnectionFactory(Uri = busBuilder.Uri)
                use conn = factory.CreateConnection()
                use model = conn.CreateModel()
                model.BasicQos(prefetchSize = 0ul, prefetchCount = 1us, ``global`` = false)
                model.ConfirmSelect()

                // use or create queue name
                let generateQueueName() =
                    let computerName = System.Environment.MachineName
                    let pid = System.Diagnostics.Process.GetCurrentProcess().Id
                    let rnd = System.Random().Next()
                    sprintf "fbus-%s-%d-%d" computerName pid rnd
                let queueName = busBuilder.Name |> Option.defaultWith generateQueueName

                // dead letter queues are bound to a single exchange (direct) - the routingKey is the target queue
                let autoDelete = busBuilder.AutoDelete
                let xchgDeadLetter = "fbus-dead-letter"
                let deadLetterQueueName = queueName + "-dead-letter"
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

                busBuilder.Handlers |> List.iter (fun x -> x.MessageType |> getExchangeName |> bindExchangeAndQueue)
                
                let msgType2HandlerInfo = busBuilder.Handlers |> List.map (fun x -> x.MessageType |> getExchangeName, x)
                                                              |> Map

                let consumer = EventingBasicConsumer(model)
                let consumerCallback (ea: BasicDeliverEventArgs) =
                    try
                        let msgType = match ea.BasicProperties.Headers.TryGetValue "fbus-msgtype" with
                                      | true, (:? string as msgType) -> msgType
                                      | _ -> failwithf "Missing header fbus-msgtype"
                        let handlerInfo = match msgType2HandlerInfo |> Map.tryFind msgType with
                                          | Some handlerInfo -> handlerInfo
                                          | _ -> failwithf "Unknown message type [%s]" msgType

                        let handler = busBuilder.Activator context handlerInfo.InterfaceType
                        if handler |> isNull then failwith "No handler found"

                        let callsite = handler.GetType().GetMethod("Handle")
                        if callsite |> isNull then failwith "Handler method not found"

                        let content = System.Text.Encoding.UTF8.GetString(ea.Body.ToArray(), 0, ea.Body.Length)
                        let msg = deserializeMessage handlerInfo.MessageType content

                        callsite.Invoke(handler, [| msg |]) |> ignore

                        model.BasicAck(deliveryTag = ea.DeliveryTag, multiple = false)
                    with
                        | exn -> model.BasicNack(deliveryTag = ea.DeliveryTag, multiple = false, requeue = false)

                consumer.Received.Add consumerCallback
                model.BasicConsume(queue = queueName, autoAck = false, consumer = consumer) |> ignore

            failwith "Not Implemented"

        member this.Stop() = 
            match busSender with
            | None -> ()
            | Some _ -> failwith "Not Implemented"
