using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Myra;
using Myra.Graphics2D;
using Myra.Graphics2D.Brushes;
using Myra.Graphics2D.TextureAtlases;
using Myra.Graphics2D.UI;
using SharpDX.Direct2D1.Effects;

namespace Course1
{
    public class Game1 : Game
    {
        private Texture2D _runButtonTexture;
        private GraphicsDeviceManager _graphics;
        private Desktop _desktop;

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
            // Загружаем картинку "button_round" из вашего Content проекта
            _runButtonTexture = Content.Load<Texture2D>("btn");
            _desktop.Root = BuildMainUI();
        }   

        // Изменили тип возвращаемого значения с Widget на Grid
        private Grid BuildMainUI()
        {
            var root = new Grid();
            root.RowsProportions.Add(new Proportion(ProportionType.Auto));
            root.RowsProportions.Add(new Proportion(ProportionType.Fill));

            // --- МЕНЮ ---
            var menuBar = new HorizontalStackPanel { Spacing = 20, Padding = new Thickness(10), Background = new SolidBrush(Color.CornflowerBlue) };
            menuBar.Widgets.Add(new Label { Text = "Файл", TextColor = Color.White });
            menuBar.Widgets.Add(new Label { Text = "Настройки", TextColor = Color.White });
            menuBar.Widgets.Add(new Label { Text = "Справка", TextColor = Color.White });
            root.Widgets.Add(menuBar);

            var contentGrid = new Grid { Padding = new Thickness(10), ColumnSpacing = 20 };
            Grid.SetRow(contentGrid, 1);
            root.Widgets.Add(contentGrid);
            contentGrid.ColumnsProportions.Add(new Proportion(ProportionType.Part, 7));
            contentGrid.ColumnsProportions.Add(new Proportion(ProportionType.Part, 3));

            // --- ЛЕВАЯ ЧАСТЬ (Поле) ---
            var leftGrid = new Grid { RowSpacing = 10 };
            Grid.SetColumn(leftGrid, 0);
            contentGrid.Widgets.Add(leftGrid);

            leftGrid.RowsProportions.Add(new Proportion(ProportionType.Fill));
            leftGrid.RowsProportions.Add(new Proportion(ProportionType.Auto));
            leftGrid.RowsProportions.Add(new Proportion(ProportionType.Auto));

            var board = new Grid { Background = new SolidBrush(Color.Wheat), ShowGridLines = true };
            for (int i = 0; i < 20; i++) board.ColumnsProportions.Add(new Proportion(ProportionType.Part, 1));
            for (int i = 0; i < 10; i++) board.RowsProportions.Add(new Proportion(ProportionType.Part, 1));
            leftGrid.Widgets.Add(board);

            var statusPanel = new VerticalStackPanel { Spacing = 5 };
            Grid.SetRow(statusPanel, 1);
            statusPanel.Widgets.Add(new Label { Text = "Исполнитель красный: Зарядов 5/5", TextColor = Color.Red });
            statusPanel.Widgets.Add(new Label { Text = "Исполнитель зелёный: Зарядов 5/5", TextColor = Color.Green });
            leftGrid.Widgets.Add(statusPanel);

            // Предполагается, что _runButtonTexture уже загружена в LoadContent
            var runButton = new Button
            {
                Content = new Label { Text = "Запустить", TextColor = Color.White },
                Background = new TextureRegion(_runButtonTexture),
                Padding = new Thickness(40, 20)
            };

            // Эффект наведения
            runButton.MouseEntered += (s, e) => {
                // Делаем чуть темнее при наведении
                runButton.Background = new SolidBrush(new Color(0, 100, 0));
            };
            runButton.MouseLeft += (s, e) => {
                // Возвращаем текстуру
                runButton.Background = new TextureRegion(_runButtonTexture);
            };

            Grid.SetRow(runButton, 2);
            leftGrid.Widgets.Add(runButton);

            // --- ПРАВАЯ ЧАСТЬ ---
            var rightGrid = new Grid { RowSpacing = 10 };
            Grid.SetColumn(rightGrid, 1);
            contentGrid.Widgets.Add(rightGrid);
            rightGrid.RowsProportions.Add(new Proportion(ProportionType.Part, 1));
            rightGrid.RowsProportions.Add(new Proportion(ProportionType.Auto));

            var table = new Grid { Background = new SolidBrush(Color.LightGray), ShowGridLines = true, Padding = new Thickness(5) };
            for (int i = 0; i < 3; i++) table.ColumnsProportions.Add(new Proportion(ProportionType.Part, 1));
            for (int i = 0; i < 7; i++) table.RowsProportions.Add(new Proportion(ProportionType.Part, 1));

            var h1 = new Label { Text = "Исполнитель" };
            var h2 = new Label { Text = "Действие" };
            var h3 = new Label { Text = "Аргументы" };
            Grid.SetColumn(h2, 1); Grid.SetColumn(h3, 2);
            table.Widgets.Add(h1); table.Widgets.Add(h2); table.Widgets.Add(h3);
            rightGrid.Widgets.Add(table);

            // КНОПКИ ДЕЙСТВИЙ (БОЛЕЕ КРУПНЫЕ)
            var actionGrid = new Grid { ColumnSpacing = 15, RowSpacing = 15 }; // Больше расстояния
            Grid.SetRow(actionGrid, 1);
            for (int i = 0; i < 3; i++) actionGrid.ColumnsProportions.Add(new Proportion(ProportionType.Part, 1));
            for (int i = 0; i < 2; i++) actionGrid.RowsProportions.Add(new Proportion(ProportionType.Part, 1)); // Row заняла место

            // Обновленная функция для создания КРУПНЫХ кнопок
            void AddButton(string text, int col, int row, Grid parent, Color bgColor, Color textColor)
            {
                var btn = new Button
                {
                    Content = new Label { Text = text, TextColor = textColor },
                    Background = new SolidBrush(bgColor),

                    // УВЕЛИЧИВАЕМ РАЗМЕР:
                    Padding = new Thickness(20, 25), // Больше отступов внутри

                    // РАСТЯГИВАЕМ НА ВСЮ ЯЧЕЙКУ:
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                    VerticalAlignment = VerticalAlignment.Stretch
                };

                Grid.SetColumn(btn, col);
                Grid.SetRow(btn, row);
                parent.Widgets.Add(btn);
            }

            // Добавляем кнопки
            AddButton("Исп. красный", 0, 0, actionGrid, Color.DarkRed, Color.White);
            AddButton("Поворот по часовой", 1, 0, actionGrid, Color.LightGray, Color.Black);
            AddButton("Вперёд", 2, 0, actionGrid, Color.LightGray, Color.Black);

            AddButton("Исп. зелёный", 0, 1, actionGrid, Color.DarkGreen, Color.White);
            AddButton("Поворот против", 1, 1, actionGrid, Color.LightGray, Color.Black);
            AddButton("Разряд", 2, 1, actionGrid, Color.LightGray, Color.Black);

            rightGrid.Widgets.Add(actionGrid);

            return root;
        }

        private void AddHoverEffect(Button btn, Color normalColor)
        {
            // При наведении делаем цвет чуть темнее (смешиваем с черным на 20%)
            Color hoverColor = Color.Lerp(normalColor, Color.Black, 0.2f);

            btn.MouseEntered += (s, e) => btn.Background = new SolidBrush(hoverColor);
            btn.MouseLeft += (s, e) => btn.Background = new SolidBrush(normalColor);
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.CornflowerBlue); // Цвет фона окна
            base.Draw(gameTime);
            _desktop.Render();
        }
    }
}