using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;

namespace DroneSimulator
{
    public class Drone
    {
        public Vector2 GridPosition { get; private set; } // Целевая логическая ячейка на сетке
        public Vector2 VisualPosition; // Текущая плавная визуальная позиция
        public Color Color { get; set; }
        public Texture2D Texture { get; set; }

        // Длительность перемещения на одну клетку.
        // Увеличь значение, чтобы дрон двигался медленнее; уменьши, чтобы двигался быстрее.
        private const float MoveDurationSeconds = 0.3f;

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

            float progress = _moveElapsedSeconds / MoveDurationSeconds;

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

        public void Draw(SpriteBatch spriteBatch, int cellSize)
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
                Color.White,
                _visualRotation,
                origin,
                scale,
                SpriteEffects.None,
                0f);
        }
    }
}
