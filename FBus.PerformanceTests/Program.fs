module Performance
open BenchmarkDotNet.Running

[<EntryPoint>]
let main argv =
    BenchmarkRunner.Run<FBus.InMemory.Performance.InMemoryBenchmark>() |> ignore
    BenchmarkRunner.Run<FBus.InMemory.Performance.InMemoryWithJsonBenchmark>() |> ignore
    0
