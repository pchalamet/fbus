open System

let usage () =
    failwithf "usage: fbus-sender <target> <msgType> <body>"


// usage: fbus-send <
[<EntryPoint>]
let main argv =
    let target, msgType, file = match argv with
                                | [| target; msgType; file |] -> target, msgType, file
                                | _ -> usage()
    let msgBody = System.IO.File.ReadAllText(file)

    let builder = FBus.Builder.init()
    let callback msgHeaders msgType msgBody = ()
    use transport = FBus.RabbitMQ.Create builder callback

    let headers = Map [ "fbus:sender", "fbus-sender"
                        "fbus:message-id", Guid.NewGuid().ToString()
                        "fbus:conversation-id", Guid.NewGuid().ToString() ]
    let body = System.Text.Encoding.UTF8.GetBytes(msgBody) |> System.ReadOnlyMemory
    transport.Send headers target msgType body

    0 // return an integer exit code
