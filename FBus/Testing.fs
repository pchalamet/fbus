module FBus.Testing
open FBus.InMemory

let configure = useContainer << useSerializer << useTransport << FBus.Builder.configure

let waitForCompletion = FBus.Transports.InMemory.WaitForCompletion
