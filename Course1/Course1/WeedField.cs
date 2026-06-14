using Microsoft.Xna.Framework;
using System.Collections.Generic;
using System.Linq;

namespace DroneSimulator
{
    public sealed class WeedField
    {
        private readonly List<Weed> _weeds = new();

        public IReadOnlyList<Weed> Weeds => _weeds;

        public bool HasAliveWeeds => _weeds.Any(IsAlive);

        public int TotalCount => _weeds.Count;

        public int AliveCount => _weeds.Count(IsAlive);

        public int DestroyedCount => _weeds.Count(weed => weed.IsDestroyed);

        public void Clear()
        {
            _weeds.Clear();
        }

        public void Add(Vector2 position)
        {
            if (_weeds.Any(weed => weed.GridPosition == position))
                return;

            _weeds.Add(new Weed(position));
        }

        public bool TryDestroyAt(Vector2 position)
        {
            var weed = _weeds.FirstOrDefault(item =>
                IsAlive(item) && item.GridPosition == position);

            if (weed == null)
                return false;

            weed.Destroy();
            return true;
        }


        public List<WeedSnapshot> CreateSnapshot()
        {
            return _weeds
                .Select(weed => new WeedSnapshot(weed.GridPosition, weed.IsDestroyed))
                .ToList();
        }

        public void RestoreSnapshot(IEnumerable<WeedSnapshot> snapshot)
        {
            _weeds.Clear();
            _weeds.AddRange(snapshot.Select(item => new Weed(item.GridPosition, item.IsDestroyed)));
        }

        private static bool IsAlive(Weed weed)
        {
            return !weed.IsDestroyed;
        }
    }

    public readonly struct WeedSnapshot
    {
        public Vector2 GridPosition { get; }
        public bool IsDestroyed { get; }

        public WeedSnapshot(Vector2 gridPosition, bool isDestroyed)
        {
            GridPosition = gridPosition;
            IsDestroyed = isDestroyed;
        }
    }
}
