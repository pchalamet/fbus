namespace Common

type HelloWorld =
    { Message: string }
    interface FBus.IMessageEvent

type HelloWorld2 =
    { Message2: string }
    interface FBus.IMessageCommand

type HelloWorld3 =
    { Message3: string }
    interface FBus.IMessageEvent
