namespace FBus.Hosting
open System
open System.Threading.Tasks
open Microsoft.Extensions.Hosting
open Microsoft.Extensions.DependencyInjection
open FBus

type BusService(busControl: IBusControl, serviceProvider: IServiceProvider) =
    interface IHostedService with
        member this.StartAsync cancellationToken =
            busControl.Start serviceProvider |> ignore
            Task.CompletedTask

        member this.StopAsync cancellationToken =
            busControl.Stop()
            Task.CompletedTask


type AspNetCoreContainer(services: IServiceCollection) =
    interface IBusContainer with
        member this.Register (handlerInfo: HandlerInfo) =
            services.AddTransient(handlerInfo.InterfaceType, handlerInfo.ImplementationType) |> ignore

        member this.Resolve (ctx: obj) (t: System.Type) =
            match ctx with
            | :? IServiceProvider as serviceProvider -> serviceProvider.GetService(t)
            | _ -> null

[<AutoOpen>]
module DependencyInjectionExtensions =

    type IServiceCollection with
        member services.AddFBus(configurator: BusBuilder -> BusBuilder) =
            let busControl = Builder.init() |> configurator
                                            |> Builder.withContainer (AspNetCoreContainer(services))
                                            |> Builder.build

            let busSender = busControl :?> IBusSender

            services.AddSingleton(busControl)
                    .AddSingleton(busSender)
                    .AddHostedService<BusService>() |> ignore
