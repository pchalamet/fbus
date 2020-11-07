module FBus.Testing
open FBus.InMemory

let setup = useTransport << useSerializer << useContainer

let waitForCompletion = FBus.Transports.InMemory.WaitForCompletion
