module FBus.Builder.Tests
open System
open NUnit.Framework
open FsUnit

open FBus

[<Test>]
let ``withConcurrency sets max parallel handlers`` () =
    let builder = FBus.Builder.configure() |> Builder.withConcurrency 4
    builder.Concurrency |> should equal (Some 4)

[<Test>]
let ``withConcurrency rejects invalid values`` () =
    (fun () -> FBus.Builder.configure() |> Builder.withConcurrency 0 |> ignore)
    |> should (throwWithMessage "Concurrency must be >= 1") typeof<Exception>

[<Test>]
let ``withName set permanent client`` () =
    let builder = FBus.Builder.configure()

    builder.IsEphemeral |> should equal true
    let builder = builder |> Builder.withName "new-client-name"
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
            member this.NewScope activationContext = failwith "Not Implemented"
            member this.Resolve activationContext handlerInfo = failwith "Not Implemented"
    }

    let builder = FBus.Builder.configure() |> Builder.withContainer expectedContainer
    builder.Container |> should equal (Some expectedContainer)

[<Test>]
let ``withTransport set transport builder`` () =
    let expectedTransportBuilder (busConfig: BusConfiguration) (callback: Map<string, string> -> ReadOnlyMemory<byte> -> unit): IBusTransport =
        failwith "Not implemented"

    let builder = FBus.Builder.configure() |> Builder.withTransport expectedTransportBuilder
    Object.ReferenceEquals(builder.Transport.Value, expectedTransportBuilder) |> should equal true

[<Test>]
let ``withSerializer set serializer`` () =
    let expectedSerializer = {
        new IBusSerializer with
            member this.Deserialize msgType body = failwith "Not Implemented"
            member this.Serialize msg = failwith "Not Implemented"
    }

    let builder = FBus.Builder.configure() |> Builder.withSerializer expectedSerializer
    Object.ReferenceEquals(builder.Serializer.Value, expectedSerializer) |> should equal true

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
let ``withRecovery set recovery flag`` () =
    let builder = FBus.Builder.configure() |> Builder.withRecovery
    builder.IsRecovery |> should equal true
