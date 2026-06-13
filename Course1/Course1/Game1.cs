using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Myra;
using System;
using System.Collections.Generic;
using System.IO;
using System.Diagnostics;
using System.Linq;

namespace DroneSimulator
{
    public class Game1 : Game
    {
        private GraphicsDeviceManager _graphics;
        private MapRenderer _mapRenderer;
        private UIManager _uiManager;
        private DroneCommandExecutor _commandExecutor;
        private Texture2D _droneTexture;

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
            return Color.White;
        }

        private void BuildLevelFromConfig(LevelConfig config, Texture2D droneTexture)
        {
            ValidateConfigForCurrentMap(config);

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

        private void ValidateConfigForCurrentMap(LevelConfig config)
        {
            var dronePositions = new HashSet<Point>();
            var weedPositions = new HashSet<Point>();

            foreach (var drone in config.Drones)
            {
                ValidatePointInsideMap(drone.X, drone.Y, "дрона");
                dronePositions.Add(new Point(drone.X, drone.Y));
            }

            foreach (var weed in config.Weeds)
            {
                ValidatePointInsideMap(weed.X, weed.Y, "сорняка");
                var point = new Point(weed.X, weed.Y);
                weedPositions.Add(point);

                if (dronePositions.Contains(point))
                {
                    throw new InvalidOperationException(
                        $"Сорняк не может находиться в стартовой клетке дрона: ({weed.X}, {weed.Y}).");
                }
            }
        }

        private void ValidatePointInsideMap(int x, int y, string objectName)
        {
            if (x < 0 || x >= _mapRenderer.GridWidth || y < 0 || y >= _mapRenderer.GridHeight)
            {
                throw new InvalidOperationException(
                    $"Позиция {objectName} ({x}, {y}) находится за границами карты.");
            }
        }

        private void CreateCommandExecutorAndUi(GameLanguage language = GameLanguage.Russian, float speed = 1f)
        {
            _commandExecutor = new DroneCommandExecutor(_mapRenderer);
            _uiManager = new UIManager(_mapRenderer, language, speed);

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
            _uiManager.DroneSpeedChanged += speedValue => Drone.MoveSpeedMultiplier = speedValue;
            _uiManager.AlgorithmResultClosed += () => _commandExecutor.RestoreMapToInitialState();
            _uiManager.LoadMapRequested += LoadMapFromUserFile;
            _uiManager.MapEditorSaveRequested += SaveMapFromEditor;
            _uiManager.AlgorithmSaveRequested += SaveAlgorithmFromTable;
            _uiManager.AlgorithmLoadRequested += LoadAlgorithmFromUserFile;
            _uiManager.HelpRequested += OpenGuideSite;

            _uiManager.UpdateDroneCharges(_commandExecutor.GetChargeInfo());
            Drone.MoveSpeedMultiplier = speed;

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


        private void OpenGuideSite()
        {
            try
            {
                string guidePath = Path.Combine(AppContext.BaseDirectory, "guid", "index.html");

                if (!File.Exists(guidePath))
                {
                    throw new FileNotFoundException($"Файл руководства не найден: {guidePath}");
                }

                Process.Start(new ProcessStartInfo
                {
                    FileName = guidePath,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                _uiManager.ShowError("Не удалось открыть руководство: " + ex.Message, includeRestoreText: false);
            }
        }

        private void SaveAlgorithmFromTable(string algorithmName, IReadOnlyList<CommandRow> rows)
        {
            if (_commandExecutor != null && _commandExecutor.IsRunning)
            {
                _uiManager.ShowAlgorithmSaveResult(
                    "Нельзя сохранять алгоритм во время выполнения.",
                    success: false);
                return;
            }

            try
            {
                string savedPath = AlgorithmConfigLoader.SaveToAlgorithmsFolder(
                    algorithmName,
                    rows,
                    _mapRenderer.Drones.Count);

                _uiManager.ShowAlgorithmSaveResult(
                    $"Алгоритм сохранён: {Path.GetFileName(savedPath)}",
                    success: true);
            }
            catch (Exception ex)
            {
                _uiManager.ShowAlgorithmSaveResult(ex.Message, success: false);
            }
        }

        private void LoadAlgorithmFromUserFile()
        {
            if (_commandExecutor != null && _commandExecutor.IsRunning)
            {
                _uiManager.ShowError("Нельзя загружать алгоритм во время выполнения.", includeRestoreText: false);
                return;
            }

            string algorithmsDirectory = AlgorithmConfigLoader.AlgorithmsDirectory;
            Directory.CreateDirectory(algorithmsDirectory);

            using var dialog = new System.Windows.Forms.OpenFileDialog
            {
                Title = "Загрузить алгоритм",
                InitialDirectory = algorithmsDirectory,
                Filter = "JSON files (*.json)|*.json|All files (*.*)|*.*",
                CheckFileExists = true,
                Multiselect = false
            };

            var result = dialog.ShowDialog();

            if (result != System.Windows.Forms.DialogResult.OK)
                return;

            try
            {
                AlgorithmConfig config = AlgorithmConfigLoader.LoadFromFile(
                    dialog.FileName,
                    _mapRenderer.Drones.Count);

                List<CommandRow> rows = AlgorithmConfigLoader.ToCommandRows(
                    config,
                    _uiManager.CurrentLanguage,
                    _mapRenderer.Drones.Count);

                _uiManager.LoadAlgorithmRows(rows);
            }
            catch (Exception ex)
            {
                _uiManager.ShowError(ex.Message, includeRestoreText: false);
            }
        }

        private void LoadMapFromUserFile()
        {
            if (_commandExecutor != null && _commandExecutor.IsRunning)
            {
                _uiManager.ShowError("Нельзя загружать карту во время выполнения алгоритма.", includeRestoreText: false);
                return;
            }

            string levelsDirectory = LevelConfigLoader.LevelsDirectory;
            Directory.CreateDirectory(levelsDirectory);

            using var dialog = new System.Windows.Forms.OpenFileDialog
            {
                Title = "Загрузить карту",
                InitialDirectory = levelsDirectory,
                Filter = "JSON files (*.json)|*.json|All files (*.*)|*.*",
                CheckFileExists = true,
                Multiselect = false
            };

            var result = dialog.ShowDialog();

            if (result != System.Windows.Forms.DialogResult.OK)
                return;

            try
            {
                GameLanguage currentLanguage = _uiManager.CurrentLanguage;
                float currentSpeed = _uiManager.CurrentSpeedMultiplier;

                LevelConfig config = LevelConfigLoader.LoadFromFile(dialog.FileName);
                BuildLevelFromConfig(config, _droneTexture);
                CreateCommandExecutorAndUi(currentLanguage, currentSpeed);
            }
            catch (Exception ex)
            {
                _uiManager.ShowError(ex.Message, includeRestoreText: false);
            }
        }

        private void SaveMapFromEditor(string mapName, LevelConfig config)
        {
            if (_commandExecutor != null && _commandExecutor.IsRunning)
            {
                _uiManager.ShowError("Нельзя применять карту во время выполнения алгоритма.", includeRestoreText: false);
                return;
            }

            try
            {
                GameLanguage currentLanguage = _uiManager.CurrentLanguage;
                float currentSpeed = _uiManager.CurrentSpeedMultiplier;

                LevelConfigLoader.ValidateForEditor(config);
                ValidateConfigForCurrentMap(config);
                string savedPath = LevelConfigLoader.SaveToLevelsFolder(mapName, config);

                BuildLevelFromConfig(config, _droneTexture);
                CreateCommandExecutorAndUi(currentLanguage, currentSpeed);
                _uiManager.ShowInfo("Карта сохранена", $"Файл: {Path.GetFileName(savedPath)}");
            }
            catch (Exception ex)
            {
                _uiManager.ShowError(ex.Message, includeRestoreText: false);
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
            _droneTexture = LoadTextureFromContent("red_drone.png");

            LevelConfig levelConfig = LevelConfigLoader.LoadMainLevel();
            BuildLevelFromConfig(levelConfig, _droneTexture);

            CreateCommandExecutorAndUi();
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
