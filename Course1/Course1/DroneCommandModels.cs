using System;

namespace DroneSimulator
{
    public enum DroneTarget
    {
        Red,
        Green,
        All
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
