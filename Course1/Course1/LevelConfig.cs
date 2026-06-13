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
        private const string ConfigFileName = "level_config.json";

        public static LevelConfig LoadFromOutputDirectory()
        {
            string path = Path.Combine(AppContext.BaseDirectory, ConfigFileName);

            if (!File.Exists(path))
                throw new FileNotFoundException($"Файл конфигурации уровня не найден: {path}");

            string json = File.ReadAllText(path);

            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            var config = JsonSerializer.Deserialize<LevelConfig>(json, options);

            if (config == null)
                throw new InvalidOperationException("Не удалось прочитать конфигурацию уровня.");

            if (config.Drones.Count == 0)
                throw new InvalidOperationException("В конфигурации уровня должен быть хотя бы один дрон.");

            return config;
        }
    }
}
