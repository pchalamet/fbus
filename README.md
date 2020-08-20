# fbus
[![Build status](https://github.com/pchalamet/fbus/workflows/build/badge.svg)](https://github.com/pchalamet/fbus/actions?query=workflow%3Abuild) [![Nuget](https://img.shields.io/nuget/v/FBus?logo=nuget)](https://nuget.org/packages/FBus)

FBus is a lightweight service-bus implementation written in F#.

It comes with default implementation for:
* RabbitMQ (with dead-letter support)
* In-memory transport for testing
* Publish (broadcast), Send (direct) and Reply (direct)
* Conversation follow-up using headers (ConversationId and MessageId)
* Persistent queue/buffering across activation
* Generic Host support with dependency injection
* System.Text.Json serialization

Following features might appear in future revisions:
* Parallelism support via sharding

Features that won't be implemented in FBus:
* Sagas: coordination is big topic by itself - technically, everything required to handle this is available (ConversationId and MessageId). This can be handled outside of a service-bus.

# Thread safety
FBus is thread-safe but that's not necessarily the case of the transports.

## RabbitMQ transport
The default transport implementation for RabbitMQ supports only a simple concurrency model:
* no concurrency at bus level for receive. This does not mean you can't have concurrency, you just have to handle it explicitely: you have to create multiple bus instances in-process and it's up to you to synchronize correctly among threads if required.
* Sending is a thread safe operation - but locking happens behind the scene to access underlying connection.

The default implementation use following settings:
* messages are sent as persistent
* a consumer fetches one message at a time and ack/nack accordingly
* message goes to dead-letter on error

# how to use it ?

## messages
There are 2 types of messages:
* events: messages that are broadcasted
* commands: messages that are sent to one client

In order to avoid sending anything over the wire and avoid mistakes, messages are marked with a dummy interface:

For events:
```
type EventMessage =
    { msg: string }
    interface FBus.IMessageEvent
````

For commands:
```
type CommandMessage =
    { msg: string }
    interface FBus.IMessageEvent
```

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
    interface IConsumer<Message> with
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
* InMemory: this can be used for testing. See sample `samples/in-memory` or unit-tests.

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
| `withHook` | Hook on consumer message processing | None |
| `withRecovery` | Connect to dead letter for recovery only | |
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

# how to build it ?
A makefile is available:
* make [build]: build FBus
* make test: build and test FBus

If you prefer to build using your IDE, solution file is named `fbus.sln`.