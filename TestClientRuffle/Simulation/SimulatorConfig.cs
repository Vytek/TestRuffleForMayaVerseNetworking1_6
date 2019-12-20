namespace Simulation
{
    internal class SimulatorConfig
    {
        public SimulatorConfig()
        {
        }

        public float DropPercentage { get; set; }
        public int MaxLatency { get; set; }
        public int MinLatency { get; set; }
    }
}