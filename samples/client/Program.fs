open System
open FBus.Builder

type ResponseConsumer() =
    interface FBus.IBusConsumer<string> with
        member this.Handle ctx (msg: string) = 
            printfn "Received string message [%A] from [%s]" msg ctx.Sender
            printfn "-> sender = %s" ctx.Sender
            printfn "-> conversation-id = %s" ctx.ConversationId
            printfn "-> message-id = %s" ctx.MessageId



[<EntryPoint>]
let main argv =

    use bus = FBus.Builder.init()
                 |> withConsumer<ResponseConsumer>
                 |> build
 
    let busSender = bus.Start()
    
    let helloWorld = { Common.HelloWorld.Message = "Hello from FBus !" }
    busSender.Publish helloWorld

    printfn "Press ENTER to exit"
    Console.ReadLine() |> ignore

    bus.Stop()
    
    0