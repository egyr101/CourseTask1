using Microsoft.Xna.Framework;

namespace DroneSimulator
{
    public sealed class Weed
    {
        public Vector2 GridPosition { get; }
        public bool IsDestroyed { get; private set; }

        public Weed(Vector2 gridPosition)
        {
            GridPosition = gridPosition;
        }

        public void Destroy()
        {
            IsDestroyed = true;
        }
    }
}
