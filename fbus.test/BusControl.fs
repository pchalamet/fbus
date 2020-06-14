module fbus.test.BusControl
open System
open NUnit.Framework
open FsUnit
open System.Threading

open FBus
open FBus.Builder


type StringConsumer(callback: IBusConversation -> string -> unit) =
    interface IBusConsumer<string> with
        member this.Handle context msg = 
            callback context msg

type IntConsumer(callback: IBusConversation -> int -> unit) =
    interface IBusConsumer<int> with
        member this.Handle context msg = 
            callback context msg


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
let msgInt = 42
let client = "test-client"
let target = "test-target"
let activationContext = "activationContext" :> obj

let consumerStringCallback (context: IBusConversation) (msg: string) =
    Interlocked.Increment(&consumerActivation) |> ignore
    msg |> should equal msg
    context.Sender |> should equal client
    context.Reply msgInt

let consumerIntCallback (context: IBusConversation) (msg: int) =
    Interlocked.Increment(&consumerActivation) |> ignore
    msg |> should equal msgInt
    context.Sender |> should equal client // always same bus here

let buildContainer = {
    new IBusContainer with
        member this.Register handlerInfo = 
            Interlocked.Increment(&registerCalls) |> ignore

            [typeof<string>; typeof<int>] |> List.contains  handlerInfo.MessageType |> should be True
            if handlerInfo.MessageType = typeof<string> then
                handlerInfo.InterfaceType |> should equal typeof<IBusConsumer<string>>
                handlerInfo.ImplementationType |> should equal typeof<StringConsumer>
            else
                handlerInfo.InterfaceType |> should equal typeof<IBusConsumer<int>>
                handlerInfo.ImplementationType |> should equal typeof<IntConsumer>

        member this.Resolve ctx handlerInfo =
            Interlocked.Increment(&resolveCalls) |> ignore
            ctx |> should equal activationContext

            [typeof<string>; typeof<int>] |> List.contains  handlerInfo.MessageType |> should be True
            if handlerInfo.MessageType = typeof<string> then
                handlerInfo.InterfaceType |> should equal typeof<IBusConsumer<string>>
                handlerInfo.ImplementationType |> should equal typeof<StringConsumer>
                StringConsumer(consumerStringCallback) :> obj
            else
                handlerInfo.InterfaceType |> should equal typeof<IBusConsumer<int>>
                handlerInfo.ImplementationType |> should equal typeof<IntConsumer>
                IntConsumer(consumerIntCallback) :> obj
}

let buildSerializer = {
    new IBusSerializer with
        member this.Deserialize msgType body = 
            Interlocked.Increment(&deserializeCalls) |> ignore
            [typeof<string>; typeof<int>] |> List.contains  msgType |> should be True
            if msgType = typeof<string> then System.Text.Encoding.UTF8.GetString(body.ToArray()) :> obj
            else System.BitConverter.ToInt32(body.ToArray(), 0) :> obj

        member this.Serialize msg =
            Interlocked.Increment(&serializeCalls) |> ignore
            [typeof<string>; typeof<int>] |> List.contains (msg.GetType()) |> should be True
            if msg.GetType() = typeof<string> then (msg :?> string) |> System.Text.Encoding.UTF8.GetBytes |> ReadOnlyMemory
            else (msg :?> int) |> System.BitConverter.GetBytes |> ReadOnlyMemory
}


let buildTransportBuilder (busBuilder: BusBuilder) (callback: Map<string, string> -> string -> ReadOnlyMemory<byte> -> unit): IBusTransport =
    busBuilder.Uri |> should equal buildUri

    { new IBusTransport with
        member this.Dispose(): unit =
            Interlocked.Increment(&transportDisposedCalls) |> ignore

        member this.Publish headers msgType body = 
            Interlocked.Increment(&publishCalls) |> ignore
            callback headers msgType body

        member this.Send headers target msgType body = 
            Interlocked.Increment(&sendCalls) |> ignore
            target |> should equal target
            callback headers msgType body
    }


[<Test>]
let ``Test bus control`` () =
    let bus = init() |> withName client
                     |> withEndpoint buildUri
                     |> withConsumer<StringConsumer>
                     |> withConsumer<IntConsumer>
                     |> withContainer buildContainer
                     |> withTransport buildTransportBuilder
                     |> withSerializer buildSerializer
                     |> build

    let busInitiator = bus.Start activationContext
    busInitiator.Publish msgString
    busInitiator.Send target msgString
    bus.Dispose()

    registerCalls |> should equal 2 // 2 consumers
    serializeCalls |> should equal 4 // 1 publish, 1 reply (from publish), 1 send, 1 reply (from send)
    publishCalls |> should equal 1 // 1 publish
    resolveCalls |> should equal 4 // 1 publish, 1 reply (from publish), 1 send, 1 reply (from send)
    deserializeCalls |> should equal 4 // 1 publish, 1 reply (from publish), 1 send, 1 reply (from send)
    consumerActivation |> should equal 4 // 1 publish, 1 reply (from publish), 1 send, 1 reply (from send)
    sendCalls |> should equal 3 // 1 reply (from publish), 1 send, 1 reply (from send)
    transportDisposedCalls |> should equal 1 // tear down
