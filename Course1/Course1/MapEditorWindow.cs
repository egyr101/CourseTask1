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
    /// Экран редактора карт. Позволяет изменить расположение дронов и сорняков
    /// без изменения размеров карты.
    /// </summary>
    public sealed class MapEditorWindow : Panel
    {
        private const int MaxDrones = 10;
        private const int EditorCellSize = 24;
        private const int ControlPanelWidth = 440;
        private const int ControlTextWrapLength = 46;

        public event Action<string, LevelConfig>? SaveRequested;
        public event Action? CloseRequested;

        private readonly int _gridWidth;
        private readonly int _gridHeight;
        private readonly Dictionary<Point, Panel> _cellPanels = new();
        private readonly List<Point> _drones = new();
        private readonly List<Point> _weeds = new();

        private GameLanguage _language = GameLanguage.Russian;
        private Point _selectedCell;
        private bool _lastStatusIsError;
        private Func<string>? _lastStatusTextProvider;

        private Label _titleLabel;
        private Label _mapNameLabel;
        private TextBox _mapNameTextBox;
        private Label _selectedCellLabel;
        private Label _objectsCountLabel;
        private Label _statusLabel;
        private Label _restrictionsTitleLabel;
        private Label _restrictionMaxDronesLabel;
        private Label _restrictionOccupiedLabel;
        private Label _restrictionWeedsLabel;

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

        public void SetLanguage(GameLanguage language)
        {
            _language = language;
            UpdateStaticTexts();
            RefreshStatusText();
            RefreshView();
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

            SetStatus(() => TextSelectCellAndAction(), false);
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
                Text = TextMapEditorTitle(),
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
            var gridPanel = new Panel
            {
                Width = _gridWidth * EditorCellSize + 2,
                Height = _gridHeight * EditorCellSize + 2,
                Padding = new Thickness(1),
                Background = new SolidBrush(new Color(80, 130, 90))
            };

            var grid = new Grid
            {
                ShowGridLines = true,
                GridLinesColor = Color.White,
                Width = _gridWidth * EditorCellSize,
                Height = _gridHeight * EditorCellSize
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
                        SetStatus(() => TextSelectedCell(point), false);
                        RefreshView();
                    };

                    _cellPanels[point] = cell;
                    grid.Widgets.Add(cell);
                }
            }

            gridPanel.Widgets.Add(grid);
            return gridPanel;
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
                Text = TextMapNameLabel(),
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

            _addDroneButton = CreateButton(TextAddDrone(), AddDroneAtSelectedCell);
            _addWeedButton = CreateButton(TextAddWeed(), AddWeedAtSelectedCell);
            _removeDroneButton = CreateButton(TextRemoveDrone(), RemoveDroneAtSelectedCell);
            _removeWeedButton = CreateButton(TextRemoveWeed(), RemoveWeedAtSelectedCell);

            panel.Widgets.Add(_addDroneButton);
            panel.Widgets.Add(_addWeedButton);
            panel.Widgets.Add(_removeDroneButton);
            panel.Widgets.Add(_removeWeedButton);

            _restrictionsTitleLabel = CreateControlLabel(TextRestrictionsTitle(), topMargin: 8);
            _restrictionMaxDronesLabel = CreateControlLabel(TextRestrictionMaxDrones());
            _restrictionOccupiedLabel = CreateControlLabel(TextRestrictionOccupied());
            _restrictionWeedsLabel = CreateControlLabel(TextRestrictionWeeds());

            panel.Widgets.Add(_restrictionsTitleLabel);
            panel.Widgets.Add(_restrictionMaxDronesLabel);
            panel.Widgets.Add(_restrictionOccupiedLabel);
            panel.Widgets.Add(_restrictionWeedsLabel);

            panel.Widgets.Add(_statusLabel);

            var bottomButtons = new VerticalStackPanel
            {
                Spacing = 8,
                Margin = new Thickness(0, 8, 0, 0)
            };

            _applyButton = CreateButton(TextSaveAndApply(), ApplyLevel);
            _closeButton = CreateButton(TextClose(), () => CloseRequested?.Invoke());
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
                SetStatus(() => TextCannotAddDroneLimit(), true);
                RefreshView();
                return;
            }

            if (IsCellOccupied(_selectedCell))
            {
                SetStatus(() => TextCellOccupied(), true);
                RefreshView();
                return;
            }

            _drones.Add(_selectedCell);
            SetStatus(() => TextDroneAdded(), false);
            RefreshView();
        }

        private void AddWeedAtSelectedCell()
        {
            if (IsCellOccupied(_selectedCell))
            {
                SetStatus(() => TextCellOccupied(), true);
                RefreshView();
                return;
            }

            _weeds.Add(_selectedCell);
            SetStatus(() => TextWeedAdded(), false);
            RefreshView();
        }

        private void RemoveDroneAtSelectedCell()
        {
            if (!_drones.Remove(_selectedCell))
            {
                SetStatus(() => TextNoDroneInCell(), true);
                RefreshView();
                return;
            }

            SetStatus(() => TextDroneRemoved(), false);
            RefreshView();
        }

        private void RemoveWeedAtSelectedCell()
        {
            if (!_weeds.Remove(_selectedCell))
            {
                SetStatus(() => TextNoWeedInCell(), true);
                RefreshView();
                return;
            }

            SetStatus(() => TextWeedRemoved(), false);
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
                SetStatus(() => TextNeedAtLeastOneDrone(), true);
                RefreshView();
                return;
            }

            if (_weeds.Count < _drones.Count)
            {
                SetStatus(() => TextCannotSaveNeedWeeds(), true);
                RefreshView();
                return;
            }

            string mapName = _mapNameTextBox.Text?.Trim() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(mapName))
            {
                SetStatus(() => TextEnterMapName(), true);
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

        private void SetStatus(Func<string> textProvider, bool isError)
        {
            _lastStatusTextProvider = textProvider;
            _lastStatusIsError = isError;
            RefreshStatusText();
        }

        private void RefreshStatusText()
        {
            if (_statusLabel == null)
                return;

            string message = _lastStatusTextProvider != null
                ? _lastStatusTextProvider()
                : string.Empty;

            _statusLabel.Text = WrapText(message, ControlTextWrapLength);
            _statusLabel.TextColor = _lastStatusIsError
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

            _selectedCellLabel.Text = TextSelectedCellLabel();
            _objectsCountLabel.Text = TextObjectsCount();

            if (_weeds.Count < _drones.Count)
            {
                _objectsCountLabel.Text += "\n" + TextNeedMoreWeedsForSaving();
                _objectsCountLabel.TextColor = new Color(170, 55, 45);
            }
            else
            {
                _objectsCountLabel.TextColor = Color.Black;
            }

            bool canAddDrone = _drones.Count < MaxDrones;
            _addDroneButton.TextColor = canAddDrone ? Color.White : new Color(170, 170, 170);
            _addDroneButton.Background = canAddDrone ? _buttonBrush : _disabledButtonBrush;
            _addDroneButton.OverBackground = _addDroneButton.Background;
            _addDroneButton.PressedBackground = _addDroneButton.Background;
            _addDroneButton.FocusedBackground = _addDroneButton.Background;
        }

        private void UpdateStaticTexts()
        {
            if (_titleLabel == null)
                return;

            _titleLabel.Text = TextMapEditorTitle();
            _mapNameLabel.Text = TextMapNameLabel();
            _addDroneButton.Text = TextAddDrone();
            _addWeedButton.Text = TextAddWeed();
            _removeDroneButton.Text = TextRemoveDrone();
            _removeWeedButton.Text = TextRemoveWeed();
            _applyButton.Text = TextSaveAndApply();
            _closeButton.Text = TextClose();
            _restrictionsTitleLabel.Text = WrapText(TextRestrictionsTitle(), ControlTextWrapLength);
            _restrictionMaxDronesLabel.Text = WrapText(TextRestrictionMaxDrones(), ControlTextWrapLength);
            _restrictionOccupiedLabel.Text = WrapText(TextRestrictionOccupied(), ControlTextWrapLength);
            _restrictionWeedsLabel.Text = WrapText(TextRestrictionWeeds(), ControlTextWrapLength);
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
                return _language == GameLanguage.Russian ? "C" : "W";
            }

            return null;
        }

        private string TextMapEditorTitle() => _language == GameLanguage.Russian ? "Редактор карт" : "Map editor";
        private string TextMapNameLabel() => _language == GameLanguage.Russian ? "Название карты" : "Map name";
        private string TextAddDrone() => _language == GameLanguage.Russian ? "Добавить дрон" : "Add drone";
        private string TextAddWeed() => _language == GameLanguage.Russian ? "Добавить сорняк" : "Add weed";
        private string TextRemoveDrone() => _language == GameLanguage.Russian ? "Удалить дрон" : "Remove drone";
        private string TextRemoveWeed() => _language == GameLanguage.Russian ? "Удалить сорняк" : "Remove weed";
        private string TextSaveAndApply() => _language == GameLanguage.Russian ? "Сохранить и применить" : "Save and apply";
        private string TextClose() => _language == GameLanguage.Russian ? "Закрыть" : "Close";
        private string TextRestrictionsTitle() => _language == GameLanguage.Russian ? "Ограничения:" : "Rules:";
        private string TextRestrictionMaxDrones() => _language == GameLanguage.Russian ? "- максимум 10 дронов;" : "- maximum 10 drones;";
        private string TextRestrictionOccupied() => _language == GameLanguage.Russian ? "- дрон и сорняк не могут стоять в одной клетке;" : "- a drone and a weed cannot occupy the same cell;";
        private string TextRestrictionWeeds() => _language == GameLanguage.Russian ? "- сорняков должно быть не меньше, чем дронов." : "- the number of weeds must be at least the number of drones.";
        private string TextSelectCellAndAction() => _language == GameLanguage.Russian ? "Выберите клетку и действие." : "Select a cell and an action.";
        private string TextSelectedCell(Point point) => _language == GameLanguage.Russian ? $"Выбрана клетка ({point.X}, {point.Y})." : $"Selected cell ({point.X}, {point.Y}).";
        private string TextCannotAddDroneLimit() => _language == GameLanguage.Russian ? "Нельзя добавить дрон: достигнут лимит 10 дронов." : "Cannot add a drone: the limit of 10 drones has been reached.";
        private string TextCellOccupied() => _language == GameLanguage.Russian ? "Клетка уже занята. Выберите свободную клетку." : "The cell is already occupied. Select an empty cell.";
        private string TextDroneAdded() => _language == GameLanguage.Russian ? "Дрон добавлен." : "Drone added.";
        private string TextWeedAdded() => _language == GameLanguage.Russian ? "Сорняк добавлен." : "Weed added.";
        private string TextNoDroneInCell() => _language == GameLanguage.Russian ? "В выбранной клетке нет дрона." : "There is no drone in the selected cell.";
        private string TextNoWeedInCell() => _language == GameLanguage.Russian ? "В выбранной клетке нет сорняка." : "There is no weed in the selected cell.";
        private string TextDroneRemoved() => _language == GameLanguage.Russian ? "Дрон удалён." : "Drone removed.";
        private string TextWeedRemoved() => _language == GameLanguage.Russian ? "Сорняк удалён." : "Weed removed.";
        private string TextNeedAtLeastOneDrone() => _language == GameLanguage.Russian ? "На карте должен быть хотя бы один дрон." : "The map must contain at least one drone.";
        private string TextCannotSaveNeedWeeds() => _language == GameLanguage.Russian ? "Нельзя сохранить карту: сорняков должно быть не меньше, чем дронов." : "Cannot save the map: the number of weeds must be at least the number of drones.";
        private string TextEnterMapName() => _language == GameLanguage.Russian ? "Введите название карты." : "Enter a map name.";
        private string TextSelectedCellLabel() => _language == GameLanguage.Russian ? $"Выбрана клетка: ({_selectedCell.X}, {_selectedCell.Y})" : $"Selected cell: ({_selectedCell.X}, {_selectedCell.Y})";
        private string TextObjectsCount() => _language == GameLanguage.Russian ? $"Дронов: {_drones.Count}/{MaxDrones}; сорняков: {_weeds.Count}" : $"Drones: {_drones.Count}/{MaxDrones}; weeds: {_weeds.Count}";
        private string TextNeedMoreWeedsForSaving() => _language == GameLanguage.Russian ? "Для сохранения нужно больше сорняков." : "More weeds are required before saving.";
    }
}
