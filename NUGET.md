FBus is a lightweight service-bus implementation written in F#.

It comes with default implementation for:
* Publish (broadcast), Send (direct) and Reply (direct)
* Conversation follow-up using headers (ConversationId and MessageId)
* RabbitMQ (with dead-letter support)
* Generic Host support with dependency injection
* System.Text.Json serialization
* Full testing capabilities using In-Memory mode
* Persistent queue/buffering across activation

Package | Status | Description
--------|--------|------------
FBus | [![Nuget](https://img.shields.io/nuget/v/FBus?logo=nuget)](https://nuget.org/packages/FBus) | Core package
FBus.RabbitMQ | [![Nuget](https://img.shields.io/nuget/v/FBus.RabbitMQ?logo=nuget)](https://nuget.org/packages/FBus.RabbitMQ) | RabbitMQ transport
FBus.Json | [![Nuget](https://img.shields.io/nuget/v/FBus.Json?logo=nuget)](https://nuget.org/packages/FBus.Json) | System.Text.Json serializer
FBus.GenericHost | [![Nuget](https://img.shields.io/nuget/v/FBus.GenericHost?logo=nuget)](https://nuget.org/packages/FBus.GenericHost) | Generic Host support
FBus.QuickStart | [![Nuget](https://img.shields.io/nuget/v/FBus.QuickStart?logo=nuget)](https://nuget.org/packages/FBus.QuickStart) | All FBus packages to quick start a project
