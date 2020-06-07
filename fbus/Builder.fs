module FBus.Builder
open System
open FBus.Core

let init () =
    let computerName = Environment.MachineName
    let pid = Diagnostics.Process.GetCurrentProcess().Id
    let rnd = Random().Next()
    let queueName = sprintf "fbus-%s-%d-%d" computerName pid rnd
    { Name = queueName
      Uri = Uri("amqp://guest:guest@localhost")
      Registrant = fun (msgType, implType) -> ()
      Activator = System.Activator.CreateInstance
      Handlers = List.empty }

let withEndpoint uri busBuilder =
    { busBuilder with Uri = uri }

let withRegistrant registrant busBuilder = 
    { busBuilder with Registrant = registrant }

let inline withHandler<'t> busBuilder =
    let findMessageHandler (t: System.Type) =
        if t.IsGenericType && t.GetGenericTypeDefinition() = typedefof<IConsumer<_>> then t |> Some
        else None

    let findMessageHandlers (t: System.Type) =    
        t.GetInterfaces() |> Array.choose findMessageHandler
                          |> Array.map (fun msgType -> msgType, t)
                          |> List.ofArray

    let handlers = typeof<'t> |> findMessageHandlers
    if handlers = List.empty then failwith "No handler implemented"
    { busBuilder with Handlers = busBuilder.Handlers @ handlers }

let build (busBuilder : BusBuilder) =
    busBuilder.Handlers |> List.iter busBuilder.Registrant
    BusControl(busBuilder) :> IBusControl
