module FBus.StressTest.Client

open System
open FBus.Builder
open Common

let rnd = Random()

type MessageConsumer() =
    interface FBus.IBusConsumer<Pong> with
        member _.Handle ctx msg = 
            printfn "Received string message [%A] from [%s]" msg ctx.Sender

            let proba = rnd.NextDouble()
            if proba < 0.5 then
                printfn ">>>> Replying"
                { Pong.Message = $"Ping/{msg.Message}"
                  Seq = msg.Seq } |> ctx.Reply


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


[<EntryPoint>]
let main argv =
    use bus = FBus.QuickStart.configure() |> withName "fbus-stresstest-client"
                                          |> withConsumer<MessageConsumer>
                                          |> withHook hook
                                          |> build
 
    let busInitiator = bus.Start()
   
    for i in [1..100000] do
        try
            let proba = rnd.NextDouble()
            if proba < 0.8 then
                printfn $">>> Sending Ping {i}"
                { Ping.Message = "Ping"; Seq = i } |> busInitiator.Publish
            elif proba < 0.9 then
                printfn $">>> Sending BangEvent {i}"
                { BangEvent.Message = "Bang !" } |> busInitiator.Publish
            else
                printfn $">>> Sending BangCommand {i}"
                { BangCommand.Message = "Bang !"} |> busInitiator.Send (Guid.NewGuid().ToString())
        with
            exn -> printfn "ERROR: %A" exn
 
    printfn "Press ENTER to exit"
    Console.ReadLine() |> ignore

    bus.Stop()
    
    0