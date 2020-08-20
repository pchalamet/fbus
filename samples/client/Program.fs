open System
open FBus.Builder

type ResponseConsumer() =
    interface FBus.IBusConsumer<Common.HelloWorld2> with
        member this.Handle ctx msg = 
            printfn "Received string message [%A] from [%s]" msg ctx.Sender
            printfn "-> sender = %s" ctx.Sender
            printfn "-> conversation-id = %s" ctx.ConversationId
            printfn "-> message-id = %s" ctx.MessageId

            { Common.HelloWorld3.Message3 = "Message2" } |> ctx.Publish

            printfn "ResponseConsumer done"



[<EntryPoint>]
let main argv =

    use bus = FBus.Builder.init() |> withName "client"
                                  |> withConsumer<ResponseConsumer>
                                  |> build
 
    let busInitiator = bus.Start()
    
    { Common.HelloWorld.Message = "Message1" } |> busInitiator.Publish

    printfn "Press ENTER to exit"
    Console.ReadLine() |> ignore

    bus.Stop()
    
    0