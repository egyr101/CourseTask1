using DroneSimulator;
using Microsoft.Xna.Framework;

namespace UnitTests
{
    [TestClass]
    public sealed class WeedTests
    {
        [TestMethod]
        public void Constructor_CreatesAliveWeedByDefault()
        {
            var weed = new Weed(new Vector2(2, 3));

            Assert.AreEqual(new Vector2(2, 3), weed.GridPosition);
            Assert.IsFalse(weed.IsDestroyed);
        }

        [TestMethod]
        public void Constructor_CanCreateDestroyedWeed()
        {
            var weed = new Weed(new Vector2(2, 3), isDestroyed: true);

            Assert.IsTrue(weed.IsDestroyed);
        }

        [TestMethod]
        public void Destroy_MarksWeedAsDestroyed()
        {
            var weed = new Weed(new Vector2(1, 1));

            weed.Destroy();

            Assert.IsTrue(weed.IsDestroyed);
        }
    }
}
