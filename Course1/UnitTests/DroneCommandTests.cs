using DroneSimulator;

namespace UnitTests
{
    [TestClass]
    public sealed class DroneCommandTests
    {
        [TestMethod]
        public void Constructor_SavesTargetTypeAndRepeat()
        {
            var target = DroneTarget.ForDrone(1);
            var command = new DroneCommand(target, DroneCommandType.MoveForward, 4);

            Assert.AreSame(target, command.Target);
            Assert.AreEqual(DroneCommandType.MoveForward, command.Type);
            Assert.AreEqual(4, command.Repeat);
        }

        [DataTestMethod]
        [DataRow(0)]
        [DataRow(-5)]
        public void Constructor_WithNonPositiveRepeat_UsesOne(int repeat)
        {
            var command = new DroneCommand(DroneTarget.All, DroneCommandType.Attack, repeat);

            Assert.AreEqual(1, command.Repeat);
        }
    }
}
