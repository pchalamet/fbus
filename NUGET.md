# ✨ FBus
FBus is a lightweight service-bus implementation written in F#.

It comes with default implementation for:
* Publish (broadcast), Send (direct) and Reply (direct)
* Conversation follow-up using headers (ConversationId and MessageId)
* RabbitMQ (with dead-letter support)
* Generic Host support with dependency injection
* System.Text.Json serialization
* Full testing capabilities using In-Memory mode
* Persistent queue/buffering across activation

# 📦 NuGet packages

Package | Description
--------|------------
[FBus](https://nuget.org/packages/FBus) | Core package
[FBus.RabbitMQ](https://nuget.org/packages/FBus.RabbitMQ) | RabbitMQ transport
[FBus.Json](https://nuget.org/packages/FBus.Json) | System.Text.Json serializer
[FBus.GenericHost](https://nuget.org/packages/FBus.GenericHost) | Generic Host support
[FBus.QuickStart](https://nuget.org/packages/FBus.QuickStart) | All FBus packages to quick start a project
