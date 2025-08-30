module FBus.StressTest.Common

type Ping =
    { Message: string 
      Seq: int }
    interface FBus.IMessageEvent

type Pong =
    { Message: string 
      Seq: int }
    interface FBus.IMessageEvent

type BangEvent =
    { Message: string }
    interface FBus.IMessageEvent

type BangCommand =
    { Message: string }
    interface FBus.IMessageCommand
