module FBus.Hosting
open System
open System.Threading.Tasks
open Microsoft.Extensions.Hosting
open Microsoft.Extensions.DependencyInjection

type BusService(busControl: Core.IBusControl, serviceProvider: IServiceProvider) =
    interface IHostedService with
        member this.StartAsync cancellationToken =
            busControl.Start()
            Task.CompletedTask

        member this.StopAsync cancellationToken =
            busControl.Stop()
            Task.CompletedTask

type IServiceCollection with
    member services.AddFBus(configurator: Core.BusBuilder -> Core.BusBuilder) =
        let containerRegistrant (msgHandler: Type) (implType: Type) = 
            services.AddTransient(msgHandler, implType) |> ignore

        let bus = Builder.init() |> configurator
                                 |> Builder.withRegistrant containerRegistrant
                                 |> Builder.build

        services.AddSingleton(bus)
                .AddSingleton<IHostedService, BusService>() |> ignore
