using Course1;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace DroneSimulator
{
    public class Drone
    {
        public Vector2 GridPosition { get; set; } // Целевая логическая ячейка на сетке
        public Vector2 VisualPosition;           // Текущая плавная визуальная позиция
        public Color Color { get; set; }
        public Texture2D Texture { get; set; }

        private readonly float _moveSpeed = 6f ; // Скорость движения (ячеек в секунду)
        private readonly float _rotationSpeed = MathHelper.Pi * 3f; // Скорость поворота (радиан в секунду)

        public Drone(Vector2 startPosition, Color color)
        {
            GridPosition = startPosition;
            VisualPosition = startPosition; // Изначально стоим ровно в стартовой ячейке
            Color = color;
        }

        // Этот метод плавно двигает визуальную позицию к целевой ячейке сетки
        public void Update(GameTime gameTime)
        {
            float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;

            // Расстояние до цели
            float distance = Vector2.Distance(VisualPosition, GridPosition);

            if (distance > 0.001f)
            {
                Vector2 direction = Vector2.Normalize(GridPosition - VisualPosition);
                VisualPosition += direction * _moveSpeed * dt;

                // Если проскочили цель из-за кадра (dt) - фиксируем в цели
                if (Vector2.Distance(VisualPosition, GridPosition) > distance)
                {
                    VisualPosition = GridPosition;
                }
            }
            else
            {
                VisualPosition = GridPosition;
            }
        }

        public void Draw(SpriteBatch spriteBatch, int cellSize)
        {
            if (Texture != null)
            {
                // Рисуем по плавным визуальным координатам
                Rectangle destRect = new Rectangle(
                    (int)(VisualPosition.X * cellSize),
                    (int)(VisualPosition.Y * cellSize),
                    cellSize,
                    cellSize
                );
                spriteBatch.Draw(Texture, destRect, Color.White);
            }
        }
    }
}