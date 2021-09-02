using FBus;
using Microsoft.Extensions.Hosting;
using Microsoft.FSharp.Core;

namespace client_cs
{
    public class HelloWorldConsumer : FBus.IBusConsumer<Common.HelloWorld>
    {
        public void Handle(IBusConversation ctx, Common.HelloWorld msg)
        {
            System.Console.WriteLine($"Received HelloWorld message {msg} from {ctx.Sender}");
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            var serverName = args.Length == 1 ? args[0] : "sample-server";
            Host.CreateDefaultBuilder(args)
                .ConfigureServices(services => FBus.GenericHost.ServiceCollectionExtensions.AddFBus(services, (FSharpFunc<BusBuilder, BusBuilder>)configureBus))
                .UseConsoleLifetime()
                .Build()
                .Run();

            FBus.BusBuilder configureBus(FBus.BusBuilder busBuilder)
            {
                busBuilder = FBus.Builder.withName(serverName, busBuilder);
                busBuilder = FBus.Json.useDefaults.Invoke(busBuilder);
                busBuilder = FBus.RabbitMQ.useDefaults.Invoke(busBuilder);
                busBuilder = FBus.Builder.withConsumer<HelloWorldConsumer>(busBuilder);
                return busBuilder;
            }
        }
    }
}
