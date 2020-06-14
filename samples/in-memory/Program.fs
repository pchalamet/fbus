open System
open FBus.Builder

type Consumer1() =
    interface FBus.IBusConsumer<string> with
        member this.Handle ctx (msg: string) = 
            printfn "Received string message [%A] from [%s]" msg ctx.Sender
            printfn "-> sender = %s" ctx.Sender
            printfn "-> conversation-id = %s" ctx.ConversationId
            printfn "-> message-id = %s" ctx.MessageId

type Consumer2() =
    interface FBus.IBusConsumer<string> with
        member this.Handle ctx (msg: string) = 
            printfn "Received string message [%A] from [%s]" msg ctx.Sender
            printfn "-> sender = %s" ctx.Sender
            printfn "-> conversation-id = %s" ctx.ConversationId
            printfn "-> message-id = %s" ctx.MessageId

            ctx.Sender |> sprintf "Hello %s" |> ctx.Reply

[<EntryPoint>]
let main argv =

    use bus1 = FBus.Builder.init()
                 |> withTransport FBus.InMemory.Transport.Create
                 |> withConsumer<Consumer1>
                 |> build
    let busInitiator1 = bus1.Start()
    
    use bus2 = FBus.Builder.init()
                 |> withTransport FBus.InMemory.Transport.Create
                 |> withConsumer<Consumer2>
                 |> build
    let busInitiator2 = bus2.Start()

    busInitiator1.Publish "Hello in-memory !"

    printfn "Press ENTER to exit"
    Console.ReadLine() |> ignore
    
    0