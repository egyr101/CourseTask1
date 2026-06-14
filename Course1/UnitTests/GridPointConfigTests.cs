using DroneSimulator;
using Microsoft.Xna.Framework;

namespace UnitTests
{
    [TestClass]
    public sealed class GridPointConfigTests
    {
        [TestMethod]
        public void ToVector2_ReturnsPointCoordinatesAsVector()
        {
            var point = new GridPointConfig { X = 4, Y = 7 };

            Assert.AreEqual(new Vector2(4, 7), point.ToVector2());
        }
    }
}
