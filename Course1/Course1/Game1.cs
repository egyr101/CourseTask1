using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Myra;
using System;
using System.IO;

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

        private void CreateFixedWeeds()
        {
            _mapRenderer.WeedField.Clear();

            // Фиксированные позиции нужны для предсказуемых учебных тестов.
            // Красный дрон уничтожает три сорняка, зелёный — два.
            _mapRenderer.WeedField.Add(new Vector2(3, 5));
            _mapRenderer.WeedField.Add(new Vector2(4, 5));
            _mapRenderer.WeedField.Add(new Vector2(4, 4));
            _mapRenderer.WeedField.Add(new Vector2(11, 8));
            _mapRenderer.WeedField.Add(new Vector2(12, 8));
        }

        protected override void Initialize()
        {
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

            // Сорняки фиксированы, чтобы тестовые алгоритмы всегда работали одинаково.
            CreateFixedWeeds();

            // 2. Создаем UI
            _commandExecutor = new DroneCommandExecutor(_mapRenderer);

            _uiManager = new UIManager(_mapRenderer);
            _commandExecutor.ErrorOccurred += error =>
            {
                _uiManager.ShowError(error);
                _uiManager.SetRunButtonEnabled(true);
            };

            _commandExecutor.Completed += result =>
            {
                _uiManager.ShowAlgorithmResult(result);
                _uiManager.SetRunButtonEnabled(true);
            };
            _commandExecutor.ChargesChanged += charges => _uiManager.UpdateDroneCharges(charges);
            _uiManager.DroneSpeedChanged += speed => Drone.MoveSpeedMultiplier = speed;
            _uiManager.AlgorithmResultClosed += () => _commandExecutor.RestoreMapToInitialState();
            _uiManager.UpdateDroneCharges(_commandExecutor.GetChargeInfo());
            _uiManager.RunRequested += rows =>
            {
                if (_commandExecutor.IsRunning)
                    return;

                _uiManager.HideMessage();
                _uiManager.SetRunButtonEnabled(false);

                _commandExecutor.Start(rows);

                if (!_commandExecutor.IsRunning)
                {
                    _uiManager.SetRunButtonEnabled(true);
                }
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