open FBus.Builder


type HelloWorldConsumer() =
    interface FBus.IBusConsumer<Common.HelloWorld> with
        member this.Handle ctx msg = 
            printfn "Received HelloWorld message [%A] from [%s]" msg ctx.Sender
            printfn "-> sender = %s" ctx.Sender
            printfn "-> conversation-id = %s" ctx.ConversationId
            printfn "-> message-id = %s" ctx.MessageId

            ctx.Sender |> sprintf "Hello %s" |> ctx.Reply


[<EntryPoint>]
let main argv =
    let serverName = match argv with
                     | [| serverName |] -> serverName
                     | _ -> "sample-server"

    use bus = init() |> withName serverName
                     |> withConsumer<HelloWorldConsumer> 
                     |> build

    bus.Start() |> ignore

    printfn "Press ENTER to exit"
    System.Console.ReadLine() |> ignore

    bus.Stop()

    0 // return an integer exit code
