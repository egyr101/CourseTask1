using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Course1.Data
{
    public enum State
    {
        Weed,
        Clean,
    }
    public class Map
    {
        private PointOfMap[,] _pointOfMaps;
        public int QuantityWeeds { get;private set; }
        public PointOfMap[,] PointOfMaps
        {
            get => _pointOfMaps;
            private set => _pointOfMaps = value;
        }

        // Создание карты
        public Map(List<(int x, int y)> weeds)
        {
            PointOfMaps = new PointOfMap[10, 20];
            for (int i = 0; i < PointOfMaps.Length / PointOfMaps.GetLength(1); i++)
            {
                for (int j = 0; j < PointOfMaps.GetLength(1); j++)
                {
                    if(weeds.Contains((i, j)))
                    {
                        PointOfMaps[i, j] = new(State.Weed);
                    }
                    else
                    {
                        PointOfMaps[i, j] = new(State.Clean);
                    }
                }
            }
            QuantityWeeds = weeds.Count;
        }

        // Первоначальная настройка позиции дрона
        public async Task SetDrone(DroneLogic drone)
        {
            await PointOfMaps[drone.X, drone.Y].ChangeForDrone();
        }

        // Уничтожение сорняка
        public async Task DestroyWeed(DroneLogic drone)
        {
            if (PointOfMaps[drone.X, drone.Y].State == State.Weed)
            {
                QuantityWeeds--;
                await PointOfMaps[drone.X, drone.Y].CleanPoint();
            }
        }

        // Смена состояния ячейки, при передвижении дрона
        public async Task StepDrone(DroneLogic drone)
        {
            await PointOfMaps[drone.X, drone.Y].ChangeForDrone();
        }

        // Вывести карту
        public async Task Show(int score)
        {
            for(int i = 0; i < PointOfMaps.Length / PointOfMaps.GetLength(1); i++)
            {
                for(int j = 0; j < PointOfMaps.GetLength(1); j++)
                {
                    if (PointOfMaps[i, j].State == State.Clean && !PointOfMaps[i, j].IsStandDrone)
                    {
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.Write(PointOfMaps[i, j] + " ");
                        Console.ResetColor();
                    }
                    else if(PointOfMaps[i, j].State == State.Weed && !PointOfMaps[i, j].IsStandDrone)
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.Write(PointOfMaps[i, j] + " ");
                        Console.ResetColor();
                    }
                    else
                    {
                        Console.ForegroundColor = ConsoleColor.Blue;
                        Console.Write(PointOfMaps[i, j] + " ");
                        Console.ResetColor();
                    }
                }
                Console.WriteLine();
            }
            Console.WriteLine(score);
            await Task.Delay(1);
        }
    }

    public class PointOfMap
    {
        private State _state;
        private bool _isStandDrone = false;

        public State State 
        {
            get;
            private set;
        }
        public bool IsStandDrone
        {
            get;
            private set;
        }

        public PointOfMap(State state)
        {
            State = state;
        }

        // Убирает сорняк в этой точке
        public async Task CleanPoint()
        {
            State = State.Clean;
            await Task.Delay(1);
        }

        // Меняет значение флажка, отвечающего за нахождение дрона в этой точке
        public async Task ChangeForDrone()
        {
            IsStandDrone = !IsStandDrone;
            await Task.Delay(1);
        }

        public override string ToString()
        {
            return "*";
        }
    }
}
