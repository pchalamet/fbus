namespace FBus
open System

type IMessageCommand = interface end
type IMessageEvent = interface end

type IBusInitiator =
    abstract Publish<'t when 't :> IMessageEvent> : msg:'t -> unit
    abstract Send<'t when 't :> IMessageCommand> : client:string -> msg:'t -> unit

type IBusControl =
    inherit IDisposable
    abstract Start: obj -> IBusInitiator
    abstract Stop: unit -> unit

type IBusConversationContext =
    abstract Sender: string
    abstract ConversationId: string
    abstract MessageId: string

type IBusConversation =
    inherit IBusConversationContext
    inherit IBusInitiator
    abstract Reply<'t when 't :> IMessageCommand> : msg:'t -> unit

type IBusConsumer<'t> =
    abstract Handle: IBusConversation -> msg:'t -> unit

type HandlerInfo =
    { MessageType: Type
      InterfaceType: Type
      ImplementationType: Type
      CallSite: Reflection.MethodInfo }

type IBusContainer =
    abstract Register: HandlerInfo -> unit
    abstract Resolve: context:obj -> HandlerInfo -> obj

type IBusTransport =
    inherit IDisposable
    abstract Publish: headers:Map<string, string> -> msgType:string -> body:ReadOnlyMemory<byte> -> unit
    abstract Send: headers:Map<string, string> -> target:string -> msgType:string -> body:ReadOnlyMemory<byte> -> unit

type IBusSerializer =
    abstract Serialize: msg:obj -> ReadOnlyMemory<byte>
    abstract Deserialize: Type -> ReadOnlyMemory<byte> -> obj

type IBusHook =
    abstract OnStart: ctx:IBusInitiator -> unit
    abstract OnStop: ctx:IBusInitiator -> unit
    abstract OnBeforeProcessing: ctx:IBusConversation -> IDisposable
    abstract OnError: ctx:IBusConversation -> msg:obj -> exn: Exception -> unit

type BusConfiguration =
    { Name: string
      IsEphemeral: bool
      IsRecovery: bool
      Container: IBusContainer
      Serializer: IBusSerializer
      Hook: IBusHook option
      Transport: BusConfiguration -> (Map<string,string> -> ReadOnlyMemory<byte> -> unit) -> IBusTransport
      Handlers : Map<string, HandlerInfo> }

[<RequireQualifiedAccessAttribute>]
type BusBuilder =
    { Name: string
      IsEphemeral: bool
      IsRecovery: bool
      Container: IBusContainer option
      Serializer: IBusSerializer option
      Hook: IBusHook option
      Transport: (BusConfiguration -> (Map<string,string> -> ReadOnlyMemory<byte> -> unit) -> IBusTransport) option
      Handlers : Map<string, HandlerInfo> }
