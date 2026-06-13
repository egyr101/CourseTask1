using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;

namespace DroneSimulator
{
    public class Drone
    {
        public Vector2 GridPosition { get; private set; } // Целевая логическая ячейка на сетке
        public Vector2 VisualPosition; // Текущая плавная визуальная позиция
        public string Name { get; set; } = "Дрон";
        public int Number { get; set; } = 1;
        public Color Color { get; set; }
        public Texture2D Texture { get; set; }

        // Базовая длительность перемещения на одну клетку при скорости 1x.
        // Чем меньше значение, тем быстрее базовое движение.
        private const float BaseMoveDurationSeconds = 0.3f;

        // Множитель скорости меняется из меню настроек.
        // 0.5 = медленнее, 1 = обычная скорость, 2 = быстрее.
        public static float MoveSpeedMultiplier { get; set; } = 1f;

        private readonly float _rotationSpeed = MathHelper.Pi * 3f; // Скорость поворота (радиан в секунду)

        private Vector2 _moveStartPosition;
        private Vector2 _moveTargetPosition;
        private float _moveElapsedSeconds;
        private bool _isMoving;

        private float _visualRotation;
        private float _targetRotation;

        public bool IsMoving => _isMoving;
        public bool IsRotating => Math.Abs(MathHelper.WrapAngle(_targetRotation - _visualRotation)) > 0.001f;
        public bool IsAnimating => IsMoving || IsRotating;

        public Drone(Vector2 startPosition, Color color)
        {
            GridPosition = startPosition;
            VisualPosition = startPosition; // Изначально стоим ровно в стартовой ячейке
            _moveStartPosition = startPosition;
            _moveTargetPosition = startPosition;
            Color = color;
        }

        public void MoveTo(Vector2 newGridPosition)
        {
            _moveStartPosition = VisualPosition;
            _moveTargetPosition = newGridPosition;
            _moveElapsedSeconds = 0f;
            _isMoving = true;

            GridPosition = newGridPosition;
        }

        public void SetPositionInstant(Vector2 newGridPosition)
        {
            GridPosition = newGridPosition;
            VisualPosition = newGridPosition;

            _moveStartPosition = newGridPosition;
            _moveTargetPosition = newGridPosition;
            _moveElapsedSeconds = 0f;
            _isMoving = false;
        }

        public void RotateTo(float targetRotation)
        {
            _targetRotation = MathHelper.WrapAngle(targetRotation);
        }

        public void SetRotationInstant(float rotation)
        {
            _visualRotation = MathHelper.WrapAngle(rotation);
            _targetRotation = _visualRotation;
        }

        // Этот метод плавно двигает визуальную позицию к целевой ячейке сетки
        // и плавно поворачивает дрон к целевому направлению.
        public void Update(GameTime gameTime)
        {
            float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;

            UpdateMovement(dt);
            UpdateRotation(dt);
        }

        private void UpdateMovement(float dt)
        {
            if (!_isMoving)
                return;

            _moveElapsedSeconds += dt;

            float safeMultiplier = Math.Max(0.1f, MoveSpeedMultiplier);
            float moveDurationSeconds = BaseMoveDurationSeconds / safeMultiplier;
            float progress = _moveElapsedSeconds / moveDurationSeconds;

            if (progress >= 1f)
            {
                VisualPosition = _moveTargetPosition;
                _isMoving = false;
                return;
            }

            VisualPosition = Vector2.Lerp(
                _moveStartPosition,
                _moveTargetPosition,
                progress);
        }

        private void UpdateRotation(float dt)
        {
            float difference = MathHelper.WrapAngle(_targetRotation - _visualRotation);

            if (Math.Abs(difference) <= 0.001f)
            {
                _visualRotation = _targetRotation;
                return;
            }

            float rotationStep = _rotationSpeed * dt;

            if (Math.Abs(difference) <= rotationStep)
            {
                _visualRotation = _targetRotation;
            }
            else
            {
                _visualRotation = MathHelper.WrapAngle(
                    _visualRotation + Math.Sign(difference) * rotationStep);
            }
        }

        public void Draw(SpriteBatch spriteBatch, int cellSize, Texture2D pixel)
        {
            if (Texture == null)
                return;

            // Рисуем не от левого верхнего угла, а из центра клетки.
            // Это нужно, чтобы поворот происходил вокруг центра дрона.
            Vector2 cellCenter = new Vector2(
                (VisualPosition.X + 0.5f) * cellSize,
                (VisualPosition.Y + 0.5f) * cellSize);

            Vector2 origin = new Vector2(Texture.Width / 2f, Texture.Height / 2f);
            float scale = cellSize / (float)Math.Max(Texture.Width, Texture.Height);

            spriteBatch.Draw(
                Texture,
                cellCenter,
                null,
                Color,
                _visualRotation,
                origin,
                scale,
                SpriteEffects.None,
                0f);

            DrawNumberBadge(spriteBatch, pixel, cellSize);
        }

        private void DrawNumberBadge(SpriteBatch spriteBatch, Texture2D pixel, int cellSize)
        {
            string numberText = Math.Max(1, Number).ToString();

            int digitWidth = 5;
            int digitHeight = 9;
            int digitSpacing = 2;
            int scale = 2;

            int digitsWidth = numberText.Length * digitWidth * scale +
                              Math.Max(0, numberText.Length - 1) * digitSpacing;

            int badgeWidth = digitsWidth + 8;
            int badgeHeight = digitHeight * scale + 6;

            // Небольшой бейдж в правом верхнем углу клетки.
            // Он не закрывает центр модели дрона, поэтому дрон остаётся хорошо видимым.
            int badgeX = (int)((VisualPosition.X + 1) * cellSize) - badgeWidth - 2;
            int badgeY = (int)(VisualPosition.Y * cellSize) + 2;

            spriteBatch.Draw(
                pixel,
                new Rectangle(badgeX, badgeY, badgeWidth, badgeHeight),
                new Color(0, 0, 0, 170));

            int digitX = badgeX + 4;
            int digitY = badgeY + 3;

            foreach (char ch in numberText)
            {
                if (ch >= '0' && ch <= '9')
                {
                    DrawDigit(spriteBatch, pixel, ch - '0', digitX, digitY, scale, Color.White);
                }

                digitX += digitWidth * scale + digitSpacing;
            }
        }

        private static void DrawDigit(
            SpriteBatch spriteBatch,
            Texture2D pixel,
            int digit,
            int x,
            int y,
            int scale,
            Color color)
        {
            bool[][] segments =
            {
                new[] { true, true, true, true, true, true, false },      // 0
                new[] { false, true, true, false, false, false, false },  // 1
                new[] { true, true, false, true, true, false, true },      // 2
                new[] { true, true, true, true, false, false, true },      // 3
                new[] { false, true, true, false, false, true, true },     // 4
                new[] { true, false, true, true, false, true, true },      // 5
                new[] { true, false, true, true, true, true, true },       // 6
                new[] { true, true, true, false, false, false, false },    // 7
                new[] { true, true, true, true, true, true, true },        // 8
                new[] { true, true, true, true, false, true, true }        // 9
            };

            var active = segments[digit];
            int t = scale;
            int w = 5 * scale;
            int h = 9 * scale;

            // Индексы сегментов: 0 верх, 1 правый верх, 2 правый низ,
            // 3 низ, 4 левый низ, 5 левый верх, 6 центр.
            if (active[0]) DrawRect(spriteBatch, pixel, x + t, y, w - 2 * t, t, color);
            if (active[1]) DrawRect(spriteBatch, pixel, x + w - t, y + t, t, h / 2 - t, color);
            if (active[2]) DrawRect(spriteBatch, pixel, x + w - t, y + h / 2, t, h / 2 - t, color);
            if (active[3]) DrawRect(spriteBatch, pixel, x + t, y + h - t, w - 2 * t, t, color);
            if (active[4]) DrawRect(spriteBatch, pixel, x, y + h / 2, t, h / 2 - t, color);
            if (active[5]) DrawRect(spriteBatch, pixel, x, y + t, t, h / 2 - t, color);
            if (active[6]) DrawRect(spriteBatch, pixel, x + t, y + h / 2 - t / 2, w - 2 * t, t, color);
        }

        private static void DrawRect(
            SpriteBatch spriteBatch,
            Texture2D pixel,
            int x,
            int y,
            int width,
            int height,
            Color color)
        {
            spriteBatch.Draw(pixel, new Rectangle(x, y, width, height), color);
        }
    }
}
