module FBus.Builder
open FBus
open System

let configure () =

    let generateClientName() =
        let computerName = Environment.MachineName
        let pid = Diagnostics.Process.GetCurrentProcess().Id
        let rnd = Random().Next()
        sprintf "%s-%d-%d" computerName pid rnd

    { BusBuilder.Name = generateClientName()
      BusBuilder.IsEphemeral = true
      BusBuilder.IsRecovery = false
      BusBuilder.Container = None
      BusBuilder.Transport = None
      BusBuilder.Serializer = None
      BusBuilder.Hook = None
      BusBuilder.Handlers = Map.empty }


let withName name busBuilder =
    if name |> String.IsNullOrWhiteSpace then failwith "Invalid bus name"
    { busBuilder with BusBuilder.Name = name 
                      BusBuilder.IsEphemeral = false }

let withTransport transport busBuilder = 
    { busBuilder with BusBuilder.Transport = Some transport }

let withContainer container busBuilder =
    { busBuilder with BusBuilder.Container = Some container }

let withSerializer serializer busBuilder =
    { busBuilder with BusBuilder.Serializer = Some serializer }

let withConsumer<'t> busBuilder =
    let findMessageHandler (t: System.Type) =
        if t.IsGenericType && t.GetGenericTypeDefinition() = typedefof<IBusConsumer<_>> then 
            let msgType = t.GetGenericArguments().[0]
            Some (msgType, t)
        else None

    let findMessageHandlers (t: System.Type) =    
        t.GetInterfaces() |> Array.choose findMessageHandler
                          |> Array.map (fun (msgType, itfType) -> let callsite = itfType.GetMethod("Handle")
                                                                  if callsite |> isNull then failwith "Handler method not found"
                                                                  { MessageType = msgType
                                                                    InterfaceType = itfType
                                                                    ImplementationType = t
                                                                    CallSite = callsite })
                          |> List.ofArray

    let handlers = typeof<'t> |> findMessageHandlers
    if handlers = List.empty then failwith "No handler implemented"
    { busBuilder with BusBuilder.Handlers = handlers |> List.fold (fun acc h -> acc |> Map.add h.MessageType.FullName h) busBuilder.Handlers }

let withRecovery busBuilder =
    { busBuilder with BusBuilder.IsRecovery = true }

let withHook hook busBuilder =
    { busBuilder with BusBuilder.Hook = Some hook }

let build (busBuilder : BusBuilder) =
    let busBuilder = if busBuilder.IsRecovery then
                        { busBuilder with BusBuilder.Name = busBuilder.Name + ":dead-letter"
                                          BusBuilder.IsEphemeral = true }
                     else
                        busBuilder

    let serializer = match busBuilder.Serializer with
                     | Some serializer -> serializer
                     | _ -> failwith "Serializer must be initialized"

    let container = match busBuilder.Container with
                    | Some container -> container
                    | _ -> failwith "Container must be initialized"

    let transport = match busBuilder.Transport with
                    | Some transport -> transport
                    | _ -> failwith "Transport must be initialized"

    let busConfig = { Name = busBuilder.Name
                      IsEphemeral = busBuilder.IsEphemeral
                      IsRecovery = busBuilder.IsRecovery
                      Container = container
                      Serializer = serializer
                      Hook = busBuilder.Hook
                      Transport = transport
                      Handlers = busBuilder.Handlers }

    new Control.Bus(busConfig) :> IBusControl
