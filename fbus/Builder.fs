module FBus.Builder
open FBus
open System

let init () =

    let generateClientName() =
        let computerName = Environment.MachineName
        let pid = Diagnostics.Process.GetCurrentProcess().Id
        let rnd = Random().Next()
        sprintf "%s-%d-%d" computerName pid rnd

    { Name = generateClientName()
      IsEphemeral = true
      Uri = Uri("amqp://guest:guest@localhost")
      Container = Container.Activator()
      Transport = Transport.RabbitMQ.Create
      Serializer = Serializer.Json() :> IBusSerializer
      Handlers = Map.empty }

let withName name busBuilder =
    { busBuilder with Name = name 
                      IsEphemeral = false }

let withTransport transport busBuilder = 
    { busBuilder with Transport = transport }

let withEndpoint uri busBuilder =
    { busBuilder with Uri = uri }

let withContainer container busBuilder =
    { busBuilder with Container = container }

let withSerializer serializer busBuilder =
    { busBuilder with Serializer = serializer }

let inline withConsumer<'t> busBuilder =
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
    { busBuilder with Handlers = handlers |> List.fold (fun acc h -> acc |> Map.add h.MessageType.FullName h) busBuilder.Handlers }


let build (busBuilder : BusBuilder) =
    new Control.BusControl(busBuilder) :> IBusControl
