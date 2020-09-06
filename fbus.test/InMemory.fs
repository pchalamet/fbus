module FBus.InMemory.Test
open System
open NUnit.Framework
open FsUnit

open FBus
open FBus.Builder
open Microsoft.Extensions.DependencyInjection
open System.Runtime.InteropServices


type InMemoryMessage1 =
    { Content1: string } 
    interface FBus.IMessageCommand

type InMemoryMessage2 =
    { Content2: string } 
    interface FBus.IMessageCommand

type IHandlerInvoked =
    abstract HasBeenInvoked: unit -> unit

type InMemoryHandler1(handlerInvoked: IHandlerInvoked) =
    interface FBus.IBusConsumer<InMemoryMessage1> with
        member this.Handle ctx msg = 
            { Content2 = msg.Content1 } |> ctx.Send "InMemoryHandler2"
            handlerInvoked.HasBeenInvoked()

type InMemoryHandler2(handlerInvoked: IHandlerInvoked) =
    interface FBus.IBusConsumer<InMemoryMessage2> with
        member this.Handle ctx msg = 
            handlerInvoked.HasBeenInvoked()

let startServer<'t> name callback =
    let handledInvoked = {
        new IHandlerInvoked with
            member this.HasBeenInvoked(): unit = 
                printfn "Handler invoked on server [%s]" name
                callback()
    }

    let checkErrorHook = {
        new IBusHook with
            member this.OnError ctx msg exn =
                failwithf "No error shall be raised: %A" exn
    }

    let svcCollection = ServiceCollection() :> IServiceCollection
    svcCollection.AddSingleton(handledInvoked) |> ignore
    let serverBus = FBus.Builder.init() |> withName name
                                         |> withContainer (FBus.Hosting.AspNetCoreContainer(svcCollection))
                                         |> withTransport FBus.InMemory.Transport.Create
                                         |> withConsumer<'t>
                                         |> withHook checkErrorHook
                                         |> FBus.Builder.build
    svcCollection.BuildServiceProvider() |> serverBus.Start |> ignore
    serverBus

[<Test>]
let ``check inmemory message exchange`` () =
    let mutable serverHasBeenInvoked1 = false
    let mutable serverHasBeenInvoked2 = false
    use bus1 = startServer<InMemoryHandler1> "InMemoryHandler1" (fun () -> serverHasBeenInvoked1 <- true)
    use bus2 = startServer<InMemoryHandler2> "InMemoryHandler2" (fun () -> serverHasBeenInvoked2 <- true)

    use clientBus = FBus.Builder.init() |> withTransport FBus.InMemory.Transport.Create |> FBus.Builder.build
    let clientInitiator = clientBus.Start() 

    { Content1 = "Hello InMemory" } |> clientInitiator.Publish

    FBus.InMemory.Transport.WaitForCompletion()

    serverHasBeenInvoked1 |> should equal true
    serverHasBeenInvoked2 |> should equal true

[<Test>]
let ``check inmemory message exchange again`` () =
    let mutable serverHasBeenInvoked1 = false
    let mutable serverHasBeenInvoked2 = false
    use bus1 = startServer<InMemoryHandler1> "InMemoryHandler1" (fun () -> serverHasBeenInvoked1 <- true)
    use bus2 = startServer<InMemoryHandler2> "InMemoryHandler2" (fun () -> serverHasBeenInvoked2 <- true)

    use clientBus = FBus.Builder.init() |> withTransport FBus.InMemory.Transport.Create |> FBus.Builder.build
    let clientInitiator = clientBus.Start() 

    { Content1 = "Hello InMemory" } |> clientInitiator.Publish

    FBus.InMemory.Transport.WaitForCompletion()

    serverHasBeenInvoked1 |> should equal true
    serverHasBeenInvoked2 |> should equal true






type MyType = {
    Int: int
    MaybeString: string option
    Map: Map<string, int option>
}



[<Test>]
let ``InMemory serializer roundtrip`` () =
    let data = { Int = 42
                 MaybeString = Some "this is a string"
                 Map = Map [ "toto", None
                             "titi", Some 42 ] }
    
    let serializer = Serializer() :> IBusSerializer
    
    let body = data |> serializer.Serialize
    let newData = body |> serializer.Deserialize typeof<MyType>
    Object.ReferenceEquals(newData, data) |> should equal true

    // test retrieve errors as well
    (fun () -> body |> serializer.Deserialize typeof<MyType> |> ignore) |> should (throwWithMessage "Failed to retrieve object") typeof<Exception>
    (fun () -> [| 01uy; 02uy; 03uy; 04uy; 
                  05uy; 06uy; 07uy; 08uy; 
                  09uy; 10uy; 11uy; 12uy; 
                  13uy; 14uy; 15uy; 16uy;|] |> ReadOnlyMemory 
                                            |> serializer.Deserialize typeof<MyType> |> ignore) 
                                            |> should (throwWithMessage "Failed to retrieve object") typeof<Exception>
    (fun () -> [| 42uy |] |> ReadOnlyMemory |> serializer.Deserialize typeof<MyType> |> ignore) |> should throw typeof<ArgumentException>

