# fbus
small service-bus in F# for (mainly) F#. But hey, it should work for C# as well.

# how to use it ?

## client (console):
```
open FBus
open FBus.Builder

let bus = init() |> build

let busSender = bus.Start()
busSender.Send "hello from FBus !"
```

## server (console)
```
open FBus
open FBus.Builder

type MessageHandler() =
    interface IConsumer<string> with
        member this.Handle(msg: string) = 
            printfn "Received message: %A" msg

let bus = init() |> withHandler<MessageHandler> |> build
bus.Start() |> ignore
```

# Extensibility

Extension points are supported:
* Transport: default is RabbitMQ but other transport can be plugged
* 