using DroneSimulator;
using Microsoft.Xna.Framework;

namespace UnitTests
{
    [TestClass]
    public sealed class WeedSnapshotTests
    {
        [TestMethod]
        public void Constructor_SavesPositionAndDestroyedState()
        {
            var snapshot = new WeedSnapshot(new Vector2(4, 5), isDestroyed: true);

            Assert.AreEqual(new Vector2(4, 5), snapshot.GridPosition);
            Assert.IsTrue(snapshot.IsDestroyed);
        }
    }
}
