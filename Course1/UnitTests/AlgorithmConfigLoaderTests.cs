using DroneSimulator;
using UnitTests.Helpers;

namespace UnitTests
{
    [TestClass]
    public sealed class AlgorithmConfigLoaderTests
    {
        private readonly List<string> _createdFiles = new();

        [TestCleanup]
        public void Cleanup()
        {
            foreach (string file in _createdFiles)
            {
                if (File.Exists(file))
                    File.Delete(file);
            }
        }

        [TestMethod]
        public void LoadFromFile_WithValidAlgorithm_ReturnsConfigAndNormalizesRepeat()
        {
            string path = WriteTempFile("algorithm.json", """
            {
              "numberDronesOnMap": 2,
              "ticks": [
                {
                  "commands": [
                    { "target": "drone", "droneNumber": 1, "action": "forward", "repeat": 0 },
                    { "target": "drone", "droneNumber": 2, "action": "attack", "repeat": 2 }
                  ]
                }
              ]
            }
            """);

            AlgorithmConfig config = AlgorithmConfigLoader.LoadFromFile(path, droneCount: 2);

            Assert.AreEqual(2, config.NumberDronesOnMap);
            Assert.AreEqual(1, config.Ticks.Count);
            Assert.AreEqual(2, config.Ticks[0].Commands.Count);
            Assert.AreEqual(1, config.Ticks[0].Commands[0].Repeat);
            Assert.AreEqual(2, config.Ticks[0].Commands[1].Repeat);
        }

        [TestMethod]
        public void LoadFromFile_WithEmptyPath_ThrowsException()
        {
            Assert.ThrowsException<InvalidOperationException>(() => AlgorithmConfigLoader.LoadFromFile("", 1));
        }

        [TestMethod]
        public void LoadFromFile_WithMissingFile_ThrowsFileNotFoundException()
        {
            string path = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".json");

            Assert.ThrowsException<FileNotFoundException>(() => AlgorithmConfigLoader.LoadFromFile(path, 1));
        }

        [TestMethod]
        public void LoadFromFile_WithNonJsonExtension_ThrowsException()
        {
            string path = WriteTempFile("algorithm.txt", "{}");

            Assert.ThrowsException<InvalidOperationException>(() => AlgorithmConfigLoader.LoadFromFile(path, 1));
        }

        [TestMethod]
        public void LoadFromFile_WithMalformedJson_ThrowsException()
        {
            string path = WriteTempFile("broken_algorithm.json", "{ broken");

            Assert.ThrowsException<InvalidOperationException>(() => AlgorithmConfigLoader.LoadFromFile(path, 1));
        }

        [TestMethod]
        public void LoadFromFile_WithoutDroneCount_ThrowsException()
        {
            string path = WriteTempFile("algorithm.json", """
            { "ticks": [ { "commands": [ { "target": "all", "action": "attack" } ] } ] }
            """);

            Assert.ThrowsException<InvalidOperationException>(() => AlgorithmConfigLoader.LoadFromFile(path, 1));
        }

        [TestMethod]
        public void LoadFromFile_WithDifferentDroneCount_ThrowsException()
        {
            string path = WriteTempFile("algorithm.json", """
            {
              "numberDronesOnMap": 3,
              "ticks": [ { "commands": [ { "target": "all", "action": "attack" } ] } ]
            }
            """);

            Assert.ThrowsException<InvalidOperationException>(() => AlgorithmConfigLoader.LoadFromFile(path, 2));
        }

        [TestMethod]
        public void LoadFromFile_WithMissingTicks_ThrowsException()
        {
            string path = WriteTempFile("algorithm.json", """
            { "numberDronesOnMap": 1, "ticks": null }
            """);

            Assert.ThrowsException<InvalidOperationException>(() => AlgorithmConfigLoader.LoadFromFile(path, 1));
        }

        [TestMethod]
        public void LoadFromFile_WithEmptyTicks_ThrowsException()
        {
            string path = WriteTempFile("algorithm.json", """
            { "numberDronesOnMap": 1, "ticks": [] }
            """);

            Assert.ThrowsException<InvalidOperationException>(() => AlgorithmConfigLoader.LoadFromFile(path, 1));
        }

        [TestMethod]
        public void LoadFromFile_WithTickWithoutCommands_ThrowsException()
        {
            string path = WriteTempFile("algorithm.json", """
            { "numberDronesOnMap": 1, "ticks": [ { "commands": [] } ] }
            """);

            Assert.ThrowsException<InvalidOperationException>(() => AlgorithmConfigLoader.LoadFromFile(path, 1));
        }

        [TestMethod]
        public void LoadFromFile_WithMoreCommandsThanDrones_ThrowsException()
        {
            string path = WriteTempFile("algorithm.json", """
            {
              "numberDronesOnMap": 1,
              "ticks": [
                { "commands": [
                  { "target": "drone", "droneNumber": 1, "action": "forward" },
                  { "target": "drone", "droneNumber": 1, "action": "right" }
                ] }
              ]
            }
            """);

            Assert.ThrowsException<InvalidOperationException>(() => AlgorithmConfigLoader.LoadFromFile(path, 1));
        }

        [TestMethod]
        public void LoadFromFile_WithAllCommandMixedWithOtherCommand_ThrowsException()
        {
            string path = WriteTempFile("algorithm.json", """
            {
              "numberDronesOnMap": 2,
              "ticks": [
                { "commands": [
                  { "target": "all", "action": "forward" },
                  { "target": "drone", "droneNumber": 1, "action": "attack" }
                ] }
              ]
            }
            """);

            Assert.ThrowsException<InvalidOperationException>(() => AlgorithmConfigLoader.LoadFromFile(path, 2));
        }

        [TestMethod]
        public void LoadFromFile_WithDroneTargetWithoutNumber_ThrowsException()
        {
            string path = WriteTempFile("algorithm.json", """
            {
              "numberDronesOnMap": 1,
              "ticks": [ { "commands": [ { "target": "drone", "action": "attack" } ] } ]
            }
            """);

            Assert.ThrowsException<InvalidOperationException>(() => AlgorithmConfigLoader.LoadFromFile(path, 1));
        }

        [TestMethod]
        public void LoadFromFile_WithDroneNumberOutsideCurrentMap_ThrowsException()
        {
            string path = WriteTempFile("algorithm.json", """
            {
              "numberDronesOnMap": 2,
              "ticks": [ { "commands": [ { "target": "drone", "droneNumber": 3, "action": "attack" } ] } ]
            }
            """);

            Assert.ThrowsException<InvalidOperationException>(() => AlgorithmConfigLoader.LoadFromFile(path, 2));
        }

        [TestMethod]
        public void LoadFromFile_WithUnknownTarget_ThrowsException()
        {
            string path = WriteTempFile("algorithm.json", """
            {
              "numberDronesOnMap": 1,
              "ticks": [ { "commands": [ { "target": "team", "action": "attack" } ] } ]
            }
            """);

            Assert.ThrowsException<InvalidOperationException>(() => AlgorithmConfigLoader.LoadFromFile(path, 1));
        }

        [TestMethod]
        public void LoadFromFile_WithUnknownAction_ThrowsException()
        {
            string path = WriteTempFile("algorithm.json", """
            {
              "numberDronesOnMap": 1,
              "ticks": [ { "commands": [ { "target": "all", "action": "jump" } ] } ]
            }
            """);

            Assert.ThrowsException<InvalidOperationException>(() => AlgorithmConfigLoader.LoadFromFile(path, 1));
        }

        [TestMethod]
        public void ToCommandRows_WithRussianLanguage_ConvertsConfigToTableRows()
        {
            var config = new AlgorithmConfig
            {
                NumberDronesOnMap = 3,
                Ticks = new List<AlgorithmTickConfig>
                {
                    new AlgorithmTickConfig
                    {
                        Commands = new List<AlgorithmCommandConfig>
                        {
                            new AlgorithmCommandConfig { Target = "drone", DroneNumber = 1, Action = "forward", Repeat = 3 },
                            new AlgorithmCommandConfig { Target = "drone", DroneNumber = 2, Action = "attack", Repeat = 1 },
                            new AlgorithmCommandConfig { Target = "drone", DroneNumber = 3, Action = "turn_right", Repeat = 2 }
                        }
                    }
                }
            };

            List<CommandRow> rows = AlgorithmConfigLoader.ToCommandRows(config, GameLanguage.Russian, droneCount: 3);

            Assert.AreEqual(2, rows.Count);
            Assert.AreEqual(1, rows[0].TickNumber);
            Assert.AreEqual("Дрон 1", rows[0].Target1);
            Assert.AreEqual("Вперёд", rows[0].Action1);
            Assert.AreEqual("3", rows[0].Argument1);
            Assert.AreEqual("Дрон 2", rows[0].Target2);
            Assert.AreEqual("Разряд", rows[0].Action2);
            Assert.AreEqual("", rows[0].Argument2);
            Assert.AreEqual("Дрон 3", rows[1].Target1);
            Assert.AreEqual("Направо", rows[1].Action1);
            Assert.AreEqual("2", rows[1].Argument1);
        }

        [TestMethod]
        public void ToCommandRows_WithEnglishLanguage_ConvertsAllTargetAndActions()
        {
            var config = new AlgorithmConfig
            {
                NumberDronesOnMap = 2,
                Ticks = new List<AlgorithmTickConfig>
                {
                    new AlgorithmTickConfig
                    {
                        Commands = new List<AlgorithmCommandConfig>
                        {
                            new AlgorithmCommandConfig { Target = "all", Action = "turn_left", Repeat = 1 }
                        }
                    }
                }
            };

            List<CommandRow> rows = AlgorithmConfigLoader.ToCommandRows(config, GameLanguage.English, droneCount: 2);

            Assert.AreEqual(1, rows.Count);
            Assert.AreEqual("All", rows[0].Target1);
            Assert.AreEqual("Left", rows[0].Action1);
            Assert.AreEqual("", rows[0].Argument1);
        }

        [TestMethod]
        public void SaveToAlgorithmsFolder_WithValidRows_SavesCanonicalJson()
        {
            var rows = new[]
            {
                TestMapFactory.Row(1, "Дрон 1", "Вперёд", "2", "Дрон 2", "Налево"),
                TestMapFactory.Row(2, "Все", "Разряд")
            };

            string path = AlgorithmConfigLoader.SaveToAlgorithmsFolder("my algorithm", rows, droneCount: 2);
            _createdFiles.Add(path);

            Assert.IsTrue(File.Exists(path));
            Assert.AreEqual("my_algorithm.json", Path.GetFileName(path));

            string json = File.ReadAllText(path);
            StringAssert.Contains(json, "\"numberDronesOnMap\": 2");
            StringAssert.Contains(json, "\"target\": \"drone\"");
            StringAssert.Contains(json, "\"action\": \"forward\"");
            StringAssert.Contains(json, "\"target\": \"all\"");
        }

        [TestMethod]
        public void SaveToAlgorithmsFolder_WithJsonExtension_DoesNotDuplicateExtension()
        {
            var rows = new[]
            {
                TestMapFactory.Row(1, "Дрон 1", "Разряд")
            };

            string path = AlgorithmConfigLoader.SaveToAlgorithmsFolder("saved_algorithm.json", rows, droneCount: 1);
            _createdFiles.Add(path);

            Assert.AreEqual("saved_algorithm.json", Path.GetFileName(path));
        }

        [TestMethod]
        public void SaveToAlgorithmsFolder_WithEmptyTable_ThrowsException()
        {
            Assert.ThrowsException<InvalidOperationException>(() =>
                AlgorithmConfigLoader.SaveToAlgorithmsFolder("empty", Array.Empty<CommandRow>(), droneCount: 1));
        }

        [TestMethod]
        public void SaveToAlgorithmsFolder_WithInvalidName_ThrowsException()
        {
            var rows = new[] { TestMapFactory.Row(1, "Дрон 1", "Вперёд") };

            Assert.ThrowsException<InvalidOperationException>(() =>
                AlgorithmConfigLoader.SaveToAlgorithmsFolder("   ", rows, droneCount: 1));
        }

        private string WriteTempFile(string fileName, string content)
        {
            string directory = Path.Combine(Path.GetTempPath(), "CourseTask1Tests", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);
            string path = Path.Combine(directory, fileName);
            File.WriteAllText(path, content);
            _createdFiles.Add(path);
            return path;
        }
    }
}
