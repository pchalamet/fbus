module FBus.Builder.Tests
open System
open NUnit.Framework
open FsUnit

open FBus
open FBus.Builder

[<Test>]
let ``withName set permanent client`` () =
    let builder = FBus.Builder.configure() |> withName "new-client-name"
    builder.Name |> should equal "new-client-name"
    builder.IsEphemeral |> should equal false

[<Test>]
let ``Invalid withName raises an error `` () =
    (fun () -> FBus.Builder.configure() |> withName "   " |> ignore) |> should (throwWithMessage "Invalid bus name") typeof<Exception>

[<Test>]
let ``withEndpoint set new uri`` () =
    let expectedUri = Uri("amqp://my-rabbitmq-server")
    let builder = FBus.Builder.configure() |> withEndpoint expectedUri
    builder.Uri |> should equal (Some expectedUri)


[<Test>]
let ``withContainer set container builder`` () =
    let expectedContainer = {
        new IBusContainer with
            member this.Register handlerInfo = failwith "Not Implemented"
            member this.Resolve activationContext handlerInfo = failwith "Not Implemented"
    }

    let builder = FBus.Builder.configure() |> withContainer expectedContainer
    builder.Container |> should equal (Some expectedContainer)

[<Test>]
let ``withTransport set transport builder`` () =
    let expectedTransportBuilder (busConfig: BusConfiguration) (callback: Map<string, string> -> string -> ReadOnlyMemory<byte> -> unit): IBusTransport =
        failwith "Not implemented"

    let builder = FBus.Builder.configure() |> withTransport expectedTransportBuilder
    builder.Transport |> should equal (Some expectedTransportBuilder)

[<Test>]
let ``withSerializer set serializer`` () =
    let expectedSerializer = {
        new IBusSerializer with
            member this.Deserialize msgType body = failwith "Not Implemented"
            member this.Serialize msg = failwith "Not Implemented"
    }

    let builder = FBus.Builder.configure() |> withSerializer expectedSerializer
    builder.Serializer |> should equal (Some expectedSerializer)


type MyConsumer1() =
    interface IBusConsumer<string> with
        member this.Handle context msg = 
            failwith "Not Implemented"

type MyConsumer2() =
    interface IBusConsumer<int> with
        member this.Handle context msg = 
            failwith "Not Implemented"

[<Test>]
let ``withConsumer add consumers`` () =
    let build = FBus.Builder.configure() |> withConsumer<MyConsumer1> |> withConsumer<MyConsumer2>

    let expectedHandlers = Map [ "System.String", { MessageType = typeof<string>
                                                    InterfaceType = typeof<IBusConsumer<string>>
                                                    ImplementationType = typeof<MyConsumer1>
                                                    CallSite = typeof<IBusConsumer<string>>.GetMethod("Handle") }
                                 "System.Int32", { MessageType = typeof<int>
                                                   InterfaceType = typeof<IBusConsumer<int>>
                                                   ImplementationType = typeof<MyConsumer2>
                                                   CallSite = typeof<IBusConsumer<int>>.GetMethod("Handle") } ]

    build.Handlers |> should equal expectedHandlers


[<Test>]
let ``withRecovery set recovery flag`` () =
    let builder = FBus.Builder.configure() |> withRecovery
    builder.IsRecovery |> should equal true
