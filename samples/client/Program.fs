open System
open FBus.Builder

type ResponseConsumer() =
    interface FBus.IBusConsumer<Common.HelloWorld2> with
        member this.Handle ctx msg = 
            printfn "Received string message [%A] from [%s]" msg ctx.Sender
            printfn "-> sender = %s" ctx.Sender
            printfn "-> conversation-id = %s" ctx.ConversationId
            printfn "-> message-id = %s" ctx.MessageId

            // { Common.HelloWorld3.Message3 = "Message2" } |> ctx.Publish

            printfn "ResponseConsumer done"



[<EntryPoint>]
let main argv =

    let hook = { new FBus.IBusHook with
                     member _.OnStart initiator =
                         printfn ">>> OnStart"
     
                     member _.OnStop initiator =
                         printfn ">>> OnStop"
     
                     member _.OnBeforeProcessing conversation =
                         printfn ">>> OnBeforeProcessing"
                         null :> IDisposable
     
                     member _.OnError conversation msg exn =
                         printfn ">>> Error: %A %A" msg exn
                }

    use bus = FBus.QuickStart.configure() |> withName "client"
                                          |> withConsumer<ResponseConsumer>
                                          |> withHook hook
                                          |> build
 
    let busInitiator = bus.Start()
    
    for i in [1..1000] do
        printfn "Sending msg %d" i
        { Common.HelloWorld.Message = $"Message{i}" } |> busInitiator.Publish

    printfn "Press ENTER to exit"
    Console.ReadLine() |> ignore

    bus.Stop()
    
    0