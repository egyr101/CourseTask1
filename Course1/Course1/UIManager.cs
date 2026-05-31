using Course1;
using Microsoft.Xna.Framework;
using Myra.Graphics2D;
using Myra.Graphics2D.Brushes;
using Myra.Graphics2D.UI;
using Myra.Graphics2D.UI.File;
using Myra.Graphics2D.UI.Styles;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace DroneSimulator
{
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

        private VerticalStackPanel _chargeInfoPanel;
        private readonly List<Label> _chargeInfoLabels = new List<Label>();

        public event Action? SaveAlgorithmRequested;
        public event Action? LoadAlgorithmRequested;
        public event Action<string>? LoadMapRequested; // Должно быть так

        public GameSettings CurrentSettings { get; private set; } = new GameSettings();

        // Событие, на которое подпишется игровой цикл для применения изменений
        public event Action<GameSettings>? SettingsChanged;

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

        private string? ShowOpenFileDialog(string filter, string title)
        {
            string? selectedPath = null;

            // Создаем отдельный поток для работы с проводником
            var thread = new System.Threading.Thread(() =>
            {
                using (var dialog = new System.Windows.Forms.OpenFileDialog())
                {
                    dialog.Filter = filter;
                    dialog.Title = title;
                    if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                    {
                        selectedPath = dialog.FileName;
                    }
                }
            });

            // Обязательно задаем режим STA для работы COM/ActiveX компонентов Windows
            thread.SetApartmentState(System.Threading.ApartmentState.STA);
            thread.Start();

            // Блокируем основной поток игры, пока пользователь не закроет проводник
            thread.Join();

            return selectedPath;
        }

        private string? ShowSaveFileDialog(string filter, string title, string defaultFileName)
        {
            string? selectedPath = null;

            var thread = new System.Threading.Thread(() =>
            {
                using (var dialog = new System.Windows.Forms.SaveFileDialog())
                {
                    dialog.Filter = filter;
                    dialog.Title = title;
                    dialog.FileName = defaultFileName;
                    if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                    {
                        selectedPath = dialog.FileName;
                    }
                }
            });

            thread.SetApartmentState(System.Threading.ApartmentState.STA);
            thread.Start();
            thread.Join();

            return selectedPath;
        }

        // --- ОБНОВЛЕННЫЕ ОСНОВНЫЕ МЕТОДЫ ---

        private void SaveAlgorithm()
        {
            string? filePath = ShowSaveFileDialog("JSON файлы (*.json)|*.json", "Сохранить алгоритм", "algorithm.json");

            if (!string.IsNullOrEmpty(filePath))
            {
                try
                {
                    var options = new JsonSerializerOptions { WriteIndented = true };
                    string jsonString = JsonSerializer.Serialize(_tableData, options);

                    File.WriteAllText(filePath, jsonString);
                }
                catch (Exception ex)
                {
                    ShowError($"Не удалось сохранить алгоритм: {ex.Message}");
                }
            }
        }

        private void LoadAlgorithm()
        {
            string? filePath = ShowOpenFileDialog("JSON файлы (*.json)|*.json", "Открыть файл алгоритма");

            if (!string.IsNullOrEmpty(filePath))
            {
                try
                {
                    if (File.Exists(filePath))
                    {
                        string jsonString = File.ReadAllText(filePath);
                        var loadedData = JsonSerializer.Deserialize<List<CommandRow>>(jsonString);

                        if (loadedData != null)
                        {
                            _tableData = loadedData;
                            _selectedRowIndex = 0;
                            RefreshTableUI();
                        }
                    }
                }
                catch (Exception ex)
                {
                    ShowError($"Не удалось загрузить алгоритм: {ex.Message}");
                }
            }
        }
        private void OpenHelpPage()
        {
            try
            {
                // Укажите здесь нужный URL-адрес или путь к локальному файлу (например, "help.html")
                string helpTarget = "https://github.com";

                var startInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = helpTarget,
                    UseShellExecute = true // Этот флаг обязателен в .NET Core/6/8, чтобы запустить системный браузер
                };

                System.Diagnostics.Process.Start(startInfo);
            }
            catch (Exception ex)
            {
                ShowError($"Не удалось открыть веб-страницу справки: {ex.Message}");
            }
        }
        private void LoadMap()
        {
            string? filePath = ShowOpenFileDialog("Файлы карт (*.json;*.txt)|*.json;*.txt", "Открыть файл карты");

            if (!string.IsNullOrEmpty(filePath))
            {
                LoadMapRequested?.Invoke(filePath);
            }
        }

        public UIManager(MapRenderer mapRenderer)
        {
            InitBrushes();
            _desktop = new Desktop();

            var rootContainer = new VerticalStackPanel { Background = _bgGreen };

            rootContainer.Widgets.Add(CreateTopMenu());

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

            var closeButton = new TextButton
            {
                Text = "Закрыть",
                Width = 110,
                Margin = new Myra.Graphics2D.Thickness(8, 4)
            };
            StyleButton(closeButton, _btnDark, 32);
            closeButton.TouchDown += (s, a) => HideMessage();

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
                Height = 120,
                Visible = false
            };

            panel.Widgets.Add(content);
            return panel;
        }

        public void ShowError(string message)
        {
            _messagePanel.Background = new SolidBrush(new Color(170, 55, 45));
            _messageTitleLabel.Text = "Ошибка выполнения алгоритма";
            _messageTextLabel.Text = message + " Карта возвращена в начальное состояние.";
            _messagePanel.Visible = true;
        }

        public void ShowAlgorithmResult(AlgorithmResult result)
        {
            _messagePanel.Background = new SolidBrush(new Color(45, 145, 80));
            _messageTitleLabel.Text = "Алгоритм выполнен успешно";
            _messageTextLabel.Text =
                $"Оценка алгоритма - {result.Score}\n" +
                $"Уничтожено {result.DestroyedWeeds} из {result.InitialWeeds}";
            _messagePanel.Visible = true;
        }

        public void HideMessage()
        {
            _messagePanel.Visible = false;
        }

        public void UpdateDroneCharges(IReadOnlyList<DroneChargeInfo> chargeInfos)
        {
            _chargeInfoLabels.Clear();
            _chargeInfoPanel.Widgets.Clear();

            foreach (var info in chargeInfos)
            {
                var label = new Label
                {
                    Text = $"{info.DroneName} дрон : {info.CurrentCharges}/{info.InitialCharges} зарядов осталось",
                    TextColor = Color.Black,
                    Margin = new Myra.Graphics2D.Thickness(0, 3)
                };

                _chargeInfoLabels.Add(label);
                _chargeInfoPanel.Widgets.Add(label);
            }
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
            // Используем HorizontalMenu вместо HorizontalStackPanel для поддержки вложенности
            var topMenu = new HorizontalMenu
            {
                Background = _headerGreen,
                Padding = new Myra.Graphics2D.Thickness(10, 0) // Корректируем внутренние отступы
            };

            // 1 Выпадающее меню "Файл"
            var fileMenu = new MenuItem { Text = "Файл" };

            var saveAlgoItem = new MenuItem { Text = "Сохранить алгоритм" };
            saveAlgoItem.Selected += (s, a) => SaveAlgorithm(); // Вызываем локальный метод сохранения
            fileMenu.Items.Add(saveAlgoItem);

            var loadAlgoItem = new MenuItem { Text = "Загрузить алгоритм" };
            loadAlgoItem.Selected += (s, a) => LoadAlgorithm(); // Вызываем локальный метод загрузки
            fileMenu.Items.Add(loadAlgoItem);

            var loadMapItem = new MenuItem { Text = "Загрузить карту" };
            loadMapItem.Selected += (s, a) => LoadMap();       // Вызываем локальный метод выбора карты
            fileMenu.Items.Add(loadMapItem);

            topMenu.Items.Add(fileMenu);

            // 2. Кнопка-меню "Выполнить"
            var runItem = new MenuItem { Text = "Выполнить" };
            runItem.Selected += (s, a) => RunRequested?.Invoke(PrepareCommandRowsForRun());
            topMenu.Items.Add(runItem);

            // 3. Меню "Помощь" (заготовка)
            var helpItem = new MenuItem { Text = "Помощь" };
            helpItem.Selected += (s, a) => OpenHelpPage();
            topMenu.Items.Add(helpItem);

            // 4. Меню "Настройки" (заготовка)
           
            var settingsItem = new MenuItem { Text = "Настройки" };
            settingsItem.Selected += (s, a) => OpenSettings(); // Вызываем метод настроек
            topMenu.Items.Add(settingsItem);

            return topMenu;
        }

        private void OpenSettings()
        {
            var dialog = new Dialog
            {
                Title = "Настройки",
                Width = 350,
                Height = 180 // Вернули компактный размер окна
            };

            var grid = new Grid
            {
                RowSpacing = 12,
                ColumnSpacing = 10,
                Padding = new Myra.Graphics2D.Thickness(15)
            };
            grid.ColumnsProportions.Add(new Proportion(ProportionType.Auto));
            grid.ColumnsProportions.Add(new Proportion(ProportionType.Part));

            // --- 1. ВЫБОР ЯЗЫКА ---
            grid.RowsProportions.Add(new Proportion(ProportionType.Auto));
            var langLabel = new Label { Text = "Язык:", TextColor = Color.White, VerticalAlignment = VerticalAlignment.Center };
            grid.Widgets.Add(langLabel);
            Grid.SetColumn(langLabel, 0);
            Grid.SetRow(langLabel, 0);

            var langCombo = new ComboBox();
            langCombo.Items.Add(new ListItem("Русский"));
            langCombo.Items.Add(new ListItem("English"));
            langCombo.SelectedIndex = CurrentSettings.Language == "ru" ? 0 : 1;
            grid.Widgets.Add(langCombo);
            Grid.SetColumn(langCombo, 1);
            Grid.SetRow(langCombo, 0);

            // --- 2. ВЫБОР СКОРОСТИ ---
            grid.RowsProportions.Add(new Proportion(ProportionType.Auto));
            var speedLabel = new Label { Text = "Интервал хода (сек):", TextColor = Color.White, VerticalAlignment = VerticalAlignment.Center };
            grid.Widgets.Add(speedLabel);
            Grid.SetColumn(speedLabel, 0);
            Grid.SetRow(speedLabel, 1);

            var speedCombo = new ComboBox();
            speedCombo.Items.Add(new ListItem("0.25 сек"));
            speedCombo.Items.Add(new ListItem("0.50 сек"));
            speedCombo.Items.Add(new ListItem("0.75 сек"));
            speedCombo.Items.Add(new ListItem("1.00 сек"));
            speedCombo.SelectedIndex = CurrentSettings.Speed switch
            {
                0.25f => 0,
                0.50f => 1,
                0.75f => 2,
                1.00f => 3,
                _ => 1
            };
            grid.Widgets.Add(speedCombo);
            Grid.SetColumn(speedCombo, 1);
            Grid.SetRow(speedCombo, 1);

            dialog.Content = grid;

            // --- ОБРАБОТКА ЗАКРЫТИЯ ДИАЛОГА ---
            dialog.Closed += (s, a) =>
            {
                if (dialog.Result)
                {
                    CurrentSettings.Language = langCombo.SelectedIndex == 0 ? "ru" : "en";

                    CurrentSettings.Speed = speedCombo.SelectedIndex switch
                    {
                        0 => 0.25f,
                        1 => 0.50f,
                        2 => 0.75f,
                        _ => 1.00f
                    };

                    // Оповещаем подписчиков об изменении только языка и скорости
                    SettingsChanged?.Invoke(CurrentSettings);
                }
            };

            dialog.ShowModal(_desktop);
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

            var btnAddRow = new TextButton { Text = "Вставить строку" };
            StyleButton(btnAddRow, _btnGreen); // Применяем стиль крупной зеленой кнопки
            btnAddRow.TouchDown += (s, a) => AddEmptyRow();

            var btnDelRow = new TextButton { Text = "Удалить строку" };
            StyleButton(btnDelRow, _btnGreen);
            btnDelRow.TouchDown += (s, a) => DeleteLastRow();

            var btnClear = new TextButton { Text = "Очистить таблицу" };
            StyleButton(btnClear, _btnGreen);
            btnClear.TouchDown += (s, a) => ClearTable();

            toolbar.Widgets.Add(btnAddRow);
            toolbar.Widgets.Add(btnDelRow);
            toolbar.Widgets.Add(btnClear);

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
            string[] headers = { "№", "Адресат", "Действие", "Аргумент", "Адресат", "Действие", "Аргумент" };
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
            else if (activeRow.Target1 == "Все")
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
            else if (activeRow.Target1 == "Все")
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
            addresseePanel.Widgets.Add(new Label { Text = "Адресаты", HorizontalAlignment = HorizontalAlignment.Center, TextColor = Color.Black });

            var btnTargetAll = new TextButton { Text = "Все", HorizontalAlignment = HorizontalAlignment.Stretch };
            StyleButton(btnTargetAll, _btnDark); // Применяем темный скругленный стиль
            btnTargetAll.TouchDown += (s, a) => InsertTarget("Все");

            var btnTargetRed = new TextButton { Text = "Красный", HorizontalAlignment = HorizontalAlignment.Stretch };
            StyleButton(btnTargetRed, _btnDark);
            btnTargetRed.TouchDown += (s, a) => InsertTarget("Красный");

            var btnTargetGreen = new TextButton { Text = "Зелёный", HorizontalAlignment = HorizontalAlignment.Stretch };
            StyleButton(btnTargetGreen, _btnDark);
            btnTargetGreen.TouchDown += (s, a) => InsertTarget("Зелёный");

            addresseePanel.Widgets.Add(btnTargetAll);
            addresseePanel.Widgets.Add(btnTargetRed);
            addresseePanel.Widgets.Add(btnTargetGreen);
            controlsLayout.Widgets.Add(addresseePanel);

            // Блок КОМАНДЫ
            var commandPanel = new VerticalStackPanel { Spacing = 5 };
            commandPanel.Widgets.Add(new Label { Text = "Команды", HorizontalAlignment = HorizontalAlignment.Center, TextColor = Color.Black });

            var cmdGrid = new Grid { RowSpacing = 5, ColumnSpacing = 5 };
            cmdGrid.ColumnsProportions.Add(new Proportion(ProportionType.Part));
            cmdGrid.ColumnsProportions.Add(new Proportion(ProportionType.Part));

            // Создаем кнопки команд и сразу привязываем к ним логику добавления текста
            var btnFwd = new TextButton { Text = "Вперёд", GridRow = 0, GridColumn = 0, GridColumnSpan = 2, HorizontalAlignment = HorizontalAlignment.Stretch };
            btnFwd.TouchDown += (s, a) => InsertAction("Вперёд");
            StyleButton(btnFwd, _btnDark);

            var btnDischarge = new TextButton { Text = "Разряд", GridRow = 1, GridColumn = 0, GridColumnSpan = 2, HorizontalAlignment = HorizontalAlignment.Stretch };
            btnDischarge.TouchDown += (s, a) => InsertAction("Разряд");
            StyleButton(btnDischarge, _btnDark);

            var btnLeft = new TextButton { Text = "Налево", GridRow = 2, GridColumn = 0, HorizontalAlignment = HorizontalAlignment.Stretch };
            btnLeft.TouchDown += (s, a) => InsertAction("Налево");
            StyleButton(btnLeft, _btnDark);

            var btnRight = new TextButton { Text = "Направо", GridRow = 2, GridColumn = 1, HorizontalAlignment = HorizontalAlignment.Stretch };
            btnRight.TouchDown += (s, a) => InsertAction("Направо");
            StyleButton(btnRight, _btnDark);

            cmdGrid.Widgets.Add(btnFwd);
            cmdGrid.Widgets.Add(btnDischarge);
            cmdGrid.Widgets.Add(btnLeft);
            cmdGrid.Widgets.Add(btnRight);

            commandPanel.Widgets.Add(cmdGrid);
            controlsLayout.Widgets.Add(commandPanel);

            return controlsLayout;
        }
    }
}