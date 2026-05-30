namespace DroneSimulator
{
    internal sealed class DroneRuntimeState
    {
        public Drone Drone { get; }
        public DroneFacing Direction { get; set; } = DroneFacing.Right;
        public int Charges { get; set; }
        public int InitialCharges { get; set; }

        public DroneRuntimeState(Drone drone)
        {
            Drone = drone;
        }
    }
}
