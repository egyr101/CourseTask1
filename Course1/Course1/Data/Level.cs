
using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading;

namespace Course1.Data
{
    public class Level
    {
        static private Dictionary<string, int> dictScore = new() {
            { "roto", 1},
            { "rota", 1},
            { "step", 2},
        };
        // Карта
        private readonly Map _map;

        // Список дронов
        private readonly List<DroneLogic> _drones;

        // Объект, который выполняет команды для дрона и фиксирует изменения на карте
        private readonly Runner _runner;

        // Количество сорняков
        private int QuantityWeeds { get; set; }

        // Очки
        private int Score;

        // Ошибка, возникшая в ходе выполнения алгоритма
        private Exception Exc { get; set; }

        public Level(Map map, List<DroneLogic> drones, Runner runner)
        {
            _map = map;
            _drones = drones;
            Score = 0;
            _runner = runner;
            QuantityWeeds = _map.QuantityWeeds;
        }

        // Запуск работы алгоритма
        public async Task Start(IEnumerable<IEnumerable<(DroneLogic drone, string action, int repeat)>> alghoritm)
        {
            // Проходимся по всем тикам из алгоритма
            foreach (var tick in alghoritm)
            {
                // Проходимся по всем командам из алгоритма
                foreach (var command in tick)
                {
                    // Добавляем repeat команд
                    for (int _ = 0; _ < command.repeat; _++)
                    {
                        // Добавляем команду в очередь соответствующему дрону
                        command.drone.AddCommand((command.action));
                    }
                }
                // Выполняем все команды в тике
                try
                {
                    await StartTick();
                }
                catch (Exception ex)
                {
                    Exc = ex;
                }
               
            }
            await ShowResult();
        }
        // Запуск работы всех команд для одного тика
        private async Task StartTick()
        {
            // Цикл работает пока хотя бы один дрон имеет команды
            while(_drones.Select((d) => d)
                          .Where((d) => d.QueueCommands.Count > 0).Any())
            {
                // Запускаем по одной команде каждого дрона
                var tasks = _drones.Select((d) => StartDrone(d));
                // Ожидаем, пока завершаться все команды
                await Task.WhenAll(tasks);

                if (_drones.Select((d) => (d.X, d.Y))
                           .Count()
                    != _drones.Select((d) => (d.X, d.Y))
                              .Distinct()
                              .Count()
                    )
                    throw new Exception("Дроны столкнулись");

                await Task.Delay(500);
                // Обновляем карту
                await _map.Show(Score);
            }
            
        }

        // Запуск работы одной команды дрона
        private async Task StartDrone(DroneLogic drone)
        {
            if (drone.QueueCommands.TryDequeue(out string? action))
            {
                try
                {
                    await _runner.Run(action, drone);
                }
                catch (Exception ex)
                {
                    throw new Exception(ex.Message);
                }

                // Вычисление добавочных очков + прибавление к накопу(используется сложная операция, поскольку операции асинхронные и могут блокировать поток)
                int scoreToAdd = dictScore.TryGetValue(action, out int score) ? score : 0;
                Interlocked.Add(ref Score, scoreToAdd);

            }
        }
        // Вывод результатов(пока просто для теста)
        private async Task ShowResult()
        {
            Console.WriteLine(Score);
            if(Exc is not null)
            {
                throw Exc;
            }
            Console.WriteLine("Выполнение прошло без ошибок");
        }
    }
}
