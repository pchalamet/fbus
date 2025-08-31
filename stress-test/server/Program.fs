module FBus.StressTest.Server
open FBus
open System
open Common

let rnd = Random()

type MessageConsumer() =
    let handle (ctx: FBus.IBusConversation) msg seq =
            printfn "Received message [%A] from [%s]" msg ctx.Sender

            let proba = rnd.NextDouble()
            if proba < 0.5 then 
                printfn ">>> Replying"
                { Pong.Message = $"{msg}/Pong"
                  Seq = seq } |> ctx.Reply

    interface FBus.IBusConsumer<Pong> with
        member _.Handle ctx msg = handle ctx msg.Message msg.Seq

    interface FBus.IAsyncBusConsumer<Ping> with
        member _.HandleAsync ctx msg = 
            task {
                handle ctx msg.Message msg.Seq
            }


let hook = { new FBus.IBusHook with
                 member _.OnStart initiator =
                     printfn ">>> OnStart"
 
                 member _.OnStop initiator =
                     printfn ">>> OnStop"
 
                 member _.OnBeforeProcessing conversation =
                     printfn ">>> OnBeforeProcessing"
                     null
 
                 member _.OnError conversation msg exn =
                     printfn ">>> Error: %A %A" msg exn
            }


[<EntryPoint>]
let main argv =
    use bus = FBus.QuickStart.configure() |> Builder.withName "fbus-stresstest-server"
                                          |> Builder.withConcurrency 2
                                          |> Builder.withConsumer<MessageConsumer>
                                          |> Builder.withHook hook
                                          |> Builder.build

    bus.Start() |> ignore

    printfn "Press ENTER to exit"
    System.Console.ReadLine() |> ignore

    bus.Stop()

    0 // return an integer exit code
