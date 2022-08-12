namespace FBus.GenericHost
open System
open Microsoft.Extensions.DependencyInjection
open FBus
open FBus.Containers
open System.Runtime.CompilerServices

[<Extension>]
type Extensions =
    [<Extension>]
    static member AddFBus(services: IServiceCollection, configurator: Func<BusBuilder, BusBuilder>) =
        let busControl = Builder.configure() |> FuncConvert.FromFunc(configurator)
                                             |> Builder.withContainer (GenericHost(services))
                                             |> Builder.build

        let busInitiator = busControl :?> IBusInitiator

        services.AddSingleton(busControl)
                .AddSingleton(busInitiator)
                .AddHostedService<BusService>()
