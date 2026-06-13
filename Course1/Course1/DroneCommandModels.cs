using System;

namespace DroneSimulator
{
    public sealed class DroneTarget
    {
        public bool IsAll { get; }
        public int DroneIndex { get; }

        private DroneTarget(bool isAll, int droneIndex)
        {
            IsAll = isAll;
            DroneIndex = droneIndex;
        }

        public static DroneTarget All { get; } = new DroneTarget(true, -1);

        public static DroneTarget ForDrone(int droneIndex)
        {
            if (droneIndex < 0)
                throw new ArgumentOutOfRangeException(nameof(droneIndex));

            return new DroneTarget(false, droneIndex);
        }
    }

    public enum DroneFacing
    {
        Top,
        Right,
        Bottom,
        Left
    }

    public enum DroneCommandType
    {
        MoveForward,
        TurnLeft,
        TurnRight,
        Attack
    }

    public sealed class DroneCommand
    {
        public DroneTarget Target { get; }
        public DroneCommandType Type { get; }
        public int Repeat { get; }

        public DroneCommand(DroneTarget target, DroneCommandType type, int repeat)
        {
            Target = target;
            Type = type;
            Repeat = Math.Max(1, repeat);
        }
    }
}
