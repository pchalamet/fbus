namespace FBus
open FBus
open System

type private FunBusConsumer<'t>(func: IFunConsumer<'t>) =
    interface IBusConsumer<'t> with
        member _.Handle ctx msg = func ctx msg


[<AbstractClass; Sealed>]
type Builder =
    static member private generateClientName () =
        let computerName = Environment.MachineName
        let pid = Diagnostics.Process.GetCurrentProcess().Id
        let rnd = Random().Next()
        $"{computerName}-{pid}-{rnd}"

    [<CompiledName("Configure")>]
    static member configure () =
        { BusBuilder.Name = Builder.generateClientName()
          BusBuilder.ShardName = None
          BusBuilder.IsEphemeral = true
          BusBuilder.IsRecovery = false
          BusBuilder.Container = None
          BusBuilder.Transport = None
          BusBuilder.Serializer = None
          BusBuilder.Hook = None
          BusBuilder.Handlers = Map.empty }

    [<CompiledName("WithName")>]
    static member withName name busBuilder =
        if name |> String.IsNullOrWhiteSpace then failwith "Invalid bus name"
        { busBuilder with BusBuilder.Name = name 
                          BusBuilder.IsEphemeral = false }

    [<CompiledName("WithShard")>]
    static member withShard name busBuilder =
        if name |> String.IsNullOrWhiteSpace then failwith "Invalid shard name"
        { busBuilder with BusBuilder.ShardName = Some name }

    [<CompiledName("WithTransport")>]
    static member withTransport transport busBuilder = 
        { busBuilder with BusBuilder.Transport = Some transport }

    [<CompiledName("WithContainer")>]
    static member withContainer container busBuilder =
        { busBuilder with BusBuilder.Container = Some container }

    [<CompiledName("WithSerializer")>]
    static member withSerializer serializer busBuilder =
        { busBuilder with BusBuilder.Serializer = Some serializer }

    [<CompiledName("WithConsumer")>]
    static member withConsumer<'t> busBuilder =
        let findMessageHandler (t: System.Type) =
            if t.IsGenericType && t.GetGenericTypeDefinition() = typedefof<IBusConsumer<_>> then 
                let msgType = t.GetGenericArguments().[0]
                Some msgType
            else None

        let findMessageHandlers (t: System.Type) =    
            t.GetInterfaces() |> Array.choose findMessageHandler
                              |> Array.map (fun msgType -> { MessageType = msgType
                                                             Handler = Class t })
                              |> List.ofArray

        let handlers = typeof<'t> |> findMessageHandlers
        if handlers = List.empty then failwith "No handler implemented"
        { busBuilder with BusBuilder.Handlers = handlers |> List.fold (fun acc h -> acc |> Map.add h.MessageType.FullName h) busBuilder.Handlers }

    [<CompiledName("WithFunConsumer")>]
    static member withFunConsumer (func: IFunConsumer<'t>) busBuilder =
        let handlerInfo = { MessageType = typeof<'t>
                            Handler = Instance func }
        { busBuilder with BusBuilder.Handlers = busBuilder.Handlers |> Map.add typeof<'t>.FullName handlerInfo }

    [<CompiledName("WithRecovery")>]
    static member withRecovery busBuilder =
        { busBuilder with BusBuilder.IsRecovery = true }

    [<CompiledName("WithHook")>]
    static member withHook hook busBuilder =
        { busBuilder with BusBuilder.Hook = Some hook }

    [<CompiledName("Build")>]
    static member build (busBuilder : BusBuilder) =
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

        let toRuntimeHandler _ (handlerInfo: HandlerInfo) =
            match handlerInfo.Handler with
            | Class _ -> handlerInfo
            | Instance func -> let funcBusConsumerType = typedefof<FunBusConsumer<_>>.MakeGenericType(handlerInfo.MessageType)
                               let funcBusConsumer = System.Activator.CreateInstance(funcBusConsumerType, func)
                               { handlerInfo with Handler = Instance funcBusConsumer }

        let runtimeHandlers = busBuilder.Handlers |> Map.map toRuntimeHandler

        let busConfig = { Name = busBuilder.Name
                          ShardName = busBuilder.ShardName
                          IsEphemeral = busBuilder.IsEphemeral
                          IsRecovery = busBuilder.IsRecovery
                          Container = container
                          Serializer = serializer
                          Hook = busBuilder.Hook
                          Transport = transport
                          Handlers = runtimeHandlers }

        new Control.Bus(busConfig) :> IBusControl
