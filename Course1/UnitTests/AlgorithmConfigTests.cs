using DroneSimulator;

namespace UnitTests
{
    [TestClass]
    public sealed class AlgorithmConfigTests
    {
        [TestMethod]
        public void Constructor_CreatesEmptyTicksCollection()
        {
            var config = new AlgorithmConfig();

            Assert.AreEqual(0, config.NumberDronesOnMap);
            Assert.IsNotNull(config.Ticks);
            Assert.AreEqual(0, config.Ticks.Count);
        }
    }
}
