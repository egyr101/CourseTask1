using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Myra;
using Myra.Graphics2D;
using Myra.Graphics2D.UI;
using Myra.Graphics2D.Brushes;

namespace Course1
{
    public class Game1 : Game
    {
        private GraphicsDeviceManager _graphics;
        private Desktop _desktop;
        private VerticalStackPanel _commandListContainer;
        private string _currentPerformer = "Красный";
        private bool _isPerformerSelected = false; // Флаг выбора исполнителя

        public Game1()
        {
            _graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
            IsMouseVisible = true;
            Window.AllowUserResizing = true;
        }

        protected override void Initialize()
        {
            MyraEnvironment.Game = this;
            base.Initialize();
        }

        protected override void LoadContent()
        {
            _desktop = new Desktop();
            _desktop.Root = BuildMainUI();
        }

        private Grid BuildMainUI()
        {
            var root = new Grid { RowSpacing = 10, Padding = new Thickness(10) };
            root.RowsProportions.Add(new Proportion(ProportionType.Auto));
            root.RowsProportions.Add(new Proportion(ProportionType.Fill));

            var menuBar = new HorizontalStackPanel { Spacing = 20, Background = new SolidBrush(Color.CornflowerBlue) };
            menuBar.Widgets.Add(new Label { Text = "Файл", TextColor = Color.White });
            menuBar.Widgets.Add(new Label { Text = "Настройки", TextColor = Color.White });
            menuBar.Widgets.Add(new Label { Text = "Справка", TextColor = Color.White });
            root.Widgets.Add(menuBar);

            var mainContent = new Grid { ColumnSpacing = 20 };
            Grid.SetRow(mainContent, 1);
            mainContent.ColumnsProportions.Add(new Proportion(ProportionType.Part, 7));
            mainContent.ColumnsProportions.Add(new Proportion(ProportionType.Part, 3));
            root.Widgets.Add(mainContent);

            var leftGrid = new Grid { RowSpacing = 20 };
            mainContent.Widgets.Add(leftGrid);
            leftGrid.RowsProportions.Add(new Proportion(ProportionType.Fill));
            leftGrid.RowsProportions.Add(new Proportion(ProportionType.Auto));


            var board = new Grid { Background = new SolidBrush(Color.Wheat), ShowGridLines = true };
            for (int i = 0; i < 20; i++) board.ColumnsProportions.Add(new Proportion(ProportionType.Part, 1));
            for (int i = 0; i < 10; i++) board.RowsProportions.Add(new Proportion(ProportionType.Part, 1));
            leftGrid.Widgets.Add(board);

            var bottomPanel = new VerticalStackPanel { Spacing = 10 };
            Grid.SetRow(bottomPanel, 1);
            bottomPanel.Widgets.Add(new Label { Text = "Исполнитель красный: Зарядов 5/5", TextColor = Color.Red });
            bottomPanel.Widgets.Add(new Label { Text = "Исполнитель зелёный: Зарядов 5/5", TextColor = Color.Green });

            var runButton = new Button
            {
                Background = new SolidBrush(Color.Green),
                Width = 200,
                Height = 50
            };
            runButton.Content = new Label { Text = "Запустить", TextColor = Color.White, HorizontalAlignment = HorizontalAlignment.Center };
            // 1. Обычное состояние (Зеленый)
            runButton.Background = new SolidBrush(Color.Green);

            // 2. Наведение (Темно-зеленый)
            runButton.MouseEntered += (s, e) => {
                if (runButton.Background != new SolidBrush(Color.Red)) // Не меняем, если уже нажата
                    runButton.Background = new SolidBrush(Color.DarkGreen);
            };

            // 3. Уход курсора (Возврат к зеленому)
            runButton.MouseLeft += (s, e) => {
                if (runButton.Background != new SolidBrush(Color.Red))
                    runButton.Background = new SolidBrush(Color.Green);
            };

            // 4. Нажатие (Красный)
            runButton.TouchDown += (s, e) => {
                runButton.Background = new SolidBrush(Color.Red);
            };

            bottomPanel.Widgets.Add(runButton);
            leftGrid.Widgets.Add(bottomPanel);

            var rightPanel = new VerticalStackPanel { Spacing = 10 };
            Grid.SetColumn(rightPanel, 1);
            mainContent.Widgets.Add(rightPanel);

            var header = new Grid { Background = new SolidBrush(Color.DarkGray) };
            header.ColumnsProportions.Add(new Proportion(ProportionType.Part, 1)); // Исполнитель
            header.ColumnsProportions.Add(new Proportion(ProportionType.Part, 1)); // Действие
            header.ColumnsProportions.Add(new Proportion(ProportionType.Part, 1)); // Аргумент
            header.ColumnsProportions.Add(new Proportion(ProportionType.Part, 0.4f)); // Удаление

            header.Widgets.Add(new Label { Text = "Исполнитель", TextColor = Color.Black });
            var h2 = new Label { Text = "Действие", TextColor = Color.Black }; Grid.SetColumn(h2, 1); header.Widgets.Add(h2);
            var h3 = new Label { Text = "Аргумент", TextColor = Color.Black }; Grid.SetColumn(h3, 2); header.Widgets.Add(h3);
            var h4 = new Label { Text = "Удалить", TextColor = Color.Black }; Grid.SetColumn(h4, 3); header.Widgets.Add(h4);
            rightPanel.Widgets.Add(header);

            _commandListContainer = new VerticalStackPanel { Height = 300 };
            rightPanel.Widgets.Add(_commandListContainer);

            var buttonsGrid = new Grid { ColumnSpacing = 5, RowSpacing = 5 };
            for (int i = 0; i < 3; i++) buttonsGrid.ColumnsProportions.Add(new Proportion(ProportionType.Part, 1));
            for (int i = 0; i < 2; i++) buttonsGrid.RowsProportions.Add(new Proportion(ProportionType.Part, 1));

            AddBtn(buttonsGrid, "Исп. красный", 0, 0, Color.DarkRed, Color.White, () => {
                _currentPerformer = "Красный";
                _isPerformerSelected = true;
            });
            AddBtn(buttonsGrid, "Поворот", 1, 0, Color.LightGray, Color.Black, () => {
                if (_isPerformerSelected) AddRow("Поворот", "-");
            });
            AddBtn(buttonsGrid, "Вперёд", 2, 0, Color.LightGray, Color.Black, () => {
                if (!_isPerformerSelected) return; // Выход, если не выбран
                var dialog = new Dialog { Title = "Введите дистанцию", Content = new TextBox() };
                dialog.ButtonOk.Click += (s, e) => {
                    AddRow("Вперёд", ((TextBox)dialog.Content).Text);
                    dialog.Close();
                };
                dialog.ShowModal(_desktop);
            });
            AddBtn(buttonsGrid, "Исп. зелёный", 0, 1, Color.DarkGreen, Color.White, () => {
                _currentPerformer = "Зелёный";
                _isPerformerSelected = true;
            });
            AddBtn(buttonsGrid, "Поворот пр.", 1, 1, Color.LightGray, Color.Black, () => {
                if (_isPerformerSelected) AddRow("Поворот против", "-");
            });
            AddBtn(buttonsGrid, "Разряд", 2, 1, Color.LightGray, Color.Black, () => { if (_isPerformerSelected) AddRow("Разряд", "-"); });

            rightPanel.Widgets.Add(buttonsGrid);
            return root;
        }

        private void AddBtn(Grid grid, string text, int c, int r, Color color, Color btnColor, System.Action onClick)
        {
            var btn = new Button
            {
                Background = new SolidBrush(color),
                Width = 120,    // Фиксированная ширина
                Height = 40,    // Фиксированная высота
                HorizontalAlignment = HorizontalAlignment.Center
            }; ;
            btn.Content = new Label { Text = text, HorizontalAlignment = HorizontalAlignment.Center, TextColor=btnColor};
            btn.Click += (s, e) => onClick();
            Grid.SetColumn(btn, c); Grid.SetRow(btn, r);
            grid.Widgets.Add(btn);
        }

        private void AddRow(string action, string arg)
        {
            var row = new Grid { Background = new SolidBrush(Color.WhiteSmoke), Padding = new Thickness(2) };
            row.ColumnsProportions.Add(new Proportion(ProportionType.Part, 1));
            row.ColumnsProportions.Add(new Proportion(ProportionType.Part, 1));
            row.ColumnsProportions.Add(new Proportion(ProportionType.Part, 1));
            row.ColumnsProportions.Add(new Proportion(ProportionType.Part, 0.4f));

            row.Widgets.Add(new Label { Text = _currentPerformer, TextColor = Color.Black });

            var act = new Label { Text = action, TextColor = Color.Black };
            Grid.SetColumn(act, 1); row.Widgets.Add(act);

            var ar = new Label { Text = arg, TextColor = Color.Black };
            Grid.SetColumn(ar, 2); row.Widgets.Add(ar);

            // Кнопка удаления
            var delBtn = new Button { Content = new Label { Text = "X", TextColor = Color.Red } };
            delBtn.Click += (s, e) => _commandListContainer.Widgets.Remove(row);
            Grid.SetColumn(delBtn, 3);
            row.Widgets.Add(delBtn);

            _commandListContainer.Widgets.Add(row);

            // Сбрасываем выбор после добавления
            _isPerformerSelected = false;
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.CornflowerBlue);
            base.Draw(gameTime);
            _desktop.Render();
        }
    }
}