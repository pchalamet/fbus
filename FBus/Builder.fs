module FBus.Builder
open FBus
open System

let private generateClientName () =
    let computerName = Environment.MachineName
    let pid = Diagnostics.Process.GetCurrentProcess().Id
    let rnd = Random().Next()
    $"{computerName}-{pid}-{rnd}"

[<CompiledName("Configure")>]
let configure () =
    { BusBuilder.Name = generateClientName()
      BusBuilder.ShardName = None
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

let withShard name busBuilder =
    if name |> String.IsNullOrWhiteSpace then failwith "Invalid shard name"
    { busBuilder with BusBuilder.ShardName = Some name }

let withTransport transport busBuilder = 
    { busBuilder with BusBuilder.Transport = Some transport }

let withContainer container busBuilder =
    { busBuilder with BusBuilder.Container = Some container }

let withSerializer serializer busBuilder =
    { busBuilder with BusBuilder.Serializer = Some serializer }

let withConsumer<'t> busBuilder =
    let findMessageHandler (t: System.Type) =
        if t.IsGenericType then
            if t.GetGenericTypeDefinition() = typedefof<IBusConsumer<_>> then 
                let msgType = t.GetGenericArguments().[0]
                Some (false, msgType)
            elif t.GetGenericTypeDefinition() = typedefof<IAsyncBusConsumer<_>> then 
                let msgType = t.GetGenericArguments().[0]
                Some (true, msgType)
            else
                None
        else
            None

    let findMessageHandlers (t: System.Type) =    
        t.GetInterfaces() |> Array.choose findMessageHandler
                          |> Array.map (fun (async, msgType) -> { MessageType = msgType
                                                                  Async = async
                                                                  Handler = t })
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
                      ShardName = busBuilder.ShardName
                      IsEphemeral = busBuilder.IsEphemeral
                      IsRecovery = busBuilder.IsRecovery
                      Container = container
                      Serializer = serializer
                      Hook = busBuilder.Hook
                      Transport = transport
                      Handlers = busBuilder.Handlers }

    new Control.Bus(busConfig) :> IBusControl
