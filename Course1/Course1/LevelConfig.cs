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
            if (string.IsNullOrWhiteSpace(path))
                throw new InvalidOperationException("Путь к файлу карты не указан.");

            if (!File.Exists(path))
                throw new FileNotFoundException($"Файл карты не найден: {path}");

            if (!string.Equals(Path.GetExtension(path), ".json", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Файл карты должен иметь расширение .json.");

            string json = File.ReadAllText(path);

            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            LevelConfig? config;

            try
            {
                config = JsonSerializer.Deserialize<LevelConfig>(json, options);
            }
            catch (JsonException ex)
            {
                throw new InvalidOperationException($"Файл карты содержит некорректный JSON: {ex.Message}");
            }

            Validate(config);
            return config!;
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
