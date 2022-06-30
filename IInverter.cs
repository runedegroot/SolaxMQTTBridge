namespace SolaxMQTTBridge
{
    public interface IInverter
    {
        public abstract string Model { get; }
        public abstract Sensor[] Sensors { get; }
    }
}
