using DroneSimulator;

namespace UnitTests
{
    [TestClass]
    public sealed class LevelConfigTests
    {
        [TestMethod]
        public void Constructor_CreatesEmptyCollections()
        {
            var config = new LevelConfig();

            Assert.IsNotNull(config.Drones);
            Assert.IsNotNull(config.Weeds);
            Assert.AreEqual(0, config.Drones.Count);
            Assert.AreEqual(0, config.Weeds.Count);
        }
    }
}
