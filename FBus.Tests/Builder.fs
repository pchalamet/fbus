module FBus.Builder.Tests
open System
open NUnit.Framework
open FsUnit

open FBus

[<Test>]
let ``withName set permanent client`` () =
    let builder = FBus.Builder.configure() |> Builder.withName "new-client-name"
    builder.Name |> should equal "new-client-name"
    builder.IsEphemeral |> should equal false

[<Test>]
let ``Invalid withName raises an error `` () =
    (fun () -> FBus.Builder.configure() |> Builder.withName "   " |> ignore) |> should (throwWithMessage "Invalid bus name") typeof<Exception>

[<Test>]
let ``withContainer set container builder`` () =
    let expectedContainer = {
        new IBusContainer with
            member this.Register handlerInfo = failwith "Not Implemented"
            member this.Resolve activationContext handlerInfo = failwith "Not Implemented"
    }

    let builder = FBus.Builder.configure() |> Builder.withContainer expectedContainer
    builder.Container |> should equal (Some expectedContainer)

[<Test>]
let ``withTransport set transport builder`` () =
    let expectedTransportBuilder (busConfig: BusConfiguration) (callback: Map<string, string> -> ReadOnlyMemory<byte> -> unit): IBusTransport =
        failwith "Not implemented"

    let builder = FBus.Builder.configure() |> Builder.withTransport expectedTransportBuilder
    builder.Transport |> should equal (Some expectedTransportBuilder)

[<Test>]
let ``withSerializer set serializer`` () =
    let expectedSerializer = {
        new IBusSerializer with
            member this.Deserialize msgType body = failwith "Not Implemented"
            member this.Serialize msg = failwith "Not Implemented"
    }

    let builder = FBus.Builder.configure() |> Builder.withSerializer expectedSerializer
    builder.Serializer |> should equal (Some expectedSerializer)


type MyConsumer1() =
    interface IBusConsumer<string> with
        member this.Handle context msg = 
            failwith "Not Implemented"

    interface IBusConsumer<float> with
        member this.Handle context msg = 
            failwith "Not Implemented"

type MyConsumer2() =
    interface IBusConsumer<int> with
        member this.Handle context msg = 
            failwith "Not Implemented"

[<Test>]
let ``withConsumer add consumers`` () =
    let funcHandler: IFunConsumer<decimal> = 
        fun ctx msg -> failwith "Not implemented"

    let build = 
        FBus.Builder.configure()
        |> Builder.withConsumer<MyConsumer1>
        |> Builder.withConsumer<MyConsumer2>
        |> Builder.withFunConsumer funcHandler

    let expectedHandlers = Map [ "System.String", { MessageType = typeof<string>
                                                    Handler = Class typeof<MyConsumer1> }
                                 "System.Double", { MessageType = typeof<float>
                                                    Handler = Class typeof<MyConsumer1> }
                                 "System.Int32", { MessageType = typeof<int>
                                                   Handler = Class typeof<MyConsumer2> } 
                                 "System.Decimal", { MessageType = typeof<decimal>
                                                     Handler = Instance funcHandler } ]

    build.Handlers |> should equal expectedHandlers


[<Test>]
let ``withRecovery set recovery flag`` () =
    let builder = FBus.Builder.configure() |> Builder.withRecovery
    builder.IsRecovery |> should equal true
