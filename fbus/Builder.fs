module FBus.Builder
open FBus
open System

let init () =
    { Name = None
      Uri = Uri("amqp://guest:guest@localhost")
      AutoDelete = true
      Container = Container.Activator()
      Transport = Transport.RabbitMQ.Create
      Serializer = Serializer.Json() :> IBusSerializer
      Handlers = List.empty }

let withEndpoint uri busBuilder =
    { busBuilder with Uri = uri }

let withAutoDelete autoDelete busBuilder =
    { busBuilder with AutoDelete = autoDelete }

let withName name busBuilder =
    { busBuilder with Name = Some name }

let withContainer container busBuilder =
    { busBuilder with Container = container }

let withTransport transport busBuilder = 
    { busBuilder with Transport = transport }

let withSerializer serializer busBuilder =
    { busBuilder with Serializer = serializer }

let inline withHandler<'t> busBuilder =
    let findMessageHandler (t: System.Type) =
        if t.IsGenericType && t.GetGenericTypeDefinition() = typedefof<IBusConsumer<_>> then 
            let msgType = t.GetGenericArguments().[0]
            Some (msgType, t)
        else None

    let findMessageHandlers (t: System.Type) =    
        t.GetInterfaces() |> Array.choose findMessageHandler
                          |> Array.map (fun (msgType, itfType) -> { MessageType = msgType
                                                                    InterfaceType = itfType
                                                                    ImplementationType = t })
                          |> List.ofArray

    let handlers = typeof<'t> |> findMessageHandlers
    if handlers = List.empty then failwith "No handler implemented"
    { busBuilder with Handlers = busBuilder.Handlers @ handlers }

let build (busBuilder : BusBuilder) =
    busBuilder.Handlers |> List.iter busBuilder.Container.Register
    new BusControl(busBuilder) :> IBusControl
