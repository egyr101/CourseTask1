using Microsoft.Xna.Framework;
using Myra.Graphics2D;
using Myra.Graphics2D.UI;
using Myra.Graphics2D.Brushes;
using System;
using System.Collections.Generic;
using System.Linq;

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
        public string Target1 { get; set; } = "";
        public string Action1 { get; set; } = "";
        public string Argument1 { get; set; } = ""; // Новый аргумент 1
        public string Target2 { get; set; } = "";
        public string Action2 { get; set; } = "";
        public string Argument2 { get; set; } = ""; // Новый аргумент 2
    }

    public class UIManager
    {
        public event Action<IReadOnlyList<CommandRow>>? RunRequested;
        public event Action? AlgorithmResultClosed;
        public event Action<float>? DroneSpeedChanged;
        public event Action<GameLanguage>? LanguageChanged;

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

        private Panel _testAlgorithmsPanel;
        private Panel _settingsPanel;

        private GameLanguage _language = GameLanguage.Russian;
        private float _speedMultiplier = 1f;

        private TextButton _loadTestsButton;
        private TextButton _runButton;
        private bool _canRunAlgorithm = true;
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
        private TextButton _targetRedButton;
        private TextButton _targetGreenButton;
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

        public UIManager(MapRenderer mapRenderer)
        {
            InitBrushes();
            _desktop = new Desktop();

            var rootContainer = new VerticalStackPanel { Background = _bgGreen };

            rootContainer.Widgets.Add(CreateTopMenu());

            _testAlgorithmsPanel = CreateTestAlgorithmsPanel();
            rootContainer.Widgets.Add(_testAlgorithmsPanel);

            _settingsPanel = CreateSettingsPanel();
            rootContainer.Widgets.Add(_settingsPanel);

            _messagePanel = CreateMessagePanel();
            rootContainer.Widgets.Add(_messagePanel);

            rootContainer.Widgets.Add(CreateMainContent(mapRenderer));

            _desktop.Root = rootContainer;

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

        public void ShowError(string message)
        {
            _shouldRollbackMapWhenMessageClosed = false;
            _messagePanel.Background = new SolidBrush(new Color(170, 55, 45));
            _messageTitleLabel.Text = _language == GameLanguage.Russian
                ? "Ошибка выполнения алгоритма"
                : "Algorithm execution error";
            _messageTextLabel.Text = _language == GameLanguage.Russian
                ? message + " Карта возвращена в начальное состояние."
                : message + " The map has been restored to the initial state.";
            _messagePanel.Visible = true;
        }

        public void ShowAlgorithmResult(AlgorithmResult result)
        {
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
            _canRunAlgorithm = isEnabled;

            if (_runButton == null)
                return;

            _runButton.TextColor = isEnabled
                ? Color.White
                : new Color(150, 150, 150);

            _runButton.Background = isEnabled
                ? null
                : new SolidBrush(new Color(40, 40, 40, 120));

            _runButton.OverBackground = _runButton.Background;
            _runButton.PressedBackground = _runButton.Background;
            _runButton.FocusedBackground = _runButton.Background;
        }

        public void Render() => _desktop.Render();

        public IReadOnlyList<CommandRow> GetCommandRows()
        {
            return _tableData
                .Select(row => new CommandRow
                {
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
            CloseMessage();
            RemoveIncompleteCommandsFromTable();
            return GetCommandRows();
        }

        private void RemoveIncompleteCommandsFromTable()
        {
            var normalizedRows = new List<CommandRow>();

            foreach (var row in _tableData)
            {
                var commands = new List<CommandRow>();

                if (IsCompleteCommand(row.Target1, row.Action1))
                {
                    commands.Add(new CommandRow
                    {
                        Target1 = row.Target1,
                        Action1 = row.Action1,
                        Argument1 = row.Argument1
                    });
                }

                if (IsCompleteCommand(row.Target2, row.Action2))
                {
                    commands.Add(new CommandRow
                    {
                        Target1 = row.Target2,
                        Action1 = row.Action2,
                        Argument1 = row.Argument2
                    });
                }

                if (commands.Count == 0)
                    continue;

                var normalizedRow = new CommandRow
                {
                    Target1 = commands[0].Target1,
                    Action1 = commands[0].Action1,
                    Argument1 = commands[0].Argument1
                };

                if (commands.Count > 1)
                {
                    normalizedRow.Target2 = commands[1].Target1;
                    normalizedRow.Action2 = commands[1].Action1;
                    normalizedRow.Argument2 = commands[1].Argument1;
                }

                normalizedRows.Add(normalizedRow);
            }

            _tableData = normalizedRows;

            if (_tableData.Count == 0)
            {
                _tableData.Add(new CommandRow());
            }

            if (_selectedRowIndex >= _tableData.Count)
            {
                _selectedRowIndex = _tableData.Count - 1;
            }

            RefreshTableUI();
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

        private Widget CreateTopMenu()
        {
            var menuPanel = new HorizontalStackPanel
            {
                Background = _headerGreen,
                Spacing = 15,
                Padding = new Myra.Graphics2D.Thickness(10, 5)
            };

            _loadTestsButton = CreateTopMenuButton("Загрузить тестовые алгоритмы");
            _loadTestsButton.TouchDown += (s, a) =>
            {
                _testAlgorithmsPanel.Visible = !_testAlgorithmsPanel.Visible;
                _settingsPanel.Visible = false;
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

            _helpButton = CreateTopMenuButton("Помощь");
            menuPanel.Widgets.Add(_helpButton);

            _settingsButton = CreateTopMenuButton("Настройки");
            _settingsButton.TouchDown += (s, a) =>
            {
                _settingsPanel.Visible = !_settingsPanel.Visible;
                _testAlgorithmsPanel.Visible = false;
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
            UpdateLanguageTexts();
            RefreshTableUI();
            LanguageChanged?.Invoke(language);
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
                _loadTestsButton.Text = "Загрузить тестовые алгоритмы";
                _runButton.Text = "Выполнить";
                _helpButton.Text = "Помощь";
                _settingsButton.Text = "Настройки";
                _messageCloseLabel.Text = "Закрыть";

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
                _loadTestsButton.Text = "Load test algorithms";
                _runButton.Text = "Run";
                _helpButton.Text = "Help";
                _settingsButton.Text = "Settings";
                _messageCloseLabel.Text = "Close";

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
            _targetRedButton.Text = TargetRedText();
            _targetGreenButton.Text = TargetGreenText();
            _forwardButton.Text = ActionForwardText();
            _attackButton.Text = ActionAttackText();
            _leftButton.Text = ActionLeftText();
            _rightButton.Text = ActionRightText();

            UpdateSettingsStateLabels();
            UpdateDroneCharges(_lastChargeInfos);
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
        private string TargetRedText() => _language == GameLanguage.Russian ? "Красный" : "Red";
        private string TargetGreenText() => _language == GameLanguage.Russian ? "Зелёный" : "Green";

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
            if (_language == GameLanguage.Russian)
            {
                return $"{info.DroneName} дрон : {info.CurrentCharges}/{info.InitialCharges} зарядов осталось";
            }

            string droneName = info.DroneName switch
            {
                "Красный" => "Red",
                "Зелёный" => "Green",
                _ => info.DroneName
            };

            return $"{droneName} drone: {info.CurrentCharges}/{info.InitialCharges} charges left";
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
                LoadTestAlgorithm(createRows());
                _testAlgorithmsPanel.Visible = false;
            };

            return button;
        }

        private void LoadTestAlgorithm(List<CommandRow> rows)
        {
            CloseMessage();
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
                Row(TargetRedText(), ActionAttackText(), "", TargetGreenText(), ActionAttackText()),
                Row(TargetRedText(), ActionForwardText(), "", TargetGreenText(), ActionForwardText()),
                Row(TargetRedText(), ActionAttackText(), "", TargetGreenText(), ActionAttackText()),
                Row(TargetRedText(), ActionForwardText()),
                Row(TargetRedText(), ActionLeftText()),
                Row(TargetRedText(), ActionAttackText())
            };
        }

        private List<CommandRow> CreateCollisionAlgorithmRows()
        {
            return new List<CommandRow>
            {
                Row(TargetRedText(), ActionForwardText(), "3", TargetGreenText(), ActionLeftText()),
                Row(TargetGreenText(), ActionForwardText(), "3"),
                Row(TargetGreenText(), ActionLeftText()),
                Row(TargetGreenText(), ActionForwardText(), "5")
            };
        }

        private List<CommandRow> CreateBoundaryCrashAlgorithmRows()
        {
            return new List<CommandRow>
            {
                Row(TargetRedText(), ActionLeftText()),
                Row(TargetRedText(), ActionForwardText(), "6")
            };
        }

        private List<CommandRow> CreateIncompleteWeedAlgorithmRows()
        {
            return new List<CommandRow>
            {
                Row(TargetRedText(), ActionAttackText(), "", TargetGreenText(), ActionAttackText()),
                Row(TargetRedText(), ActionForwardText(), "", TargetGreenText(), ActionForwardText())
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
            _tableData.Add(new CommandRow());
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
                var numLabel = new Label { Text = rowNum.ToString(), Margin = new Myra.Graphics2D.Thickness(5), TextColor = Color.Black };
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
            if (_tableData.Count == 0) AddEmptyRow();

            // Записываем в выделенную строку
            var activeRow = _tableData[_selectedRowIndex];

            if (string.IsNullOrEmpty(activeRow.Target1))
            {
                activeRow.Target1 = target;
            }
            // Если первый адресат "Все", то второй адресат заблокирован. Создаем новую строку!
            else if (IsAllTarget(activeRow.Target1))
            {
                var newRow = new CommandRow { Target1 = target };
                _tableData.Add(newRow);
                _selectedRowIndex = _tableData.Count - 1; // Автоматически выделяем новую строку
            }
            else if (string.IsNullOrEmpty(activeRow.Target2))
            {
                activeRow.Target2 = target;
            }
            else
            {
                // Если строка полностью заполнена, создаем новую строку
                var newRow = new CommandRow { Target1 = target };
                _tableData.Add(newRow);
                _selectedRowIndex = _tableData.Count - 1;
            }
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
            if (_tableData.Count == 0) AddEmptyRow();

            var activeRow = _tableData[_selectedRowIndex];

            if (string.IsNullOrEmpty(activeRow.Action1))
            {
                activeRow.Action1 = action;
            }
            // Если первый адресат "Все", то второе действие заблокировано. Создаем новую строку!
            else if (IsAllTarget(activeRow.Target1))
            {
                var newRow = new CommandRow { Action1 = action };
                _tableData.Add(newRow);
                _selectedRowIndex = _tableData.Count - 1;
            }
            else if (string.IsNullOrEmpty(activeRow.Action2))
            {
                activeRow.Action2 = action;
            }
            else
            {
                // Создаем новую строку
                var newRow = new CommandRow { Action1 = action };
                _tableData.Add(newRow);
                _selectedRowIndex = _tableData.Count - 1;
            }
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

            _targetRedButton = new TextButton { Text = TargetRedText(), HorizontalAlignment = HorizontalAlignment.Stretch };
            StyleButton(_targetRedButton, _btnDark);
            _targetRedButton.TouchDown += (s, a) => InsertTarget(TargetRedText());

            _targetGreenButton = new TextButton { Text = TargetGreenText(), HorizontalAlignment = HorizontalAlignment.Stretch };
            StyleButton(_targetGreenButton, _btnDark);
            _targetGreenButton.TouchDown += (s, a) => InsertTarget(TargetGreenText());

            addresseePanel.Widgets.Add(_targetAllButton);
            addresseePanel.Widgets.Add(_targetRedButton);
            addresseePanel.Widgets.Add(_targetGreenButton);
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