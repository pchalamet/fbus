# fbus
small service-bus in F# for (mainly) F#.

It comes with default implementation for:
* RabbitMQ (with dead-letter support)
* Publish (broadcast) and Send (direct)
* Persistent queue/buffering across activation
* Generic Host support with dependency injection
* System.Text.Json serialization

Following features will appear in future revisions:
* Reply
* Parallelism support via sharding

Things that won't be:
* Sagas

# how to use it ?

## client (console)
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

## server (generic host)
```
...
let configureBus builder =
    builder |> withName serverName
            |> withAutoDelete false
            |> withHandler<HelloWorldProcessor> 

Host.CreateDefaultBuilder(argv)
    .ConfigureServices(fun services -> services.AddFBus(configureBus) |> ignore)
    .UseConsoleLifetime()
    .Build()
    .Run()
```


# Extensibility
Extension points are supported:
* Transport: default is RabbitMQ. Transport can be changed using `withTransport`.
* Serialization: default is System.Text.Json. Serialization can be changed using `withSerialization`.
* Container: default is none. Container can be changed using `withContainer`.
* Hosting: no hosting by default. GenericHost can be configured using `AddFBus`.

# Api

## Builder
Prior using the bus, a configuration must be built:

| FBus.Builder | Description | Default |
|--------------|-------------|---------|
| init | Create default configuration | |
| withName | Change service name. Used to identify an endpoint (see `IBusSender.Send`) | Name based on computer name, pid and random number |
| withTransport | Transport to use. | RabbitMQ |
| withEndpoint | Transport endpoint | amqp://guest:guest@localhost |
| withContainer | Container to use | System.Activator
| withSerializer | Serializer to use | System.Text.Json with [FSharp.SystemTextJson](https://github.com/Tarmil/FSharp.SystemTextJson) |
| withAutoDelete | Destroy queues upon exit | true |
| withHandler | Add message consumer | None |
| build | Create a bus instance based on configuration | | 

## Bus
`IBusControl` is the primary interface to control the bus:

| IBusControl | Description | Comments |
|-------------|-------------|----------|
| Start | Start the bus. Returns `IBusSender` | Must be called before sending messages. |
| Stop | Stop the bus. | |

Once bus is started, `IBusSender` is available:

| IBusSender | Description |
|------------|-------------|
| `Publish` | Broadcast the message to all subscribers |
| `Send` | Send only the message to given client |

## Consumer
`withHandler` register a new handler when a new message is received. Note exact type must match handler signature. A new instance is created each time a message has to be processed.

```
type IBusConsumer<'t> =
    abstract Handle: IContext -> 't -> unit
```

`IContext` is an empty interface but some information will be added later on.

# Generic Host
Support for Generic Host is available alongside dependency injection. See `AddFBus` and samples for more details.
