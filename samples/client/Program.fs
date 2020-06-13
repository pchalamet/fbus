open System
open FBus
open FBus.Builder

type ResponseConsumer() =
    interface FBus.IBusConsumer<string> with
        member this.Handle ctx (msg: string) = 
            printfn "Received string message [%A] from [%s]" msg ctx.Sender



[<EntryPoint>]
let main argv =

    use bus = FBus.Builder.init()
                 |> withName "sample-client"
                 |> withConsumer<ResponseConsumer>
                 |> build
 
    let busSender = bus.Start()
    
    let helloWorld = { Common.HelloWorld.Message = "Hello from FBus !" }
    match argv with
    | [| toServer |] -> busSender.Send toServer helloWorld
    | _ -> busSender.Publish helloWorld

    printfn "Press ENTER to exit"
    Console.ReadLine() |> ignore

    bus.Stop()
    
    0