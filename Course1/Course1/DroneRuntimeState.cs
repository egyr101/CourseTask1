namespace DroneSimulator
{
    internal sealed class DroneRuntimeState
    {
        public Drone Drone { get; }
        public DroneFacing Direction { get; set; } = DroneFacing.Right;
        public int Charges { get; set; } = 100;

        public DroneRuntimeState(Drone drone)
        {
            Drone = drone;
        }
    }
}
