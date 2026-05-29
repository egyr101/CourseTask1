using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DroneSimulator
{
    public sealed class DroneCommandExecutor
    {
        private readonly MapRenderer _mapRenderer;
        private readonly List<DroneRuntimeState> _drones;

        private readonly Queue<List<DroneCommand>> _ticks = new();
        private readonly Dictionary<int, Queue<DroneCommandType>> _activeTickQueues = new();

        public bool IsRunning { get; private set; }
        public bool IsCompleted { get; private set; }
        public string? LastError { get; private set; }
        public string? LastMessage { get; private set; }

        public DroneCommandExecutor(MapRenderer mapRenderer)
        {
            _mapRenderer = mapRenderer;

            _drones = mapRenderer.Drones
                .Select(drone => new DroneRuntimeState(drone))
                .ToList();

            foreach (var droneState in _drones)
            {
                droneState.Drone.SetRotationInstant(GetRotationForDirection(droneState.Direction));
            }
        }

        public void Start(IEnumerable<CommandRow> rows)
        {
            _ticks.Clear();
            _activeTickQueues.Clear();
            LastError = null;
            LastMessage = null;
            IsCompleted = false;

            foreach (var tick in CommandTableParser.Parse(rows))
            {
                _ticks.Enqueue(tick);
            }

            if (!_mapRenderer.WeedField.HasAliveWeeds)
            {
                CompleteAlgorithm();
                return;
            }

            IsRunning = _ticks.Count > 0;

            if (!IsRunning)
                LastMessage = "Нет команд для выполнения.";
        }

        public void Stop()
        {
            IsRunning = false;
            _ticks.Clear();
            _activeTickQueues.Clear();
        }

        public void Update(GameTime gameTime)
        {
            if (!IsRunning)
                return;

            if (AnyDroneIsAnimating())
                return;

            try
            {
                CheckCollisions();

                if (CheckCompletion())
                    return;

                if (!HasActiveTick())
                {
                    if (!HasWaitingTicks())
                    {
                        IsRunning = false;
                        LastMessage = "Алгоритм завершился, но на поле остались сорняки.";
                        return;
                    }

                    LoadNextTick();
                }

                ExecuteOneParallelStep();

                CheckCollisions();
                CheckCompletion();
            }
            catch (Exception ex)
            {
                LastError = ex.Message;
                Stop();
            }
        }

        private bool AnyDroneIsAnimating()
        {
            return _drones.Any(drone => drone.Drone.IsAnimating);
        }

        private bool HasActiveTick()
        {
            return _activeTickQueues.Count > 0;
        }

        private bool HasWaitingTicks()
        {
            return _ticks.Count > 0;
        }

        private bool CheckCompletion()
        {
            if (_mapRenderer.WeedField.HasAliveWeeds)
                return false;

            CompleteAlgorithm();
            return true;
        }

        private void CompleteAlgorithm()
        {
            IsCompleted = true;
            LastMessage = "Все сорняки уничтожены. Алгоритм завершён.";
            Stop();
        }

        private void LoadNextTick()
        {
            var nextTick = _ticks.Dequeue();

            foreach (var command in nextTick)
            {
                AddCommandToTargetDrones(command);
            }
        }

        private void AddCommandToTargetDrones(DroneCommand command)
        {
            foreach (int droneIndex in GetTargetDroneIndexes(command.Target))
            {
                var queue = GetOrCreateActiveQueue(droneIndex);

                for (int i = 0; i < command.Repeat; i++)
                {
                    queue.Enqueue(command.Type);
                }
            }
        }

        private Queue<DroneCommandType> GetOrCreateActiveQueue(int droneIndex)
        {
            if (!_activeTickQueues.TryGetValue(droneIndex, out var queue))
            {
                queue = new Queue<DroneCommandType>();
                _activeTickQueues[droneIndex] = queue;
            }

            return queue;
        }

        private void ExecuteOneParallelStep()
        {
            var droneIndexes = _activeTickQueues.Keys.ToList();

            foreach (int droneIndex in droneIndexes)
            {
                ExecuteNextCommandForDrone(droneIndex);
            }
        }

        private void ExecuteNextCommandForDrone(int droneIndex)
        {
            var queue = _activeTickQueues[droneIndex];

            if (queue.Count == 0)
            {
                _activeTickQueues.Remove(droneIndex);
                return;
            }

            var command = queue.Dequeue();
            ExecuteCommand(_drones[droneIndex], command);

            if (queue.Count == 0)
            {
                _activeTickQueues.Remove(droneIndex);
            }
        }

        private void ExecuteCommand(
            DroneRuntimeState droneState,
            DroneCommandType command)
        {
            switch (command)
            {
                case DroneCommandType.MoveForward:
                    MoveForward(droneState);
                    break;

                case DroneCommandType.TurnLeft:
                    TurnDroneLeft(droneState);
                    break;

                case DroneCommandType.TurnRight:
                    TurnDroneRight(droneState);
                    break;

                case DroneCommandType.Attack:
                    Attack(droneState);
                    break;
            }
        }


        private static void TurnDroneLeft(DroneRuntimeState droneState)
        {
            droneState.Direction = TurnLeft(droneState.Direction);
            droneState.Drone.RotateTo(GetRotationForDirection(droneState.Direction));
        }

        private static void TurnDroneRight(DroneRuntimeState droneState)
        {
            droneState.Direction = TurnRight(droneState.Direction);
            droneState.Drone.RotateTo(GetRotationForDirection(droneState.Direction));
        }

        private void MoveForward(DroneRuntimeState droneState)
        {
            Vector2 nextPosition =
                droneState.Drone.GridPosition +
                GetDirectionVector(droneState.Direction);

            if (IsOutsideMap(nextPosition))
            {
                throw new InvalidOperationException("Дрон вышел за границы поля.");
            }

            droneState.Drone.MoveTo(nextPosition);
        }

        private void Attack(DroneRuntimeState droneState)
        {
            if (droneState.Charges <= 0)
            {
                throw new InvalidOperationException("У дрона не осталось зарядов.");
            }

            droneState.Charges--;

            Vector2 targetPosition =
                droneState.Drone.GridPosition +
                GetDirectionVector(droneState.Direction);

            if (IsOutsideMap(targetPosition))
            {
                throw new InvalidOperationException("Дрон пытается атаковать за границы поля.");
            }

            bool weedDestroyed = _mapRenderer.WeedField.TryDestroyAt(targetPosition);

            if (!weedDestroyed)
            {
                throw new InvalidOperationException("Перед дроном нет сорняка для уничтожения.");
            }
        }

        private bool IsOutsideMap(Vector2 position)
        {
            return position.X < 0 ||
                   position.X >= _mapRenderer.GridWidth ||
                   position.Y < 0 ||
                   position.Y >= _mapRenderer.GridHeight;
        }

        private void CheckCollisions()
        {
            int uniquePositions = _drones
                .Select(drone => drone.Drone.GridPosition)
                .Distinct()
                .Count();

            if (uniquePositions != _drones.Count)
            {
                throw new InvalidOperationException("Дроны столкнулись.");
            }
        }

        private IEnumerable<int> GetTargetDroneIndexes(DroneTarget target)
        {
            switch (target)
            {
                case DroneTarget.Red:
                    yield return 0;
                    break;

                case DroneTarget.Green:
                    yield return 1;
                    break;

                case DroneTarget.All:
                    for (int i = 0; i < _drones.Count; i++)
                        yield return i;
                    break;
            }
        }

        private static DroneFacing TurnLeft(DroneFacing direction)
        {
            return (DroneFacing)(((int)direction + 3) % 4);
        }

        private static DroneFacing TurnRight(DroneFacing direction)
        {
            return (DroneFacing)(((int)direction + 1) % 4);
        }


        private static float GetRotationForDirection(DroneFacing direction)
        {
            return direction switch
            {
                DroneFacing.Top => 0f,
                DroneFacing.Right => MathHelper.PiOver2,
                DroneFacing.Bottom => MathHelper.Pi,
                DroneFacing.Left => -MathHelper.PiOver2,
                _ => 0f
            };
        }

        private static Vector2 GetDirectionVector(DroneFacing direction)
        {
            return direction switch
            {
                DroneFacing.Top => new Vector2(0, -1),
                DroneFacing.Right => new Vector2(1, 0),
                DroneFacing.Bottom => new Vector2(0, 1),
                DroneFacing.Left => new Vector2(-1, 0),
                _ => Vector2.Zero
            };
        }
    }
}
