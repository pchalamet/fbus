module FBus.Core
open RabbitMQ.Client
open RabbitMQ.Client.Events


type IBusSender =
    abstract Publish: 't -> Async<Unit>
    abstract Send: 't -> Async<Unit>

type IBusControl =
    abstract Start: unit -> unit
    abstract Stop: unit -> unit
    abstract Sender: IBusSender

type IConsumer<'t> =
    abstract Handle: 't -> unit

type BusBuilder =
    { Name: string
      Uri : System.Uri
      Registrant: System.Type -> System.Type -> unit
      Activator: System.Type -> obj
      Handlers : (System.Type * System.Type) list }


let getExchangeName (t: System.Type) =
    let typename = t.FullName //.Replace("+", "")
    let xchgname = sprintf "fbus-%s" typename
    xchgname

type BusControl(busBuilder: BusBuilder) =

    let mutable busSender = None

    interface IBusControl with
        member _.Sender: IBusSender =
            match busSender with
            | Some busSender -> busSender
            | None -> failwith "Bus is not started"
 
        member this.Start() =
            match busSender with
            | Some _ -> ()
            | None -> 
                let factory = ConnectionFactory(Uri = busBuilder.Uri)
                let conn = factory.CreateConnection()
                let model = conn.CreateModel()

                let queue = model.QueueDeclare(busBuilder.Name)

                let bindExchangeAndQueue xchgName =
                    model.ExchangeDeclare(exchange = xchgName, ``type`` = ExchangeType.Fanout, 
                                          durable = true, autoDelete = false)
                    model.QueueBind(queue = queue.QueueName, exchange = xchgName, routingKey = "")

                busBuilder.Handlers |> List.iter (bindExchangeAndQueue << getExchangeName << fst)

                let consumer = EventingBasicConsumer(model)
                let consumerCallback (ea: BasicDeliverEventArgs) =
                    ()

                consumer.Received.Add consumerCallback
                model.BasicConsume(queue = queue.QueueName, autoAck = false, consumer = consumer) |> ignore

            failwith "Not Implemented"

        member this.Stop() = 
            match busSender with
            | None -> ()
            | Some _ -> failwith "Not Implemented"
