# fbus
small service-bus in F# for (mainly) F#.

It comes with default implementation for:
* RabbitMQ (with dead-letter support)
* Publish (broadcast), Send (direct) and Reply (direct)
* Conversation follow-up using headers (ConversationId and MessageId)
* Persistent queue/buffering across activation
* Generic Host support with dependency injection
* System.Text.Json serialization

Following features will appear in future revisions:
* Logger extension point
* Handlers using pure function
* Parallelism support via sharding

Features that won't be implemented in FBus:
* Sagas: coordination is big topic by itself - technically, everything required to handle this is available (ConversationId and MessageId). This can be handled outside of a service-bus.

# how to use it ?

## client (console)
```
open FBus
open FBus.Builder

use bus = init() |> build

let busSender = bus.Start()
busSender.Send "hello from FBus !"
```

## server (console)
```
open FBus
open FBus.Builder

type MessageConsumer() =
    interface IConsumer<string> with
        member this.Handle(msg: string) = 
            printfn "Received message: %A" msg

use bus = init() |> withConsumer<MessageConsumer> 
                 |> build
bus.Start() |> ignore
```

## server (generic host)
```
...
let configureBus builder =
    builder |> withName "server"
            |> withHandler<HelloWorldConsumer> 

Host.CreateDefaultBuilder(argv)
    .ConfigureServices(fun services -> services.AddFBus(configureBus) |> ignore)
    .UseConsoleLifetime()
    .Build()
    .Run()
```

# Extensibility
Following extension points are supported:
* Transport: default is RabbitMQ. Transport can be changed using `withTransport`.
* Serialization: default is System.Text.Json. Serialization can be changed using `withSerialization`.
* Container: default is none. Container can be changed using `withContainer`.
* Hosting: no hosting by default. GenericHost can be configured using `AddFBus`.

## Generic Host
Support for Generic Host is available alongside dependencies injection. See `AddFBus` and samples for more details.

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
| withConsumer | Add message consumer | None |
| build | Create a bus instance based on configuration | | 

Note: bus client is ephemeral by default (hence no traces left upon exit) - this is useful if you just want to connect to the bus for spying for eg :-) Assigning a name (see `withName`) makes the client public so no queues are not deleted upon exit.

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
`withConsumer` registers an handler - which will be able to process a message. Note exact type must match handler signature. A new instance is created each time a message has to be processed.

```
type IBusConsumer<'t> =
    abstract Handle: IContext -> 't -> unit
```

`IContext` inherits from `IBusSender` and provides some information to handler:
| IContext | Description |
|------------|-------------|
| Sender | Name of the client |
| ConversationId | Id of the conversation (identifier is flowing from initiator to subsequent consumers) |
| MessageId | Id the this message |
| Reply | Provide a shortcut to reply to sender |
| Publish | see `IBusSender` |
| Send | see `IBusSender` |
