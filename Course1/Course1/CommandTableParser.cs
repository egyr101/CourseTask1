using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace DroneSimulator
{
    public static class CommandTableParser
    {
        public static IEnumerable<List<DroneCommand>> Parse(IEnumerable<CommandRow> rows)
        {
            foreach (var row in rows)
            {
                var tick = new List<DroneCommand>();

                TryAddCommand(tick, row.Target1, row.Action1, row.Argument1);
                TryAddCommand(tick, row.Target2, row.Action2, row.Argument2);

                if (tick.Count > 0)
                    yield return tick;
            }
        }

        private static void TryAddCommand(
            List<DroneCommand> commands,
            string targetText,
            string actionText,
            string argumentText)
        {
            if (string.IsNullOrWhiteSpace(targetText) ||
                string.IsNullOrWhiteSpace(actionText))
            {
                return;
            }

            commands.Add(new DroneCommand(
                ParseTarget(targetText),
                ParseAction(actionText),
                ParseRepeat(argumentText)));
        }

        private static DroneTarget ParseTarget(string text)
        {
            string normalized = text.Trim();

            if (normalized == "Все" || normalized.Equals("All", StringComparison.OrdinalIgnoreCase))
                return DroneTarget.All;

            if (normalized == "Красный" || normalized == "Красный дрон" ||
                normalized.Equals("Red", StringComparison.OrdinalIgnoreCase) ||
                normalized.Equals("Red drone", StringComparison.OrdinalIgnoreCase))
            {
                return DroneTarget.ForDrone(0);
            }

            if (normalized == "Зелёный" || normalized == "Зеленый" ||
                normalized == "Зелёный дрон" || normalized == "Зеленый дрон" ||
                normalized.Equals("Green", StringComparison.OrdinalIgnoreCase) ||
                normalized.Equals("Green drone", StringComparison.OrdinalIgnoreCase))
            {
                return DroneTarget.ForDrone(1);
            }

            var russianMatch = Regex.Match(normalized, "^Дрон\\s+(\\d+)$", RegexOptions.IgnoreCase);
            if (russianMatch.Success)
                return DroneTarget.ForDrone(int.Parse(russianMatch.Groups[1].Value) - 1);

            var englishMatch = Regex.Match(normalized, "^Drone\\s+(\\d+)$", RegexOptions.IgnoreCase);
            if (englishMatch.Success)
                return DroneTarget.ForDrone(int.Parse(englishMatch.Groups[1].Value) - 1);

            throw new InvalidOperationException($"Неизвестный адресат команды: {text}");
        }

        private static DroneCommandType ParseAction(string text)
        {
            return text.Trim() switch
            {
                "Вперёд" => DroneCommandType.MoveForward,
                "Вперед" => DroneCommandType.MoveForward,
                "Forward" => DroneCommandType.MoveForward,

                "Налево" => DroneCommandType.TurnLeft,
                "Left" => DroneCommandType.TurnLeft,
                "Направо" => DroneCommandType.TurnRight,
                "Right" => DroneCommandType.TurnRight,

                "Разряд" => DroneCommandType.Attack,
                "Attack" => DroneCommandType.Attack,

                _ => throw new InvalidOperationException($"Неизвестная команда: {text}")
            };
        }

        private static int ParseRepeat(string text)
        {
            if (int.TryParse(text, out int repeat) && repeat > 0)
                return repeat;

            return 1;
        }
    }
}
