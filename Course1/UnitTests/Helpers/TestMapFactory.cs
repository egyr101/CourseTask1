using DroneSimulator;
using Microsoft.Xna.Framework;
using System.Reflection;

namespace UnitTests.Helpers
{
    internal static class TestMapFactory
    {
        public static MapRenderer CreateMap(
            IEnumerable<Vector2> dronePositions,
            IEnumerable<Vector2>? weedPositions = null,
            int gridWidth = 20,
            int gridHeight = 15)
        {
            var map = (MapRenderer)System.Runtime.Serialization.FormatterServices
                .GetUninitializedObject(typeof(MapRenderer));

            map.GridWidth = gridWidth;
            map.GridHeight = gridHeight;
            map.CellSize = 40;
            map.Drones = dronePositions
                .Select((position, index) => new Drone(position, Color.White)
                {
                    Name = $"Дрон {index + 1}",
                    Number = index + 1
                })
                .ToList();

            var weedField = new WeedField();
            foreach (var weedPosition in weedPositions ?? Enumerable.Empty<Vector2>())
            {
                weedField.Add(weedPosition);
            }

            typeof(MapRenderer)
                .GetField("<WeedField>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic)!
                .SetValue(map, weedField);

            return map;
        }

        public static void FinishDroneAnimations(MapRenderer map)
        {
            var gameTime = new GameTime(TimeSpan.Zero, TimeSpan.FromSeconds(10));

            foreach (var drone in map.Drones)
            {
                drone.Update(gameTime);
            }
        }

        public static CommandRow Row(
            int tick,
            string target1,
            string action1,
            string argument1 = "",
            string target2 = "",
            string action2 = "",
            string argument2 = "")
        {
            return new CommandRow
            {
                TickNumber = tick,
                Target1 = target1,
                Action1 = action1,
                Argument1 = argument1,
                Target2 = target2,
                Action2 = action2,
                Argument2 = argument2
            };
        }
    }
}
