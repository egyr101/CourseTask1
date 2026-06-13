using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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

        public static string SaveToLevelsFolder(string mapName, LevelConfig config)
        {
            Validate(config);

            string safeName = CreateSafeMapFileName(mapName);
            Directory.CreateDirectory(LevelsDirectory);

            string path = Path.Combine(LevelsDirectory, safeName);

            var options = new JsonSerializerOptions
            {
                WriteIndented = true
            };

            string json = JsonSerializer.Serialize(config, options);
            File.WriteAllText(path, json);

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

        private static string CreateSafeMapFileName(string mapName)
        {
            if (string.IsNullOrWhiteSpace(mapName))
                throw new InvalidOperationException("Название карты не указано.");

            string nameWithoutExtension = mapName.Trim();

            if (nameWithoutExtension.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
            {
                nameWithoutExtension = nameWithoutExtension.Substring(0, nameWithoutExtension.Length - ".json".Length);
            }

            if (string.IsNullOrWhiteSpace(nameWithoutExtension))
                throw new InvalidOperationException("Название карты не указано.");

            var invalidChars = Path.GetInvalidFileNameChars().ToHashSet();
            var result = new List<char>();

            foreach (char ch in nameWithoutExtension)
            {
                if (invalidChars.Contains(ch))
                {
                    result.Add('_');
                }
                else if (char.IsWhiteSpace(ch))
                {
                    result.Add('_');
                }
                else
                {
                    result.Add(ch);
                }
            }

            string safeName = new string(result.ToArray()).Trim('_');

            if (string.IsNullOrWhiteSpace(safeName))
                throw new InvalidOperationException("Название карты содержит только недопустимые символы.");

            return safeName + ".json";
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
