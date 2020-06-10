open System
open FBus.Builder

[<EntryPoint>]
let main argv =

    use bus = FBus.Builder.init()
                 |> withName "sample-client"
                 |> build
 
    let busSender = bus.Start()
    
    let helloWorld = { Common.HelloWorld.Message = "Hello from FBus !" }
    match argv with
    | [| toServer |] -> busSender.Send toServer helloWorld
    | _ -> busSender.Publish helloWorld


    

    0 // return an integer exit code
