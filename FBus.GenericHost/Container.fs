namespace FBus.GenericHost
open System
open System.Threading.Tasks
open Microsoft.Extensions.Hosting
open Microsoft.Extensions.DependencyInjection
open FBus

type BusService(busControl: IBusControl, serviceProvider: IServiceProvider) =
    interface IHostedService with
        member _.StartAsync cancellationToken =
            busControl.Start serviceProvider |> ignore
            Task.CompletedTask

        member _.StopAsync cancellationToken =
            busControl.Stop()
            Task.CompletedTask


type AspNetCoreContainer(services: IServiceCollection) =
    interface IBusContainer with
        member _.Register (handlerInfo: HandlerInfo) =
            services.AddTransient(handlerInfo.InterfaceType, handlerInfo.ImplementationType) |> ignore

        member _.Resolve ctx handlerInfo =
            match ctx with
            | :? IServiceProvider as serviceProvider -> serviceProvider.GetService(handlerInfo.InterfaceType)
            | _ -> null
