module FBus.GenericHost
open Microsoft.Extensions.DependencyInjection
open FBus
open FBus.Containers

type IServiceCollection with
    member services.AddFBus(configurator: BusBuilder -> BusBuilder) =
        let busControl = Builder.configure() |> configurator
                                             |> Builder.withContainer (GenericHost(services))
                                             |> Builder.build

        let busInitiator = busControl :?> IBusInitiator

        services.AddSingleton(busControl)
                .AddSingleton(busInitiator)
                .AddHostedService<BusService>() |> ignore


//
// Extension method for C# - this breaks F# clients :-(
// 
// [<System.Runtime.CompilerServices.Extension>]
// module FBus.GenericHost
// open Microsoft.Extensions.DependencyInjection
// open FBus
// open FBus.Containers
// open System.Runtime.CompilerServices

// [<Extension>]
// let AddFBus(services: IServiceCollection, configurator: System.Func<BusBuilder, BusBuilder>) =
//     let busControl = Builder.configure() |> configurator.Invoke
//                                          |> Builder.withContainer (GenericHost(services))
//                                          |> Builder.build

//     let busInitiator = busControl :?> IBusInitiator

//     services.AddSingleton(busControl)
//             .AddSingleton(busInitiator)
//             .AddHostedService<BusService>() |> ignore
