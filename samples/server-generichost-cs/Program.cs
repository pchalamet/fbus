using Microsoft.Extensions.Hosting;
using Microsoft.FSharp.Core;
using FBus;
using FBus.GenericHost;
using FBus.Extensions;

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
                .ConfigureServices(services => services.AddFBus(configureBus))
                .UseConsoleLifetime()
                .Build()
                .Run();

            FBus.BusBuilder configureBus(FBus.BusBuilder busBuilder)
            {
                return busBuilder.WithName(serverName)
                                 .WithConsumer<HelloWorldConsumer>()
                                 .UseJson()
                                 .UseRabbitMQ();
            }
        }
    }
}
