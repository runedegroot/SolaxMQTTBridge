using System;
using System.Text.Json.Nodes;

namespace SolaxMQTTBridge
{
    public interface IInverter
    {
        string Model { get; }
        Sensor[] Sensors { get; }
        Func<JsonNode, string> GetStatus { get; }
        Func<JsonNode, bool> IsActive { get; }
    }
}
