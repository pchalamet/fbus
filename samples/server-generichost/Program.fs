open FBus
open Microsoft.Extensions.Hosting
open FBus.GenericHost
open System

type HelloWorldConsumer() =
    interface FBus.IBusConsumer<Common.HelloWorld> with
        member this.Handle ctx msg = 
            printfn "Received HelloWorld message [%A] from [%s]" msg ctx.Sender

[<EntryPoint>]
let main argv =
    let serverName = match argv with
                     | [| serverName |] -> serverName
                     | _ -> "sample-server"

    let configureBus builder =
        builder |> Builder.withName serverName
                |> Json.useDefaults
                |> RabbitMQ.useDefaults
                |> Builder.withConsumer<HelloWorldConsumer> 

    Host.CreateDefaultBuilder(argv)
        .ConfigureServices(fun services -> services.AddFBus(configureBus) |> ignore)
        .UseConsoleLifetime()
        .Build()
        .Run()

    0 // return an integer exit code
