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
    
    let json = Serializers.Json() :> IBusSerializer
    let msgtype, body = data |> json.Serialize
    body |> json.Deserialize msgtype |> should equal data
