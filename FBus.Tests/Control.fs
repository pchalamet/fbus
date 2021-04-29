module FBus.Control.Tests
open System
open NUnit.Framework
open FsUnit
open System.Threading

open FBus
open FBus.Builder


type StringMessage =
    { String: string } 
    interface FBus.IMessageEvent

type IntMessage =
    { Int: int }
    interface FBus.IMessageEvent

type StringConsumer(callback: IBusConversation -> string -> unit) =
    interface IBusConsumer<StringMessage> with
        member this.Handle context msg = 
            callback context msg.String

type IntConsumer(callback: IBusConversation -> int -> unit) =
    interface IBusConsumer<IntMessage> with
        member this.Handle context msg = 
            callback context msg.Int

exception TestBusException of string

let mutable registerCalls = 0
let mutable serializeCalls = 0
let mutable publishCalls = 0
let mutable resolveCalls = 0
let mutable deserializeCalls = 0
let mutable consumerActivation = 0
let mutable sendCalls = 0
let mutable transportDisposedCalls = 0

let buildUri = Uri("amqp://build-uri")

let msgString = "test message"
let fatalString = "fatal message"
let msgInt = 42
let client = "test-client"
let target = "test-target"
let activationContext = "activationContext" :> obj

let mutable latestConversationId = ""


let consumerStringCallback (context: IBusConversation) (msg: string) =
    Interlocked.Increment(&consumerActivation) |> ignore
    if msg = fatalString then TestBusException "Fatal !" |> raise

    msg |> should equal msgString
    context.Sender |> should equal client
    latestConversationId <- context.ConversationId
    { Int = msgInt } |> context.Reply

let consumerIntCallback (context: IBusConversation) (msg: int) =
    Interlocked.Increment(&consumerActivation) |> ignore
    msg |> should equal msgInt
    context.Sender |> should equal client // always same bus here
    context.ConversationId |> should equal latestConversationId

let buildContainer = {
    new IBusContainer with
        member this.Register handlerInfo = 
            Interlocked.Increment(&registerCalls) |> ignore

            [typeof<StringMessage>; typeof<IntMessage>] |> List.contains  handlerInfo.MessageType |> should be True
            if handlerInfo.MessageType = typeof<StringMessage> then
                handlerInfo.InterfaceType |> should equal typeof<IBusConsumer<StringMessage>>
                handlerInfo.ImplementationType |> should equal typeof<StringConsumer>
            else
                handlerInfo.InterfaceType |> should equal typeof<IBusConsumer<IntMessage>>
                handlerInfo.ImplementationType |> should equal typeof<IntConsumer>

        member this.Resolve ctx handlerInfo =
            Interlocked.Increment(&resolveCalls) |> ignore
            ctx |> should equal activationContext

            [typeof<StringMessage>; typeof<IntMessage>] |> List.contains  handlerInfo.MessageType |> should be True
            if handlerInfo.MessageType = typeof<StringMessage> then
                handlerInfo.InterfaceType |> should equal typeof<IBusConsumer<StringMessage>>
                handlerInfo.ImplementationType |> should equal typeof<StringConsumer>
                StringConsumer(consumerStringCallback) :> obj
            else
                handlerInfo.InterfaceType |> should equal typeof<IBusConsumer<IntMessage>>
                handlerInfo.ImplementationType |> should equal typeof<IntConsumer>
                IntConsumer(consumerIntCallback) :> obj
}

let serializer = FBus.Serializers.Json() :> IBusSerializer

let buildSerializer = {
    new IBusSerializer with
        member this.Deserialize msgType body = 
            Interlocked.Increment(&deserializeCalls) |> ignore
            [typeof<StringMessage>; typeof<IntMessage>] |> List.contains  msgType |> should be True
            serializer.Deserialize msgType body

        member this.Serialize msg =
            Interlocked.Increment(&serializeCalls) |> ignore
            [typeof<StringMessage>; typeof<IntMessage>] |> List.contains (msg.GetType()) |> should be True
            serializer.Serialize msg
}


let buildTransportBuilder uri (busConfig: BusConfiguration) (callback: Map<string, string> -> string -> ReadOnlyMemory<byte> -> unit): IBusTransport =
    uri |> should equal buildUri

    { new IBusTransport with
        member this.Dispose(): unit =
            Interlocked.Increment(&transportDisposedCalls) |> ignore

        member this.Publish headers msgType body = 
            Interlocked.Increment(&publishCalls) |> ignore
            callback headers msgType body

        member this.Send headers target msgType body = 
            Interlocked.Increment(&sendCalls) |> ignore
            target |> should equal target
            try
                callback headers msgType body
            with
                _ -> ()
    }


[<Test>]
let ``Test bus control`` () =
    let mutable onStart = 0
    let mutable onStop = 0
    let mutable onError = 0

    let hook = { new FBus.IBusHook with
                    member _.OnStart initiator = onStart <- onStart + 1
                    member _.OnStop initiator = onStop <- onStop + 1
                    member _.OnBeforeProcessing ctx = null
                    member this.OnError ctx msg exn =
                        match msg with
                        | :? StringMessage as s when s.String = fatalString -> match exn with
                                                                               | :? TestBusException -> onError <- onError + 1
                                                                               | _ -> ()
                        | _ -> ()
    }

    let bus = configure() |> withName client
                          |> withConsumer<StringConsumer>
                          |> withConsumer<IntConsumer>
                          |> withContainer buildContainer
                          |> withTransport (buildTransportBuilder buildUri)
                          |> withSerializer buildSerializer
                          |> withHook hook
                          |> build

    let busInitiator = bus.Start activationContext
    { String = msgString } |> busInitiator.Publish 
    { String = msgString } |> busInitiator.Send target
    { String = fatalString } |> busInitiator.Send target 
    bus.Dispose()

    registerCalls |> should equal 2 // 2 consumers
    serializeCalls |> should equal 5 // 1 publish, 1 reply (from publish), 1 send, 1 reply (from send) 1 failure
    publishCalls |> should equal 1 // 1 publish
    resolveCalls |> should equal 5 // 1 publish, 1 reply (from publish), 1 send, 1 reply (from send) 1 failure
    deserializeCalls |> should equal 5 // 1 publish, 1 reply (from publish), 1 send, 1 reply (from send) 1 failure
    consumerActivation |> should equal 5 // 1 publish, 1 reply (from publish), 1 send, 1 reply (from send) 1 failure
    sendCalls |> should equal 4 // 1 reply (from publish), 1 send, 1 reply (from send) 1 failure
    transportDisposedCalls |> should equal 1 // tear down

    onError |> should equal 1
    onStart |> should equal 1
    onStop |> should equal 1
