module FBus.Container.Tests
open NUnit.Framework
open FsUnit
open FBus

type StringConsumer() =
    new(v: int) =
        failwith "Should not be invoked"
        StringConsumer()

    interface IBusConsumer<string> with
        member this.Handle context msg = 
            failwith "Not Implemented"


[<Test>]
let ``Activator Resolve is no-op`` () =
    let activator = Containers.InMemory() :> FBus.IBusContainer
    let handlerInfo = { MessageType = typeof<string>
                        InterfaceType = typeof<IBusConsumer<string>>
                        ImplementationType = typeof<StringConsumer> }
    activator.Register handlerInfo


[<Test>]
let ``Activator create type with default constructor`` () =
    let activator = Containers.InMemory() :> FBus.IBusContainer
    let handlerInfo = { MessageType = typeof<string>
                        InterfaceType = typeof<IBusConsumer<string>>
                        ImplementationType = typeof<StringConsumer> }
    let consumer = activator.Resolve null handlerInfo
    consumer.GetType() |> should equal handlerInfo.ImplementationType
