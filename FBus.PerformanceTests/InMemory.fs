namespace FBus.InMemory.Performance
open FBus
open BenchmarkDotNet.Attributes
open System


type InMemoryMessage =
    { Content1: string } 
    interface IMessageCommand

type InMemoryHandler() =
    interface IBusConsumer<InMemoryMessage> with
        member _.Handle ctx msg = 
            ()

type InMemoryBenchmark() =

    let mutable bus: IBusControl = Unchecked.defaultof<IBusControl>
    let mutable busInitiator: IBusInitiator = Unchecked.defaultof<IBusInitiator>
    let msg = { Content1 = "toto" }

    [<GlobalSetup>]
    member _.GlobalSetup() =
        bus <- Builder.init() |> InMemory.useTransport
                              |> InMemory.useSerializer
                              |> InMemory.useContainer
                              |> Builder.withConsumer<InMemoryHandler>
                              |> Builder.build

        busInitiator <- bus.Start()

    [<GlobalCleanup>]
    member _.GlobalCleanup() =
        bus.Stop()
        bus.Dispose()

    [<Benchmark>]
    member _.Post () =
        busInitiator.Publish msg
        InMemory.waitForCompletion()


type InMemoryWithJsonBenchmark() =

    let mutable bus: IBusControl = Unchecked.defaultof<IBusControl>
    let mutable busInitiator: IBusInitiator = Unchecked.defaultof<IBusInitiator>
    let msg = { Content1 = "toto" }

    [<GlobalSetup>]
    member _.GlobalSetup() =
        bus <- Builder.init() |> InMemory.useTransport
                              |> InMemory.useSerializer
                              |> InMemory.useContainer
                              |> Builder.build

        busInitiator <- bus.Start()

    [<GlobalCleanup>]
    member _.GlobalCleanup() =
        bus.Stop()
        bus.Dispose()

    [<Benchmark>]
    member _.Post () =
        busInitiator.Publish msg
        InMemory.waitForCompletion()
