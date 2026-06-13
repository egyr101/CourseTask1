using Microsoft.Xna.Framework;
using Myra.Graphics2D;
using Myra.Graphics2D.Brushes;
using Myra.Graphics2D.UI;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DroneSimulator
{
    /// <summary>
    /// Окно редактора карт. Позволяет изменить расположение дронов и сорняков
    /// без изменения размеров карты.
    /// </summary>
    public sealed class MapEditorWindow : Panel
    {
        private const int MaxDrones = 10;
        private const int EditorCellSize = 28;
        private const int ControlPanelWidth = 420;
        private const int ControlTextWrapLength = 44;

        public event Action<string, LevelConfig>? SaveRequested;
        public event Action? CloseRequested;

        private readonly int _gridWidth;
        private readonly int _gridHeight;
        private readonly Dictionary<Point, Panel> _cellPanels = new();
        private readonly List<Point> _drones = new();
        private readonly List<Point> _weeds = new();

        private Point _selectedCell;

        private Label _titleLabel;
        private Label _mapNameLabel;
        private TextBox _mapNameTextBox;
        private Label _selectedCellLabel;
        private Label _objectsCountLabel;
        private Label _statusLabel;

        private TextButton _addDroneButton;
        private TextButton _addWeedButton;
        private TextButton _removeDroneButton;
        private TextButton _removeWeedButton;
        private TextButton _applyButton;
        private TextButton _closeButton;

        private readonly IBrush _windowBrush = new SolidBrush(new Color(230, 245, 235));
        private readonly IBrush _emptyBrush = new SolidBrush(new Color(194, 232, 194));
        private readonly IBrush _selectedBrush = new SolidBrush(new Color(250, 225, 90));
        private readonly IBrush _droneBrush = new SolidBrush(new Color(75, 130, 210));
        private readonly IBrush _weedBrush = new SolidBrush(new Color(25, 150, 65));
        private readonly IBrush _blockedBrush = new SolidBrush(new Color(180, 70, 65));
        private readonly IBrush _buttonBrush = new SolidBrush(new Color(45, 45, 45));
        private readonly IBrush _disabledButtonBrush = new SolidBrush(new Color(105, 105, 105));

        public MapEditorWindow(MapRenderer mapRenderer)
        {
            _gridWidth = mapRenderer.GridWidth;
            _gridHeight = mapRenderer.GridHeight;
            _selectedCell = new Point(0, 0);

            Background = _windowBrush;
            Margin = new Thickness(10, 8);
            Padding = new Thickness(10);
            Visible = false;

            BuildLayout();
            ResetFromMap(mapRenderer);
        }

        public void ResetFromMap(MapRenderer mapRenderer)
        {
            _drones.Clear();
            _weeds.Clear();

            foreach (var drone in mapRenderer.Drones)
            {
                _drones.Add(ToPoint(drone.GridPosition));
            }

            foreach (var weed in mapRenderer.WeedField.Weeds.Where(weed => !weed.IsDestroyed))
            {
                _weeds.Add(ToPoint(weed.GridPosition));
            }

            _selectedCell = new Point(0, 0);

            if (_mapNameTextBox != null && string.IsNullOrWhiteSpace(_mapNameTextBox.Text))
            {
                _mapNameTextBox.Text = "level_custom";
            }

            SetStatus("Выберите клетку и действие.", false);
            RefreshView();
        }

        private static Point ToPoint(Vector2 position)
        {
            return new Point((int)position.X, (int)position.Y);
        }

        private void BuildLayout()
        {
            var root = new VerticalStackPanel
            {
                Spacing = 8,
                HorizontalAlignment = HorizontalAlignment.Center
            };

            _titleLabel = new Label
            {
                Text = "Редактор карт",
                TextColor = Color.Black,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 0, 0, 4)
            };
            root.Widgets.Add(_titleLabel);

            var body = new HorizontalStackPanel
            {
                Spacing = 12
            };

            body.Widgets.Add(CreateMapGrid());
            body.Widgets.Add(CreateControlPanel());

            root.Widgets.Add(body);
            Widgets.Add(root);
        }

        private Widget CreateMapGrid()
        {
            var grid = new Grid
            {
                ShowGridLines = true,
                GridLinesColor = Color.White
            };

            for (int x = 0; x < _gridWidth; x++)
            {
                grid.ColumnsProportions.Add(new Proportion(ProportionType.Auto));
            }

            for (int y = 0; y < _gridHeight; y++)
            {
                grid.RowsProportions.Add(new Proportion(ProportionType.Auto));
            }

            for (int y = 0; y < _gridHeight; y++)
            {
                for (int x = 0; x < _gridWidth; x++)
                {
                    var point = new Point(x, y);
                    var cell = new Panel
                    {
                        GridColumn = x,
                        GridRow = y,
                        Width = EditorCellSize,
                        Height = EditorCellSize,
                        Background = _emptyBrush
                    };

                    cell.TouchDown += (s, a) =>
                    {
                        _selectedCell = point;
                        SetStatus($"Выбрана клетка ({point.X}, {point.Y}).", false);
                        RefreshView();
                    };

                    _cellPanels[point] = cell;
                    grid.Widgets.Add(cell);
                }
            }

            return grid;
        }

        private Widget CreateControlPanel()
        {
            var panel = new VerticalStackPanel
            {
                Width = ControlPanelWidth,
                Spacing = 8,
                Padding = new Thickness(8),
                Background = new SolidBrush(new Color(215, 238, 222))
            };

            _mapNameLabel = new Label
            {
                Text = "Название карты",
                TextColor = Color.Black
            };

            _mapNameTextBox = new TextBox
            {
                Text = "level_custom",
                Height = 34,
                TextColor = Color.Black,
                Background = new SolidBrush(Color.White),
                HorizontalAlignment = HorizontalAlignment.Stretch
            };

            _selectedCellLabel = CreateControlLabel(string.Empty);
            _objectsCountLabel = CreateControlLabel(string.Empty);
            _statusLabel = CreateControlLabel(string.Empty);

            panel.Widgets.Add(_mapNameLabel);
            panel.Widgets.Add(_mapNameTextBox);
            panel.Widgets.Add(_selectedCellLabel);
            panel.Widgets.Add(_objectsCountLabel);

            _addDroneButton = CreateButton("Добавить дрон", AddDroneAtSelectedCell);
            _addWeedButton = CreateButton("Добавить сорняк", AddWeedAtSelectedCell);
            _removeDroneButton = CreateButton("Удалить дрон", RemoveDroneAtSelectedCell);
            _removeWeedButton = CreateButton("Удалить сорняк", RemoveWeedAtSelectedCell);

            panel.Widgets.Add(_addDroneButton);
            panel.Widgets.Add(_addWeedButton);
            panel.Widgets.Add(_removeDroneButton);
            panel.Widgets.Add(_removeWeedButton);

            panel.Widgets.Add(CreateControlLabel("Ограничения:", topMargin: 8));
            panel.Widgets.Add(CreateControlLabel("- максимум 10 дронов;"));
            panel.Widgets.Add(CreateControlLabel("- дрон и сорняк не могут стоять в одной клетке."));

            panel.Widgets.Add(_statusLabel);

            var bottomButtons = new VerticalStackPanel
            {
                Spacing = 8,
                Margin = new Thickness(0, 8, 0, 0)
            };

            _applyButton = CreateButton("Сохранить и применить", ApplyLevel);
            _closeButton = CreateButton("Закрыть", () => CloseRequested?.Invoke());
            bottomButtons.Widgets.Add(_applyButton);
            bottomButtons.Widgets.Add(_closeButton);

            panel.Widgets.Add(bottomButtons);

            return panel;
        }

        private Label CreateControlLabel(string text, int topMargin = 0)
        {
            return new Label
            {
                Text = WrapText(text, ControlTextWrapLength),
                TextColor = Color.Black,
                Width = ControlPanelWidth - 24,
                Margin = new Thickness(0, topMargin, 0, 0)
            };
        }

        private TextButton CreateButton(string text, Action action)
        {
            var button = new TextButton
            {
                Text = text,
                Height = 40,
                Padding = new Thickness(10, 5),
                TextColor = Color.White,
                Background = _buttonBrush,
                OverBackground = _buttonBrush,
                PressedBackground = _buttonBrush,
                FocusedBackground = _buttonBrush,
                HorizontalAlignment = HorizontalAlignment.Stretch
            };

            button.TouchDown += (s, a) => action();
            return button;
        }

        private void AddDroneAtSelectedCell()
        {
            if (_drones.Count >= MaxDrones)
            {
                SetStatus("Нельзя добавить дрон: достигнут лимит 10 дронов.", true);
                RefreshView();
                return;
            }

            if (IsCellOccupied(_selectedCell))
            {
                SetStatus("Клетка уже занята. Выберите свободную клетку.", true);
                RefreshView();
                return;
            }

            _drones.Add(_selectedCell);
            SetStatus("Дрон добавлен.", false);
            RefreshView();
        }

        private void AddWeedAtSelectedCell()
        {
            if (IsCellOccupied(_selectedCell))
            {
                SetStatus("Клетка уже занята. Выберите свободную клетку.", true);
                RefreshView();
                return;
            }

            _weeds.Add(_selectedCell);
            SetStatus("Сорняк добавлен.", false);
            RefreshView();
        }

        private void RemoveDroneAtSelectedCell()
        {
            if (!_drones.Remove(_selectedCell))
            {
                SetStatus("В выбранной клетке нет дрона.", true);
                RefreshView();
                return;
            }

            SetStatus("Дрон удалён.", false);
            RefreshView();
        }

        private void RemoveWeedAtSelectedCell()
        {
            if (!_weeds.Remove(_selectedCell))
            {
                SetStatus("В выбранной клетке нет сорняка.", true);
                RefreshView();
                return;
            }

            SetStatus("Сорняк удалён.", false);
            RefreshView();
        }

        private bool IsCellOccupied(Point point)
        {
            return _drones.Contains(point) || _weeds.Contains(point);
        }

        private void ApplyLevel()
        {
            if (_drones.Count == 0)
            {
                SetStatus("На карте должен быть хотя бы один дрон.", true);
                RefreshView();
                return;
            }

            string mapName = _mapNameTextBox.Text?.Trim() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(mapName))
            {
                SetStatus("Введите название карты.", true);
                RefreshView();
                return;
            }

            var config = new LevelConfig
            {
                Drones = _drones
                    .Select(point => new GridPointConfig { X = point.X, Y = point.Y })
                    .ToList(),
                Weeds = _weeds
                    .Select(point => new GridPointConfig { X = point.X, Y = point.Y })
                    .ToList()
            };

            SaveRequested?.Invoke(mapName, config);
        }

        private void SetStatus(string message, bool isError)
        {
            _statusLabel.Text = WrapText(message, ControlTextWrapLength);
            _statusLabel.TextColor = isError
                ? new Color(170, 55, 45)
                : Color.Black;
        }

        private static string WrapText(string text, int maxLineLength)
        {
            if (string.IsNullOrWhiteSpace(text) || text.Length <= maxLineLength)
                return text;

            var words = text.Split(' ');
            var lines = new List<string>();
            string currentLine = string.Empty;

            foreach (string word in words)
            {
                if (currentLine.Length == 0)
                {
                    currentLine = word;
                    continue;
                }

                if (currentLine.Length + 1 + word.Length > maxLineLength)
                {
                    lines.Add(currentLine);
                    currentLine = word;
                }
                else
                {
                    currentLine += " " + word;
                }
            }

            if (currentLine.Length > 0)
            {
                lines.Add(currentLine);
            }

            return string.Join("\n", lines);
        }

        private void RefreshView()
        {
            foreach (var pair in _cellPanels)
            {
                pair.Value.Background = GetBrushForCell(pair.Key);
                pair.Value.Widgets.Clear();

                string? text = GetTextForCell(pair.Key);
                if (!string.IsNullOrEmpty(text))
                {
                    pair.Value.Widgets.Add(new Label
                    {
                        Text = text,
                        TextColor = Color.White,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Center
                    });
                }
            }

            _selectedCellLabel.Text = $"Выбрана клетка: ({_selectedCell.X}, {_selectedCell.Y})";
            _objectsCountLabel.Text = $"Дронов: {_drones.Count}/{MaxDrones}; сорняков: {_weeds.Count}";

            bool canAddDrone = _drones.Count < MaxDrones;
            _addDroneButton.TextColor = canAddDrone ? Color.White : new Color(170, 170, 170);
            _addDroneButton.Background = canAddDrone ? _buttonBrush : _disabledButtonBrush;
            _addDroneButton.OverBackground = _addDroneButton.Background;
            _addDroneButton.PressedBackground = _addDroneButton.Background;
            _addDroneButton.FocusedBackground = _addDroneButton.Background;
        }

        private IBrush GetBrushForCell(Point point)
        {
            if (point == _selectedCell)
            {
                if (IsCellOccupied(point))
                    return _blockedBrush;

                return _selectedBrush;
            }

            if (_drones.Contains(point))
                return _droneBrush;

            if (_weeds.Contains(point))
                return _weedBrush;

            return _emptyBrush;
        }

        private string? GetTextForCell(Point point)
        {
            int droneIndex = _drones.IndexOf(point);
            if (droneIndex >= 0)
            {
                return (droneIndex + 1).ToString();
            }

            if (_weeds.Contains(point))
            {
                return "W";
            }

            return null;
        }
    }
}
