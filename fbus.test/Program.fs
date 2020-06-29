module Program

let [<EntryPoint>] main _ =
    FBus.InMemory.Test.``check inmemory message exchange``()
    // FBus.Test.BusControl.``Test bus control``()
    0
