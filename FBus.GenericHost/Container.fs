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
            let itfType =
                if handlerInfo.Async then typedefof<IAsyncBusConsumer<_>>.MakeGenericType(handlerInfo.MessageType)
                else typedefof<IBusConsumer<_>>.MakeGenericType(handlerInfo.MessageType)
            services.AddScoped(itfType, handlerInfo.Handler) |> ignore

        member _.NewScope ctx =
            match ctx with
            | :? IServiceProvider as serviceProvider -> serviceProvider.CreateScope() :> IDisposable
            | _ -> null

        member _.Resolve ctx handlerInfo =
            let itfType =
                if handlerInfo.Async then typedefof<IAsyncBusConsumer<_>>.MakeGenericType(handlerInfo.MessageType)
                else typedefof<IBusConsumer<_>>.MakeGenericType(handlerInfo.MessageType)
            match ctx with
            | :? IServiceProvider as serviceProvider -> serviceProvider.GetService(itfType)
            | _ -> null
