using System;
using System.IO;
using System.Text.Json;

namespace DroneSimulator
{
    internal static class JsonFileHelper
    {
        private static readonly JsonSerializerOptions CaseInsensitiveOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        public static T LoadRequiredJson<T>(string path, string fileKind)
            where T : class
        {
            EnsureReadableJsonFile(path, fileKind);

            try
            {
                string json = File.ReadAllText(path);
                return JsonSerializer.Deserialize<T>(json, CaseInsensitiveOptions)!;
            }
            catch (JsonException ex)
            {
                throw new InvalidOperationException(
                    $"Файл {fileKind} содержит некорректный JSON: {ex.Message}");
            }
        }

        public static void SaveJson<T>(string path, T value, JsonSerializerOptions options)
        {
            string json = JsonSerializer.Serialize(value, options);
            File.WriteAllText(path, json);
        }

        private static void EnsureReadableJsonFile(string path, string fileKind)
        {
            if (string.IsNullOrWhiteSpace(path))
                throw new InvalidOperationException($"Путь к файлу {fileKind} не указан.");

            if (!File.Exists(path))
                throw new FileNotFoundException($"Файл {fileKind} не найден: {path}");

            if (!string.Equals(Path.GetExtension(path), ".json", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException($"Файл {fileKind} должен иметь расширение .json.");
        }
    }
}
