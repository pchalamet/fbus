module FBus.Json.Tests
open NUnit.Framework
open FsUnit
open FBus

type MyType = {
    Int: int
    MaybeString: string option
    Map: Map<string, int option>
}

[<Test>]
let ``Json roundtrip`` () =
    let data = { Int = 42
                 MaybeString = Some "this is a string"
                 Map = Map [ "toto", None
                             "titi", Some 42 ] }
    
    let json = Json.Serializer() :> IBusSerializer
    
    data |> json.Serialize |> json.Deserialize typeof<MyType> |> should equal data
