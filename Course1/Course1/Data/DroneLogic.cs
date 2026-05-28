using System;
using System.Collections.Generic;
namespace Course1.Data
{
    public enum Direction
    {
        Top,
        Right,
        Bottom,
        Left
    }
     
    public class DroneLogic
    {
        private int _x;
        private int _y;

        public Direction Dir{ get; set; }

        // Заполняется в Level
        public int QuantityCharges { get; set; }

        // Заполняется в Level
        public Queue<string> QueueCommands { get; private set; }
        public int X
        {
            get => _x;
            set
            {
                if (value < 0 || value > 9)
                {
                    throw new Exception("Выход за поле!");
                }
                _x = value;
            }
        }
        public int Y
        {
            get => _y;
            set
            {
                if (value < 0 || value > 19)
                {
                    throw new Exception("Выход за поле!");
                }
                _y = value;
            }
        }

        // Базовый конструктор
        public DroneLogic(int x, int y)
        {
            X = x;
            Y = y;
            Dir = (Direction)1;
            QueueCommands = [];
        }
        // Добавить команду
        public void AddCommand(string command)
        {
            QueueCommands.Enqueue(command);
        }
    }
}
