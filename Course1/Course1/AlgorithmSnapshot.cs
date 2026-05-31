using Microsoft.Xna.Framework;
using System.Collections.Generic;

namespace DroneSimulator
{
    internal sealed class AlgorithmSnapshot
    {
        public List<DroneRuntimeSnapshot> Drones { get; } = new();
        public List<WeedSnapshot> Weeds { get; } = new();
    }

    internal readonly struct DroneRuntimeSnapshot
    {
        public Vector2 GridPosition { get; }
        public DroneFacing Direction { get; }
        public int Charges { get; }
        public int InitialCharges { get; }

        public DroneRuntimeSnapshot(
            Vector2 gridPosition,
            DroneFacing direction,
            int charges,
            int initialCharges)
        {
            GridPosition = gridPosition;
            Direction = direction;
            Charges = charges;
            InitialCharges = initialCharges;
        }
    }
}
