module GenericHost
open Microsoft.Extensions.DependencyInjection
open FBus
open FBus.GenericHost

type IServiceCollection with
    member services.AddFBus(configurator: BusBuilder -> BusBuilder) =
        let busControl = Builder.init() |> configurator
                                        |> Builder.withContainer (AspNetCoreContainer(services))
                                        |> Builder.build

        let busInitiator = busControl :?> IBusInitiator

        services.AddSingleton(busControl)
                .AddSingleton(busInitiator)
                .AddHostedService<BusService>() |> ignore
