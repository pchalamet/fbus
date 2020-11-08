module FBus.InMemory.Tests
open System
open NUnit.Framework
open FsUnit

open FBus
open FBus.Builder



type MyType = {
    Int: int
    MaybeString: string option
    Map: Map<string, int option>
}



[<Test>]
let ``InMemory serializer roundtrip`` () =
    let data = { Int = 42
                 MaybeString = Some "this is a string"
                 Map = Map [ "toto", None
                             "titi", Some 42 ] }
    
    let serializer = Serializers.InMemory() :> IBusSerializer
    
    let body = data |> serializer.Serialize
    let newData = body |> serializer.Deserialize typeof<MyType>
    Object.ReferenceEquals(newData, data) |> should equal true

    // check purge works as expected
    FBus.Testing.clearSerializerCache()
    (fun () -> body |> serializer.Deserialize typeof<MyType> |> ignore) |> should (throwWithMessage "Failed to retrieve message") typeof<Exception>

    // test retrieve errors as well
    (fun () -> [| 01uy; 02uy; 03uy; 04uy; 
                  05uy; 06uy; 07uy; 08uy; 
                  09uy; 10uy; 11uy; 12uy; 
                  13uy; 14uy; 15uy; 16uy;|] |> ReadOnlyMemory 
                                            |> serializer.Deserialize typeof<MyType> |> ignore) 
                                            |> should (throwWithMessage "Failed to retrieve message") typeof<Exception>
    (fun () -> [| 42uy |] |> ReadOnlyMemory |> serializer.Deserialize typeof<MyType> |> ignore) |> should throw typeof<ArgumentException>
