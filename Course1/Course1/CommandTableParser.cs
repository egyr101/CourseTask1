using System;
using System.Collections.Generic;

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
            return text.Trim() switch
            {
                "Все" => DroneTarget.All,

                "Красный" => DroneTarget.Red,
                "Красный дрон" => DroneTarget.Red,

                "Зелёный" => DroneTarget.Green,
                "Зеленый" => DroneTarget.Green,
                "Зелёный дрон" => DroneTarget.Green,
                "Зеленый дрон" => DroneTarget.Green,

                _ => throw new InvalidOperationException($"Неизвестный адресат команды: {text}")
            };
        }

        private static DroneCommandType ParseAction(string text)
        {
            return text.Trim() switch
            {
                "Вперёд" => DroneCommandType.MoveForward,
                "Вперед" => DroneCommandType.MoveForward,

                "Налево" => DroneCommandType.TurnLeft,
                "Направо" => DroneCommandType.TurnRight,

                "Разряд" => DroneCommandType.Attack,

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
