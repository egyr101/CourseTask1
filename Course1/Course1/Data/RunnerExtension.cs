
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Course1.Data
{
    // КЛАСС-РАСШИРЕНИЕ
    public static class RunnerExtension
    {
        static public Dictionary<string, Func<Runner, DroneLogic, Task>> commands = new()
        {
            {"step" , Step},
            {"roto", RotateOn },
            {"rota", RotateAgainst },
            {"atk", Attack }
        };
        public static async Task Run(this Runner runner, string action, DroneLogic drone)
        {
            await commands[action](runner, drone);
        }

        public static async Task RotateOn(this Runner runner, DroneLogic drone)
        {
            await drone.RotateOn(); 
        }

        public static async Task RotateAgainst(this Runner runner, DroneLogic drone)
        {
            await drone.RotateAgainst();   
        }

        public static async Task Step(this Runner runner, DroneLogic drone)
        {
            await runner.Map.StepDrone(drone);
            await drone.Step();
            await runner.Map.StepDrone(drone);
        }

        public static async Task Attack(this Runner runner, DroneLogic drone)
        {
            await runner.Map.StepDrone(drone);
            await drone.Step();
            await runner.Map.StepDrone(drone);
            await runner.Map.DestroyWeed(drone);
            await drone.Attack();   
        }
    }
}
