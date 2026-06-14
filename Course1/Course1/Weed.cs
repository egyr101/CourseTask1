using Microsoft.Xna.Framework;

namespace DroneSimulator
{
    public sealed class Weed
    {
        public Vector2 GridPosition { get; }
        public bool IsDestroyed { get; private set; }

        public Weed(Vector2 gridPosition, bool isDestroyed = false)
        {
            GridPosition = gridPosition;
            IsDestroyed = isDestroyed;
        }

        public void Destroy()
        {
            IsDestroyed = true;
        }

    }
}
