module Performance
open BenchmarkDotNet.Running

[<EntryPoint>]
let main argv =
    BenchmarkRunner.Run<FBus.PerformanceTests.Serializer.SerializerBenchmark>() |> ignore
    0
