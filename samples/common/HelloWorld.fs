namespace Common

type HelloWorld =
    { Message: string }
    interface FBus.IMessageEvent
    interface FBus.IMessageKey with
        member this.Key: string =
            this.Message

type HelloWorld2 =
    { Message2: string }
    interface FBus.IMessageEvent

type HelloWorld3 =
    { Message3: string }
    interface FBus.IMessageEvent
