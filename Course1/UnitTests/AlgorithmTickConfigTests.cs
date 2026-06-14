using DroneSimulator;

namespace UnitTests
{
    [TestClass]
    public sealed class AlgorithmTickConfigTests
    {
        [TestMethod]
        public void Constructor_CreatesEmptyCommandsCollection()
        {
            var tick = new AlgorithmTickConfig();

            Assert.IsNotNull(tick.Commands);
            Assert.AreEqual(0, tick.Commands.Count);
        }
    }
}
