using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace DroneSimulator
{
    public sealed class LevelConfig
    {
        public List<GridPointConfig> Drones { get; set; } = new();
        public List<GridPointConfig> Weeds { get; set; } = new();
    }

    public sealed class GridPointConfig
    {
        public int X { get; set; }
        public int Y { get; set; }

        public Vector2 ToVector2() => new Vector2(X, Y);
    }

    public static class LevelConfigLoader
    {
        public const string LevelsFolderName = "levels";
        public const string MainLevelFileName = "level_main.json";

        public static string LevelsDirectory =>
            Path.Combine(AppContext.BaseDirectory, LevelsFolderName);

        public static string MainLevelPath =>
            Path.Combine(LevelsDirectory, MainLevelFileName);

        public static LevelConfig LoadMainLevel()
        {
            return LoadFromFile(MainLevelPath);
        }

        public static LevelConfig LoadFromFile(string path)
        {
            LevelConfig config = JsonFileHelper.LoadRequiredJson<LevelConfig>(path, "карты");

            Validate(config);
            return config;
        }

        public static string SaveToLevelsFolder(string mapName, LevelConfig config)
        {
            Validate(config);

            string safeName = FileNameHelper.CreateJsonFileName(
                mapName,
                "Название карты не указано.",
                "Название карты содержит только недопустимые символы.");
            Directory.CreateDirectory(LevelsDirectory);

            string path = Path.Combine(LevelsDirectory, safeName);

            var options = new JsonSerializerOptions
            {
                WriteIndented = true
            };

            JsonFileHelper.SaveJson(path, config, options);

            return path;
        }

        public static void ValidateForEditor(LevelConfig config)
        {
            Validate(config);
        }

        private static void Validate(LevelConfig? config)
        {
            if (config == null)
                throw new InvalidOperationException("Не удалось прочитать файл карты.");

            if (config.Drones == null)
                throw new InvalidOperationException("В файле карты отсутствует раздел \"drones\".");

            if (config.Weeds == null)
                throw new InvalidOperationException("В файле карты отсутствует раздел \"weeds\".");

            if (config.Drones.Count == 0)
                throw new InvalidOperationException("В файле карты должен быть хотя бы один дрон.");

            if (config.Drones.Count > 10)
                throw new InvalidOperationException("На карте не может быть больше 10 дронов.");

            if (config.Weeds.Count < config.Drones.Count)
                throw new InvalidOperationException("Количество сорняков не должно быть меньше количества дронов.");

            EnsureNoDuplicatePoints(config.Drones, "drones");
            EnsureNoDuplicatePoints(config.Weeds, "weeds");
        }

        private static void EnsureNoDuplicatePoints(List<GridPointConfig> points, string sectionName)
        {
            var used = new HashSet<string>();

            foreach (var point in points)
            {
                string key = $"{point.X}:{point.Y}";

                if (!used.Add(key))
                {
                    throw new InvalidOperationException(
                        $"В разделе \"{sectionName}\" повторяется позиция ({point.X}, {point.Y}).");
                }
            }
        }
    }
}
