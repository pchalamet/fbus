module FBus.Builder
open System
open FBus.Core

let init () =
    { Name = None
      Uri = Uri("amqp://guest:guest@localhost")
      AutoDelete = true
      Registrant = fun (_) -> ()
      Activator = fun _ t -> System.Activator.CreateInstance(t)
      Handlers = List.empty }

let withEndpoint uri busBuilder =
    { busBuilder with Uri = uri }

let withAutoDelete autoDelete busBuilder =
    { busBuilder with AutoDelete = autoDelete }

let withName name busBuilder =
    { busBuilder with Name = Some name }

let withActivator activator busBuilder = 
    { busBuilder with Activator = activator }

let withRegistrant registrant busBuilder = 
    { busBuilder with Registrant = registrant }

let inline withHandler<'t> busBuilder =
    let findMessageHandler (t: System.Type) =
        if t.IsGenericType && t.GetGenericTypeDefinition() = typedefof<IConsumer<_>> then 
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
    busBuilder.Handlers |> List.iter busBuilder.Registrant
    BusControl(busBuilder) :> IBusControl
