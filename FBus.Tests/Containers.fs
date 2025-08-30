module FBus.Container.Tests
open NUnit.Framework
open FsUnit
open FBus

type StringRequest =
    { Content: string }
    interface FBus.IMessageEvent

type StringConsumer() =
    new(v: int) =
        failwith "Should not be invoked"
        StringConsumer()

    interface IBusConsumer<StringRequest> with
        member this.Handle context msg = 
            failwith "Not Implemented"


[<Test>]
let ``Activator Register is no-op`` () =
    let activator = Containers.Activator() :> FBus.IBusContainer
    let handlerInfo = { MessageType = typeof<string>
                        Async = false
                        Handler = typeof<StringConsumer> }
    activator.Register handlerInfo


[<Test>]
let ``Activator create type with default constructor`` () =
    let activator = Containers.Activator() :> FBus.IBusContainer
    let handlerInfo = { MessageType = typeof<string>
                        Async = false
                        Handler = typeof<StringConsumer> }
    let consumer = activator.Resolve null handlerInfo
    consumer.GetType() |> should equal typeof<StringConsumer>
