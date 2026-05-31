using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Myra;
using System;
using System.IO;
using System.Linq;

namespace DroneSimulator
{
    public class Game1 : Game
    {
        private GraphicsDeviceManager _graphics;
        private MapRenderer _mapRenderer;
        private UIManager _uiManager;
        private DroneCommandExecutor _commandExecutor;

        public Game1()
        {
            _graphics = new GraphicsDeviceManager(this);
            // Разрешение экрана под дизайн
            _graphics.PreferredBackBufferWidth = 1800;
            _graphics.PreferredBackBufferHeight = 1000;
            IsMouseVisible = true;
            Content.RootDirectory = "Content";
        }

        private Texture2D LoadTextureFromContent(string fileName)
        {
            string path = Path.Combine(AppContext.BaseDirectory, "Content", fileName);

            using FileStream stream = File.OpenRead(path);
            return Texture2D.FromStream(GraphicsDevice, stream);
        }

        private void GenerateWeeds()
        {
            var random = new Random();
            int weedCount = random.Next(4, 9);

            _mapRenderer.WeedField.GenerateRandom(
                weedCount,
                _mapRenderer.GridWidth,
                _mapRenderer.GridHeight,
                _mapRenderer.Drones.Select(drone => drone.GridPosition),
                random);
        }

        protected override void Initialize()
        {
            _graphics.PreferredBackBufferWidth = 1280;
            _graphics.PreferredBackBufferHeight = 720;
            _graphics.ApplyChanges();

            // РАЗРЕШАЕМ СВОБОДНО РАСТЯГИВАТЬ ОКНО ИГРЫ МЫШКОЙ:
            Window.AllowUserResizing = true;

            base.Initialize();
        }

        protected override void LoadContent()
        {
            // Инициализация Myra
            MyraEnvironment.Game = this;

            // 1. Создаем карту
            _mapRenderer = new MapRenderer(GraphicsDevice);

            // Загружаем PNG напрямую из папки Content.
            // Так проект не зависит от MGCB/dotnet-mgcb при обычном запуске.
            Texture2D redDroneTex = LoadTextureFromContent("red_drone.png");
            Texture2D greenDroneTex = LoadTextureFromContent("green_drone.png");

            // Создаем дронов на координатах X, Y ячейки
            var redDrone = new Drone(new Vector2(2, 5), Color.White) { Texture = redDroneTex };
            var greenDrone = new Drone(new Vector2(10, 8), Color.White) { Texture = greenDroneTex };

            _mapRenderer.Drones.Add(redDrone);
            _mapRenderer.Drones.Add(greenDrone);

            // Сорняки создаются случайно: количество и позиции не зашиты в MapRenderer или Executor.
            GenerateWeeds();

            // 2. Создаем UI
            _commandExecutor = new DroneCommandExecutor(_mapRenderer);

            _uiManager = new UIManager(_mapRenderer);
            _commandExecutor.ErrorOccurred += error => _uiManager.ShowError(error);
            _commandExecutor.Completed += result => _uiManager.ShowAlgorithmResult(result);
            _commandExecutor.ChargesChanged += charges => _uiManager.UpdateDroneCharges(charges);
            _uiManager.UpdateDroneCharges(_commandExecutor.GetChargeInfo());
            _uiManager.SettingsChanged += (newSettings) =>
            {
                // Здесь настраивается только скорость хода дронов
                // Например: someRunner.Delay = (int)(newSettings.Speed * 1000);
            };
            _uiManager.RunRequested += rows =>
            {
                _uiManager.HideMessage();
                _commandExecutor.Start(rows);
            };
        }

        protected override void Update(GameTime gameTime)
        {
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || Keyboard.GetState().IsKeyDown(Keys.Escape))
                Exit();

            // ОБНОВЛЕНИЕ ДРОНОВ ДЛЯ ПЛАВНОГО ДВИЖЕНИЯ
            if (_mapRenderer != null && _mapRenderer.Drones != null)
            {
                foreach (var drone in _mapRenderer.Drones)
                {
                    drone.Update(gameTime);
                }
            }

            _commandExecutor?.Update(gameTime);

            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime)
        {
            // Сначала перерисовываем игровую карту в текстуру
            _mapRenderer.DrawMap();

            // Очищаем основной экран
            GraphicsDevice.Clear(Color.CornflowerBlue);

            // Рисуем весь интерфейс (он сам вытянет текстуру карты и покажет её слева)
            _uiManager.Render();

            base.Draw(gameTime);
        }
    }
}