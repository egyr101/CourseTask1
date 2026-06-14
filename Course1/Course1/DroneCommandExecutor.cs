using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DroneSimulator
{
    public sealed class DroneCommandExecutor
    {
        private const string IncompleteAlgorithmError = "Алгоритм завершился, но на поле остались сорняки.";

        private readonly MapRenderer _mapRenderer;
        private readonly List<DroneRuntimeState> _drones;

        private readonly Queue<List<DroneCommand>> _ticks = new();
        private readonly Dictionary<int, Queue<DroneCommandType>> _activeTickQueues = new();

        private AlgorithmSnapshot? _initialSnapshot;
        private bool _isStepMode;
        private bool _hasPendingStepResultCheck;

        public event Action<string>? ErrorOccurred;
        public event Action<AlgorithmResult>? Completed;
        public event Action<IReadOnlyList<DroneChargeInfo>>? ChargesChanged;

        private int _algorithmScore;
        private int _initialWeedCount;

        public bool IsRunning { get; private set; }
        public bool IsStepMode => _isStepMode;
        public bool IsAutoRunning => IsRunning && !_isStepMode;
        public bool CanExecuteStep => !AnyDroneIsAnimating() && (!IsRunning || _isStepMode);
        public bool IsCompleted { get; private set; }
        public string? LastError { get; private set; }
        public string? LastMessage { get; private set; }

        public DroneCommandExecutor(MapRenderer mapRenderer)
        {
            _mapRenderer = mapRenderer;

            _drones = mapRenderer.Drones
                .Select(drone => new DroneRuntimeState(drone))
                .ToList();

            DistributeChargesByWeedCount();

            foreach (var droneState in _drones)
            {
                droneState.Drone.SetRotationInstant(GetRotationForDirection(droneState.Direction));
            }
        }

        private void DistributeChargesByWeedCount()
        {
            if (_drones.Count == 0)
                return;

            int totalCharges = _mapRenderer.WeedField.TotalCount;
            int baseCharges = totalCharges / _drones.Count;
            int extraCharges = totalCharges % _drones.Count;

            for (int i = 0; i < _drones.Count; i++)
            {
                int charges = baseCharges + (i < extraCharges ? 1 : 0);
                _drones[i].Charges = charges;
                _drones[i].InitialCharges = charges;
            }
        }

        private string GetDroneName(int index)
        {
            if (index >= 0 && index < _drones.Count)
                return _drones[index].Drone.Name;

            return $"Дрон {index + 1}";
        }

        public void Start(IEnumerable<CommandRow> rows)
        {
            BeginExecution(rows, stepMode: false);
        }

        public void Step(IEnumerable<CommandRow> rows)
        {
            if (IsAutoRunning || AnyDroneIsAnimating())
                return;

            try
            {
                if (!IsRunning)
                {
                    BeginExecution(rows, stepMode: true);

                    if (!IsRunning || !_isStepMode)
                        return;
                }

                ExecuteManualStep();
            }
            catch (Exception ex)
            {
                Fail(ex.Message);
            }
        }

        private void BeginExecution(IEnumerable<CommandRow> rows, bool stepMode)
        {
            NotifyChargesChanged();

            _ticks.Clear();
            _activeTickQueues.Clear();
            LastError = null;
            LastMessage = null;
            IsCompleted = false;
            IsRunning = false;
            _isStepMode = false;
            _hasPendingStepResultCheck = false;
            _algorithmScore = 0;
            _initialWeedCount = _mapRenderer.WeedField.TotalCount;

            _initialSnapshot = CreateSnapshot();

            try
            {
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
                _isStepMode = stepMode && IsRunning;

                if (!IsRunning)
                    LastMessage = "Нет команд для выполнения.";
            }
            catch (Exception ex)
            {
                Fail(ex.Message);
            }
        }

        public void Stop()
        {
            IsRunning = false;
            _isStepMode = false;
            _hasPendingStepResultCheck = false;
            _ticks.Clear();
            _activeTickQueues.Clear();
        }

        public void RestoreMapToInitialState()
        {
            RestoreInitialSnapshot();
            IsCompleted = false;
            IsRunning = false;
            _isStepMode = false;
            _hasPendingStepResultCheck = false;
            LastMessage = null;
            LastError = null;
        }

        public IReadOnlyList<DroneChargeInfo> GetChargeInfo()
        {
            return _drones
                .Select((droneState, index) => new DroneChargeInfo(
                    GetDroneName(index),
                    droneState.Charges,
                    droneState.InitialCharges))
                .ToList();
        }

        public void NotifyChargesChanged()
        {
            ChargesChanged?.Invoke(GetChargeInfo());
        }

        public void Update(GameTime gameTime)
        {
            if (!IsRunning)
                return;

            if (AnyDroneIsAnimating())
                return;

            try
            {
                if (_isStepMode)
                {
                    if (_hasPendingStepResultCheck)
                    {
                        _hasPendingStepResultCheck = false;
                        CheckStateAfterExecutedStep();
                    }

                    return;
                }

                if (!PrepareNextParallelStep())
                    return;

                ExecuteOneParallelStep();
                CheckStateAfterExecutedStep();
            }
            catch (Exception ex)
            {
                Fail(ex.Message);
            }
        }

        private void ExecuteManualStep()
        {
            if (!PrepareNextParallelStep())
                return;

            ExecuteOneParallelStep();
            _hasPendingStepResultCheck = true;

            if (!AnyDroneIsAnimating())
            {
                _hasPendingStepResultCheck = false;
                CheckStateAfterExecutedStep();
            }
        }

        private bool PrepareNextParallelStep()
        {
            CheckCollisions();

            if (CheckCompletion())
                return false;

            if (!HasActiveTick())
            {
                if (!HasWaitingTicks())
                    return FailBecauseAlgorithmEndedWithAliveWeeds();

                LoadNextTick();
            }

            return true;
        }

        private void CheckStateAfterExecutedStep()
        {
            CheckCollisions();

            if (!CheckCompletion())
                FailIfAlgorithmEndedWithAliveWeeds();
        }

        private bool FailBecauseAlgorithmEndedWithAliveWeeds()
        {
            Fail(IncompleteAlgorithmError);
            return false;
        }

        private void FailIfAlgorithmEndedWithAliveWeeds()
        {
            if (!HasActiveTick() && !HasWaitingTicks())
                Fail(IncompleteAlgorithmError);
        }

        private AlgorithmSnapshot CreateSnapshot()
        {
            var snapshot = new AlgorithmSnapshot();

            foreach (var droneState in _drones)
            {
                snapshot.Drones.Add(new DroneRuntimeSnapshot(
                    droneState.Drone.GridPosition,
                    droneState.Direction,
                    droneState.Charges,
                    droneState.InitialCharges));
            }

            snapshot.Weeds.AddRange(_mapRenderer.WeedField.CreateSnapshot());
            return snapshot;
        }

        private void RestoreInitialSnapshot()
        {
            if (_initialSnapshot == null)
                return;

            for (int i = 0; i < _initialSnapshot.Drones.Count && i < _drones.Count; i++)
            {
                var snapshot = _initialSnapshot.Drones[i];
                var droneState = _drones[i];

                droneState.Direction = snapshot.Direction;
                droneState.Charges = snapshot.Charges;
                droneState.InitialCharges = snapshot.InitialCharges;
                droneState.Drone.SetPositionInstant(snapshot.GridPosition);
                droneState.Drone.SetRotationInstant(GetRotationForDirection(snapshot.Direction));
            }

            _mapRenderer.WeedField.RestoreSnapshot(_initialSnapshot.Weeds);
            NotifyChargesChanged();
        }

        private void Fail(string errorMessage)
        {
            LastError = errorMessage;
            LastMessage = null;

            RestoreInitialSnapshot();
            Stop();

            ErrorOccurred?.Invoke(errorMessage);
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

            var result = new AlgorithmResult(
                _algorithmScore,
                _mapRenderer.WeedField.DestroyedCount,
                _initialWeedCount);

            Stop();
            Completed?.Invoke(result);
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

            _algorithmScore += GetCommandScore(command);
        }

        private static int GetCommandScore(DroneCommandType command)
        {
            return command switch
            {
                DroneCommandType.MoveForward => 2,
                DroneCommandType.TurnLeft => 1,
                DroneCommandType.TurnRight => 1,
                _ => 0
            };
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
            NotifyChargesChanged();

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
            if (target.IsAll)
            {
                for (int i = 0; i < _drones.Count; i++)
                    yield return i;

                yield break;
            }

            if (target.DroneIndex < 0 || target.DroneIndex >= _drones.Count)
            {
                throw new InvalidOperationException(
                    $"Дрон с номером {target.DroneIndex + 1} отсутствует на карте.");
            }

            yield return target.DroneIndex;
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
