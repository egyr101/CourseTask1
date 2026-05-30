namespace DroneSimulator
{
    public sealed class DroneChargeInfo
    {
        public string DroneName { get; }
        public int CurrentCharges { get; }
        public int InitialCharges { get; }

        public DroneChargeInfo(string droneName, int currentCharges, int initialCharges)
        {
            DroneName = droneName;
            CurrentCharges = currentCharges;
            InitialCharges = initialCharges;
        }
    }
}
