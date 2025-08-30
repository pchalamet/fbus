namespace FBus
open System
open System.Threading.Tasks

type IMessage = interface end

type IMessageCommand = inherit IMessage

type IMessageEvent = inherit IMessage

type IMessageKey = 
    abstract Key: string with get

type IBusInitiator =
    abstract Publish<'t when 't :> IMessageEvent> : msg:'t -> unit
    abstract Send<'t when 't :> IMessageCommand> : client:string -> msg:'t -> unit

type IBusControl =
    inherit IDisposable
    abstract Start: objnull -> IBusInitiator
    abstract Stop: unit -> unit

type IBusConversationContext =
    abstract Sender: string
    abstract ConversationId: string
    abstract MessageId: string

type IBusConversation =
    inherit IBusConversationContext
    inherit IBusInitiator
    abstract Reply<'t when 't :> IMessageEvent> : msg:'t -> unit

type IBusConsumer<'t when 't :> IMessage> =
    abstract Handle: IBusConversation -> msg:'t -> unit

type IAsyncBusConsumer<'t when 't :> IMessage> =
    abstract member HandleAsync: IBusConversation -> 't -> Task

type HandlerInfo =
    { MessageType: Type
      Async: bool
      Handler: Type }

type IBusContainer =
    abstract Register: HandlerInfo -> unit
    abstract NewScope: context:objnull -> IDisposable | null
    abstract Resolve: context:objnull -> HandlerInfo -> objnull

type IBusTransport =
    inherit IDisposable
    abstract Publish: headers:Map<string, string> -> msgType:string -> body:ReadOnlyMemory<byte> -> routing:string -> unit
    abstract Send: headers:Map<string, string> -> target:string -> msgType:string -> body:ReadOnlyMemory<byte> -> routing:string -> unit

type IBusSerializer =
    abstract Serialize: msg:obj -> string * ReadOnlyMemory<byte>
    abstract Deserialize: Type -> ReadOnlyMemory<byte> -> obj

type IBusHook =
    abstract OnStart: ctx:IBusInitiator -> unit
    abstract OnStop: ctx:IBusInitiator -> unit
    abstract OnBeforeProcessing: ctx:IBusConversation -> IDisposable | null
    abstract OnError: ctx:IBusConversation -> msg:obj|null -> exn: Exception -> unit

type BusConfiguration =
    { Name: string
      IsEphemeral: bool
      ShardName: string option
      IsRecovery: bool
      Container: IBusContainer
      Serializer: IBusSerializer
      Hook: IBusHook option
      Concurrency: int
      Transport: BusConfiguration -> (Map<string,string> -> ReadOnlyMemory<byte> -> unit) -> IBusTransport
      Handlers : Map<string, HandlerInfo> }

[<RequireQualifiedAccessAttribute>]
type BusBuilder =
    { Name: string
      ShardName: string option
      IsEphemeral: bool
      IsRecovery: bool
      Container: IBusContainer option
      Serializer: IBusSerializer option
      Hook: IBusHook option
      Concurrency: int option
      Transport: (BusConfiguration -> (Map<string,string> -> ReadOnlyMemory<byte> -> unit) -> IBusTransport) option
      Handlers : Map<string, HandlerInfo> }
