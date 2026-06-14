using DroneSimulator;

namespace UnitTests
{
    [TestClass]
    public sealed class DroneChargeInfoTests
    {
        [TestMethod]
        public void Constructor_SavesAllValues()
        {
            var info = new DroneChargeInfo("Дрон 1", currentCharges: 2, initialCharges: 3);

            Assert.AreEqual("Дрон 1", info.DroneName);
            Assert.AreEqual(2, info.CurrentCharges);
            Assert.AreEqual(3, info.InitialCharges);
        }
    }
}
