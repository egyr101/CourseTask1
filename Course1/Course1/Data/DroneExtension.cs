
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Course1.Data
{
    // КЛАСС-РАСШИРЕНИЕ
    public static class DroneExtension
    {
        public static async Task RotateOn(this DroneLogic drone)
        {
            drone.Dir = (Direction)(((int)drone.Dir + 1) % 4);
            await Task.Delay(1);
        }

        public static async Task RotateAgainst(this DroneLogic drone)
        {
            drone.Dir = (Direction)(((int)drone.Dir + 3) % 4);
            await Task.Delay(1);
        }

        public static async Task Step(this DroneLogic drone)
        {
            switch (drone.Dir)
            {
                case Direction.Top:
                    drone.Y++;
                    break;
                case Direction.Right:
                    drone.X++;
                    break;
                case Direction.Bottom:
                    drone.Y--;
                    break;
                case Direction.Left:
                    drone.X--;
                    break;
            }
            await Task.Delay(1);
        }

        public static async Task Attack(this DroneLogic drone)
        {
            if (drone.QuantityCharges is 0) throw new Exception("У дрона не осталось зарядов!");
            drone.QuantityCharges--;
            await Task.Delay(1);
        }
    }
}
