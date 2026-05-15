//using Microsoft.Xna.Framework;
//using Microsoft.Xna.Framework.Graphics;
//using Microsoft.Xna.Framework.Input;
//using System;

//public class Game1 : Game
//{
//    private GraphicsDeviceManager _graphics;
//    private SpriteBatch _spriteBatch;

//    // Текстуры-заглушки (для прямоугольников и линий)
//    private Texture2D _pixel;

//    // Сюда нужно будет загрузить ваши спрайты (цветок, дрон)
//    // private Texture2D _droneRedTexture;

//    // Шрифты
//    private SpriteFont _font;

//    public Game1()
//    {
//        _graphics = new GraphicsDeviceManager(this);
//        Content.RootDirectory = "Content";
//        IsMouseVisible = true;
//    }

//    protected override void Initialize()
//    {
//        _graphics.PreferredBackBufferWidth = 1280;
//        _graphics.PreferredBackBufferHeight = 720;
//        _graphics.ApplyChanges();
//        base.Initialize();
//    }

//    protected override void LoadContent()
//    {
//        _spriteBatch = new SpriteBatch(GraphicsDevice);

//        // Создаем текстуру 1x1 для отрисовки прямоугольников/линий
//        _pixel = new Texture2D(GraphicsDevice, 1, 1);
//        _pixel.SetData(new[] { Color.White });

//        // _font = Content.Load<SpriteFont>("MyFont"); // Загрузите свой шрифт
//    }

//    protected override void Draw(GameTime gameTime)
//    {
//        GraphicsDevice.Clear(new Color(245, 245, 245)); // Светлый фон

//        _spriteBatch.Begin();

//        // 1. Отрисовка верхней панели
//        DrawRectangle(0, 0, 1280, 40, Color.CornflowerBlue);

//        // 2. Отрисовка сетки (слева)
//        DrawGrid(50, 80, 20, 10, 40); // x, y, cols, rows, cellSize

//        // 3. Отрисовка правой таблицы
//        DrawTable(800, 80, 450, 500);

//        // 4. Отрисовка панели управления (снизу справа)
//        DrawControlPanel(800, 600);

//        // 5. Отрисовка инфо о дронах (снизу слева)
//        DrawDroneStatus(50, 600, "Красный", Color.Red);
//        DrawDroneStatus(50, 660, "Зеленый", Color.Green);

//        _spriteBatch.End();

//        base.Draw(gameTime);
//    }

//    // --- ХЕЛПЕРЫ ---

//    private void DrawRectangle(int x, int y, int w, int h, Color color)
//    {
//        _spriteBatch.Draw(_pixel, new Rectangle(x, y, w, h), color);
//    }

//    private void DrawGrid(int x, int y, int cols, int rows, int cellSize)
//    {
//        Color cellColor = new Color(240, 220, 180); // Цвет фона сетки
//        Color lineColor = Color.Black;

//        // Фон сетки
//        DrawRectangle(x, y, cols * cellSize, rows * cellSize, cellColor);

//        // Линии
//        for (int i = 0; i <= cols; i++)
//            DrawLine(x + i * cellSize, y, x + i * cellSize, y + rows * cellSize, lineColor);
//        for (int i = 0; i <= rows; i++)
//            DrawLine(x, y + i * cellSize, x + cols * cellSize, y + i * cellSize, lineColor);
//    }

//    private void DrawTable(int x, int y, int w, int h)
//    {
//        // Фон таблицы
//        DrawRectangle(x, y, w, h, Color.Gray);

//        // Тут логика отрисовки строк и заголовков (Исполнитель, Действие, Аргументы)
//        DrawRectangle(x, y, w, 40, Color.DarkGray); // Заголовок
//    }

//    private void DrawControlPanel(int x, int y)
//    {
//        // Кнопка "Запустить"
//        DrawRectangle(450, 640, 200, 50, Color.LimeGreen);

//        // Кнопки команд (упрощенно)
//        DrawRectangle(x, y, 120, 40, Color.LightGray); // Поворот...
//    }

//    private void DrawDroneStatus(int x, int y, string name, Color color)
//    {
//        // Отрисовка иконки дрона и текста "Количество зарядов 5/5"
//        DrawRectangle(x, y, 50, 50, color);
//    }

//    private void DrawLine(int x1, int y1, int x2, int y2, Color color)
//    {
//        float angle = (float)Math.Atan2(y2 - y1, x2 - x1);
//        float length = (float)Math.Sqrt((x2 - x1) * (x2 - x1) + (y2 - y1) * (y2 - y1));
//        _spriteBatch.Draw(_pixel, new Vector2(x1, y1), null, color, angle, Vector2.Zero, new Vector2(length, 1), SpriteEffects.None, 0);
//    }
//}