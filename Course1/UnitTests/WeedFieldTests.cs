using DroneSimulator;
using Microsoft.Xna.Framework;

namespace UnitTests
{
    [TestClass]
    public sealed class WeedFieldTests
    {
        [TestMethod]
        public void NewField_HasNoWeeds()
        {
            var field = new WeedField();

            Assert.AreEqual(0, field.TotalCount);
            Assert.AreEqual(0, field.AliveCount);
            Assert.AreEqual(0, field.DestroyedCount);
            Assert.IsFalse(field.HasAliveWeeds);
        }

        [TestMethod]
        public void Add_AddsNewWeed()
        {
            var field = new WeedField();

            field.Add(new Vector2(3, 4));

            Assert.AreEqual(1, field.TotalCount);
            Assert.AreEqual(1, field.AliveCount);
            Assert.IsTrue(field.HasAliveWeeds);
            Assert.AreEqual(new Vector2(3, 4), field.Weeds[0].GridPosition);
        }

        [TestMethod]
        public void Add_WithDuplicatePosition_DoesNotAddSecondWeed()
        {
            var field = new WeedField();

            field.Add(new Vector2(3, 4));
            field.Add(new Vector2(3, 4));

            Assert.AreEqual(1, field.TotalCount);
        }

        [TestMethod]
        public void TryDestroyAt_WithAliveWeed_DestroysItAndReturnsTrue()
        {
            var field = new WeedField();
            field.Add(new Vector2(1, 2));

            bool result = field.TryDestroyAt(new Vector2(1, 2));

            Assert.IsTrue(result);
            Assert.AreEqual(0, field.AliveCount);
            Assert.AreEqual(1, field.DestroyedCount);
            Assert.IsFalse(field.HasAliveWeeds);
        }

        [TestMethod]
        public void TryDestroyAt_WithMissingOrAlreadyDestroyedWeed_ReturnsFalse()
        {
            var field = new WeedField();
            field.Add(new Vector2(1, 2));

            Assert.IsFalse(field.TryDestroyAt(new Vector2(5, 5)));
            Assert.IsTrue(field.TryDestroyAt(new Vector2(1, 2)));
            Assert.IsFalse(field.TryDestroyAt(new Vector2(1, 2)));
        }

        [TestMethod]
        public void Clear_RemovesAllWeeds()
        {
            var field = new WeedField();
            field.Add(new Vector2(1, 1));
            field.Add(new Vector2(2, 2));

            field.Clear();

            Assert.AreEqual(0, field.TotalCount);
            Assert.IsFalse(field.HasAliveWeeds);
        }

        [TestMethod]
        public void CreateSnapshotAndRestoreSnapshot_RestoresPositionsAndDestroyedState()
        {
            var field = new WeedField();
            field.Add(new Vector2(1, 1));
            field.Add(new Vector2(2, 2));
            field.TryDestroyAt(new Vector2(2, 2));

            var snapshot = field.CreateSnapshot();
            field.Clear();
            field.Add(new Vector2(9, 9));

            field.RestoreSnapshot(snapshot);

            Assert.AreEqual(2, field.TotalCount);
            Assert.AreEqual(1, field.AliveCount);
            Assert.AreEqual(1, field.DestroyedCount);
            Assert.IsTrue(field.Weeds.Any(weed => weed.GridPosition == new Vector2(1, 1) && !weed.IsDestroyed));
            Assert.IsTrue(field.Weeds.Any(weed => weed.GridPosition == new Vector2(2, 2) && weed.IsDestroyed));
        }
    }
}
