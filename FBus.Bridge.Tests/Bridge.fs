module FBus.Bridge.Tests
open NUnit.Framework
open FsUnit
open FBus
open System
open System.Text.Json

type MyType =
    { Int: int
      MaybeString: string option
      Map: Map<string, int option> }
    interface FBus.IMessageEvent

[<Test>]
let ``Bridge serialization`` () =
    let data = { Int = 42
                 MaybeString = Some "this is a string"
                 Map = Map [ "toto", None
                             "titi", Some 42 ] }
    
    let bridgeMessage = { FBus.Serializers.BridgeEventMessage.Type = "Provided.Message.Type"
                          FBus.Serializers.BridgeEventMessage.Message = JsonSerializer.Serialize(data) }
 
    let bridge = Serializers.BridgeSerializer() :> IBusSerializer
    let msgtype, body = bridgeMessage |> bridge.Serialize
    msgtype |> should equal "Provided.Message.Type"
    System.Text.Encoding.UTF8.GetString(body.ToArray()) |> should equal bridgeMessage.Message

[<Test>]
let ``Bridge serialization failure if something else`` () =
    let data = { Int = 42
                 MaybeString = Some "this is a string"
                 Map = Map [ "toto", None
                             "titi", Some 42 ] }
    
    let bridge = Serializers.BridgeSerializer() :> IBusSerializer
    (fun () -> data |> bridge.Serialize |> ignore) |> should (throwWithMessage "Expecting BridgeMessage") typeof<Exception>

[<Test>]
let ``Bridge deserializer must fail`` () =
    let data = { Int = 42
                 MaybeString = Some "this is a string"
                 Map = Map [ "toto", None
                             "titi", Some 42 ] }
    
    let bridge = Serializers.BridgeSerializer() :> IBusSerializer
    (fun () -> bridge.Deserialize (typeof<int>) (ReadOnlyMemory()) |> ignore) |> should (throwWithMessage "BridgeSerializer is not designed to deserialize") typeof<Exception>

