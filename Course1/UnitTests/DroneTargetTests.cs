using DroneSimulator;

namespace UnitTests
{
    [TestClass]
    public sealed class DroneTargetTests
    {
        [TestMethod]
        public void All_ReturnsTargetForAllDrones()
        {
            DroneTarget target = DroneTarget.All;

            Assert.IsTrue(target.IsAll);
            Assert.AreEqual(-1, target.DroneIndex);
        }

        [TestMethod]
        public void ForDrone_WithValidIndex_ReturnsSingleDroneTarget()
        {
            DroneTarget target = DroneTarget.ForDrone(3);

            Assert.IsFalse(target.IsAll);
            Assert.AreEqual(3, target.DroneIndex);
        }

        [TestMethod]
        public void ForDrone_WithNegativeIndex_ThrowsException()
        {
            Assert.ThrowsException<ArgumentOutOfRangeException>(() => DroneTarget.ForDrone(-1));
        }
    }
}
