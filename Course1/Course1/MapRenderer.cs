using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;

namespace DroneSimulator
{
    public class MapRenderer
    {
        private GraphicsDevice _graphicsDevice;
        private SpriteBatch _spriteBatch;
        public RenderTarget2D MapTexture { get; private set; }

        private Texture2D _pixel; // Для рисования линий сетки

        public int GridWidth { get; set; } = 20;
        public int GridHeight { get; set; } = 15;
        public int CellSize { get; set; } = 40;

        public List<Drone> Drones { get; set; }
        public WeedField WeedField { get; }

        public MapRenderer(GraphicsDevice graphicsDevice)
        {
            _graphicsDevice = graphicsDevice;
            _spriteBatch = new SpriteBatch(graphicsDevice);
            Drones = new List<Drone>();
            WeedField = new WeedField();

            // Создаем холст, на котором будем рисовать карту
            MapTexture = new RenderTarget2D(_graphicsDevice, GridWidth * CellSize, GridHeight * CellSize);

            // Создаем текстуру 1x1 пиксель для рисования линий
            _pixel = new Texture2D(graphicsDevice, 1, 1);
            _pixel.SetData(new[] { Color.White });
        }

        private void DrawWeeds()
        {
            foreach (var weed in WeedField.Weeds)
            {
                if (weed.IsDestroyed)
                    continue;

                int x = (int)weed.GridPosition.X * CellSize;
                int y = (int)weed.GridPosition.Y * CellSize;
                int padding = CellSize / 5;

                var stemColor = new Color(25, 115, 45);
                var leafColor = new Color(20, 150, 55);
                var flowerColor = new Color(80, 190, 70);

                // Центральный стебель.
                _spriteBatch.Draw(
                    _pixel,
                    new Rectangle(x + CellSize / 2 - 2, y + padding, 4, CellSize - padding * 2),
                    stemColor);

                // Листья.
                _spriteBatch.Draw(
                    _pixel,
                    new Rectangle(x + padding, y + CellSize / 2 - 4, CellSize - padding * 2, 8),
                    leafColor);

                _spriteBatch.Draw(
                    _pixel,
                    new Rectangle(x + padding + 4, y + CellSize / 2 + 7, CellSize - padding * 2 - 8, 6),
                    leafColor);

                // Верхушка сорняка.
                _spriteBatch.Draw(
                    _pixel,
                    new Rectangle(x + CellSize / 2 - 6, y + padding - 2, 12, 12),
                    flowerColor);
            }
        }

        public void DrawMap()
        {
            // Переключаем рендер на нашу текстуру
            _graphicsDevice.SetRenderTarget(MapTexture);
            _graphicsDevice.Clear(new Color(180, 230, 180)); // Светло-зеленый фон как на макете

            _spriteBatch.Begin();

            // Рисуем сетку
            Color lineColor = new Color(255, 255, 255, 150); // Полупрозрачный белый
            for (int x = 0; x <= GridWidth; x++)
            {
                _spriteBatch.Draw(_pixel, new Rectangle(x * CellSize, 0, 1, GridHeight * CellSize), lineColor);
            }
            for (int y = 0; y <= GridHeight; y++)
            {
                _spriteBatch.Draw(_pixel, new Rectangle(0, y * CellSize, GridWidth * CellSize, 1), lineColor);
            }

            // Рисуем сорняки до дронов, чтобы дроны всегда были поверх объектов карты.
            DrawWeeds();

            // Рисуем дронов
            foreach (var drone in Drones)
            {
                drone.Draw(_spriteBatch, CellSize, _pixel);
            }

            _spriteBatch.End();

            // Возвращаем рендер обратно на главный экран
            _graphicsDevice.SetRenderTarget(null);
        }
    }
}