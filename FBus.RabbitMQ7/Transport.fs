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

type RabbitMQ7(uri, busConfig: BusConfiguration, msgCallback) =

    let channelLock = new System.Threading.SemaphoreSlim(1, 1)
    let sendLock = new System.Threading.SemaphoreSlim(1, 1)
    let maxConcurrency = max 1 busConfig.Concurrency
    // Default prefetch: 10 when single-threaded; else max(10, concurrency)
    let prefetchCount: uint16 = if maxConcurrency <= 1 then 10us else uint16 (max 10 maxConcurrency)
    let processingSemaphore = new System.Threading.SemaphoreSlim(maxConcurrency, maxConcurrency)
    let factory = ConnectionFactory(Uri = uri, AutomaticRecoveryEnabled = true)
    let conn = factory.CreateConnectionAsync() |> awaitResult
    let options = CreateChannelOptions(publisherConfirmationsEnabled = true, publisherConfirmationTrackingEnabled = true)
    let channel = conn.CreateChannelAsync(options) |> awaitResult
    let mutable sendChannel = conn.CreateChannelAsync() |> awaitResult |> Some

    let tryGetHeaderAsString (key: string) (props: IReadOnlyBasicProperties) =
        props.Headers |> Option.ofObj |> Option.bind (fun headers ->
            match headers.TryGetValue key with
            | true, (:? (byte[]) as s) -> Some (System.Text.Encoding.UTF8.GetString(s))
            | _ -> None)

    // ========================================================================================================
    // WARNING: IModel is not thread safe: https://www.rabbitmq.com/dotnet-api-guide.html#concurrency
    // ========================================================================================================
    let safeAction action =
        channelLock.WaitAsync() |> await
        try action()
        finally channelLock.Release() |> ignore

    let send headers (xchgName: string) (routingKey: string) body =
        // Use a dedicated lock for sending to avoid blocking acks/nacks during recovery
        sendLock.WaitAsync() |> await
        try
            let headers = headers |> Map.map (fun _ v -> v :> obj)
            let rec trySend remaining (wait: int) =
                // Ensure connection is open before creating/using a channel
                if not conn.IsOpen then
                    if remaining = 0 then failwith "Connection not open"
                    System.Threading.Thread.Sleep(wait)
                    trySend (remaining-1) (min 5000 (wait*2))
                else
                    try
                        // (Re)create channel only when needed
                        if sendChannel.IsNone || not sendChannel.Value.IsOpen then
                            sendChannel |> Option.iter (fun ch ->
                                try ch.Dispose() with _ -> ()
                                sendChannel <- None)
                            sendChannel <- conn.CreateChannelAsync() |> awaitResult |> Some

                        let props = BasicProperties(Headers = headers, Persistent = true)
                        sendChannel.Value.BasicPublishAsync(exchange = xchgName,
                                                            routingKey = routingKey,
                                                            mandatory = false,
                                                            basicProperties = props,
                                                            body = body).AsTask() |> await
                    with _ ->
                        // reset the channel and retry with backoff
                        sendChannel |> Option.iter (fun ch ->
                            try ch.Dispose() with _ -> ()
                            sendChannel <- None)
                        if remaining = 0 then reraise()
                        System.Threading.Thread.Sleep(wait)
                        trySend (remaining-1) (min 5000 (wait*2))
            trySend 7 50
        finally
            sendLock.Release() |> ignore

    let ack (ea: BasicDeliverEventArgs) =
        safeAction (fun () -> channel.BasicAckAsync(deliveryTag = ea.DeliveryTag, multiple = false).AsTask() |> await)

    let nack (ea: BasicDeliverEventArgs) =
        safeAction (fun () -> channel.BasicNackAsync(deliveryTag = ea.DeliveryTag, multiple = false, requeue = false).AsTask() |> await)

    // ========================================================================================================


    let getExchangeMsg (msgType: string) = $"fbus:msg:{msgType}"

    let getExchangeShard (clientName: string) = $"fbus:shard:{clientName}"

    let getQueueClient (clientName: string) (shardName: string option) = 
        match shardName with
        | Some shardName -> $"fbus:client:{clientName}#{shardName}"
        | _ -> $"fbus:client:{clientName}"

    let configureAck () =
        channel.BasicQosAsync(prefetchSize = 0ul, prefetchCount = prefetchCount, ``global`` = false)
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
        let consumerCallback msgCallback (ea: BasicDeliverEventArgs) = task {
            do! processingSemaphore.WaitAsync()
            try
                try
                    let headers =
                        ea.BasicProperties.Headers |> Option.ofObj |> Option.map (fun headers ->
                            headers
                            |> Seq.choose (fun kvp ->
                                ea.BasicProperties
                                |> tryGetHeaderAsString kvp.Key
                                |> Option.map (fun s -> kvp.Key, s))
                            |> Map)
                        |> Option.defaultValue Map.empty

                    msgCallback headers ea.Body
                    ack ea
                with
                    _ -> nack ea
            finally
                processingSemaphore.Release() |> ignore
        }
        let handler = AsyncEventHandler<BasicDeliverEventArgs>(fun _ ea -> consumerCallback msgCallback ea)
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
            processingSemaphore.Dispose()
            sendChannel |> Option.iter (fun ch -> try ch.Dispose() with _ -> ())
            channel.Dispose()
            conn.Dispose()
