module FBus.Hosting
open System
open System.Threading.Tasks
open Microsoft.Extensions.Hosting
open Microsoft.Extensions.DependencyInjection
open FBus

type BusService(busControl: IBusControl, serviceProvider: IServiceProvider) =
    interface IHostedService with
        member this.StartAsync cancellationToken =
            busControl.Start serviceProvider
            Task.CompletedTask

        member this.StopAsync cancellationToken =
            busControl.Stop()
            Task.CompletedTask

type IServiceCollection with
    member services.AddFBus(configurator: BusBuilder -> BusBuilder) =
        let containerRegistrant (handlerInfo: HandlerInfo) = 
            services.AddTransient(handlerInfo.InterfaceType, handlerInfo.ImplementationType) |> ignore

        let containerActivator (ctx: obj) (t: System.Type) =
            match ctx with
            | :? IServiceProvider as serviceProvider -> serviceProvider.GetService(t)
            | _ -> null

        let busControl = Builder.init() |> configurator
                                        |> Builder.withRegistrant containerRegistrant
                                        |> Builder.withActivator containerActivator
                                        |> Builder.build

        let busSender = busControl :?> IBusSender

        services.AddSingleton(busControl)
                .AddSingleton(busSender)
                .AddHostedService<BusService>() |> ignore
