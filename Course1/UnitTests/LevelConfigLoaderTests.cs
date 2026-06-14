using DroneSimulator;
using System.Text.Json;

namespace UnitTests
{
    [TestClass]
    public sealed class LevelConfigLoaderTests
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
        public void LoadFromFile_WithValidJson_ReturnsConfig()
        {
            string path = WriteTempFile("valid_map.json", """
            {
              "drones": [ { "x": 1, "y": 2 }, { "x": 3, "y": 4 } ],
              "weeds": [ { "x": 5, "y": 6 }, { "x": 7, "y": 8 } ]
            }
            """);

            LevelConfig config = LevelConfigLoader.LoadFromFile(path);

            Assert.AreEqual(2, config.Drones.Count);
            Assert.AreEqual(1, config.Drones[0].X);
            Assert.AreEqual(2, config.Drones[0].Y);
            Assert.AreEqual(2, config.Weeds.Count);
            Assert.AreEqual(7, config.Weeds[1].X);
            Assert.AreEqual(8, config.Weeds[1].Y);
        }

        [TestMethod]
        public void LoadFromFile_WithEmptyPath_ThrowsException()
        {
            Assert.ThrowsException<InvalidOperationException>(() => LevelConfigLoader.LoadFromFile(""));
        }

        [TestMethod]
        public void LoadFromFile_WithMissingFile_ThrowsFileNotFoundException()
        {
            string path = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".json");

            Assert.ThrowsException<FileNotFoundException>(() => LevelConfigLoader.LoadFromFile(path));
        }

        [TestMethod]
        public void LoadFromFile_WithNonJsonExtension_ThrowsException()
        {
            string path = WriteTempFile("map.txt", "{} ");

            Assert.ThrowsException<InvalidOperationException>(() => LevelConfigLoader.LoadFromFile(path));
        }

        [TestMethod]
        public void LoadFromFile_WithMalformedJson_ThrowsException()
        {
            string path = WriteTempFile("broken_map.json", "{ not json");

            Assert.ThrowsException<InvalidOperationException>(() => LevelConfigLoader.LoadFromFile(path));
        }

        [TestMethod]
        public void ValidateForEditor_WithMissingDronesSection_ThrowsException()
        {
            var config = new LevelConfig { Drones = null!, Weeds = new List<GridPointConfig>() };

            Assert.ThrowsException<InvalidOperationException>(() => LevelConfigLoader.ValidateForEditor(config));
        }

        [TestMethod]
        public void ValidateForEditor_WithMissingWeedsSection_ThrowsException()
        {
            var config = new LevelConfig { Drones = new List<GridPointConfig>(), Weeds = null! };

            Assert.ThrowsException<InvalidOperationException>(() => LevelConfigLoader.ValidateForEditor(config));
        }

        [TestMethod]
        public void ValidateForEditor_WithNoDrones_ThrowsException()
        {
            var config = new LevelConfig
            {
                Drones = new List<GridPointConfig>(),
                Weeds = new List<GridPointConfig> { Point(1, 1) }
            };

            Assert.ThrowsException<InvalidOperationException>(() => LevelConfigLoader.ValidateForEditor(config));
        }

        [TestMethod]
        public void ValidateForEditor_WithMoreThanTenDrones_ThrowsException()
        {
            var config = new LevelConfig
            {
                Drones = Enumerable.Range(0, 11).Select(i => Point(i, 0)).ToList(),
                Weeds = Enumerable.Range(0, 11).Select(i => Point(i, 1)).ToList()
            };

            Assert.ThrowsException<InvalidOperationException>(() => LevelConfigLoader.ValidateForEditor(config));
        }

        [TestMethod]
        public void ValidateForEditor_WithFewerWeedsThanDrones_ThrowsException()
        {
            var config = new LevelConfig
            {
                Drones = new List<GridPointConfig> { Point(1, 1), Point(2, 2) },
                Weeds = new List<GridPointConfig> { Point(3, 3) }
            };

            Assert.ThrowsException<InvalidOperationException>(() => LevelConfigLoader.ValidateForEditor(config));
        }

        [TestMethod]
        public void ValidateForEditor_WithDuplicateDronePositions_ThrowsException()
        {
            var config = new LevelConfig
            {
                Drones = new List<GridPointConfig> { Point(1, 1), Point(1, 1) },
                Weeds = new List<GridPointConfig> { Point(3, 3), Point(4, 4) }
            };

            Assert.ThrowsException<InvalidOperationException>(() => LevelConfigLoader.ValidateForEditor(config));
        }

        [TestMethod]
        public void ValidateForEditor_WithDuplicateWeedPositions_ThrowsException()
        {
            var config = new LevelConfig
            {
                Drones = new List<GridPointConfig> { Point(1, 1) },
                Weeds = new List<GridPointConfig> { Point(3, 3), Point(3, 3) }
            };

            Assert.ThrowsException<InvalidOperationException>(() => LevelConfigLoader.ValidateForEditor(config));
        }

        [TestMethod]
        public void SaveToLevelsFolder_WithValidConfig_SavesJsonWithSafeName()
        {
            var config = new LevelConfig
            {
                Drones = new List<GridPointConfig> { Point(1, 1) },
                Weeds = new List<GridPointConfig> { Point(2, 2) }
            };

            string path = LevelConfigLoader.SaveToLevelsFolder("my test map", config);
            _createdFiles.Add(path);

            Assert.IsTrue(File.Exists(path));
            Assert.AreEqual("my_test_map.json", Path.GetFileName(path));

            LevelConfig loaded = LevelConfigLoader.LoadFromFile(path);
            Assert.AreEqual(1, loaded.Drones.Count);
            Assert.AreEqual(1, loaded.Weeds.Count);
        }

        [TestMethod]
        public void SaveToLevelsFolder_WithJsonExtension_DoesNotDuplicateExtension()
        {
            var config = new LevelConfig
            {
                Drones = new List<GridPointConfig> { Point(1, 1) },
                Weeds = new List<GridPointConfig> { Point(2, 2) }
            };

            string path = LevelConfigLoader.SaveToLevelsFolder("custom_map.json", config);
            _createdFiles.Add(path);

            Assert.AreEqual("custom_map.json", Path.GetFileName(path));
        }

        [TestMethod]
        public void SaveToLevelsFolder_WithInvalidName_ThrowsException()
        {
            var config = new LevelConfig
            {
                Drones = new List<GridPointConfig> { Point(1, 1) },
                Weeds = new List<GridPointConfig> { Point(2, 2) }
            };

            Assert.ThrowsException<InvalidOperationException>(() => LevelConfigLoader.SaveToLevelsFolder("   ", config));
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

        private static GridPointConfig Point(int x, int y)
        {
            return new GridPointConfig { X = x, Y = y };
        }
    }
}
