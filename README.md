# FBus

[![Build status](https://github.com/pchalamet/fbus/workflows/build/badge.svg)](https://github.com/pchalamet/fbus/actions?query=workflow%3Abuild) 

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

## Available packages

Package | Status | Description
--------|--------|------------
FBus | [![Nuget](https://img.shields.io/nuget/v/FBus?logo=nuget)](https://nuget.org/packages/FBus) | Core package
FBus.RabbitMQ | [![Nuget](https://img.shields.io/nuget/v/FBus.RabbitMQ?logo=nuget)](https://nuget.org/packages/FBus.RabbitMQ) | RabbitMQ transport
FBus.Json | [![Nuget](https://img.shields.io/nuget/v/FBus.Json?logo=nuget)](https://nuget.org/packages/FBus.Json) | System.Text.Json serializer
FBus.GenericHost | [![Nuget](https://img.shields.io/nuget/v/FBus.GenericHost?logo=nuget)](https://nuget.org/packages/FBus.GenericHost) | Generic Host support
FBus.Dependencies | [![Nuget](https://img.shields.io/nuget/v/FBus.Dependencies?logo=nuget)](https://nuget.org/packages/FBus.Dependencies) | All FBus packages in a single reference package

# Usage

## Messages
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
    interface FBus.IMessageCommand
```

## In-Process console
### Client
```
open FBus
open FBus.Builder

use bus = FBus.Testing.configure() |> build

let busInitiator = bus.Start()
busInitiator.Send "hello from FBus !"
```

### Server
```
open FBus
open FBus.Builder

type MessageConsumer() =
    interface IConsumer<Message> with
        member this.Handle context msg = 
            printfn "Received message: %A" msg

use bus = FBus.Testing.configure() |> withConsumer<MessageConsumer> 
                                   |> build
bus.Start() |> ignore
```

## Server (generic host)
```
...
let configureBus builder =
    builder |> withName "server"
            |> withConsumer<MessageConsumer>
            |> Json.useSerializer
            |> RabbitMQ.useTransport

Host.CreateDefaultBuilder(argv)
    .ConfigureServices(fun services -> services.AddFBus(configureBus) |> ignore)
    .UseConsoleLifetime()
    .Build()
    .Run()
```

# Api

## Builder
Prior using the bus, a configuration must be built:
FBus.Builder | Description | Default
-------------|-------------|--------
`configure` | Start configuration with default parameters. |
`withName` | Change service name. Used to identify a bus client (see `IBusInitiator.Send` and `IBusConversation.Send`) | Name based on computer name, pid and random number.
`withTransport` | Transport to use. | None
`withEndpoint` | Transport endpoint | None
`withContainer` | Container to use | None
`withSerializer` | Serializer to use | None
`withConsumer` | Add message consumer | None
`withHook` | Hook on consumer message processing | None
`withRecovery` | Connect to dead letter for recovery only | false
`build` | Create a bus instance based on configuration | n/a

Note: bus clients are ephemeral by default - this is useful if you just want to connect to the bus for spying or sending commands for eg :-) Assigning a name (see `withName`) makes the client public so no queues are deleted upon exit.

## Testing
FBus can work in-memory - this is especially useful when unit-testing.

FBus.Testing | Description | Comments
-------------|-------------|---------
configure | Configure FBus for unit-testing | Configure transport, serializer and activator.
waitForCompletion | Wait for all messages to be processed | This method blocks until completion.

## Bus
`IBusControl` is the primary interface to control the bus:
IBusControl | Description | Comments
------------|-------------|---------
`Start` | Start the bus. Returns `IBusInitiator` | Must be called before sending messages. Start accepts a resolve context which can be used by the container.
`Stop` | Stop the bus. |

Once bus is started, `IBusInitiator` is available:
IBusInitiator | Description
--------------|------------
`Publish` | Broadcast the message to all subscribers.
`Send` | Send only the message to given client.

Note: a new conversation is started when using this interface.

## Consumer
`withConsumer` registers an handler - which will be able to process a message. Note exact type must match handler signature. A new instance is created each time a message has to be processed.

```
type IBusConsumer<'t> =
    abstract Handle: IBusConversation -> 't -> unit
```

`IBusConversation` provides information to handlers and means to interact with the bus:
IBusConversation | Description
-----------------|------------
`Sender` | Name of the client.
`ConversationId` | Id of the conversation (identifier is flowing from initiator to subsequent consumers).
`MessageId` | Id the this message.
`Reply` | Provide a shortcut to reply to sender.
`Publish` | Broadcast the message to all subscribers.
`Send` | Send only the message to given client.

Note: the current conversation is used when using this interface.

# Extensibility
Following extension points are supported:
* Transports: which middleware is transporting messages.
* Serializers: how messages are exchanged on the wire.
* Containers: where and how consumers are allocated and hosted.
* Hooks: handlers in case of failures.

## Transports
Two transports are available out of the box:
* RabbitMQ: transport implementation for RabbitMQ.
* InMemory: transport, serializer and container operating purely in memory. This can be used for testing. See sample `samples/in-memory` or unit-tests.

See `FBus.IBusTransport`.


## Containers
Support for Generic Host is available alongside dependencies injection. See `AddFBus` and samples for more details.

See `FBus.IBusContainer`.

## Serializers
Support for Json is available (using FSharp.SystemTextJson underneath).

See `FBus.IBusSerializer`.

## Consumers
Consumers can be configured at will. There is one major restriction: only one handler per type is supported. If you want several subscribers, you will have to handle delegation.

See `FBus.IBusConsumer<>`.

## Hooks
Allow one to observe errors while processing messages.

See `FBus.IBusHook`.

## Available extensions

### RabbitMQ (package FBus.RabbitMQ)

FBus.RabbitMQ | Description | Comments
--------------|-------------|---------
useTransport | Configure RabbitMQ as transport | Endpoint is set to `amqp://guest:guest@localhost`.

### Json (package FBus.Json)

FBus.Json | Description | Comments
----------|-------------|---------
useSerializer | Configure System.Text.Json as serializer | FSharp.SystemTextJson](https://github.com/Tarmil/FSharp.SystemTextJson) is used to deal with F# types.

### GenericHost

FBus.GenericHost | Description | Comments
-----------------|-------------|---------
AddFBus | Inject FBus in GenericHost container | `FBus.IBusControl` and `FBus.IBusInitiator` are available in injection context. 

# Thread safety
FBus is thread-safe. Plugin implementation shall be thread-safe as well.

## RabbitMQ transport
`FBus.RabbitMQ` package implements a RabbitMQ transport. It supports only a simple concurrency model:
* no concurrency at bus level for receive. This does not mean you can't have concurrency, you just have to handle it explicitely: you have to create multiple bus instances in-process and it's up to you to synchronize correctly among threads if required.
* Sending is a thread safe operation - but locking happens behind the scene to access underlying channel/connection.
* Automatic recovery is configured on connection.

The default implementation use following settings:
* messages are sent as persistent
* a consumer fetches one message at a time and ack/nack accordingly
* message goes to dead-letter on error

# Build it
A makefile is available:
* make [build]: build FBus
* make test: build and test FBus

If you prefer to build using your IDE, solution file is named `fbus.sln`.