using System;
using System.Text.Json.Nodes;

namespace SolaxMQTTBridge
{
    public readonly record struct Sensor(
        string Name,
        string Identifier,
        string DeviceClass,
        string StateClass,
        string UnitOfMeasurement,
        Func<JsonNode, string> ValueRetriever,
        bool alwaysAvailable = false,
        string defaultValue = null
    );
}
