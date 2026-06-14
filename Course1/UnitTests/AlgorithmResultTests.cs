using DroneSimulator;

namespace UnitTests
{
    [TestClass]
    public sealed class AlgorithmResultTests
    {
        [TestMethod]
        public void Constructor_SavesAllValues()
        {
            var result = new AlgorithmResult(score: 7, destroyedWeeds: 3, initialWeeds: 5);

            Assert.AreEqual(7, result.Score);
            Assert.AreEqual(3, result.DestroyedWeeds);
            Assert.AreEqual(5, result.InitialWeeds);
        }
    }
}
