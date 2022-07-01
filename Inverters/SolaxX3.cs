using System;
using System.Text.Json.Nodes;

namespace SolaxMQTTBridge.Inverters
{
    public class SolaxX3 : IInverter
    {
        public string Model => "X3";

        public Sensor[] Sensors => new Sensor[]
        {
            new("PV 1 Current", "pv1_current", "current", "measurement", "A", json => json["Data"][0].ToString()),
            new("PV 2 Current", "pv2_current", "current", "measurement", "A", json => json["Data"][1].ToString()),

            new("PV 1 Voltage", "pv1_voltage", "voltage", "measurement", "V", json => json["Data"][2].ToString()),
            new("PV 2 Voltage", "pv2_voltage", "voltage", "measurement", "V", json => json["Data"][3].ToString()),

            new("PV 1 Power", "pv1_power", "power", "measurement", "W", json => json["Data"][11].ToString()),
            new("PV 2 Power", "pv2_power", "power", "measurement", "W", json => json["Data"][12].ToString()),

            new("Phase 1 Current", "phase1_current", "current", "measurement", "A", json => json["Data"][4].ToString()),
            new("Phase 2 Current", "phase2_current", "current", "measurement", "A", json => json["Data"][46].ToString()),
            new("Phase 3 Current", "phase3_current", "current", "measurement", "A", json => json["Data"][47].ToString()),

            new("Phase 1 Voltage", "phase1_voltage", "voltage", "measurement", "V", json => json["Data"][5].ToString()),
            new("Phase 2 Voltage", "phase2_voltage", "voltage", "measurement", "V", json => json["Data"][48].ToString()),
            new("Phase 3 Voltage", "phase3_voltage", "voltage", "measurement", "V", json => json["Data"][49].ToString()),

            new("Phase 1 Power", "phase1_power", "power", "measurement", "W", json => json["Data"][43].ToString()),
            new("Phase 2 Power", "phase2_power", "power", "measurement", "W", json => json["Data"][44].ToString()),
            new("Phase 3 Power", "phase3_power", "power", "measurement", "W", json => json["Data"][45].ToString()),

            new("Phase 1 Frequency", "phase1_frequency", "frequency", "measurement", "Hz", json => json["Data"][50].ToString()),
            new("Phase 2 Frequency", "phase2_frequency", "frequency", "measurement", "Hz", json => json["Data"][51].ToString()),
            new("Phase 3 Frequency", "phase3_frequency", "frequency", "measurement", "Hz", json => json["Data"][52].ToString()),

            new("Grid Power",  "grid_power",  "power",       "measurement",      "W",   json => json["Data"][6].ToString()),
            new("Temperature", "temperature", "temperature", "measurement",      "°C",  json => json["Data"][7].ToString()),
            new("Yield Today", "yield_today", "energy",      "total_increasing", "kWh", json => json["Data"][8].ToString()),
            new("Yield Total", "yield_total", "energy",      "total",            "kWh", json => json["Data"][9].ToString())
        };

        public Func<JsonNode, bool> IsActive => json => json["Data"][68].ToString().Equals("2");

        public Func<JsonNode, string> GetStatus => json => json["Data"][68].ToString() switch
        {
            "0" => "Waiting",
            "1" => "Grid sync",
            "2" => "Normal",
            "3" => "Lost grid",
            _ => "Unknown"
        };
    }
}
