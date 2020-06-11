open System
open FBus.Builder
open FBus.Hosting.GenericHost
open Microsoft.Extensions.Hosting


type HelloWorldProcessor() =
    interface FBus.IConsumer<Common.HelloWorld> with
        member this.Handle(msg: Common.HelloWorld) = 
            printfn "Received HelloWorld message: %A" msg

[<EntryPoint>]
let main argv =
    let serverName = match argv with
                     | [| serverName |] -> serverName
                     | _ -> "sample-server"

    let configureBus builder =
        builder |> withName serverName
                |> withAutoDelete false
                |> withHandler<HelloWorldProcessor> 

    Host.CreateDefaultBuilder(argv)
        .ConfigureServices(fun services -> services.AddFBus(configureBus) |> ignore)
        .UseConsoleLifetime()
        .Build()
        .Run()

    0 // return an integer exit code
