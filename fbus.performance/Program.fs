module Performance
open BenchmarkDotNet.Running

[<EntryPoint>]
let main argv =
    BenchmarkRunner.Run<FBus.Performance.InMemoryBenchmark>() |> ignore
    BenchmarkRunner.Run<FBus.Performance.InMemoryWithJsonBenchmark>() |> ignore
    0
