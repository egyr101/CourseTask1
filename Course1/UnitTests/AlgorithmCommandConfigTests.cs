using DroneSimulator;

namespace UnitTests
{
    [TestClass]
    public sealed class AlgorithmCommandConfigTests
    {
        [TestMethod]
        public void Constructor_SetsDefaultValues()
        {
            var command = new AlgorithmCommandConfig();

            Assert.AreEqual(string.Empty, command.Target);
            Assert.IsNull(command.DroneNumber);
            Assert.AreEqual(string.Empty, command.Action);
            Assert.AreEqual(1, command.Repeat);
        }
    }
}
