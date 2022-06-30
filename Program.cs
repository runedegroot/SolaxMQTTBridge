using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Extensions.ManagedClient;
using MQTTnet.Server;
using System.Threading.Tasks;

namespace SolaxMQTTBridge
{
    class Program
    {
        static async Task Main(string[] args)
        {
            await Host.CreateDefaultBuilder(args)
                .ConfigureServices((context, services) =>
                {
                    services.AddHostedService<MQTTService>();
                    services.AddSingleton(CreateServer());
                    services.AddSingleton(new MqttFactory().CreateManagedMqttClient());
                    services.AddSingleton(CreateClientOptions(context.Configuration));
                })
                .RunConsoleAsync();
        }

        private static MqttServer CreateServer()
        {
            var mqttFactory = new MqttFactory();
            var mqttServerOptions = new MqttServerOptionsBuilder()
                .WithDefaultEndpoint()
                .WithDefaultEndpointPort(2901)
                .Build();

            return mqttFactory.CreateMqttServer(mqttServerOptions);
        }

        private static ManagedMqttClientOptions CreateClientOptions(IConfiguration configuration)
        {
            var mqttClientOptions = new MqttClientOptionsBuilder()
                .WithTcpServer(configuration.GetValue<string>("MQTT_HOST"), configuration.GetValue<int>("MQTT_PORT"))
                .Build();

            return new ManagedMqttClientOptionsBuilder()
                .WithClientOptions(mqttClientOptions)
                .Build();
        }
    }
}
