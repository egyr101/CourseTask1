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

        private static string GetDroneName(int index)
        {
            return $"Дрон {index + 1}";
        }

        private static Color GetDroneTint(int index)
        {
            // Все дроны используют одну и ту же модель без цветового отличия.
            // Номер дрона рисуется поверх модели на карте.
            return Color.White;
        }

        private void BuildLevelFromConfig(LevelConfig config, Texture2D droneTexture)
        {
            _mapRenderer.Drones.Clear();
            _mapRenderer.WeedField.Clear();

            for (int i = 0; i < config.Drones.Count; i++)
            {
                var drone = new Drone(config.Drones[i].ToVector2(), GetDroneTint(i))
                {
                    Name = GetDroneName(i),
                    Number = i + 1,
                    Texture = droneTexture
                };

                _mapRenderer.Drones.Add(drone);
            }

            foreach (var weed in config.Weeds)
            {
                _mapRenderer.WeedField.Add(weed.ToVector2());
            }
        }

        protected override void Initialize()
        {
            base.Initialize();
        }

        protected override void LoadContent()
        {
            MyraEnvironment.Game = this;

            _mapRenderer = new MapRenderer(GraphicsDevice);

            Texture2D droneTexture = LoadTextureFromContent("red_drone.png");

            LevelConfig levelConfig = LevelConfigLoader.LoadFromOutputDirectory();
            BuildLevelFromConfig(levelConfig, droneTexture);

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
            _mapRenderer.DrawMap();
            GraphicsDevice.Clear(Color.CornflowerBlue);
            _uiManager.Render();
            base.Draw(gameTime);
        }
    }
}
