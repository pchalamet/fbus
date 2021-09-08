open FBus


type HelloWorldConsumer() =
    interface FBus.IBusConsumer<Common.HelloWorld> with
        member this.Handle ctx msg = 
            printfn "Received HelloWorld message [%A] from [%s]" msg ctx.Sender
            printfn "-> sender = %s" ctx.Sender
            printfn "-> conversation-id = %s" ctx.ConversationId
            printfn "-> message-id = %s" ctx.MessageId

            for idx in [1..10] do
                { Common.HelloWorld2.Message2 = sprintf "Hello %s (%d)" ctx.Sender idx } |> ctx.Reply

            printfn "HelloWorldConsumer done"


let handler (ctx:FBus.IBusConversation) (msg: Common.HelloWorld) =
    printfn "Received HelloWorld message [%A] from [%s]" msg ctx.Sender
    printfn "-> sender = %s" ctx.Sender
    printfn "-> conversation-id = %s" ctx.ConversationId
    printfn "-> message-id = %s" ctx.MessageId

    for idx in [1..10] do
        { Common.HelloWorld2.Message2 = sprintf "Hello %s (%d)" ctx.Sender idx } |> ctx.Reply

    printfn "HelloWorldConsumer done"


[<EntryPoint>]
let main argv =
    let serverName = match argv with
                     | [| serverName |] -> serverName
                     | _ -> "sample-server"

    use bus = FBus.QuickStart.configure() |> Builder.withName serverName
                                          |> Builder.withFunConsumer handler
                                          |> Builder.build

    bus.Start() |> ignore

    printfn "Press ENTER to exit"
    System.Console.ReadLine() |> ignore

    bus.Stop()

    0 // return an integer exit code
