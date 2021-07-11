namespace FBus.Containers
open System
open System.Threading.Tasks
open Microsoft.Extensions.Hosting
open Microsoft.Extensions.DependencyInjection
open FBus

type internal BusService(busControl: IBusControl, serviceProvider: IServiceProvider) =
    interface IHostedService with
        member _.StartAsync cancellationToken =
            busControl.Start serviceProvider |> ignore
            Task.CompletedTask

        member _.StopAsync cancellationToken =
            busControl.Stop()
            Task.CompletedTask

type GenericHost(services: IServiceCollection) =
    interface IBusContainer with
        member _.Register (handlerInfo: HandlerInfo) =
            match handlerInfo.Handler with
            | Class implementationType -> services.AddTransient(handlerInfo.InterfaceType, implementationType) |> ignore
            | Instance target -> services.AddSingleton(handlerInfo.InterfaceType, target) |> ignore

        member _.Resolve ctx handlerInfo =
            match ctx with
            | :? IServiceProvider as serviceProvider -> serviceProvider.GetService(handlerInfo.InterfaceType)
            | _ -> null
