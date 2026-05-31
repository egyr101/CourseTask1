using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Myra;

namespace DroneSimulator
{
    public class Game1 : Game
    {
        private GraphicsDeviceManager _graphics;
        private MapRenderer _mapRenderer;
        private UIManager _uiManager;

        public Game1()
        {
            _graphics = new GraphicsDeviceManager(this);
            // Разрешение экрана под дизайн
            _graphics.PreferredBackBufferWidth = 1800;
            _graphics.PreferredBackBufferHeight = 1000;
            IsMouseVisible = true;
            Content.RootDirectory = "Content";
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

            // ЗАГРУЗКА ТВОИХ ТЕКСТУР (Убедись, что картинки добавлены в Content Pipeline (MGCB)!)
            // Если текстур пока нет, закомментируй эти 2 строки, дроны просто не нарисуются, но программа не упадет.
            Texture2D redDroneTex = Content.Load<Texture2D>("red_drone");
            Texture2D greenDroneTex = Content.Load<Texture2D>("green_drone");

            // Создаем дронов на координатах X, Y ячейки
            var redDrone = new Drone(new Vector2(2, 5), Color.White) { Texture = redDroneTex };
            var greenDrone = new Drone(new Vector2(10, 8), Color.White) { Texture = greenDroneTex };

            _mapRenderer.Drones.Add(redDrone);
            _mapRenderer.Drones.Add(greenDrone);

            // 2. Создаем UI
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