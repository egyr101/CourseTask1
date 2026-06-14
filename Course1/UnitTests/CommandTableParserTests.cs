using DroneSimulator;
using UnitTests.Helpers;

namespace UnitTests
{
    [TestClass]
    public sealed class CommandTableParserTests
    {
        [TestMethod]
        public void Parse_WithRussianCommands_ReturnsExpectedCommands()
        {
            var rows = new[]
            {
                TestMapFactory.Row(1, "Дрон 1", "Вперёд", "3", "Дрон 2", "Разряд")
            };

            var ticks = CommandTableParser.Parse(rows).ToList();

            Assert.AreEqual(1, ticks.Count);
            Assert.AreEqual(2, ticks[0].Count);
            Assert.AreEqual(0, ticks[0][0].Target.DroneIndex);
            Assert.AreEqual(DroneCommandType.MoveForward, ticks[0][0].Type);
            Assert.AreEqual(3, ticks[0][0].Repeat);
            Assert.AreEqual(1, ticks[0][1].Target.DroneIndex);
            Assert.AreEqual(DroneCommandType.Attack, ticks[0][1].Type);
            Assert.AreEqual(1, ticks[0][1].Repeat);
        }

        [TestMethod]
        public void Parse_WithEnglishCommands_ReturnsExpectedCommands()
        {
            var rows = new[]
            {
                TestMapFactory.Row(1, "All", "Forward", "2"),
                TestMapFactory.Row(2, "Drone 3", "Left", "", "Drone 4", "Right", "0")
            };

            var ticks = CommandTableParser.Parse(rows).ToList();

            Assert.AreEqual(2, ticks.Count);
            Assert.IsTrue(ticks[0][0].Target.IsAll);
            Assert.AreEqual(DroneCommandType.MoveForward, ticks[0][0].Type);
            Assert.AreEqual(2, ticks[0][0].Repeat);
            Assert.AreEqual(2, ticks[1][0].Target.DroneIndex);
            Assert.AreEqual(DroneCommandType.TurnLeft, ticks[1][0].Type);
            Assert.AreEqual(3, ticks[1][1].Target.DroneIndex);
            Assert.AreEqual(DroneCommandType.TurnRight, ticks[1][1].Type);
            Assert.AreEqual(1, ticks[1][1].Repeat);
        }

        [TestMethod]
        public void Parse_WithLegacyRedAndGreenAliases_ReturnsDroneOneAndTwo()
        {
            var rows = new[]
            {
                TestMapFactory.Row(1, "Красный", "Вперёд", "", "Green", "Attack")
            };

            var commands = CommandTableParser.Parse(rows).Single();

            Assert.AreEqual(0, commands[0].Target.DroneIndex);
            Assert.AreEqual(1, commands[1].Target.DroneIndex);
        }

        [TestMethod]
        public void Parse_WithRowsOfSameTick_GroupsCommandsIntoSingleTick()
        {
            var rows = new[]
            {
                TestMapFactory.Row(1, "Дрон 1", "Вперёд"),
                TestMapFactory.Row(1, "Дрон 2", "Налево"),
                TestMapFactory.Row(2, "Дрон 1", "Разряд")
            };

            var ticks = CommandTableParser.Parse(rows).ToList();

            Assert.AreEqual(2, ticks.Count);
            Assert.AreEqual(2, ticks[0].Count);
            Assert.AreEqual(1, ticks[1].Count);
        }

        [TestMethod]
        public void Parse_WithIncompleteCommands_IgnoresThem()
        {
            var rows = new[]
            {
                TestMapFactory.Row(1, "Дрон 1", "", "3", "", "Вперёд", "2"),
                TestMapFactory.Row(2, "Дрон 2", "Разряд")
            };

            var ticks = CommandTableParser.Parse(rows).ToList();

            Assert.AreEqual(1, ticks.Count);
            Assert.AreEqual(1, ticks[0].Count);
            Assert.AreEqual(1, ticks[0][0].Target.DroneIndex);
            Assert.AreEqual(DroneCommandType.Attack, ticks[0][0].Type);
        }

        [TestMethod]
        public void Parse_WithUnknownTarget_ThrowsException()
        {
            var rows = new[] { TestMapFactory.Row(1, "Robot 1", "Forward") };

            Assert.ThrowsException<InvalidOperationException>(() => CommandTableParser.Parse(rows).ToList());
        }

        [TestMethod]
        public void Parse_WithUnknownAction_ThrowsException()
        {
            var rows = new[] { TestMapFactory.Row(1, "Drone 1", "Jump") };

            Assert.ThrowsException<InvalidOperationException>(() => CommandTableParser.Parse(rows).ToList());
        }

        [TestMethod]
        public void Parse_WithDroneZero_ThrowsException()
        {
            var rows = new[] { TestMapFactory.Row(1, "Drone 0", "Forward") };

            Assert.ThrowsException<ArgumentOutOfRangeException>(() => CommandTableParser.Parse(rows).ToList());
        }
    }
}
