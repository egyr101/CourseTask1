using System;
using System.Collections.Generic;
using System.IO;

namespace DroneSimulator
{
    internal static class FileNameHelper
    {
        public static string CreateJsonFileName(
            string sourceName,
            string emptyNameError,
            string invalidNameError)
        {
            if (string.IsNullOrWhiteSpace(sourceName))
                throw new InvalidOperationException(emptyNameError);

            string nameWithoutExtension = RemoveJsonExtension(sourceName.Trim());

            if (string.IsNullOrWhiteSpace(nameWithoutExtension))
                throw new InvalidOperationException(emptyNameError);

            string safeName = ReplaceInvalidChars(nameWithoutExtension).Trim('_');

            if (string.IsNullOrWhiteSpace(safeName))
                throw new InvalidOperationException(invalidNameError);

            return safeName + ".json";
        }

        private static string RemoveJsonExtension(string fileName)
        {
            return fileName.EndsWith(".json", StringComparison.OrdinalIgnoreCase)
                ? fileName.Substring(0, fileName.Length - ".json".Length)
                : fileName;
        }

        private static string ReplaceInvalidChars(string fileName)
        {
            var invalidChars = new HashSet<char>(Path.GetInvalidFileNameChars());
            var result = new char[fileName.Length];

            for (int i = 0; i < fileName.Length; i++)
            {
                char current = fileName[i];
                result[i] = invalidChars.Contains(current) || char.IsWhiteSpace(current)
                    ? '_'
                    : current;
            }

            return new string(result);
        }
    }
}
