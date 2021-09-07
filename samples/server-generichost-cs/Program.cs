using Microsoft.Extensions.Hosting;
using Microsoft.FSharp.Core;
using FBus;
using FBus.GenericHost;

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
                .ConfigureServices(services => services.AddFBus((FSharpFunc<BusBuilder, BusBuilder>)configureBus))
                .UseConsoleLifetime()
                .Build()
                .Run();

            FBus.BusBuilder configureBus(FBus.BusBuilder busBuilder)
            {
                busBuilder = Builder.WithName(serverName, busBuilder);
                busBuilder = Json.UseDefaults(busBuilder);
                busBuilder = FBus.RabbitMQ.UseDefaults(busBuilder);
                busBuilder = Builder.WithConsumer<HelloWorldConsumer>(busBuilder);
                return busBuilder;
            }
        }
    }
}
