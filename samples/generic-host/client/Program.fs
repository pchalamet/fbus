open System
open FBus.Builder
open FBus.Hosting
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Hosting



type Msg1 = {
    Content: string
}

type Msg2 = {
    Content: string
    Id: int
}

type Toto =
    interface FBus.Core.IConsumer<Msg1> with
        member this.Handle(msg: Msg1): unit = 
            failwith "Not Implemented"

type Titi =
    interface FBus.Core.IConsumer<Msg2> with
        member this.Handle(msg: Msg2): unit = 
            failwith "Not Implemented"


[<EntryPoint>]
let main argv =
    let bus = init() |> withHandler<Toto> 
                     |> withHandler<Titi>
                     |> build
    bus.Start()

//     let configureBus builder =
//         builder

//     Host.CreateDefaultBuilder(argv)
// //        .ConfigureServices(fun services -> services.AddHostedService<IntegratorWorker>() |> ignore)
//         .ConfigureServices(fun services -> services.AddFBus(configureBus) |> ignore)
//         .UseConsoleLifetime()
//         .Build()
//         .Run()

    0 // return an integer exit code
