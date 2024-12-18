using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using MQTTnet;
using MQTTnet.Extensions.ManagedClient;
using MQTTnet.Server;
using SolaxMQTTBridge.Inverters;
using System;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace SolaxMQTTBridge
{
    public class MQTTService : IHostedService
    {
        private readonly IManagedMqttClient _client;
        private readonly MqttServer _server;
        private readonly ManagedMqttClientOptions _clientOptions;
        private readonly string _topic;
        private readonly string _discoveryPrefix;

        private static readonly IInverter Inverter = new SolaxX3();
        private static readonly JsonSerializerOptions _serializerOptions = new JsonSerializerOptions
        {
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        public MQTTService(IManagedMqttClient client, MqttServer server, ManagedMqttClientOptions clientOptions, IConfiguration configuration)
        {
            _client = client;
            _server = server;
            _clientOptions = clientOptions;
            _topic = configuration.GetValue<string>("MQTT_TOPIC").Trim('/');
            _discoveryPrefix = configuration.GetValue<string>("MQTT_DISCOVERY_PREFIX").Trim('/');
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            // Start client
            await _client.StartAsync(_clientOptions);

            // Start server
            _server.InterceptingPublishAsync += MessageInterceptor;
            _server.InterceptingSubscriptionAsync += SubscriptionInterceptor;
            await _server.StartAsync();
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            // Wait until the queue is fully processed.
            SpinWait.SpinUntil(() => _client.PendingApplicationMessagesCount == 0, 10000);

            Console.WriteLine($"Pending messages = {_client.PendingApplicationMessagesCount}");

            await _server.StopAsync();
            await _client.StopAsync();
        }

        private async Task MessageInterceptor(InterceptingPublishEventArgs e)
        {
            // Handle sync time protocol
            // Whenever a 'reqsynctime' message is receiveed for an installation, we publish the current time to the 'respsynctime' topic
            if (MqttTopicFilterComparer.Compare(e.ApplicationMessage.Topic, "reqsynctime/#") == MqttTopicFilterCompareResult.IsMatch)
            {
                // Retrieve the SN
                var match = Regex.Match(e.ApplicationMessage.Topic, @"^reqsynctime\/(.*)$");
                var sn = match.Groups[1].Value;

                Console.WriteLine($"Sync time request received'");

                // Create the payload
                var now = DateTime.Now;
                var payload = $$""" {"month": "{{now.Month}}", "hour": "{{now.Hour}}", "year": "{{now.Year}}", "day": "{{now.Day}}", "minute": "{{now.Minute}}", "second": "{{now.Second}}"}""";
                var message = new MqttApplicationMessageBuilder().WithTopic($"respsynctime/{sn}").WithPayload(payload).Build();

                // Inject the response message
                await _server.InjectApplicationMessage(
                    new InjectedMqttApplicationMessage(message)
                    {
                        SenderClientId = "SolaxMQTTBridge"
                    });
            }

            // Catch whenever synctime protocol is succeeded
            if (MqttTopicFilterComparer.Compare(e.ApplicationMessage.Topic, "Synctime/#") == MqttTopicFilterCompareResult.IsMatch)
            {
                Console.WriteLine($"Sync time response received with payload '{e.ApplicationMessage.ConvertPayloadToString()}'");
            }

            // Forward output data to our main mqtt
            if (MqttTopicFilterComparer.Compare(e.ApplicationMessage.Topic, "loc/#") == MqttTopicFilterCompareResult.IsMatch)
            {
                // Retrieve the payload
                var payload = e.ApplicationMessage.ConvertPayloadToString();

                Console.WriteLine($"Location data received with payload '{payload}'");

                // Send data to our mqtt
                var payloadJson = JsonNode.Parse(payload);

                var prefix = $"{_topic}/sensor";

                // Push sensor status
                await _client.EnqueueAsync($"{prefix}/status", Inverter.GetStatus(payloadJson));

                // Push data sensors
                if (Inverter.IsActive(payloadJson))
                {
                    var sensors = Inverter.Sensors;
                    foreach (var sensor in sensors)
                    {
                        await _client.EnqueueAsync($"{prefix}/{sensor.Identifier}", sensor.ValueRetriever(payloadJson), retain: true);
                    }
                }
                
                // If the inverter is not active, push default values
                else
                {
                    var defaultSensors = Inverter.Sensors.Where(s => s.defaultValue != null);
                    foreach (var sensor in defaultSensors)
                    {
                        await _client.EnqueueAsync($"{prefix}/{sensor.Identifier}", sensor.defaultValue, retain: true);
                    }
                }
            }

            // Autodiscovery when inverter is up
            if (MqttTopicFilterComparer.Compare(e.ApplicationMessage.Topic, "base/up/#") == MqttTopicFilterCompareResult.IsMatch)
            {
                Console.WriteLine($"Up message received with payload '{e.ApplicationMessage.ConvertPayloadToString()}'");

                // Add status sensor
                var statusPayload = new
                {
                    name = "Status",
                    unique_id = $"{_topic}_status",
                    object_id = $"{_topic}_status",
                    state_topic = $"{_topic}/sensor/status",
                    device = new
                    {
                        name = _topic,
                        identifiers = _topic,
                        manufacturer = "Solax",
                        model = Inverter.Model
                    }
                };
                var statusPayloadJson = JsonSerializer.Serialize(statusPayload, _serializerOptions);
                await _client.EnqueueAsync($"{_discoveryPrefix}/sensor/{_topic}/status/config", statusPayloadJson, retain: true);

                // Add data sensors
                var sensors = Inverter.Sensors;
                foreach (var sensor in sensors)
                {
                    var payload = new
                    {
                        name = sensor.Name,
                        unique_id = $"{_topic}_{sensor.Identifier}",
                        object_id = $"{_topic}_{sensor.Identifier}",
                        device_class = sensor.DeviceClass,
                        state_class = sensor.StateClass,
                        availability = sensor.alwaysAvailable
                            ? null
                            : new
                            {
                                topic = $"{_topic}/sensor/status",
                                value_template = """{{ "online" if value == "Normal" else "offline" }}"""
                            },
                        unit_of_measurement = sensor.UnitOfMeasurement,
                        state_topic = $"{_topic}/sensor/{sensor.Identifier}",
                        device = new
                        {
                            name = _topic,
                            identifiers = _topic,
                            manufacturer = "Solax",
                            model = Inverter.Model
                        }
                    };
                    var payloadJson = JsonSerializer.Serialize(payload, _serializerOptions);
                    await _client.EnqueueAsync($"{_discoveryPrefix}/sensor/{_topic}/{sensor.Identifier}/config", payloadJson, retain: true);
                }
            }
        }

        private Task SubscriptionInterceptor(InterceptingSubscriptionEventArgs arg)
        {
            Console.WriteLine($"Client with ID '{arg.ClientId}' subscribed to topic '{arg.TopicFilter.Topic}'");

            return Task.CompletedTask;
        }
    }
}
