﻿open System
open FBus.Builder

type HelloMessageRequest =
    { Msg: string }
    interface FBus.IMessageEvent

type HelloMessageResponse =
    { Msg: string }
    interface FBus.IMessageCommand


type Consumer1() =
    interface FBus.IBusConsumer<string> with
        member _.Handle ctx (msg: string) = 
            printfn "Received string message [%A] from [%s]" msg ctx.Sender
            printfn "-> sender = %s" ctx.Sender
            printfn "-> conversation-id = %s" ctx.ConversationId
            printfn "-> message-id = %s" ctx.MessageId

type Consumer2() =
    interface FBus.IBusConsumer<string> with
        member _.Handle ctx (msg: string) = 
            printfn "Received string message [%A] from [%s]" msg ctx.Sender
            printfn "-> sender = %s" ctx.Sender
            printfn "-> conversation-id = %s" ctx.ConversationId
            printfn "-> message-id = %s" ctx.MessageId

            { HelloMessageResponse.Msg = ctx.Sender |> sprintf "Hello %s" } |> ctx.Reply

[<EntryPoint>]
let main argv =
    let session = FBus.Testing.Session()
    use bus1 = FBus.Builder.configure() |> session.Use
                                        |> withConsumer<Consumer1>
                                        |> build
    let busInitiator1 = bus1.Start()
    
    use bus2 = FBus.Builder.configure() |> session.Use
                                        |> withConsumer<Consumer2>
                                        |> build
    let busInitiator2 = bus2.Start()

    busInitiator1.Publish { HelloMessageRequest.Msg = "Hello in-memory !" }

    printfn "Press ENTER to exit"
    Console.ReadLine() |> ignore
    
    0