module fbus.test.Container
open NUnit.Framework
open FsUnit
open FBus

type StringConsumer() =
    interface IBusConsumer<string> with
        member this.Handle context msg = 
            failwith "Not Implemented"

[<Test>]
let ``Activator create type with default constructor`` () =
    let activator = FBus.Container.Activator() :> FBus.IBusContainer
    let handlerInfo = { MessageType = typeof<string>
                        InterfaceType = typeof<IBusConsumer<string>>
                        ImplementationType = typeof<StringConsumer> }
    let consumer = activator.Resolve null handlerInfo
    consumer.GetType() |> should equal handlerInfo.ImplementationType
