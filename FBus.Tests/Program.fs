module Program

let [<EntryPoint>] main _ =
    FBus.Transports.Tests.``check inmemory message exchange``()
    0
