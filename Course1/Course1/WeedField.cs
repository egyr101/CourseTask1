using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DroneSimulator
{
    public sealed class WeedField
    {
        private readonly List<Weed> _weeds = new();

        public IReadOnlyList<Weed> Weeds => _weeds;

        public bool HasAliveWeeds => _weeds.Any(weed => !weed.IsDestroyed);

        public int TotalCount => _weeds.Count;

        public int AliveCount => _weeds.Count(weed => !weed.IsDestroyed);

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
                !item.IsDestroyed && item.GridPosition == position);

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

            foreach (var item in snapshot)
            {
                _weeds.Add(new Weed(item.GridPosition, item.IsDestroyed));
            }
        }

        public void GenerateRandom(
            int count,
            int gridWidth,
            int gridHeight,
            IEnumerable<Vector2> blockedPositions,
            Random random)
        {
            Clear();

            var blocked = new HashSet<Vector2>(blockedPositions);
            var availablePositions = new List<Vector2>();

            for (int y = 0; y < gridHeight; y++)
            {
                for (int x = 0; x < gridWidth; x++)
                {
                    var position = new Vector2(x, y);

                    if (!blocked.Contains(position))
                        availablePositions.Add(position);
                }
            }

            int weedsToCreate = Math.Min(count, availablePositions.Count);

            for (int i = 0; i < weedsToCreate; i++)
            {
                int index = random.Next(availablePositions.Count);
                Add(availablePositions[index]);
                availablePositions.RemoveAt(index);
            }
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
