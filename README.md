# fbus
FBus is a lightweight service-bus implementation written in F#.

![GitHub Workflow Status](https://img.shields.io/github/workflow/status/pchalamet/fbus/build) ![Nuget](https://img.shields.io/nuget/v/FBus)

It comes with default implementation for:
* RabbitMQ (with dead-letter support)
* In-memory transport for testing
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

let busInitiator = bus.Start()
busInitiator.Send "hello from FBus !"
```

## server (console)
```
open FBus
open FBus.Builder

type MessageConsumer() =
    interface IConsumer<string> with
        member this.Handle context msg = 
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
            |> withConsumer<MessageConsumer> 

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

## Transports
Two transports are available out of the box:
* RabbitMQ: this is the default.
* InMemory: this can be used for testing. See sample `samples/in-memory`.

## Generic Host
Support for Generic Host is available alongside dependencies injection. See `AddFBus` and samples for more details.

# Api

## Builder
Prior using the bus, a configuration must be built:
| FBus.Builder | Description | Default |
|--------------|-------------|---------|
| `init` | Create default configuration. | |
| `withName` | Change service name. Used to identify a bus client (see `IBusInitiator.Send` and `IBusConversation.Send`) | Name based on computer name, pid and random number. |
| `withTransport` | Transport to use. | `RabbitMQ` |
| `withEndpoint` | Transport endpoint | `amqp://guest:guest@localhost` |
| `withContainer` | Container to use | `Activator` |
| `withSerializer` | Serializer to use | System.Text.Json with [FSharp.SystemTextJson](https://github.com/Tarmil/FSharp.SystemTextJson) |
| `withConsumer` | Add message consumer | None |
| `build` | Create a bus instance based on configuration | | 

Note: bus clients are ephemeral by default - this is useful if you just want to connect to the bus for spying or sending commands for eg :-) Assigning a name (see `withName`) makes the client public so no queues are deleted upon exit.

## Bus
`IBusControl` is the primary interface to control the bus:
| IBusControl | Description | Comments |
|-------------|-------------|----------|
| `Start` | Start the bus. Returns `IBusInitiator` | Must be called before sending messages. Start accepts a resolve context which can be used by the container. |
| `Stop` | Stop the bus. | |

Once bus is started, `IBusInitiator` is available:
| IBusInitiator | Description |
|------------|-------------|
| `Publish` | Broadcast the message to all subscribers. |
| `Send` | Send only the message to given client. |

Note: a new conversation is started when using this interface.

## Consumer
`withConsumer` registers an handler - which will be able to process a message. Note exact type must match handler signature. A new instance is created each time a message has to be processed.

```
type IBusConsumer<'t> =
    abstract Handle: IBusConversation -> 't -> unit
```

`IBusConversation` provides information to handlers and means to interact with the bus:
| IBusConversation | Description |
|------------|-------------|
| `Sender` | Name of the client. |
| `ConversationId` | Id of the conversation (identifier is flowing from initiator to subsequent consumers). |
| `MessageId` | Id the this message. |
| `Reply` | Provide a shortcut to reply to sender. |
| `Publish` | Broadcast the message to all subscribers. |
| `Send` | Send only the message to given client. |

Note: the current conversation is used when using this interface.
