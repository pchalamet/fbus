open FBus.Builder
open FBus.Hosting
open Microsoft.Extensions.Hosting


type HelloWorldProcessor() =
    interface FBus.IBusConsumer<Common.HelloWorld> with
        member this.Handle ctx (msg: Common.HelloWorld) = 
            printfn "Received HelloWorld message [%A] from [%s]" msg ctx.Sender
            ctx.Reply "Hello !!!"

[<EntryPoint>]
let main argv =
    let serverName = match argv with
                     | [| serverName |] -> serverName
                     | _ -> "sample-server"

    use bus = init() |> withName serverName
                     |> withAutoDelete false
                     |> withHandler<HelloWorldProcessor> 
                     |> withTTL (System.TimeSpan.FromMinutes(1.0))
                     |> build

    bus.Start() |> ignore

    printfn "Press ENTER to exit"
    System.Console.ReadLine() |> ignore

    bus.Stop()

    0 // return an integer exit code
