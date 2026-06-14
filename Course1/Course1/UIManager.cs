using Microsoft.Xna.Framework;
using Myra.Graphics2D;
using Myra.Graphics2D.UI;
using Myra.Graphics2D.Brushes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text.RegularExpressions;

namespace DroneSimulator
{
    public enum GameLanguage
    {
        Russian,
        English
    }

    // Класс, описывающий одну строку в нашей таблице
    public class CommandRow
    {
        // Номер тика, к которому относится строка таблицы.
        // Один тик может занимать несколько строк, если дронов больше двух.
        public int TickNumber { get; set; }

        public string Target1 { get; set; } = "";
        public string Action1 { get; set; } = "";
        public string Argument1 { get; set; } = "";
        public string Target2 { get; set; } = "";
        public string Action2 { get; set; } = "";
        public string Argument2 { get; set; } = "";
    }

    public class UIManager
    {
        public event Action<IReadOnlyList<CommandRow>>? RunRequested;
        public event Action<IReadOnlyList<CommandRow>>? StepRunRequested;
        public event Action? AlgorithmResultClosed;
        public event Action<float>? DroneSpeedChanged;
        public event Action<GameLanguage>? LanguageChanged;
        public event Action? LoadMapRequested;
        public event Action<string, LevelConfig>? MapEditorSaveRequested;
        public event Action<string, IReadOnlyList<CommandRow>>? AlgorithmSaveRequested;
        public event Action? AlgorithmLoadRequested;
        public event Action? HelpRequested;
        public event Action? ResetMapRequested;

        private Desktop _desktop;
        private Grid _tableGrid;
        private ScrollViewer _tableScroll;
        private int _selectedRowIndex = 0; // По умолчанию выделена первая строка (индекс 0)
        private SolidBrush _selectedRowBrush = new SolidBrush(new Color(200, 235, 200)); // Мягкий зелёный цвет выделения
        // Наша "База данных" таблицы
        private List<CommandRow> _tableData = new List<CommandRow>();

        private Panel _messagePanel;
        private Label _messageTitleLabel;
        private Label _messageTextLabel;
        private Label _messageCloseLabel;
        private bool _shouldRollbackMapWhenMessageClosed;

        private Panel _filePanel;
        private Panel _testAlgorithmsPanel;
        private Panel _settingsPanel;
        private Panel _saveAlgorithmPanel;
        private TextBox _algorithmNameTextBox;
        private Label _algorithmSaveStatusLabel;
        private MapEditorWindow _mapEditorWindow;
        private MapRenderer _mapRendererForEditor;
        private Widget _mainContent;

        private GameLanguage _language = GameLanguage.Russian;
        private float _speedMultiplier = 1f;

        private TextButton _fileButton;
        private TextButton _loadMapButton;
        private TextButton _openMapEditorButton;
        private TextButton _saveAlgorithmButton;
        private TextButton _loadAlgorithmButton;

        private TextButton _loadTestsButton;
        private TextButton _runButton;
        private TextButton _stepRunButton;
        private TextButton _resetMapButton;
        private bool _runButtonAllowedByExecutor = true;
        private bool _stepRunButtonAllowedByExecutor = true;
        private bool _testAlgorithmsButtonAllowedByExecutor = true;
        private bool _canRunAlgorithm = true;
        private bool _canStepRunAlgorithm = true;
        private bool _canLoadTestAlgorithms = true;
        private bool _canResetMap = true;
        private bool _isMapEditorOpen;
        private TextButton _helpButton;
        private TextButton _settingsButton;

        private TextButton _successTestButton;
        private TextButton _collisionTestButton;
        private TextButton _boundaryTestButton;
        private TextButton _incompleteWeedsTestButton;

        private Label _settingsLanguageLabel;
        private Label _settingsSpeedLabel;
        private Label _settingsCurrentLanguageLabel;
        private Label _settingsCurrentSpeedLabel;

        private Label _addresseeTitleLabel;
        private Label _commandsTitleLabel;
        private TextButton _targetAllButton;
        private readonly List<TextButton> _targetDroneButtons = new List<TextButton>();
        private int _droneTargetCount;
        private TextButton _forwardButton;
        private TextButton _attackButton;
        private TextButton _leftButton;
        private TextButton _rightButton;

        private TextButton _addRowButton;
        private TextButton _deleteRowButton;
        private TextButton _clearTableButton;

        private VerticalStackPanel _chargeInfoPanel;
        private readonly List<Label> _chargeInfoLabels = new List<Label>();
        private IReadOnlyList<DroneChargeInfo> _lastChargeInfos = new List<DroneChargeInfo>();

        // Цветовая палитра
        private IBrush _bgGreen;
        private IBrush _headerGreen;
        private IBrush _btnGreen;
        private IBrush _btnDark; // Скругленная темная кисть для кнопок команд

        // Добавь вспомогательный метод инициализации кистей (вызовем его в конструкторе):
        private void InitBrushes()
        {
            var device = Myra.MyraEnvironment.Game.GraphicsDevice;

            _bgGreen = new SolidBrush(new Color(225, 240, 225));
            _headerGreen = new SolidBrush(new Color(30, 145, 80));

            // Создаем скругленные зеленые кнопки (радиус 6 пикселей)
            _btnGreen = CreateRoundedBrush(device, 6, new Color(60, 180, 120));

            // Создаем скругленные темные кнопки (радиус 6 пикселей)
            _btnDark = CreateRoundedBrush(device, 6, new Color(45, 45, 45));
        }

        public GameLanguage CurrentLanguage => _language;
        public float CurrentSpeedMultiplier => _speedMultiplier;

        public UIManager(MapRenderer mapRenderer, GameLanguage language = GameLanguage.Russian, float speedMultiplier = 1f)
        {
            InitBrushes();
            _desktop = new Desktop();
            _mapRendererForEditor = mapRenderer;
            _droneTargetCount = mapRenderer.Drones.Count;
            _language = language;
            _speedMultiplier = speedMultiplier;

            var rootContainer = new VerticalStackPanel { Background = _bgGreen };

            rootContainer.Widgets.Add(CreateTopMenu());

            _filePanel = CreateFilePanel();
            rootContainer.Widgets.Add(_filePanel);

            _testAlgorithmsPanel = CreateTestAlgorithmsPanel();
            rootContainer.Widgets.Add(_testAlgorithmsPanel);

            _settingsPanel = CreateSettingsPanel();
            rootContainer.Widgets.Add(_settingsPanel);

            _saveAlgorithmPanel = CreateSaveAlgorithmPanel();
            rootContainer.Widgets.Add(_saveAlgorithmPanel);

            _messagePanel = CreateMessagePanel();
            rootContainer.Widgets.Add(_messagePanel);

            _mapEditorWindow = CreateMapEditorWindow(mapRenderer);
            rootContainer.Widgets.Add(_mapEditorWindow);

            _mainContent = CreateMainContent(mapRenderer);
            rootContainer.Widgets.Add(_mainContent);

            _desktop.Root = rootContainer;

            UpdateLanguageTexts();

            // Добавляем первую пустую строку при запуске
            AddEmptyRow();
        }

        private Panel CreateMessagePanel()
        {
            _messageTitleLabel = new Label
            {
                Text = "Ошибка выполнения",
                TextColor = Color.White,
                Margin = new Myra.Graphics2D.Thickness(8, 4)
            };

            _messageTextLabel = new Label
            {
                Text = string.Empty,
                TextColor = Color.White,
                Margin = new Myra.Graphics2D.Thickness(8, 4)
            };

            // Используем Grid + Label вместо TextButton.
            // В некоторых версиях Myra текст TextButton внутри небольшого информационного окна
            // может не отрисовываться корректно, хотя сама серая кнопка видна.
            // Отдельный Label внутри панели гарантированно показывает надпись.
            var closeButton = new Grid
            {
                Width = 140,
                Height = 44,
                Margin = new Myra.Graphics2D.Thickness(8, 4),
                Background = _btnDark
            };

            _messageCloseLabel = new Label
            {
                Text = "Закрыть",
                TextColor = Color.White,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };

            closeButton.Widgets.Add(_messageCloseLabel);

            closeButton.TouchDown += (s, a) => CloseMessage();

            var content = new VerticalStackPanel
            {
                Spacing = 4,
                Padding = new Myra.Graphics2D.Thickness(8)
            };

            content.Widgets.Add(_messageTitleLabel);
            content.Widgets.Add(_messageTextLabel);
            content.Widgets.Add(closeButton);

            var panel = new Panel
            {
                Background = new SolidBrush(new Color(170, 55, 45)),
                Margin = new Myra.Graphics2D.Thickness(10, 8),
                Height = 155,
                Visible = false
            };

            panel.Widgets.Add(content);
            return panel;
        }

        public void ShowError(string message, bool includeRestoreText = true)
        {
            HideTopPanelsBeforeShowingMessage();
            _shouldRollbackMapWhenMessageClosed = false;
            _messagePanel.Background = new SolidBrush(new Color(170, 55, 45));
            _messageTitleLabel.Text = _language == GameLanguage.Russian
                ? "Ошибка"
                : "Error";

            if (includeRestoreText)
            {
                _messageTextLabel.Text = _language == GameLanguage.Russian
                    ? message + " Карта возвращена в начальное состояние."
                    : message + " The map has been restored to the initial state.";
            }
            else
            {
                _messageTextLabel.Text = message;
            }

            _messagePanel.Visible = true;
        }


        public void ShowInfo(string title, string message)
        {
            HideTopPanelsBeforeShowingMessage();
            _shouldRollbackMapWhenMessageClosed = false;
            _messagePanel.Background = new SolidBrush(new Color(45, 145, 80));
            _messageTitleLabel.Text = title;
            _messageTextLabel.Text = message;
            _messagePanel.Visible = true;
        }

        public void ShowAlgorithmResult(AlgorithmResult result)
        {
            HideTopPanelsBeforeShowingMessage();
            _shouldRollbackMapWhenMessageClosed = true;
            _messagePanel.Background = new SolidBrush(new Color(45, 145, 80));
            _messageTitleLabel.Text = _language == GameLanguage.Russian
                ? "Алгоритм выполнен успешно"
                : "Algorithm completed successfully";

            _messageTextLabel.Text = _language == GameLanguage.Russian
                ? $"Оценка алгоритма - {result.Score}\n" +
                  $"Уничтожено {result.DestroyedWeeds} из {result.InitialWeeds}"
                : $"Algorithm score - {result.Score}\n" +
                  $"Destroyed {result.DestroyedWeeds} of {result.InitialWeeds}";
            _messagePanel.Visible = true;
        }

        public void HideMessage()
        {
            _shouldRollbackMapWhenMessageClosed = false;
            _messagePanel.Visible = false;
        }

        private void CloseMessage()
        {
            bool shouldRollback = _shouldRollbackMapWhenMessageClosed;

            HideMessage();

            if (shouldRollback)
            {
                AlgorithmResultClosed?.Invoke();
            }
        }

        public void UpdateDroneCharges(IReadOnlyList<DroneChargeInfo> chargeInfos)
        {
            _lastChargeInfos = chargeInfos;
            _chargeInfoLabels.Clear();
            _chargeInfoPanel.Widgets.Clear();

            foreach (var info in chargeInfos)
            {
                var label = new Label
                {
                    Text = FormatChargeInfo(info),
                    TextColor = Color.Black,
                    Margin = new Myra.Graphics2D.Thickness(0, 3)
                };

                _chargeInfoLabels.Add(label);
                _chargeInfoPanel.Widgets.Add(label);
            }
        }

        public void SetRunButtonEnabled(bool isEnabled)
        {
            _runButtonAllowedByExecutor = isEnabled;
            RefreshTopActionButtonStates();
        }

        public void SetStepRunButtonEnabled(bool isEnabled)
        {
            _stepRunButtonAllowedByExecutor = isEnabled;
            RefreshTopActionButtonStates();
        }

        public void SetTestAlgorithmsButtonEnabled(bool isEnabled)
        {
            _testAlgorithmsButtonAllowedByExecutor = isEnabled;
            RefreshTopActionButtonStates();
        }

        private void RefreshTopActionButtonStates()
        {
            _canRunAlgorithm = _runButtonAllowedByExecutor && !_isMapEditorOpen;
            _canStepRunAlgorithm = _stepRunButtonAllowedByExecutor && !_isMapEditorOpen;
            _canLoadTestAlgorithms = _testAlgorithmsButtonAllowedByExecutor && !_isMapEditorOpen;
            _canResetMap = !_isMapEditorOpen;

            ApplyTopButtonEnabledState(_runButton, _canRunAlgorithm);
            ApplyTopButtonEnabledState(_stepRunButton, _canStepRunAlgorithm);
            ApplyTopButtonEnabledState(_loadTestsButton, _canLoadTestAlgorithms);
            ApplyTopButtonEnabledState(_resetMapButton, _canResetMap);
        }

        private void ApplyTopButtonEnabledState(TextButton? button, bool isEnabled)
        {
            if (button == null)
                return;

            button.TextColor = isEnabled
                ? Color.White
                : new Color(150, 150, 150);

            button.Background = isEnabled
                ? null
                : new SolidBrush(new Color(40, 40, 40, 120));

            button.OverBackground = button.Background;
            button.PressedBackground = button.Background;
            button.FocusedBackground = button.Background;
        }

        public void Render() => _desktop.Render();

        public IReadOnlyList<CommandRow> GetCommandRows()
        {
            return _tableData
                .Select(row => new CommandRow
                {
                    TickNumber = row.TickNumber,
                    Target1 = row.Target1,
                    Action1 = row.Action1,
                    Argument1 = row.Argument1,
                    Target2 = row.Target2,
                    Action2 = row.Action2,
                    Argument2 = row.Argument2
                })
                .ToList();
        }


        private IReadOnlyList<CommandRow> PrepareCommandRowsForRun()
        {
            CloseAllTopPanels();
            RemoveIncompleteCommandsFromTable();
            return GetCommandRows();
        }

        private void RemoveIncompleteCommandsFromTable()
        {
            var normalizedRows = new List<CommandRow>();
            int newTickNumber = 1;

            foreach (var tickRows in GetContiguousTickGroups(_tableData))
            {
                var commands = new List<CommandCellData>();

                foreach (var row in tickRows)
                {
                    if (IsCompleteCommand(row.Target1, row.Action1))
                    {
                        commands.Add(new CommandCellData(row.Target1, row.Action1, row.Argument1));
                    }

                    if (IsCompleteCommand(row.Target2, row.Action2))
                    {
                        commands.Add(new CommandCellData(row.Target2, row.Action2, row.Argument2));
                    }
                }

                if (commands.Count == 0)
                    continue;

                // Один тик не должен содержать больше отдельных команд, чем дронов на карте.
                // Это защищает таблицу от ручных/старых данных, которые нарушают новое ограничение.
                if (_droneTargetCount > 0 && commands.Count > _droneTargetCount)
                {
                    commands = commands.Take(_droneTargetCount).ToList();
                }

                for (int i = 0; i < commands.Count; i += 2)
                {
                    var row = new CommandRow
                    {
                        TickNumber = newTickNumber,
                        Target1 = commands[i].Target,
                        Action1 = commands[i].Action,
                        Argument1 = commands[i].Argument
                    };

                    if (i + 1 < commands.Count)
                    {
                        row.Target2 = commands[i + 1].Target;
                        row.Action2 = commands[i + 1].Action;
                        row.Argument2 = commands[i + 1].Argument;
                    }

                    normalizedRows.Add(row);
                }

                newTickNumber++;
            }

            _tableData = normalizedRows;

            if (_tableData.Count == 0)
            {
                _tableData.Add(new CommandRow { TickNumber = 1 });
            }

            if (_selectedRowIndex >= _tableData.Count)
            {
                _selectedRowIndex = _tableData.Count - 1;
            }

            RefreshTableUI();
        }

        private sealed class CommandCellData
        {
            public string Target { get; }
            public string Action { get; }
            public string Argument { get; }

            public CommandCellData(string target, string action, string argument)
            {
                Target = target;
                Action = action;
                Argument = argument;
            }
        }

        private static IEnumerable<List<CommandRow>> GetContiguousTickGroups(IEnumerable<CommandRow> rows)
        {
            var currentGroup = new List<CommandRow>();
            int? currentTick = null;
            int fallbackTick = 1;

            foreach (var row in rows)
            {
                int tick = row.TickNumber > 0 ? row.TickNumber : fallbackTick;

                if (currentTick.HasValue && tick != currentTick.Value)
                {
                    yield return currentGroup;
                    currentGroup = new List<CommandRow>();
                }

                currentGroup.Add(row);
                currentTick = tick;
                fallbackTick++;
            }

            if (currentGroup.Count > 0)
            {
                yield return currentGroup;
            }
        }

        private static bool IsCompleteCommand(string target, string action)
        {
            return !string.IsNullOrWhiteSpace(target) &&
                   !string.IsNullOrWhiteSpace(action);
        }

        private void StyleButton(TextButton button, IBrush background, int height = 35)
        {
            button.Background = background;
            button.OverBackground = background;      // При наведении мыши
            button.PressedBackground = background;   // При нажатии
            button.FocusedBackground = background;   // При фокусе (решает проблему с кнопкой "Все")

            button.TextColor = Color.White;
            button.Height = height;
            button.Padding = new Myra.Graphics2D.Thickness(12, 6);
        }

        private void ShowMainContent()
        {
            if (_mainContent != null)
                _mainContent.Visible = true;
        }

        private void HideMainContent()
        {
            if (_mainContent != null)
                _mainContent.Visible = false;
        }

        private void OpenMapEditorMode()
        {
            _isMapEditorOpen = true;
            RefreshTopActionButtonStates();
            HideMainContent();
            _mapEditorWindow.Visible = true;
        }

        private void CloseMapEditorMode()
        {
            if (_mapEditorWindow != null)
                _mapEditorWindow.Visible = false;

            _isMapEditorOpen = false;
            ShowMainContent();
            RefreshTopActionButtonStates();
        }

        private void HideDropDownPanels()
        {
            if (_filePanel != null)
                _filePanel.Visible = false;

            if (_testAlgorithmsPanel != null)
                _testAlgorithmsPanel.Visible = false;

            if (_settingsPanel != null)
                _settingsPanel.Visible = false;

            if (_saveAlgorithmPanel != null)
                _saveAlgorithmPanel.Visible = false;
        }

        private void CloseAllTopPanels()
        {
            HideDropDownPanels();

            CloseMapEditorMode();

            if (_messagePanel != null && _messagePanel.Visible)
                CloseMessage();
        }

        private void HideTopPanelsBeforeShowingMessage()
        {
            HideDropDownPanels();

            CloseMapEditorMode();

            if (_messagePanel != null && _messagePanel.Visible && _shouldRollbackMapWhenMessageClosed)
                CloseMessage();
        }

        private Panel CreateFilePanel()
        {
            var panel = new Panel
            {
                Background = new SolidBrush(new Color(210, 235, 220)),
                Margin = new Myra.Graphics2D.Thickness(10, 6),
                Visible = false
            };

            var content = new HorizontalStackPanel
            {
                Spacing = 8,
                Padding = new Myra.Graphics2D.Thickness(8)
            };

            _loadMapButton = CreateFileMenuButton("Загрузить карту", () =>
            {
                CloseAllTopPanels();
                LoadMapRequested?.Invoke();
            });

            _openMapEditorButton = CreateFileMenuButton("Открыть редактор карт", () =>
            {
                CloseAllTopPanels();
                _mapEditorWindow.ResetFromMap(_mapRendererForEditor);
                OpenMapEditorMode();
            });
            _saveAlgorithmButton = CreateFileMenuButton("Сохранить алгоритм", () =>
            {
                bool shouldShow = !_saveAlgorithmPanel.Visible;
                CloseAllTopPanels();

                if (shouldShow)
                {
                    _algorithmSaveStatusLabel.Text = string.Empty;
                    _algorithmNameTextBox.Text = string.Empty;
                }

                _saveAlgorithmPanel.Visible = shouldShow;
            });

            _loadAlgorithmButton = CreateFileMenuButton("Загрузить алгоритм", () =>
            {
                CloseAllTopPanels();
                AlgorithmLoadRequested?.Invoke();
            });

            content.Widgets.Add(_loadMapButton);
            content.Widgets.Add(_openMapEditorButton);
            content.Widgets.Add(_saveAlgorithmButton);
            content.Widgets.Add(_loadAlgorithmButton);

            panel.Widgets.Add(content);
            return panel;
        }

        private TextButton CreateFileMenuButton(string text, Action onClick)
        {
            var button = new TextButton
            {
                Text = text,
                HorizontalAlignment = HorizontalAlignment.Stretch
            };

            StyleButton(button, _btnDark, 34);
            button.TouchDown += (s, a) => onClick();
            return button;
        }

        private MapEditorWindow CreateMapEditorWindow(MapRenderer mapRenderer)
        {
            var editor = new MapEditorWindow(mapRenderer);

            editor.SaveRequested += (mapName, config) => MapEditorSaveRequested?.Invoke(mapName, config);
            editor.CloseRequested += () =>
            {
                CloseMapEditorMode();
            };

            return editor;
        }

        private Widget CreateTopMenu()
        {
            var menuPanel = new HorizontalStackPanel
            {
                Background = _headerGreen,
                Spacing = 15,
                Padding = new Myra.Graphics2D.Thickness(10, 5)
            };

            _fileButton = CreateTopMenuButton("Файл");
            _fileButton.TouchDown += (s, a) =>
            {
                bool shouldShow = !_filePanel.Visible;
                CloseAllTopPanels();
                _filePanel.Visible = shouldShow;
            };
            menuPanel.Widgets.Add(_fileButton);

            _loadTestsButton = CreateTopMenuButton("Загрузить тестовые алгоритмы");
            _loadTestsButton.TouchDown += (s, a) =>
            {
                if (!_canLoadTestAlgorithms)
                    return;

                bool shouldShow = !_testAlgorithmsPanel.Visible;
                CloseAllTopPanels();
                _testAlgorithmsPanel.Visible = shouldShow;
            };
            menuPanel.Widgets.Add(_loadTestsButton);

            _runButton = CreateTopMenuButton("Выполнить");
            _runButton.TouchDown += (s, a) =>
            {
                if (!_canRunAlgorithm)
                    return;

                RunRequested?.Invoke(PrepareCommandRowsForRun());
            };
            menuPanel.Widgets.Add(_runButton);

            _stepRunButton = CreateTopMenuButton("Выполнить пошагово");
            _stepRunButton.TouchDown += (s, a) =>
            {
                if (!_canStepRunAlgorithm)
                    return;

                StepRunRequested?.Invoke(PrepareCommandRowsForRun());
            };
            menuPanel.Widgets.Add(_stepRunButton);

            _resetMapButton = CreateTopMenuButton("Возврат на начальную позицию");
            _resetMapButton.TouchDown += (s, a) =>
            {
                if (!_canResetMap)
                    return;

                CloseAllTopPanels();
                ResetMapRequested?.Invoke();
            };
            menuPanel.Widgets.Add(_resetMapButton);

            _helpButton = CreateTopMenuButton("Помощь");
            _helpButton.TouchDown += (s, a) =>
            {
                CloseAllTopPanels();
                HelpRequested?.Invoke();
            };
            menuPanel.Widgets.Add(_helpButton);

            _settingsButton = CreateTopMenuButton("Настройки");
            _settingsButton.TouchDown += (s, a) =>
            {
                bool shouldShow = !_settingsPanel.Visible;
                CloseAllTopPanels();
                _settingsPanel.Visible = shouldShow;
            };
            menuPanel.Widgets.Add(_settingsButton);

            return menuPanel;
        }

        private TextButton CreateTopMenuButton(string text)
        {
            return new TextButton
            {
                Text = text,
                Background = null,
                OverBackground = null,
                PressedBackground = null,
                TextColor = Color.White
            };
        }

        private Panel CreateTestAlgorithmsPanel()
        {
            var panel = new Panel
            {
                Background = new SolidBrush(new Color(210, 235, 220)),
                Margin = new Myra.Graphics2D.Thickness(10, 6),
                Visible = false
            };

            var content = new HorizontalStackPanel
            {
                Spacing = 8,
                Padding = new Myra.Graphics2D.Thickness(8)
            };

            _successTestButton = CreateTestAlgorithmButton(
                "Успешный алгоритм",
                CreateSuccessfulAlgorithmRows);
            content.Widgets.Add(_successTestButton);

            _collisionTestButton = CreateTestAlgorithmButton(
                "Столкновение дронов",
                CreateCollisionAlgorithmRows);
            content.Widgets.Add(_collisionTestButton);

            _boundaryTestButton = CreateTestAlgorithmButton(
                "Дрон врезается в границу карты",
                CreateBoundaryCrashAlgorithmRows);
            content.Widgets.Add(_boundaryTestButton);

            _incompleteWeedsTestButton = CreateTestAlgorithmButton(
                "Уничтожены не все сорняки",
                CreateIncompleteWeedAlgorithmRows);
            content.Widgets.Add(_incompleteWeedsTestButton);

            panel.Widgets.Add(content);
            return panel;
        }

        private Panel CreateSettingsPanel()
        {
            var panel = new Panel
            {
                Background = new SolidBrush(new Color(210, 235, 220)),
                Margin = new Myra.Graphics2D.Thickness(10, 6),
                Visible = false
            };

            var content = new VerticalStackPanel
            {
                Spacing = 8,
                Padding = new Myra.Graphics2D.Thickness(8)
            };

            _settingsLanguageLabel = new Label
            {
                Text = "Язык",
                TextColor = Color.Black
            };
            content.Widgets.Add(_settingsLanguageLabel);

            var languageButtons = new HorizontalStackPanel { Spacing = 8 };
            languageButtons.Widgets.Add(CreateSettingsButton("Русский", () => SetLanguage(GameLanguage.Russian)));
            languageButtons.Widgets.Add(CreateSettingsButton("English", () => SetLanguage(GameLanguage.English)));
            content.Widgets.Add(languageButtons);

            _settingsCurrentLanguageLabel = new Label
            {
                Text = "Текущий язык: Русский",
                TextColor = Color.Black
            };
            content.Widgets.Add(_settingsCurrentLanguageLabel);

            _settingsSpeedLabel = new Label
            {
                Text = "Скорость передвижения дронов",
                TextColor = Color.Black,
                Margin = new Myra.Graphics2D.Thickness(0, 8, 0, 0)
            };
            content.Widgets.Add(_settingsSpeedLabel);

            var speedButtons = new HorizontalStackPanel { Spacing = 8 };
            speedButtons.Widgets.Add(CreateSettingsButton("0.5x", () => SetDroneSpeed(0.5f)));
            speedButtons.Widgets.Add(CreateSettingsButton("1x", () => SetDroneSpeed(1f)));
            speedButtons.Widgets.Add(CreateSettingsButton("2x", () => SetDroneSpeed(2f)));
            content.Widgets.Add(speedButtons);

            _settingsCurrentSpeedLabel = new Label
            {
                Text = "Текущая скорость: 1x",
                TextColor = Color.Black
            };
            content.Widgets.Add(_settingsCurrentSpeedLabel);

            panel.Widgets.Add(content);
            return panel;
        }

        private Panel CreateSaveAlgorithmPanel()
        {
            var panel = new Panel
            {
                Background = new SolidBrush(new Color(210, 235, 220)),
                Margin = new Myra.Graphics2D.Thickness(10, 6),
                Visible = false
            };

            var content = new VerticalStackPanel
            {
                Spacing = 8,
                Padding = new Myra.Graphics2D.Thickness(8)
            };

            var titleLabel = new Label
            {
                Text = _language == GameLanguage.Russian ? "Название алгоритма" : "Algorithm name",
                TextColor = Color.Black
            };
            content.Widgets.Add(titleLabel);

            _algorithmNameTextBox = new TextBox
            {
                Text = string.Empty,
                Width = 360,
                Height = 34,
                TextColor = Color.Black,
                Background = new SolidBrush(Color.White)
            };
            content.Widgets.Add(_algorithmNameTextBox);

            _algorithmSaveStatusLabel = new Label
            {
                Text = string.Empty,
                TextColor = new Color(120, 40, 40),
                Width = 520
            };
            content.Widgets.Add(_algorithmSaveStatusLabel);

            var buttons = new HorizontalStackPanel
            {
                Spacing = 8
            };

            var saveButton = CreateSettingsButton(
                _language == GameLanguage.Russian ? "Сохранить" : "Save",
                () =>
                {
                    string algorithmName = _algorithmNameTextBox.Text ?? string.Empty;

                    if (string.IsNullOrWhiteSpace(algorithmName))
                    {
                        _algorithmSaveStatusLabel.Text = _language == GameLanguage.Russian
                            ? "Введите название алгоритма."
                            : "Enter an algorithm name.";
                        return;
                    }

                    AlgorithmSaveRequested?.Invoke(algorithmName, GetCommandRows());
                });
            buttons.Widgets.Add(saveButton);

            var cancelButton = CreateSettingsButton(
                _language == GameLanguage.Russian ? "Отмена" : "Cancel",
                () => _saveAlgorithmPanel.Visible = false);
            buttons.Widgets.Add(cancelButton);

            content.Widgets.Add(buttons);
            panel.Widgets.Add(content);
            return panel;
        }

        public void ShowAlgorithmSaveResult(string message, bool success)
        {
            if (_algorithmSaveStatusLabel != null)
            {
                _algorithmSaveStatusLabel.Text = message;
                _algorithmSaveStatusLabel.TextColor = success
                    ? new Color(30, 120, 60)
                    : new Color(160, 45, 45);
            }
        }

        public void LoadAlgorithmRows(IReadOnlyList<CommandRow> rows)
        {
            CloseAllTopPanels();
            var copiedRows = rows
                .Select(row => new CommandRow
                {
                    TickNumber = row.TickNumber,
                    Target1 = row.Target1,
                    Action1 = row.Action1,
                    Argument1 = row.Argument1,
                    Target2 = row.Target2,
                    Action2 = row.Action2,
                    Argument2 = row.Argument2
                })
                .ToList();

            SetSequentialTickNumbersIfMissing(copiedRows);

            if (copiedRows.Count == 0)
            {
                copiedRows.Add(new CommandRow { TickNumber = 1 });
            }

            _tableData = copiedRows;
            _selectedRowIndex = 0;
            RefreshTableUI();
        }

        private TextButton CreateSettingsButton(string text, Action onClick)
        {
            var button = new TextButton
            {
                Text = text,
                HorizontalAlignment = HorizontalAlignment.Stretch
            };

            StyleButton(button, _btnDark, 34);
            button.TouchDown += (s, a) => onClick();
            return button;
        }

        private void SetLanguage(GameLanguage language)
        {
            _language = language;
            TranslateCommandTableToCurrentLanguage();
            UpdateLanguageTexts();
            RefreshTableUI();
            LanguageChanged?.Invoke(language);
        }

        private void TranslateCommandTableToCurrentLanguage()
        {
            foreach (var row in _tableData)
            {
                row.Target1 = LocalizeTargetFromAnyLanguage(row.Target1);
                row.Target2 = LocalizeTargetFromAnyLanguage(row.Target2);
                row.Action1 = LocalizeActionFromAnyLanguage(row.Action1);
                row.Action2 = LocalizeActionFromAnyLanguage(row.Action2);
            }
        }

        private string LocalizeTargetFromAnyLanguage(string target)
        {
            if (string.IsNullOrWhiteSpace(target))
                return string.Empty;

            string normalized = target.Trim();

            if (normalized == "Все" || normalized.Equals("All", StringComparison.OrdinalIgnoreCase))
                return TargetAllText();

            if (normalized == "Красный" || normalized == "Красный дрон" ||
                normalized.Equals("Red", StringComparison.OrdinalIgnoreCase) ||
                normalized.Equals("Red drone", StringComparison.OrdinalIgnoreCase))
            {
                return TargetDroneText(0);
            }

            if (normalized == "Зелёный" || normalized == "Зеленый" ||
                normalized == "Зелёный дрон" || normalized == "Зеленый дрон" ||
                normalized.Equals("Green", StringComparison.OrdinalIgnoreCase) ||
                normalized.Equals("Green drone", StringComparison.OrdinalIgnoreCase))
            {
                return TargetDroneText(1);
            }

            var russianMatch = Regex.Match(normalized, "^Дрон\\s+(\\d+)$", RegexOptions.IgnoreCase);
            if (russianMatch.Success && int.TryParse(russianMatch.Groups[1].Value, out int russianNumber))
                return FormatDroneTargetNumber(russianNumber);

            var englishMatch = Regex.Match(normalized, "^Drone\\s+(\\d+)$", RegexOptions.IgnoreCase);
            if (englishMatch.Success && int.TryParse(englishMatch.Groups[1].Value, out int englishNumber))
                return FormatDroneTargetNumber(englishNumber);

            return target;
        }

        private string FormatDroneTargetNumber(int droneNumber)
        {
            if (droneNumber < 1)
                return string.Empty;

            return _language == GameLanguage.Russian
                ? $"Дрон {droneNumber}"
                : $"Drone {droneNumber}";
        }

        private string LocalizeActionFromAnyLanguage(string action)
        {
            if (string.IsNullOrWhiteSpace(action))
                return string.Empty;

            string normalized = action.Trim();

            if (normalized == "Вперёд" || normalized == "Вперед" || normalized.Equals("Forward", StringComparison.OrdinalIgnoreCase))
                return ActionForwardText();

            if (normalized == "Налево" || normalized.Equals("Left", StringComparison.OrdinalIgnoreCase))
                return ActionLeftText();

            if (normalized == "Направо" || normalized.Equals("Right", StringComparison.OrdinalIgnoreCase))
                return ActionRightText();

            if (normalized == "Разряд" || normalized.Equals("Attack", StringComparison.OrdinalIgnoreCase))
                return ActionAttackText();

            return action;
        }

        private void SetDroneSpeed(float speedMultiplier)
        {
            _speedMultiplier = speedMultiplier;
            UpdateSettingsStateLabels();
            DroneSpeedChanged?.Invoke(speedMultiplier);
        }

        private void UpdateLanguageTexts()
        {
            if (_language == GameLanguage.Russian)
            {
                _fileButton.Text = "Файл";
                _loadTestsButton.Text = "Загрузить тестовые алгоритмы";
                _runButton.Text = "Выполнить";
                _stepRunButton.Text = "Выполнить пошагово";
                _resetMapButton.Text = "Возврат на начальную позицию";
                _helpButton.Text = "Помощь";
                _settingsButton.Text = "Настройки";
                _messageCloseLabel.Text = "Закрыть";

                _loadMapButton.Text = "Загрузить карту";
                _openMapEditorButton.Text = "Открыть редактор карт";
                _saveAlgorithmButton.Text = "Сохранить алгоритм";
                _loadAlgorithmButton.Text = "Загрузить алгоритм";

                _successTestButton.Text = "Успешный алгоритм";
                _collisionTestButton.Text = "Столкновение дронов";
                _boundaryTestButton.Text = "Дрон врезается в границу карты";
                _incompleteWeedsTestButton.Text = "Уничтожены не все сорняки";

                _addRowButton.Text = "Вставить строку";
                _deleteRowButton.Text = "Удалить строку";
                _clearTableButton.Text = "Очистить таблицу";

                _addresseeTitleLabel.Text = "Адресаты";
                _commandsTitleLabel.Text = "Команды";
            }
            else
            {
                _fileButton.Text = "File";
                _loadTestsButton.Text = "Load test algorithms";
                _runButton.Text = "Run";
                _stepRunButton.Text = "Run step by step";
                _resetMapButton.Text = "Return to initial position";
                _helpButton.Text = "Help";
                _settingsButton.Text = "Settings";
                _messageCloseLabel.Text = "Close";

                _loadMapButton.Text = "Load map";
                _openMapEditorButton.Text = "Open map editor";
                _saveAlgorithmButton.Text = "Save algorithm";
                _loadAlgorithmButton.Text = "Load algorithm";

                _successTestButton.Text = "Successful algorithm";
                _collisionTestButton.Text = "Drone collision";
                _boundaryTestButton.Text = "Drone hits map border";
                _incompleteWeedsTestButton.Text = "Not all weeds destroyed";

                _addRowButton.Text = "Insert row";
                _deleteRowButton.Text = "Delete row";
                _clearTableButton.Text = "Clear table";

                _addresseeTitleLabel.Text = "Targets";
                _commandsTitleLabel.Text = "Commands";
            }

            _targetAllButton.Text = TargetAllText();
            for (int i = 0; i < _targetDroneButtons.Count; i++)
            {
                _targetDroneButtons[i].Text = TargetDroneText(i);
            }
            _forwardButton.Text = ActionForwardText();
            _attackButton.Text = ActionAttackText();
            _leftButton.Text = ActionLeftText();
            _rightButton.Text = ActionRightText();

            UpdateSettingsStateLabels();
            UpdateDroneCharges(_lastChargeInfos);
            _mapEditorWindow?.SetLanguage(_language);
            RefreshTopActionButtonStates();
        }

        private void UpdateSettingsStateLabels()
        {
            if (_language == GameLanguage.Russian)
            {
                _settingsLanguageLabel.Text = "Язык";
                _settingsSpeedLabel.Text = "Скорость передвижения дронов";
                _settingsCurrentLanguageLabel.Text = "Текущий язык: Русский";
                _settingsCurrentSpeedLabel.Text = $"Текущая скорость: {FormatSpeedMultiplier()}";
            }
            else
            {
                _settingsLanguageLabel.Text = "Language";
                _settingsSpeedLabel.Text = "Drone movement speed";
                _settingsCurrentLanguageLabel.Text = "Current language: English";
                _settingsCurrentSpeedLabel.Text = $"Current speed: {FormatSpeedMultiplier()}";
            }
        }

        private string FormatSpeedMultiplier()
        {
            return _speedMultiplier.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture) + "x";
        }

        private string TargetAllText() => _language == GameLanguage.Russian ? "Все" : "All";

        private string TargetDroneText(int index)
        {
            return _language == GameLanguage.Russian
                ? $"Дрон {index + 1}"
                : $"Drone {index + 1}";
        }

        private string TargetForTest(int index)
        {
            if (_droneTargetCount <= 0)
                return TargetDroneText(0);

            return TargetDroneText(Math.Min(index, _droneTargetCount - 1));
        }

        private string ActionForwardText() => _language == GameLanguage.Russian ? "Вперёд" : "Forward";
        private string ActionAttackText() => _language == GameLanguage.Russian ? "Разряд" : "Attack";
        private string ActionLeftText() => _language == GameLanguage.Russian ? "Налево" : "Left";
        private string ActionRightText() => _language == GameLanguage.Russian ? "Направо" : "Right";

        private static bool IsAllTarget(string target)
        {
            string normalized = target.Trim();
            return normalized == "Все" || normalized == "All";
        }

        private string FormatChargeInfo(DroneChargeInfo info)
        {
            string droneName = LocalizeDroneName(info.DroneName);

            if (_language == GameLanguage.Russian)
            {
                return $"{droneName}: {info.CurrentCharges}/{info.InitialCharges} зарядов осталось";
            }

            return $"{droneName}: {info.CurrentCharges}/{info.InitialCharges} charges left";
        }

        private string LocalizeDroneName(string droneName)
        {
            if (_language == GameLanguage.Russian)
                return droneName;

            if (droneName.StartsWith("Дрон "))
                return "Drone " + droneName.Substring("Дрон ".Length);

            return droneName;
        }

        private int MaxRowsPerTick => Math.Max(1, (_droneTargetCount + 1) / 2);

        private int GetNextTickNumber()
        {
            return _tableData.Count == 0
                ? 1
                : _tableData.Max(row => row.TickNumber) + 1;
        }

        private bool IsCommandSlotOccupied(string target, string action)
        {
            return !string.IsNullOrWhiteSpace(target) ||
                   !string.IsNullOrWhiteSpace(action);
        }


        private bool TickContainsAllTarget(int tickNumber)
        {
            return _tableData
                .Where(row => row.TickNumber == tickNumber)
                .Any(row => IsAllTarget(row.Target1) || IsAllTarget(row.Target2));
        }

        private int CountOccupiedSlotsInTick(int tickNumber)
        {
            int count = 0;

            foreach (var row in _tableData.Where(row => row.TickNumber == tickNumber))
            {
                if (IsCommandSlotOccupied(row.Target1, row.Action1))
                    count++;

                if (IsCommandSlotOccupied(row.Target2, row.Action2))
                    count++;
            }

            return count;
        }

        private int CountRowsInTick(int tickNumber)
        {
            return _tableData.Count(row => row.TickNumber == tickNumber);
        }

        private CommandRow CreateNewTickRow()
        {
            var row = new CommandRow { TickNumber = GetNextTickNumber() };
            _tableData.Add(row);
            _selectedRowIndex = _tableData.Count - 1;
            return row;
        }

        private CommandRow GetOrCreateAdditionalRowForTick(int tickNumber)
        {
            if (!TickContainsAllTarget(tickNumber) &&
                CountOccupiedSlotsInTick(tickNumber) < _droneTargetCount &&
                CountRowsInTick(tickNumber) < MaxRowsPerTick)
            {
                var newRow = new CommandRow { TickNumber = tickNumber };
                int insertIndex = _tableData.FindLastIndex(row => row.TickNumber == tickNumber) + 1;
                _tableData.Insert(insertIndex, newRow);
                _selectedRowIndex = insertIndex;
                return newRow;
            }

            return CreateNewTickRow();
        }

        private CommandRow FindRowWithFreeTargetSlotInTick(int tickNumber)
        {
            foreach (var row in _tableData.Where(row => row.TickNumber == tickNumber))
            {
                if (string.IsNullOrWhiteSpace(row.Target1))
                    return row;

                if (!IsAllTarget(row.Target1) && string.IsNullOrWhiteSpace(row.Target2))
                    return row;
            }

            return GetOrCreateAdditionalRowForTick(tickNumber);
        }

        private CommandRow FindRowWithFreeActionSlotInTick(int tickNumber)
        {
            foreach (var row in _tableData.Where(row => row.TickNumber == tickNumber))
            {
                if (string.IsNullOrWhiteSpace(row.Action1))
                    return row;

                if (!IsAllTarget(row.Target1) && string.IsNullOrWhiteSpace(row.Action2))
                    return row;
            }

            return GetOrCreateAdditionalRowForTick(tickNumber);
        }

        private void SelectRow(CommandRow row)
        {
            int index = _tableData.IndexOf(row);
            if (index >= 0)
            {
                _selectedRowIndex = index;
            }
        }

        private void SetSequentialTickNumbersIfMissing(List<CommandRow> rows)
        {
            bool hasTickNumbers = rows.Any(row => row.TickNumber > 0);

            if (hasTickNumbers)
                return;

            for (int i = 0; i < rows.Count; i++)
            {
                rows[i].TickNumber = i + 1;
            }
        }

        private TextButton CreateTestAlgorithmButton(
            string text,
            Func<List<CommandRow>> createRows)
        {
            var button = new TextButton
            {
                Text = text,
                HorizontalAlignment = HorizontalAlignment.Stretch
            };

            StyleButton(button, _btnDark, 34);
            button.TouchDown += (s, a) =>
            {
                if (!_canLoadTestAlgorithms)
                    return;

                LoadTestAlgorithm(createRows());
                CloseAllTopPanels();
            };

            return button;
        }

        private void LoadTestAlgorithm(List<CommandRow> rows)
        {
            CloseMessage();
            SetSequentialTickNumbersIfMissing(rows);
            _tableData = rows;
            _selectedRowIndex = 0;
            RefreshTableUI();
        }

        private CommandRow Row(
            string target1,
            string action1,
            string argument1 = "",
            string target2 = "",
            string action2 = "",
            string argument2 = "")
        {
            return new CommandRow
            {
                Target1 = target1,
                Action1 = action1,
                Argument1 = argument1,
                Target2 = target2,
                Action2 = action2,
                Argument2 = argument2
            };
        }

        private List<CommandRow> CreateSuccessfulAlgorithmRows()
        {
            return new List<CommandRow>
            {
                Row(TargetForTest(0), ActionAttackText(), "", TargetForTest(1), ActionAttackText()),
                Row(TargetForTest(0), ActionForwardText(), "", TargetForTest(1), ActionForwardText()),
                Row(TargetForTest(0), ActionAttackText(), "", TargetForTest(1), ActionAttackText()),
                Row(TargetForTest(0), ActionForwardText()),
                Row(TargetForTest(0), ActionLeftText()),
                Row(TargetForTest(0), ActionAttackText())
            };
        }

        private List<CommandRow> CreateCollisionAlgorithmRows()
        {
            return new List<CommandRow>
            {
                Row(TargetForTest(0), ActionForwardText(), "3", TargetForTest(1), ActionLeftText()),
                Row(TargetForTest(1), ActionForwardText(), "3"),
                Row(TargetForTest(1), ActionLeftText()),
                Row(TargetForTest(1), ActionForwardText(), "5")
            };
        }

        private List<CommandRow> CreateBoundaryCrashAlgorithmRows()
        {
            return new List<CommandRow>
            {
                Row(TargetForTest(0), ActionLeftText()),
                Row(TargetForTest(0), ActionForwardText(), "6")
            };
        }

        private List<CommandRow> CreateIncompleteWeedAlgorithmRows()
        {
            return new List<CommandRow>
            {
                Row(TargetForTest(0), ActionAttackText(), "", TargetForTest(1), ActionAttackText()),
                Row(TargetForTest(0), ActionForwardText(), "", TargetForTest(1), ActionForwardText())
            };
        }

        private Widget CreateChargeInfoPanel()
        {
            _chargeInfoPanel = new VerticalStackPanel
            {
                Spacing = 2,
                Margin = new Myra.Graphics2D.Thickness(0, 10, 0, 0),
                Padding = new Myra.Graphics2D.Thickness(10, 8),
                Background = new SolidBrush(new Color(235, 248, 235))
            };

            return _chargeInfoPanel;
        }

        private Widget CreateMainContent(MapRenderer mapRenderer)
        {
            var mainSplit = new HorizontalStackPanel { Proportions = { new Proportion(ProportionType.Part, 2), new Proportion(ProportionType.Part, 1) } };

            // ЛЕВАЯ ПАНЕЛЬ
            var leftPanel = new VerticalStackPanel { Padding = new Myra.Graphics2D.Thickness(10) };

            var mapImage = new Image { Renderable = new Myra.Graphics2D.TextureAtlases.TextureRegion(mapRenderer.MapTexture) };
            leftPanel.Widgets.Add(mapImage);
            leftPanel.Widgets.Add(CreateChargeInfoPanel());

            mainSplit.Widgets.Add(leftPanel);

            // ПРАВАЯ ПАНЕЛЬ
            var rightPanel = new VerticalStackPanel { Padding = new Myra.Graphics2D.Thickness(10) };
            rightPanel.Widgets.Add(CreateTableToolbar());
            rightPanel.Widgets.Add(CreateTable());
            rightPanel.Widgets.Add(new Panel { Height = 20 });
            rightPanel.Widgets.Add(CreateBottomControls());
            mainSplit.Widgets.Add(rightPanel);

            return mainSplit;
        }

        private Widget CreateTableToolbar()
        {
            var toolbar = new HorizontalStackPanel { Spacing = 5, Margin = new Myra.Graphics2D.Thickness(0, 0, 0, 5) };

            _addRowButton = new TextButton { Text = "Вставить строку" };
            StyleButton(_addRowButton, _btnGreen);
            _addRowButton.TouchDown += (s, a) => AddEmptyRow();

            _deleteRowButton = new TextButton { Text = "Удалить строку" };
            StyleButton(_deleteRowButton, _btnGreen);
            _deleteRowButton.TouchDown += (s, a) => DeleteLastRow();

            _clearTableButton = new TextButton { Text = "Очистить таблицу" };
            StyleButton(_clearTableButton, _btnGreen);
            _clearTableButton.TouchDown += (s, a) => ClearTable();

            toolbar.Widgets.Add(_addRowButton);
            toolbar.Widgets.Add(_deleteRowButton);
            toolbar.Widgets.Add(_clearTableButton);

            return toolbar;
        }

        private Widget CreateTable()
        {
            _tableScroll = new ScrollViewer { Height = 400, ShowVerticalScrollBar = true };
            _tableGrid = new Grid
            {
                ShowGridLines = true,
                GridLinesColor = Color.Gray,
                ColumnsProportions =
    {
        new Proportion(ProportionType.Auto), // № (0)
        new Proportion(ProportionType.Part), // Адресат 1 (1)
        new Proportion(ProportionType.Part), // Действие 1 (2)
        new Proportion(ProportionType.Part), // Аргумент 1 (3) -- НОВЫЙ
        new Proportion(ProportionType.Part), // Адресат 2 (4)
        new Proportion(ProportionType.Part), // Действие 2 (5)
        new Proportion(ProportionType.Part)  // Аргумент 2 (6) -- НОВЫЙ
    }
            };
            _tableScroll.Content = _tableGrid;
            return _tableScroll;
        }
        private void UpdateSelectionVisuals()
        {
            foreach (var widget in _tableGrid.Widgets)
            {
                if (widget is Panel panel)
                {
                    int gridRow = panel.GridRow;
                    int rowIndex = gridRow - 1; // Индекс строки в базе данных

                    if (rowIndex >= 0 && rowIndex < _tableData.Count)
                    {
                        bool isSelected = (rowIndex == _selectedRowIndex);
                        bool isArg = (panel.GridColumn == 3 || panel.GridColumn == 6);
                        bool isNum = (panel.GridColumn == 0);

                        // Просто меняем цвет подложки, не трогая внутренние элементы (текстбоксы)
                        if (isSelected)
                        {
                            panel.Background = _selectedRowBrush;
                        }
                        else
                        {
                            if (isArg)
                                panel.Background = new SolidBrush(Color.LightGray);
                            else if (isNum)
                                panel.Background = new SolidBrush(Color.FromNonPremultiplied(240, 240, 240, 255));
                            else
                                panel.Background = new SolidBrush(Color.White);
                        }
                    }
                }
            }
        }
        // Создает текстовое поле ввода. При изменении текста автоматически обновляет модель данных
        // Редактируемая ячейка (Аргумент)
        private Widget CreateEditableCell(string text, int col, int row, int rowIndex, IBrush backgroundBrush, System.Action<string> onTextChanged)
        {
            var panel = new Panel { GridColumn = col, GridRow = row, Background = backgroundBrush };

            var textBox = new TextBox
            {
                Text = text,
                Margin = new Myra.Graphics2D.Thickness(2),
                Background = null,
                TextColor = Color.Black,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch
            };

            textBox.TextChanged += (s, a) =>
            {
                string cleanText = new string(textBox.Text.Where(char.IsDigit).ToArray());
                if (textBox.Text != cleanText)
                {
                    textBox.Text = cleanText;
                    textBox.CursorPosition = cleanText.Length;
                }
                onTextChanged(cleanText);
            };

            textBox.TouchDown += (s, a) => {
                _selectedRowIndex = rowIndex;
                UpdateSelectionVisuals(); // Быстрое обновление цветов (фокус ввода НЕ теряется!)
            };

            panel.Widgets.Add(textBox);
            return panel;
        }

        // ==========================================
        // ЛОГИКА ТАБЛИЦЫ
        // ==========================================

        private void AddEmptyRow()
        {
            _tableData.Add(new CommandRow { TickNumber = GetNextTickNumber() });
            RefreshTableUI();
        }

        private void DeleteLastRow() // Название метода в тулбаре можно оставить прежним
        {
            if (_tableData.Count > 0)
            {
                _tableData.RemoveAt(_selectedRowIndex);

                // Корректируем индекс выделения после удаления
                _selectedRowIndex = System.Math.Max(0, _selectedRowIndex - 1);

                if (_tableData.Count == 0)
                    AddEmptyRow(); // Если удалили всё, создаем одну чистую строку
                else
                    RefreshTableUI();
            }
        }

        private void ClearTable()
        {
            _tableData.Clear();
            _selectedRowIndex = 0;
            RefreshTableUI();
        }

        // Этот метод полностью перерисовывает таблицу на основе списка _tableData
        private void RefreshTableUI()
        {
            _tableGrid.Widgets.Clear();
            _tableGrid.RowsProportions.Clear();

            // 1. Отрисовка заголовков
            _tableGrid.RowsProportions.Add(new Proportion(ProportionType.Auto));
            string[] headers = _language == GameLanguage.Russian
                ? new[] { "№", "Адресат", "Действие", "Аргумент", "Адресат", "Действие", "Аргумент" }
                : new[] { "#", "Target", "Action", "Argument", "Target", "Action", "Argument" };
            for (int i = 0; i < headers.Length; i++)
            {
                _tableGrid.Widgets.Add(new Label { Text = headers[i], GridColumn = i, GridRow = 0, Margin = new Myra.Graphics2D.Thickness(5), TextColor = Color.Black });
            }

            // 2. Отрисовка данных
            for (int i = 0; i < _tableData.Count; i++)
            {
                int rowNum = i + 1;
                _tableGrid.RowsProportions.Add(new Proportion(ProportionType.Auto));

                var rowData = _tableData[i];
                bool isSelected = (i == _selectedRowIndex);

                // Определение цветов ячеек в зависимости от выделения
                IBrush cellBg = isSelected ? _selectedRowBrush : new SolidBrush(Color.White);
                IBrush argBg = isSelected ? _selectedRowBrush : new SolidBrush(Color.LightGray);
                IBrush numBg = isSelected ? _selectedRowBrush : new SolidBrush(Color.FromNonPremultiplied(240, 240, 240, 255));

                // Номер строки (тоже кликабельный)
                var numLabel = new Label { Text = rowData.TickNumber.ToString(), Margin = new Myra.Graphics2D.Thickness(5), TextColor = Color.Black };
                var numPanel = new Panel { GridColumn = 0, GridRow = rowNum, Background = numBg };
                numPanel.Widgets.Add(numLabel);
                int localIndex = i; // Локальная копия индекса для замыкания
                numPanel.TouchDown += (s, a) => {
                    _selectedRowIndex = localIndex;
                    UpdateSelectionVisuals(); // Замени здесь RefreshTableUI() на UpdateSelectionVisuals()
                };
                _tableGrid.Widgets.Add(numPanel);

                // Передаем localIndex и цвета в каждую ячейку
                _tableGrid.Widgets.Add(CreateTableCell(rowData.Target1, 1, rowNum, localIndex, cellBg));
                _tableGrid.Widgets.Add(CreateTableCell(rowData.Action1, 2, rowNum, localIndex, cellBg));
                _tableGrid.Widgets.Add(CreateEditableCell(rowData.Argument1, 3, rowNum, localIndex, argBg, text => rowData.Argument1 = text));

                _tableGrid.Widgets.Add(CreateTableCell(rowData.Target2, 4, rowNum, localIndex, cellBg));
                _tableGrid.Widgets.Add(CreateTableCell(rowData.Action2, 5, rowNum, localIndex, cellBg));
                _tableGrid.Widgets.Add(CreateEditableCell(rowData.Argument2, 6, rowNum, localIndex, argBg, text => rowData.Argument2 = text));
            }
        }

        // Вспомогательный метод для создания ячейки таблицы
        private Widget CreateTableCell(string text, int col, int row, int rowIndex, IBrush backgroundBrush)
        {
            var panel = new Panel { GridColumn = col, GridRow = row, Background = backgroundBrush };
            panel.Widgets.Add(new Label { Text = text, Margin = new Myra.Graphics2D.Thickness(5), TextColor = Color.Black });

            panel.TouchDown += (s, a) => {
                _selectedRowIndex = rowIndex;
                UpdateSelectionVisuals(); // Быстрое обновление цветов
            };

            return panel;
        }

        // Метод добавления Адресата (вызывается кнопками снизу)
        private void InsertTarget(string target)
        {
            if (_tableData.Count == 0)
                AddEmptyRow();

            var activeRow = _tableData[_selectedRowIndex];
            CommandRow targetRow = activeRow;

            if (string.IsNullOrWhiteSpace(activeRow.Target1))
            {
                activeRow.Target1 = target;
            }
            else if (IsAllTarget(activeRow.Target1))
            {
                targetRow = CreateNewTickRow();
                targetRow.Target1 = target;
            }
            else if (string.IsNullOrWhiteSpace(activeRow.Target2))
            {
                activeRow.Target2 = target;
            }
            else
            {
                targetRow = FindRowWithFreeTargetSlotInTick(activeRow.TickNumber);

                if (string.IsNullOrWhiteSpace(targetRow.Target1))
                {
                    targetRow.Target1 = target;
                }
                else
                {
                    targetRow.Target2 = target;
                }
            }

            SelectRow(targetRow);
            RefreshTableUI();
        }

        private IBrush CreateRoundedBrush(Microsoft.Xna.Framework.Graphics.GraphicsDevice device, int radius, Color color)
        {
            int sliceMargin = radius + 1;
            int size = sliceMargin * 2 + 5;

            var tex = new Microsoft.Xna.Framework.Graphics.Texture2D(device, size, size);
            Color[] data = new Color[size * size];

            for (int x = 0; x < size; x++)
            {
                for (int y = 0; y < size; y++)
                {
                    float dist = 0f;
                    bool isCorner = false;

                    // Находим точное расстояние до центра воображаемой окружности в углах
                    if (x < radius && y < radius)
                    {
                        dist = Vector2.Distance(new Vector2(x, y), new Vector2(radius, radius));
                        isCorner = true;
                    }
                    else if (x >= size - radius && y < radius)
                    {
                        int rx = x - (size - radius);
                        dist = Vector2.Distance(new Vector2(rx, y), new Vector2(0, radius));
                        isCorner = true;
                    }
                    else if (x < radius && y >= size - radius)
                    {
                        int ry = y - (size - radius);
                        dist = Vector2.Distance(new Vector2(x, ry), new Vector2(radius, 0));
                        isCorner = true;
                    }
                    else if (x >= size - radius && y >= size - radius)
                    {
                        int rx = x - (size - radius);
                        int ry = y - (size - radius);
                        dist = Vector2.Distance(new Vector2(rx, ry), new Vector2(0, 0));
                        isCorner = true;
                    }

                    if (isCorner)
                    {
                        if (dist <= radius - 0.5f)
                        {
                            data[x + y * size] = color; // Внутри скругления - сплошной цвет
                        }
                        else if (dist >= radius + 0.5f)
                        {
                            data[x + y * size] = Color.Transparent; // Полная прозрачность вне круга
                        }
                        else
                        {
                            // Математическое сглаживание (Anti-aliasing) на стыке пикселей
                            float alpha = 1.0f - (dist - (radius - 0.5f));

                            // ПРЕДУМНОЖЕНИЕ АЛЬФЫ (Premultiplied Alpha) - убирает белые зазубрины в MonoGame!
                            data[x + y * size] = new Color(
                                (byte)(color.R * alpha),
                                (byte)(color.G * alpha),
                                (byte)(color.B * alpha),
                                (byte)(255 * alpha)
                            );
                        }
                    }
                    else
                    {
                        data[x + y * size] = color; // Тело кнопки
                    }
                }
            }
            tex.SetData(data);

            var thickness = new Myra.Graphics2D.Thickness(sliceMargin);
            return new Myra.Graphics2D.TextureAtlases.NinePatchRegion(tex, new Rectangle(0, 0, size, size), thickness);
        }
        private void InsertAction(string action)
        {
            if (_tableData.Count == 0)
                AddEmptyRow();

            var activeRow = _tableData[_selectedRowIndex];
            CommandRow targetRow = activeRow;

            if (string.IsNullOrWhiteSpace(activeRow.Action1))
            {
                activeRow.Action1 = action;
            }
            else if (IsAllTarget(activeRow.Target1))
            {
                targetRow = CreateNewTickRow();
                targetRow.Action1 = action;
            }
            else if (string.IsNullOrWhiteSpace(activeRow.Action2))
            {
                activeRow.Action2 = action;
            }
            else
            {
                targetRow = FindRowWithFreeActionSlotInTick(activeRow.TickNumber);

                if (string.IsNullOrWhiteSpace(targetRow.Action1))
                {
                    targetRow.Action1 = action;
                }
                else
                {
                    targetRow.Action2 = action;
                }
            }

            SelectRow(targetRow);
            RefreshTableUI();
        }

        // ==========================================
        // НИЖНЯЯ ПАНЕЛЬ (Кнопки)
        // ==========================================

        private Widget CreateBottomControls()
        {
            var controlsLayout = new HorizontalStackPanel { Spacing = 20 };


            // Блок АДРЕСАТЫ
            var addresseePanel = new VerticalStackPanel { Spacing = 5 };
            _addresseeTitleLabel = new Label { Text = "Адресаты", HorizontalAlignment = HorizontalAlignment.Center, TextColor = Color.Black };
            addresseePanel.Widgets.Add(_addresseeTitleLabel);

            _targetAllButton = new TextButton { Text = TargetAllText(), HorizontalAlignment = HorizontalAlignment.Stretch };
            StyleButton(_targetAllButton, _btnDark);
            _targetAllButton.TouchDown += (s, a) => InsertTarget(TargetAllText());

            addresseePanel.Widgets.Add(_targetAllButton);

            _targetDroneButtons.Clear();
            for (int i = 0; i < _droneTargetCount; i++)
            {
                int localIndex = i;
                var droneButton = new TextButton
                {
                    Text = TargetDroneText(localIndex),
                    HorizontalAlignment = HorizontalAlignment.Stretch
                };

                StyleButton(droneButton, _btnDark);
                droneButton.TouchDown += (s, a) => InsertTarget(TargetDroneText(localIndex));

                _targetDroneButtons.Add(droneButton);
                addresseePanel.Widgets.Add(droneButton);
            }

            controlsLayout.Widgets.Add(addresseePanel);

            // Блок КОМАНДЫ
            var commandPanel = new VerticalStackPanel { Spacing = 5 };
            _commandsTitleLabel = new Label { Text = "Команды", HorizontalAlignment = HorizontalAlignment.Center, TextColor = Color.Black };
            commandPanel.Widgets.Add(_commandsTitleLabel);

            var cmdGrid = new Grid { RowSpacing = 5, ColumnSpacing = 5 };
            cmdGrid.ColumnsProportions.Add(new Proportion(ProportionType.Part));
            cmdGrid.ColumnsProportions.Add(new Proportion(ProportionType.Part));

            // Создаем кнопки команд и сразу привязываем к ним логику добавления текста
            _forwardButton = new TextButton { Text = ActionForwardText(), GridRow = 0, GridColumn = 0, GridColumnSpan = 2, HorizontalAlignment = HorizontalAlignment.Stretch };
            _forwardButton.TouchDown += (s, a) => InsertAction(ActionForwardText());
            StyleButton(_forwardButton, _btnDark);

            _attackButton = new TextButton { Text = ActionAttackText(), GridRow = 1, GridColumn = 0, GridColumnSpan = 2, HorizontalAlignment = HorizontalAlignment.Stretch };
            _attackButton.TouchDown += (s, a) => InsertAction(ActionAttackText());
            StyleButton(_attackButton, _btnDark);

            _leftButton = new TextButton { Text = ActionLeftText(), GridRow = 2, GridColumn = 0, HorizontalAlignment = HorizontalAlignment.Stretch };
            _leftButton.TouchDown += (s, a) => InsertAction(ActionLeftText());
            StyleButton(_leftButton, _btnDark);

            _rightButton = new TextButton { Text = ActionRightText(), GridRow = 2, GridColumn = 1, HorizontalAlignment = HorizontalAlignment.Stretch };
            _rightButton.TouchDown += (s, a) => InsertAction(ActionRightText());
            StyleButton(_rightButton, _btnDark);

            cmdGrid.Widgets.Add(_forwardButton);
            cmdGrid.Widgets.Add(_attackButton);
            cmdGrid.Widgets.Add(_leftButton);
            cmdGrid.Widgets.Add(_rightButton);

            commandPanel.Widgets.Add(cmdGrid);
            controlsLayout.Widgets.Add(commandPanel);

            return controlsLayout;
        }
    }
}