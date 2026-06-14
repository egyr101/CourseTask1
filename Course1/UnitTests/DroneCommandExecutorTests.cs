using DroneSimulator;
using Microsoft.Xna.Framework;
using UnitTests.Helpers;

namespace UnitTests
{
    [TestClass]
    public sealed class DroneCommandExecutorTests
    {
        [TestMethod]
        public void Constructor_DistributesChargesByWeedCountWithDifferenceNoMoreThanOne()
        {
            MapRenderer map = TestMapFactory.CreateMap(
                new[] { new Vector2(0, 0), new Vector2(0, 1), new Vector2(0, 2) },
                new[] { new Vector2(1, 0), new Vector2(1, 1), new Vector2(1, 2), new Vector2(2, 0), new Vector2(2, 1) });

            var executor = new DroneCommandExecutor(map);
            var charges = executor.GetChargeInfo().ToList();

            Assert.AreEqual(3, charges.Count);
            Assert.AreEqual(5, charges.Sum(item => item.InitialCharges));
            Assert.AreEqual(5, charges.Sum(item => item.CurrentCharges));
            Assert.IsTrue(charges.Max(item => item.InitialCharges) - charges.Min(item => item.InitialCharges) <= 1);
            CollectionAssert.AreEqual(new[] { 2, 2, 1 }, charges.Select(item => item.InitialCharges).ToArray());
        }

        [TestMethod]
        public void NotifyChargesChanged_RaisesCurrentChargeInfo()
        {
            MapRenderer map = TestMapFactory.CreateMap(
                new[] { new Vector2(0, 0) },
                new[] { new Vector2(1, 0) });

            var executor = new DroneCommandExecutor(map);
            IReadOnlyList<DroneChargeInfo>? received = null;
            executor.ChargesChanged += info => received = info;

            executor.NotifyChargesChanged();

            Assert.IsNotNull(received);
            Assert.AreEqual(1, received!.Count);
            Assert.AreEqual("Дрон 1", received[0].DroneName);
            Assert.AreEqual(1, received[0].CurrentCharges);
            Assert.AreEqual(1, received[0].InitialCharges);
        }

        [TestMethod]
        public void Start_WithNoCommandsAndAliveWeeds_SetsMessageAndDoesNotRun()
        {
            MapRenderer map = TestMapFactory.CreateMap(
                new[] { new Vector2(0, 0) },
                new[] { new Vector2(1, 0) });
            var executor = new DroneCommandExecutor(map);

            executor.Start(Array.Empty<CommandRow>());

            Assert.IsFalse(executor.IsRunning);
            Assert.AreEqual("Нет команд для выполнения.", executor.LastMessage);
            Assert.IsNull(executor.LastError);
        }

        [TestMethod]
        public void Start_WhenNoAliveWeeds_CompletesImmediately()
        {
            MapRenderer map = TestMapFactory.CreateMap(
                new[] { new Vector2(0, 0) },
                Array.Empty<Vector2>());
            var executor = new DroneCommandExecutor(map);
            AlgorithmResult? result = null;
            executor.Completed += value => result = value;

            executor.Start(Array.Empty<CommandRow>());

            Assert.IsFalse(executor.IsRunning);
            Assert.IsTrue(executor.IsCompleted);
            Assert.IsNotNull(result);
            Assert.AreEqual(0, result!.Score);
            Assert.AreEqual(0, result.DestroyedWeeds);
            Assert.AreEqual(0, result.InitialWeeds);
        }

        [TestMethod]
        public void Start_WithAllAttack_DestroysAllWeedsAndCompletes()
        {
            MapRenderer map = TestMapFactory.CreateMap(
                new[] { new Vector2(0, 0), new Vector2(0, 1) },
                new[] { new Vector2(1, 0), new Vector2(1, 1) });
            var executor = new DroneCommandExecutor(map);
            AlgorithmResult? result = null;
            executor.Completed += value => result = value;

            executor.Start(new[] { TestMapFactory.Row(1, "Все", "Разряд") });
            RunAutoToEnd(executor, map);

            Assert.IsFalse(executor.IsRunning);
            Assert.IsTrue(executor.IsCompleted);
            Assert.IsNotNull(result);
            Assert.AreEqual(0, result!.Score);
            Assert.AreEqual(2, result.DestroyedWeeds);
            Assert.AreEqual(2, result.InitialWeeds);
            Assert.AreEqual(0, map.WeedField.AliveCount);
        }

        [TestMethod]
        public void Start_WithTurnAndAttack_AddsTurnScoreAndCompletes()
        {
            MapRenderer map = TestMapFactory.CreateMap(
                new[] { new Vector2(0, 0) },
                new[] { new Vector2(0, 1) });
            var executor = new DroneCommandExecutor(map);
            AlgorithmResult? result = null;
            executor.Completed += value => result = value;

            executor.Start(new[]
            {
                TestMapFactory.Row(1, "Дрон 1", "Направо"),
                TestMapFactory.Row(2, "Дрон 1", "Разряд")
            });
            RunAutoToEnd(executor, map);

            Assert.IsNotNull(result);
            Assert.AreEqual(1, result!.Score);
            Assert.AreEqual(1, result.DestroyedWeeds);
        }

        [TestMethod]
        public void Start_WithMovementAndAttack_AddsMovementScoreAndCompletes()
        {
            MapRenderer map = TestMapFactory.CreateMap(
                new[] { new Vector2(0, 0) },
                new[] { new Vector2(3, 0) });
            var executor = new DroneCommandExecutor(map);
            AlgorithmResult? result = null;
            executor.Completed += value => result = value;

            executor.Start(new[]
            {
                TestMapFactory.Row(1, "Дрон 1", "Вперёд", "2"),
                TestMapFactory.Row(2, "Дрон 1", "Разряд")
            });
            RunAutoToEnd(executor, map);

            Assert.IsNotNull(result);
            Assert.AreEqual(4, result!.Score);
            Assert.AreEqual(new Vector2(2, 0), map.Drones[0].GridPosition);
            Assert.AreEqual(0, map.WeedField.AliveCount);
        }

        [TestMethod]
        public void Start_WhenAlgorithmEndsButWeedsRemain_RaisesErrorAndRollsBack()
        {
            MapRenderer map = TestMapFactory.CreateMap(
                new[] { new Vector2(0, 0) },
                new[] { new Vector2(1, 0), new Vector2(2, 0) });
            var executor = new DroneCommandExecutor(map);
            string? error = null;
            executor.ErrorOccurred += message => error = message;

            executor.Start(new[] { TestMapFactory.Row(1, "Дрон 1", "Разряд") });
            RunAutoToEnd(executor, map);

            Assert.IsFalse(executor.IsRunning);
            Assert.IsNotNull(error);
            StringAssert.Contains(error!, "остались сорняки");
            Assert.AreEqual(2, map.WeedField.AliveCount);
            Assert.AreEqual(2, executor.GetChargeInfo()[0].CurrentCharges);
        }

        [TestMethod]
        public void Start_WithForwardOutsideMap_RaisesErrorAndRollsBack()
        {
            MapRenderer map = TestMapFactory.CreateMap(
                new[] { new Vector2(0, 0) },
                new[] { new Vector2(0, 1) },
                gridWidth: 1,
                gridHeight: 2);
            var executor = new DroneCommandExecutor(map);
            string? error = null;
            executor.ErrorOccurred += message => error = message;

            executor.Start(new[] { TestMapFactory.Row(1, "Дрон 1", "Вперёд") });
            RunAutoToEnd(executor, map);

            Assert.IsNotNull(error);
            StringAssert.Contains(error!, "границы поля");
            Assert.AreEqual(new Vector2(0, 0), map.Drones[0].GridPosition);
            Assert.AreEqual(1, map.WeedField.AliveCount);
        }

        [TestMethod]
        public void Start_WithAttackOutsideMap_RaisesErrorAndRollsBack()
        {
            MapRenderer map = TestMapFactory.CreateMap(
                new[] { new Vector2(0, 0) },
                new[] { new Vector2(0, 1) },
                gridWidth: 1,
                gridHeight: 2);
            var executor = new DroneCommandExecutor(map);
            string? error = null;
            executor.ErrorOccurred += message => error = message;

            executor.Start(new[] { TestMapFactory.Row(1, "Дрон 1", "Разряд") });
            RunAutoToEnd(executor, map);

            Assert.IsNotNull(error);
            StringAssert.Contains(error!, "атаковать за границы");
            Assert.AreEqual(1, executor.GetChargeInfo()[0].CurrentCharges);
            Assert.AreEqual(1, map.WeedField.AliveCount);
        }

        [TestMethod]
        public void Start_WithAttackOnEmptyCell_RaisesErrorAndRollsBack()
        {
            MapRenderer map = TestMapFactory.CreateMap(
                new[] { new Vector2(0, 0) },
                new[] { new Vector2(2, 0) });
            var executor = new DroneCommandExecutor(map);
            string? error = null;
            executor.ErrorOccurred += message => error = message;

            executor.Start(new[] { TestMapFactory.Row(1, "Дрон 1", "Разряд") });
            RunAutoToEnd(executor, map);

            Assert.IsNotNull(error);
            StringAssert.Contains(error!, "нет сорняка");
            Assert.AreEqual(1, executor.GetChargeInfo()[0].CurrentCharges);
            Assert.AreEqual(1, map.WeedField.AliveCount);
        }

        [TestMethod]
        public void Start_WithDroneWithoutCharges_RaisesErrorAndRollsBack()
        {
            MapRenderer map = TestMapFactory.CreateMap(
                new[] { new Vector2(0, 0), new Vector2(0, 1) },
                new[] { new Vector2(1, 1) });
            var executor = new DroneCommandExecutor(map);
            string? error = null;
            executor.ErrorOccurred += message => error = message;

            executor.Start(new[] { TestMapFactory.Row(1, "Дрон 2", "Разряд") });
            RunAutoToEnd(executor, map);

            Assert.IsNotNull(error);
            StringAssert.Contains(error!, "не осталось зарядов");
            Assert.AreEqual(1, map.WeedField.AliveCount);
        }

        [TestMethod]
        public void Start_WithCollision_RaisesErrorAndRollsBack()
        {
            MapRenderer map = TestMapFactory.CreateMap(
                new[] { new Vector2(0, 0), new Vector2(0, 0) },
                new[] { new Vector2(1, 0), new Vector2(1, 1) });
            var executor = new DroneCommandExecutor(map);
            string? error = null;
            executor.ErrorOccurred += message => error = message;

            executor.Start(new[] { TestMapFactory.Row(1, "Дрон 1", "Разряд") });
            RunAutoToEnd(executor, map);

            Assert.IsNotNull(error);
            StringAssert.Contains(error!, "столкнулись");
            Assert.AreEqual(new Vector2(0, 0), map.Drones[0].GridPosition);
            Assert.AreEqual(new Vector2(0, 0), map.Drones[1].GridPosition);
        }

        [TestMethod]
        public void Start_WithCommandForMissingDrone_RaisesError()
        {
            MapRenderer map = TestMapFactory.CreateMap(
                new[] { new Vector2(0, 0) },
                new[] { new Vector2(1, 0) });
            var executor = new DroneCommandExecutor(map);
            string? error = null;
            executor.ErrorOccurred += message => error = message;

            executor.Start(new[] { TestMapFactory.Row(1, "Дрон 2", "Разряд") });
            RunAutoToEnd(executor, map);

            Assert.IsNotNull(error);
            StringAssert.Contains(error!, "отсутствует на карте");
        }

        [TestMethod]
        public void Step_WithRepeatedForward_ExecutesOneElementaryActionPerClick()
        {
            MapRenderer map = TestMapFactory.CreateMap(
                new[] { new Vector2(0, 0) },
                new[] { new Vector2(3, 0) });
            var executor = new DroneCommandExecutor(map);
            AlgorithmResult? result = null;
            executor.Completed += value => result = value;
            var rows = new[]
            {
                TestMapFactory.Row(1, "Дрон 1", "Вперёд", "2"),
                TestMapFactory.Row(2, "Дрон 1", "Разряд")
            };

            executor.Step(rows);
            TestMapFactory.FinishDroneAnimations(map);
            executor.Update(new GameTime(TimeSpan.Zero, TimeSpan.FromSeconds(1)));
            Assert.AreEqual(new Vector2(1, 0), map.Drones[0].GridPosition);
            Assert.IsTrue(executor.IsRunning);
            Assert.IsTrue(executor.IsStepMode);

            executor.Step(rows);
            TestMapFactory.FinishDroneAnimations(map);
            executor.Update(new GameTime(TimeSpan.Zero, TimeSpan.FromSeconds(1)));
            Assert.AreEqual(new Vector2(2, 0), map.Drones[0].GridPosition);
            Assert.IsTrue(executor.IsRunning);

            executor.Step(rows);
            TestMapFactory.FinishDroneAnimations(map);
            executor.Update(new GameTime(TimeSpan.Zero, TimeSpan.FromSeconds(1)));

            Assert.IsFalse(executor.IsRunning);
            Assert.IsNotNull(result);
            Assert.AreEqual(4, result!.Score);
            Assert.AreEqual(1, result.DestroyedWeeds);
        }

        [TestMethod]
        public void RestoreMapToInitialState_RestoresPositionsWeedsChargesAndState()
        {
            MapRenderer map = TestMapFactory.CreateMap(
                new[] { new Vector2(0, 0) },
                new[] { new Vector2(1, 0) });
            var executor = new DroneCommandExecutor(map);

            executor.Start(new[] { TestMapFactory.Row(1, "Дрон 1", "Разряд") });
            RunAutoToEnd(executor, map);
            Assert.AreEqual(0, map.WeedField.AliveCount);

            executor.RestoreMapToInitialState();

            Assert.IsFalse(executor.IsRunning);
            Assert.IsFalse(executor.IsCompleted);
            Assert.IsNull(executor.LastError);
            Assert.IsNull(executor.LastMessage);
            Assert.AreEqual(new Vector2(0, 0), map.Drones[0].GridPosition);
            Assert.AreEqual(1, map.WeedField.AliveCount);
            Assert.AreEqual(1, executor.GetChargeInfo()[0].CurrentCharges);
        }

        [TestMethod]
        public void Stop_ClearsRunningState()
        {
            MapRenderer map = TestMapFactory.CreateMap(
                new[] { new Vector2(0, 0) },
                new[] { new Vector2(2, 0) });
            var executor = new DroneCommandExecutor(map);
            executor.Start(new[] { TestMapFactory.Row(1, "Дрон 1", "Вперёд") });

            executor.Stop();

            Assert.IsFalse(executor.IsRunning);
            Assert.IsFalse(executor.IsStepMode);
            Assert.IsTrue(executor.CanExecuteStep);
        }

        private static void RunAutoToEnd(DroneCommandExecutor executor, MapRenderer map, int maxIterations = 100)
        {
            var gameTime = new GameTime(TimeSpan.Zero, TimeSpan.FromSeconds(1));

            for (int i = 0; i < maxIterations && executor.IsRunning; i++)
            {
                executor.Update(gameTime);
                TestMapFactory.FinishDroneAnimations(map);
                executor.Update(gameTime);
            }

            Assert.IsFalse(executor.IsRunning, "Алгоритм не завершился за допустимое число итераций.");
        }
    }
}
