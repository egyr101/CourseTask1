using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace DroneSimulator
{
    public sealed class AlgorithmConfig
    {
        // Количество дронов на карте, для которой был сохранён алгоритм.
        // Это защищает от загрузки алгоритма на карту с другим числом дронов.
        [JsonPropertyName("numberDronesOnMap")]
        public int NumberDronesOnMap { get; set; }

        public List<AlgorithmTickConfig> Ticks { get; set; } = new();
    }

    public sealed class AlgorithmTickConfig
    {
        public List<AlgorithmCommandConfig> Commands { get; set; } = new();
    }

    public sealed class AlgorithmCommandConfig
    {
        // "all" или "drone".
        public string Target { get; set; } = string.Empty;

        // Номер дрона, начиная с 1. Используется только если Target = "drone".
        public int? DroneNumber { get; set; }

        // "forward", "left", "right", "attack".
        public string Action { get; set; } = string.Empty;

        // Количество повторений команды. Если не указано или меньше 1, считается 1.
        public int Repeat { get; set; } = 1;
    }

    public static class AlgorithmConfigLoader
    {
        public const string AlgorithmsFolderName = "algorithms";

        public static string AlgorithmsDirectory =>
            Path.Combine(AppContext.BaseDirectory, AlgorithmsFolderName);

        public static string SaveToAlgorithmsFolder(
            string algorithmName,
            IEnumerable<CommandRow> rows,
            int droneCount)
        {
            AlgorithmConfig config = FromCommandRows(rows, droneCount);
            Validate(config, droneCount);

            string safeName = FileNameHelper.CreateJsonFileName(
                algorithmName,
                "Название алгоритма не указано.",
                "Название алгоритма содержит только недопустимые символы.");
            Directory.CreateDirectory(AlgorithmsDirectory);

            string path = Path.Combine(AlgorithmsDirectory, safeName);

            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };

            JsonFileHelper.SaveJson(path, config, options);

            return path;
        }

        public static AlgorithmConfig LoadFromFile(string path, int droneCount)
        {
            AlgorithmConfig config = JsonFileHelper.LoadRequiredJson<AlgorithmConfig>(path, "алгоритма");

            Validate(config, droneCount);
            return config;
        }

        public static List<CommandRow> ToCommandRows(
            AlgorithmConfig config,
            GameLanguage language,
            int droneCount)
        {
            Validate(config, droneCount);

            var result = new List<CommandRow>();
            int tickNumber = 1;

            foreach (var tick in config.Ticks)
            {
                for (int i = 0; i < tick.Commands.Count; i += 2)
                {
                    var row = new CommandRow
                    {
                        TickNumber = tickNumber
                    };

                    FillCommandSlot(row, tick.Commands[i], language, firstSlot: true);

                    if (i + 1 < tick.Commands.Count)
                    {
                        FillCommandSlot(row, tick.Commands[i + 1], language, firstSlot: false);
                    }

                    result.Add(row);
                }

                tickNumber++;
            }

            if (result.Count == 0)
                result.Add(new CommandRow { TickNumber = 1 });

            return result;
        }

        private static AlgorithmConfig FromCommandRows(IEnumerable<CommandRow> rows, int droneCount)
        {
            var result = new AlgorithmConfig
            {
                NumberDronesOnMap = droneCount
            };

            foreach (var tick in CommandTableParser.Parse(rows))
            {
                var tickConfig = new AlgorithmTickConfig();

                foreach (var command in tick)
                {
                    var commandConfig = new AlgorithmCommandConfig
                    {
                        Target = command.Target.IsAll ? "all" : "drone",
                        DroneNumber = command.Target.IsAll ? null : command.Target.DroneIndex + 1,
                        Action = ToActionCode(command.Type),
                        Repeat = Math.Max(1, command.Repeat)
                    };

                    tickConfig.Commands.Add(commandConfig);
                }

                if (tickConfig.Commands.Count > 0)
                    result.Ticks.Add(tickConfig);
            }

            if (result.Ticks.Count == 0)
                throw new InvalidOperationException("В таблице нет команд для сохранения.");

            Validate(result, droneCount);
            return result;
        }

        private static void FillCommandSlot(
            CommandRow row,
            AlgorithmCommandConfig command,
            GameLanguage language,
            bool firstSlot)
        {
            string targetText = ToTargetText(command, language);
            string actionText = ToActionText(command.Action, language);
            string repeatText = command.Repeat > 1 ? command.Repeat.ToString() : string.Empty;

            if (firstSlot)
            {
                row.Target1 = targetText;
                row.Action1 = actionText;
                row.Argument1 = repeatText;
            }
            else
            {
                row.Target2 = targetText;
                row.Action2 = actionText;
                row.Argument2 = repeatText;
            }
        }

        private static string ToTargetText(AlgorithmCommandConfig command, GameLanguage language)
        {
            if (IsAllTarget(command.Target))
                return language == GameLanguage.Russian ? "Все" : "All";

            int number = command.DroneNumber ?? 1;
            return language == GameLanguage.Russian
                ? $"Дрон {number}"
                : $"Drone {number}";
        }

        private static string ToActionText(string actionCode, GameLanguage language)
        {
            string normalized = NormalizeAction(actionCode);

            return normalized switch
            {
                "forward" => language == GameLanguage.Russian ? "Вперёд" : "Forward",
                "left" => language == GameLanguage.Russian ? "Налево" : "Left",
                "right" => language == GameLanguage.Russian ? "Направо" : "Right",
                "attack" => language == GameLanguage.Russian ? "Разряд" : "Attack",
                _ => throw new InvalidOperationException($"Неизвестное действие алгоритма: {actionCode}")
            };
        }

        private static string ToActionCode(DroneCommandType action)
        {
            return action switch
            {
                DroneCommandType.MoveForward => "forward",
                DroneCommandType.TurnLeft => "left",
                DroneCommandType.TurnRight => "right",
                DroneCommandType.Attack => "attack",
                _ => throw new InvalidOperationException($"Неизвестное действие алгоритма: {action}")
            };
        }

        private static void Validate(AlgorithmConfig? config, int droneCount)
        {
            if (config == null)
                throw new InvalidOperationException("Не удалось прочитать файл алгоритма.");

            if (config.NumberDronesOnMap <= 0)
            {
                throw new InvalidOperationException(
                    "В файле алгоритма отсутствует корректное поле \"numberDronesOnMap\".");
            }

            if (config.NumberDronesOnMap != droneCount)
            {
                throw new InvalidOperationException(
                    $"Алгоритм рассчитан на карту с количеством дронов: {config.NumberDronesOnMap}. " +
                    $"На текущей карте дронов: {droneCount}. Загрузите подходящую карту или выберите другой алгоритм.");
            }

            if (config.Ticks == null)
                throw new InvalidOperationException("В файле алгоритма отсутствует раздел \"ticks\".");

            if (config.Ticks.Count == 0)
                throw new InvalidOperationException("Файл алгоритма не содержит тиков.");

            for (int tickIndex = 0; tickIndex < config.Ticks.Count; tickIndex++)
            {
                var tick = config.Ticks[tickIndex];

                if (tick.Commands == null || tick.Commands.Count == 0)
                {
                    throw new InvalidOperationException(
                        $"Тик {tickIndex + 1} в файле алгоритма не содержит команд.");
                }

                if (tick.Commands.Count > droneCount)
                {
                    throw new InvalidOperationException(
                        $"Тик {tickIndex + 1} содержит больше команд, чем дронов на карте.");
                }

                bool containsAll = tick.Commands.Any(command => IsAllTarget(command.Target));

                if (containsAll && tick.Commands.Count > 1)
                {
                    throw new InvalidOperationException(
                        $"Тик {tickIndex + 1} не может одновременно содержать команду для всех дронов и отдельные команды.");
                }

                foreach (var command in tick.Commands)
                {
                    ValidateCommand(command, tickIndex + 1, droneCount);
                }
            }
        }

        private static void ValidateCommand(
            AlgorithmCommandConfig command,
            int tickNumber,
            int droneCount)
        {
            if (command == null)
                throw new InvalidOperationException($"В тике {tickNumber} есть пустая команда.");

            if (IsAllTarget(command.Target))
            {
                command.DroneNumber = null;
            }
            else if (IsDroneTarget(command.Target))
            {
                if (!command.DroneNumber.HasValue)
                {
                    throw new InvalidOperationException(
                        $"В тике {tickNumber} для цели drone не указан droneNumber.");
                }

                if (command.DroneNumber.Value < 1 || command.DroneNumber.Value > droneCount)
                {
                    throw new InvalidOperationException(
                        $"В тике {tickNumber} указан несуществующий дрон: {command.DroneNumber.Value}.");
                }
            }
            else
            {
                throw new InvalidOperationException(
                    $"В тике {tickNumber} указан неизвестный target: {command.Target}.");
            }

            _ = ToActionText(command.Action, GameLanguage.Russian);

            if (command.Repeat < 1)
                command.Repeat = 1;
        }

        private static bool IsAllTarget(string target)
        {
            return string.Equals(target?.Trim(), "all", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsDroneTarget(string target)
        {
            return string.Equals(target?.Trim(), "drone", StringComparison.OrdinalIgnoreCase);
        }

        private static string NormalizeAction(string action)
        {
            string normalized = (action ?? string.Empty).Trim().ToLowerInvariant();

            return normalized switch
            {
                "forward" => "forward",
                "moveforward" => "forward",
                "move_forward" => "forward",
                "left" => "left",
                "turnleft" => "left",
                "turn_left" => "left",
                "right" => "right",
                "turnright" => "right",
                "turn_right" => "right",
                "attack" => "attack",
                _ => normalized
            };
        }

    }
}
