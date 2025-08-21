namespace FBus.Transports
open FBus
open RabbitMQ.Client
open RabbitMQ.Client.Events

//
// Binding model is as follow:
//
// ----------------------------------------------------------------------------------------------------------
//                   Exchanges                     |                         Queues
// ----------------------------------------------------------------------------------------------------------
//
//              Exchange Binding               Queue Binding
//
// fbus:msg:MsgType <--+--- fbus:shard:Client1 <------ fbus:client:Client1 (* concurrent and/or ephemeral *)
//                     |
//                     +--- fbus:shard:Client2 <--+--- fbus:client:Client2-1 (* sharded and/or ephemeral *)
//                                                |
//                                                +--- fbus:client:Client2-2
//


[<AutoOpen>]
module Async =
    let inline await (t: System.Threading.Tasks.Task) = t |> Async.AwaitTask |> Async.RunSynchronously
    let inline awaitResult<'t> (t: System.Threading.Tasks.Task<'t>) = t |> Async.AwaitTask |> Async.RunSynchronously

type RabbitMQ(uri, busConfig: BusConfiguration, msgCallback) =

    let channelLock = new System.Threading.SemaphoreSlim(1, 1)
    let factory = ConnectionFactory(Uri = uri, AutomaticRecoveryEnabled = true)
    let conn = factory.CreateConnectionAsync() |> awaitResult
    let options = CreateChannelOptions(publisherConfirmationsEnabled = true,publisherConfirmationTrackingEnabled = true)
    let channel = conn.CreateChannelAsync(options) |> awaitResult
    let mutable sendChannel = conn.CreateChannelAsync() |> awaitResult

    let tryGetHeaderAsString (key: string) (props: IReadOnlyBasicProperties) =
            match props.Headers.TryGetValue key with
            | true, (:? (byte[]) as s) -> Some (System.Text.Encoding.UTF8.GetString(s))
            | _ -> None



    // ========================================================================================================
    // WARNING: IModel is not thread safe: https://www.rabbitmq.com/dotnet-api-guide.html#concurrency
    // ========================================================================================================
    let safeDo action =
        channelLock.WaitAsync() |> await
        try action()
        finally channelLock.Release() |> ignore

    let send headers (xchgName: string) (routingKey: string) body =
        safeDo (fun () ->
            if sendChannel.IsClosed then
                sendChannel <- conn.CreateChannelAsync() |> awaitResult

            let headers = headers |> Map.map (fun _ v -> v :> obj)
            let props = BasicProperties(Headers = headers, Persistent = true)
            sendChannel.BasicPublishAsync(exchange = xchgName,
                                          routingKey = routingKey,
                                          mandatory = false,
                                          basicProperties = props,
                                          body = body).AsTask() |> await
        )

    let ack (ea: BasicDeliverEventArgs) =
        safeDo (fun () -> channel.BasicAckAsync(deliveryTag = ea.DeliveryTag, multiple = false).AsTask() |> await)

    let nack (ea: BasicDeliverEventArgs) =
        safeDo (fun () -> channel.BasicNackAsync(deliveryTag = ea.DeliveryTag, multiple = false, requeue = false).AsTask() |> await)

    // ========================================================================================================


    let getExchangeMsg (msgType: string) = $"fbus:msg:{msgType}"

    let getExchangeShard (clientName: string) = $"fbus:shard:{clientName}"

    let getQueueClient (clientName: string) (shardName: string option) = 
        match shardName with
        | Some shardName -> $"fbus:client:{clientName}#{shardName}"
        | _ -> $"fbus:client:{clientName}"

    let configureAck () =
        channel.BasicQosAsync(prefetchSize = 0ul, prefetchCount = 10us, ``global`` = false)
        |> await

    let queueName = getQueueClient busConfig.Name busConfig.ShardName

    let configureDeadLettersQueues() =
       // ===============================================================================================
        // dead letter queues are bound to a single exchange (direct) - the routingKey is the target queue
        // ===============================================================================================
        let xchgDeadLetter = "fbus:dead-letter"
        let deadLetterQueueName = queueName + ":dead-letter"
        if busConfig.IsEphemeral then Map.empty
        else
            channel.ExchangeDeclareAsync(exchange = xchgDeadLetter,
                                    ``type`` = ExchangeType.Direct,
                                    durable = true, autoDelete = false) |> await
            channel.QueueDeclareAsync(queue = deadLetterQueueName,
                                      durable = true, exclusive = false, autoDelete = false)  |> await
            channel.QueueBindAsync(queue = deadLetterQueueName, exchange = xchgDeadLetter, routingKey = deadLetterQueueName) |> await

            Map [ "x-dead-letter-exchange", xchgDeadLetter :> obj
                  "x-dead-letter-routing-key", deadLetterQueueName :> obj ]

    let configureQueues queueArgs = 
        // =========================================================================================
        // message queues are bound to an exchange (fanout) - all bound subscribers receive messages
        // =========================================================================================
        channel.QueueDeclareAsync(queueName,
                                  durable = true, exclusive = false, autoDelete = busConfig.IsEphemeral,
                                  arguments = queueArgs) |> await

    let bindExchangeAndQueue msgType =
        let xchgMsg = getExchangeMsg msgType
        channel.ExchangeDeclareAsync(exchange = xchgMsg, ``type`` = ExchangeType.Fanout, 
                                     durable = true, autoDelete = false) |> await

        // NOTE: if exchange creation crashes then check "rabbitmq_consistent_hash_exchange" is correctly installed
        let xchgShard = getExchangeShard busConfig.Name
        channel.ExchangeDeclareAsync(exchange = xchgShard, ``type`` = "x-consistent-hash", 
                                     durable = true, autoDelete = busConfig.IsEphemeral) |> await

        channel.ExchangeBindAsync(xchgShard, xchgMsg, routingKey = "") |> await

        channel.QueueBindAsync(queue = queueName, exchange = xchgShard, routingKey = "1") |> await

    let subscribeMessages () =
        busConfig.Handlers |> Map.iter (fun msgType _ -> msgType |> bindExchangeAndQueue)

    let listenMessages () =
        let consumer = AsyncEventingBasicConsumer(channel)
        let consumerCallback msgCallback (ea: BasicDeliverEventArgs) =
            try
                let headers = ea.BasicProperties.Headers |> Seq.choose (fun kvp -> ea.BasicProperties
                                                                                   |> tryGetHeaderAsString kvp.Key
                                                                                   |> Option.map (fun s -> kvp.Key, s))
                                                         |> Map

                msgCallback headers ea.Body
                ack ea
            with
                _ -> nack ea
        let handler = AsyncEventHandler<BasicDeliverEventArgs>(fun _ ea -> task { consumerCallback msgCallback ea |> ignore })
        consumer.add_ReceivedAsync handler
        channel.BasicConsumeAsync(queue = queueName, autoAck = false, consumer = consumer) |> await

    do
        configureAck()
        configureDeadLettersQueues() |> configureQueues
        subscribeMessages ()
        listenMessages()

    interface IBusTransport with
        member _.Publish headers msgType body key =
            let xchgName = getExchangeMsg msgType
            send headers xchgName key body

        member _.Send headers client msgType body key =
            let xchgName = getExchangeShard client
            send headers xchgName key body

        member _.Dispose() =
            sendChannel.Dispose()
            channel.Dispose()
            conn.Dispose()
