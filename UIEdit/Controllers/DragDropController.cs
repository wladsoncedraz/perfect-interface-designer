using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using UIEdit.Models;

// By: SpinxDev 2025 xD
namespace UIEdit.Controllers
{
    public class DragDropController
    {

        #region Injection Properties
        private readonly Canvas _canvas;
        private bool _isDragging;
        private bool _isMouseDown;
        private const double DragStartThreshold = 4.0;
        private Point _dragStartMouse;
        private Point _dragStartMargin;
        private Canvas _activeContainer;
        private UIControl _activeModel;
        private TextBlock _coordsOverlay;
        private readonly List<UIElement> _guideOverlays = new List<UIElement>();
        private readonly List<Canvas> _selectedContainers = new List<Canvas>();
        private readonly List<UIControl> _selectedModels = new List<UIControl>();
        private readonly Dictionary<Canvas, Rectangle> _selectionOverlays = new Dictionary<Canvas, Rectangle>();
        private readonly List<Tuple<Canvas, UIControl, Point>> _dragSelectionStartMargins = new List<Tuple<Canvas, UIControl, Point>>();

        // Properties to main window
        private Canvas _dialogContainer;
        private UIEdit.Models.UIDialog _selectedDialog;
        private bool _isResizing;
        private ResizeDirection _resizeDirection;
        private Point _resizeStartMouse;
        private Size _resizeStartSize;
        private readonly Dictionary<Canvas, List<Rectangle>> _resizeHandles = new Dictionary<Canvas, List<Rectangle>>();

        // Properties to resize containers
        private Canvas _selectedEditContainer;
        private UIControl _selectedEditControl;

        private enum ResizeDirection
        {
            None,
            Right,
            Bottom,
            BottomRight
        }

        public event Action<bool> OnDragStateChanged;
        public event Action OnDragBegin;
        public event Action<UIControl> OnSelectionChanged;
        public event Action<UIControl> OnItemDoubleClick;
        public event Action<UIEdit.Models.UIDialog> OnDialogSelected;
        public event Action<UIEdit.Models.UIDialog> OnDialogResized;

        public int SnapSize { get; set; } = 4;
        public bool SnapEnabled { get; set; } = true;
        public double AlignThreshold { get; set; } = 5.0;
        public UIControl SelectedModel => _selectedModels.FirstOrDefault();
        public Canvas SelectedContainer => _selectedContainers.FirstOrDefault();
        #endregion

        #region Constructor
        public DragDropController(Canvas canvas)
        {
            _canvas = canvas;
            Hook();
        }
        #endregion

        #region Methods
        /// <summary>
        /// Configures event handlers for mouse interactions on the canvas.
        /// </summary>
        private void Hook()
        {
            _canvas.PreviewMouseLeftButtonDown += CanvasOnPreviewMouseLeftButtonDown;
            _canvas.PreviewMouseMove += CanvasOnPreviewMouseMove;
            _canvas.PreviewMouseLeftButtonUp += CanvasOnPreviewMouseLeftButtonUp;
            _canvas.MouseMove += CanvasOnMouseMove;
        }

        /// <summary>
        /// Handles mouse left button down events on the canvas for selection, dragging, and resizing.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CanvasOnPreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var element = e.OriginalSource as FrameworkElement;
            while (element != null && !(element is Canvas && (element.Tag is UIControl || element.Tag is UIEdit.Models.UIDialog)))
            {
                element = element.Parent as FrameworkElement;
            }

            if (element is Canvas container && container.Tag is UIEdit.Models.UIDialog dialog)
            {
                ClearSelection();
                SelectDialog(container, dialog);

                var mousePos = e.GetPosition(container);
                _resizeDirection = GetResizeDirection(container, mousePos);

                if (_resizeDirection != ResizeDirection.None)
                {
                    _isResizing = true;
                    _resizeStartMouse = e.GetPosition(_canvas);
                    _resizeStartSize = new Size(dialog.Width, dialog.Height);
                    _canvas.CaptureMouse();
                    e.Handled = true;
                    return;
                }

                _canvas.Focus();
                e.Handled = true;
                return;
            }

            if (element is Canvas controlContainer && controlContainer.Tag is UIControl model)
            {
                var ctrlPressed = Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl);
                var alreadySelected = _selectedContainers.Contains(controlContainer);

                if (IsMainBackgroundEdit(model))
                {
                    var mousePos = e.GetPosition(controlContainer);
                    _resizeDirection = GetResizeDirection(controlContainer, mousePos);

                    if (_resizeDirection != ResizeDirection.None)
                    {
                        ClearSelection();
                        _selectedEditContainer = controlContainer;
                        _selectedEditControl = model;
                        _isResizing = true;
                        _resizeStartMouse = e.GetPosition(_canvas);
                        _resizeStartSize = new Size(model.Width, model.Height);
                        _canvas.CaptureMouse();
                        e.Handled = true;
                        return;
                    }
                }

                if (ctrlPressed)
                {
                    if (alreadySelected) RemoveFromSelection(controlContainer, model); else AddToSelection(controlContainer, model);
                    OnSelectionChanged?.Invoke(_selectedModels.FirstOrDefault());
                    _canvas.Focus();
                    e.Handled = true;
                    return;
                }

                if (!alreadySelected)
                {
                    ClearSelection();
                    AddToSelection(controlContainer, model);

                    if (IsMainBackgroundEdit(model))
                    {
                        AddDialogResizeHandles(controlContainer);
                        _selectedEditContainer = controlContainer;
                        _selectedEditControl = model;
                    }
                }

                _canvas.Focus();

                if (e.ClickCount == 2)
                {
                    if (_selectedModels.Count == 1) OnItemDoubleClick?.Invoke(_selectedModels[0]);
                    e.Handled = true;
                    return;
                }

                _activeContainer = controlContainer;
                _activeModel = model;
                _isDragging = false;
                _isMouseDown = true;
                _dragStartMouse = e.GetPosition(_canvas);
                _dragStartMargin = new Point(controlContainer.Margin.Left, controlContainer.Margin.Top);
                _dragSelectionStartMargins.Clear();
                foreach (var sc in _selectedContainers)
                {
                    _dragSelectionStartMargins.Add(Tuple.Create(sc, (UIControl)sc.Tag, new Point(sc.Margin.Left, sc.Margin.Top)));
                }
                _canvas.CaptureMouse();
                _canvas.Focus();
                OnDragStateChanged?.Invoke(true);
                OnSelectionChanged?.Invoke(model);
                ClearGuides();
                e.Handled = true;
            }
            else
            {
                ClearSelection();
                OnSelectionChanged?.Invoke(null);
                _canvas.Focus();
            }
        }

        /// <summary>
        /// Handles mouse move events on the canvas for dragging and resizing selected items.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CanvasOnPreviewMouseMove(object sender, MouseEventArgs e)
        {
            if (_isResizing && _selectedDialog != null && _dialogContainer != null)
            {
                var currentMouse = e.GetPosition(_canvas);
                var resizeDeltaX = currentMouse.X - _resizeStartMouse.X;
                var resizeDeltaY = currentMouse.Y - _resizeStartMouse.Y;

                var newWidth = _resizeStartSize.Width;
                var newHeight = _resizeStartSize.Height;

                switch (_resizeDirection)
                {
                    case ResizeDirection.Right:
                        newWidth = Math.Max(100, _resizeStartSize.Width + resizeDeltaX);
                        break;
                    case ResizeDirection.Bottom:
                        newHeight = Math.Max(100, _resizeStartSize.Height + resizeDeltaY);
                        break;
                    case ResizeDirection.BottomRight:
                        newWidth = Math.Max(100, _resizeStartSize.Width + resizeDeltaX);
                        newHeight = Math.Max(100, _resizeStartSize.Height + resizeDeltaY);
                        break;
                }

                if (SnapEnabled && !(Keyboard.IsKeyDown(Key.LeftAlt) || Keyboard.IsKeyDown(Key.RightAlt)))
                {
                    newWidth = Math.Round(newWidth / SnapSize) * SnapSize;
                    newHeight = Math.Round(newHeight / SnapSize) * SnapSize;
                }

                _selectedDialog.Width = newWidth;
                _selectedDialog.Height = newHeight;
                _dialogContainer.Width = newWidth;
                _dialogContainer.Height = newHeight;

                var image = _dialogContainer.Children.OfType<Image>().FirstOrDefault();
                if (image != null)
                {
                    image.Width = newWidth;
                    image.Height = newHeight;
                }

                UpdateDialogResizeHandles(_dialogContainer);
                ShowCoords(newWidth, newHeight);
                OnDialogResized?.Invoke(_selectedDialog);
                return;
            }

            if (_isResizing && _selectedEditControl != null && _selectedEditContainer != null)
            {
                var currentMouse = e.GetPosition(_canvas);
                var resizeDeltaX = currentMouse.X - _resizeStartMouse.X;
                var resizeDeltaY = currentMouse.Y - _resizeStartMouse.Y;

                var newWidth = _resizeStartSize.Width;
                var newHeight = _resizeStartSize.Height;

                switch (_resizeDirection)
                {
                    case ResizeDirection.Right:
                        newWidth = Math.Max(50, _resizeStartSize.Width + resizeDeltaX);
                        break;
                    case ResizeDirection.Bottom:
                        newHeight = Math.Max(50, _resizeStartSize.Height + resizeDeltaY);
                        break;
                    case ResizeDirection.BottomRight:
                        newWidth = Math.Max(50, _resizeStartSize.Width + resizeDeltaX);
                        newHeight = Math.Max(50, _resizeStartSize.Height + resizeDeltaY);
                        break;
                }

                if (SnapEnabled && !(Keyboard.IsKeyDown(Key.LeftAlt) || Keyboard.IsKeyDown(Key.RightAlt)))
                {
                    newWidth = Math.Round(newWidth / SnapSize) * SnapSize;
                    newHeight = Math.Round(newHeight / SnapSize) * SnapSize;
                }

                var oldWidth = _selectedEditControl.Width;
                var oldHeight = _selectedEditControl.Height;

                _selectedEditControl.Width = newWidth;
                _selectedEditControl.Height = newHeight;
                _selectedEditContainer.Width = newWidth;
                _selectedEditContainer.Height = newHeight;

                var image = _selectedEditContainer.Children.OfType<Image>().FirstOrDefault();
                if (image != null)
                {
                    image.Width = newWidth;
                    image.Height = newHeight;
                }

                UpdateDialogResizeHandles(_selectedEditContainer);
                ShowCoords(newWidth, newHeight);
                OnDragStateChanged?.Invoke(true);
                return;
            }

            if (!_isMouseDown || _activeContainer == null || _activeModel == null) return;
            var current = e.GetPosition(_canvas);
            var dx = current.X - _dragStartMouse.X;
            var dy = current.Y - _dragStartMouse.Y;
            if (!_isDragging)
            {
                if (Math.Abs(dx) < DragStartThreshold && Math.Abs(dy) < DragStartThreshold) return;
                _isDragging = true;
                OnDragBegin?.Invoke();
            }
            var targetActiveX = _dragStartMargin.X + dx;
            var targetActiveY = _dragStartMargin.Y + dy;
            var snappedActiveX = targetActiveX;
            var snappedActiveY = targetActiveY;
            if (SnapEnabled && !(Keyboard.IsKeyDown(Key.LeftAlt) || Keyboard.IsKeyDown(Key.RightAlt)))
            {
                snappedActiveX = Math.Round(targetActiveX / SnapSize) * SnapSize;
                snappedActiveY = Math.Round(targetActiveY / SnapSize) * SnapSize;
            }
            var deltaX = snappedActiveX - _dragStartMargin.X;
            var deltaY = snappedActiveY - _dragStartMargin.Y;

            foreach (var entry in _dragSelectionStartMargins)
            {
                var start = entry.Item3;
                var c = entry.Item1;
                var m = entry.Item2;
                var nx = start.X + deltaX;
                var ny = start.Y + deltaY;
                if (nx < 0) nx = 0; if (ny < 0) ny = 0;

                c.Margin = new Thickness(nx, ny, 0, 0);
                m.X = nx;
                m.Y = ny;
            }
            ShowCoords(_activeModel.X, _activeModel.Y);
            UpdateSelectionOverlays();
            RenderAlignmentGuides(_activeModel.X, _activeModel.Y);
        }

        /// <summary>
        /// Handles mouse left button up events on the canvas to finalize dragging or resizing operations.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CanvasOnPreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (_isResizing)
            {
                _isResizing = false;
                _resizeDirection = ResizeDirection.None;
                _canvas.ReleaseMouseCapture();
                HideCoords();

                _selectedEditContainer = null;
                _selectedEditControl = null;

                OnDragStateChanged?.Invoke(false);
                return;
            }

            if (!_isMouseDown) return;
            _isDragging = false;
            _isMouseDown = false;
            _canvas.ReleaseMouseCapture();
            OnDragStateChanged?.Invoke(false);
            HideCoords();
            ClearGuides();
            _activeContainer = null;
            _activeModel = null;
        }

        /// <summary>
        /// Moves all selected items by the specified delta values, applying snapping if enabled.
        /// </summary>
        /// <param name="dx"></param>
        /// <param name="dy"></param>
        /// <param name="applySnap"></param>
        public void MoveSelectedBy(double dx, double dy, bool applySnap)
        {
            if (!_selectedContainers.Any()) return;
            var refC0 = _selectedContainers.First();
            var refTargetX = refC0.Margin.Left + dx;
            var refTargetY = refC0.Margin.Top + dy;
            if ((SnapEnabled && applySnap) && !(Keyboard.IsKeyDown(Key.LeftAlt) || Keyboard.IsKeyDown(Key.RightAlt)))
            {
                refTargetX = Math.Round(refTargetX / SnapSize) * SnapSize;
                refTargetY = Math.Round(refTargetY / SnapSize) * SnapSize;
            }
            var dX = refTargetX - refC0.Margin.Left;
            var dY = refTargetY - refC0.Margin.Top;

            foreach (var c in _selectedContainers)
            {
                var m = (UIControl)c.Tag;
                var nx = c.Margin.Left + dX;
                var ny = c.Margin.Top + dY;
                if (nx < 0) nx = 0; if (ny < 0) ny = 0;
                c.Margin = new Thickness(nx, ny, 0, 0);
                m.X = nx;
                m.Y = ny;
            }
            var refC = _selectedContainers.First();
            ShowCoords(refC.Margin.Left, refC.Margin.Top);
            UpdateSelectionOverlays();
        }

        /// <summary>
        /// Displays the current coordinates (x, y) near the mouse cursor.
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        private void ShowCoords(double x, double y)
        {
            if (_coordsOverlay == null)
            {
                _coordsOverlay = new TextBlock
                {
                    Foreground = Brushes.Yellow,
                    Background = Brushes.Black,
                    Opacity = 0.7,
                    Padding = new Thickness(2, 0, 2, 0)
                };
                _canvas.Children.Add(_coordsOverlay);
            }
            _coordsOverlay.Text = $"({(int)x}, {(int)y})";
            Canvas.SetLeft(_coordsOverlay, x + 4);
            Canvas.SetTop(_coordsOverlay, y - 12 < 0 ? 0 : y - 12);
            Panel.SetZIndex(_coordsOverlay, 10000);
        }

        /// <summary>
        /// Hides the coordinates overlay if it is currently displayed.
        /// </summary>
        private void HideCoords()
        {
            if (_coordsOverlay == null) return;
            _canvas.Children.Remove(_coordsOverlay);
            _coordsOverlay = null;
        }

        /// <summary>
        /// Renders alignment guides on the canvas to assist with positioning.
        /// </summary>
        /// <param name="newX"></param>
        /// <param name="newY"></param>
        private void RenderAlignmentGuides(double newX, double newY)
        {
            if (_activeContainer == null) return;
            ClearGuides();

            var movingLeft = newX;
            var movingRight = newX + _activeContainer.Width;
            var movingTop = newY;
            var movingBottom = newY + _activeContainer.Height;

            var otherContainers = _canvas.Children.OfType<Canvas>()
                .Where(c => c != _activeContainer && c.Tag is UIControl).ToList();

            foreach (var other in otherContainers)
            {
                var otherLeft = other.Margin.Left;
                var otherRight = other.Margin.Left + other.Width;
                var otherTop = other.Margin.Top;
                var otherBottom = other.Margin.Top + other.Height;

                if (Math.Abs(movingLeft - otherLeft) <= AlignThreshold) AddVerticalGuide(otherLeft);
                if (Math.Abs(movingRight - otherRight) <= AlignThreshold) AddVerticalGuide(otherRight);
                if (Math.Abs(movingLeft - otherRight) <= AlignThreshold) AddVerticalGuide(otherRight);
                if (Math.Abs(movingRight - otherLeft) <= AlignThreshold) AddVerticalGuide(otherLeft);

                if (Math.Abs(movingTop - otherTop) <= AlignThreshold) AddHorizontalGuide(otherTop);
                if (Math.Abs(movingBottom - otherBottom) <= AlignThreshold) AddHorizontalGuide(otherBottom);
                if (Math.Abs(movingTop - otherBottom) <= AlignThreshold) AddHorizontalGuide(otherBottom);
                if (Math.Abs(movingBottom - otherTop) <= AlignThreshold) AddHorizontalGuide(otherTop);
            }
        }

        /// <summary>
        /// Adds a vertical alignment guide line at the specified x-coordinate.
        /// </summary>
        /// <param name="x"></param>
        private void AddVerticalGuide(double x)
        {
            var line = new Line
            {
                X1 = x,
                X2 = x,
                Y1 = 0,
                Y2 = Math.Max(_canvas.ActualHeight, _canvas.RenderSize.Height),
                Stroke = Brushes.Red,
                StrokeThickness = 1,
                StrokeDashArray = new DoubleCollection { 3, 3 },
                IsHitTestVisible = false
            };
            Panel.SetZIndex(line, 9999);
            _canvas.Children.Add(line);
            _guideOverlays.Add(line);
        }

        /// <summary>
        /// Adds a horizontal alignment guide line at the specified y-coordinate.
        /// </summary>
        /// <param name="y"></param>
        private void AddHorizontalGuide(double y)
        {
            var line = new Line
            {
                X1 = 0,
                X2 = Math.Max(_canvas.ActualWidth, _canvas.RenderSize.Width),
                Y1 = y,
                Y2 = y,
                Stroke = Brushes.Red,
                StrokeThickness = 1,
                StrokeDashArray = new DoubleCollection { 3, 3 },
                IsHitTestVisible = false
            };
            Panel.SetZIndex(line, 9999);
            _canvas.Children.Add(line);
            _guideOverlays.Add(line);
        }

        /// <summary>
        /// Clears all alignment guide lines from the canvas.
        /// </summary>
        private void ClearGuides()
        {
            foreach (var g in _guideOverlays) _canvas.Children.Remove(g);
            _guideOverlays.Clear();
        }

        /// <summary>
        /// Adds the specified container and model to the current selection, applying visual highlighting.
        /// </summary>
        /// <param name="container"></param>
        /// <param name="model"></param>
        private void AddToSelection(Canvas container, UIControl model)
        {
            if (!_selectedContainers.Contains(container))
            {
                _selectedContainers.Add(container);
                _selectedModels.Add(model);
                var rect = new Rectangle { Stroke = Brushes.LimeGreen, StrokeThickness = 2, Fill = Brushes.Transparent, IsHitTestVisible = false };
                container.Children.Add(rect);
                rect.Width = container.Width; rect.Height = container.Height;
                Canvas.SetLeft(rect, 0); Canvas.SetTop(rect, 0);
                Panel.SetZIndex(rect, 9999);
                _selectionOverlays[container] = rect;
                OnSelectionChanged?.Invoke(model);
            }
        }

        /// <summary>
        /// Removes the specified container and model from the current selection, removing visual highlighting.
        /// </summary>
        /// <param name="container"></param>
        /// <param name="model"></param>
        private void RemoveFromSelection(Canvas container, UIControl model)
        {
            if (_selectedContainers.Contains(container))
            {
                _selectedContainers.Remove(container);
                _selectedModels.Remove(model);
                if (_selectionOverlays.TryGetValue(container, out var rect))
                {
                    if (rect.Parent is Panel p) p.Children.Remove(rect);
                    _selectionOverlays.Remove(container);
                }
                OnSelectionChanged?.Invoke(_selectedModels.FirstOrDefault());
            }
        }

        /// <summary>
        /// Updates the visual overlays for all selected items to match their current size and position.
        /// </summary>
        private void UpdateSelectionOverlays()
        {
            foreach (var kv in _selectionOverlays.ToList())
            {
                var c = kv.Key; var r = kv.Value;
                r.Width = c.Width; r.Height = c.Height;
                Canvas.SetLeft(r, 0); Canvas.SetTop(r, 0);
            }
        }

        /// <summary>
        /// Clears the current selection, removing all visual highlights and resetting selection state.
        /// </summary>
        private void ClearSelection()
        {
            foreach (var kv in _selectionOverlays)
            {
                if (kv.Value.Parent is Panel p) p.Children.Remove(kv.Value);
            }
            _selectionOverlays.Clear();
            _selectedContainers.Clear();
            _selectedModels.Clear();

            ClearDialogSelection();

            OnSelectionChanged?.Invoke(null);
        }

        /// <summary>
        /// Selects a dialog (UIDialog) on the canvas, highlighting it and adding resize handles.
        /// </summary>
        /// <param name="container"></param>
        /// <param name="dialog"></param>
        public void SelectDialog(Canvas container, UIEdit.Models.UIDialog dialog)
        {
            _dialogContainer = container;
            _selectedDialog = dialog;

            var rect = new Rectangle
            {
                Stroke = Brushes.Blue,
                StrokeThickness = 2,
                Fill = Brushes.Transparent,
                IsHitTestVisible = false
            };
            container.Children.Add(rect);
            rect.Width = container.Width;
            rect.Height = container.Height;
            Canvas.SetLeft(rect, 0);
            Canvas.SetTop(rect, 0);
            Panel.SetZIndex(rect, 9999);

            AddDialogResizeHandles(container);

            OnDialogSelected?.Invoke(dialog);
        }

        /// <summary>
        /// Clears the current dialog selection, removing visual highlights and resetting state.
        /// </summary>
        private void ClearDialogSelection()
        {
            if (_dialogContainer != null)
            {
                var selectionRect = _dialogContainer.Children.OfType<Rectangle>()
                    .FirstOrDefault(r => r.Stroke == Brushes.Blue);
                if (selectionRect != null)
                {
                    _dialogContainer.Children.Remove(selectionRect);
                }

                RemoveDialogResizeHandles(_dialogContainer);
            }

            if (_selectedEditContainer != null)
            {
                RemoveDialogResizeHandles(_selectedEditContainer);
            }

            _dialogContainer = null;
            _selectedDialog = null;
            _selectedEditContainer = null;
            _selectedEditControl = null;
        }

        /// <summary>
        /// Adds resize handles to the specified container for resizing functionality.
        /// </summary>
        /// <param name="container"></param>
        private void AddDialogResizeHandles(Canvas container)
        {
            if (!_resizeHandles.ContainsKey(container))
            {
                _resizeHandles[container] = new List<Rectangle>();
            }

            var handles = _resizeHandles[container];
            handles.Clear();

            // Right
            var rightHandle = CreateResizeHandle();
            Canvas.SetLeft(rightHandle, container.Width - 5);
            Canvas.SetTop(rightHandle, container.Height / 2 - 5);
            container.Children.Add(rightHandle);
            handles.Add(rightHandle);

            // Left
            var bottomHandle = CreateResizeHandle();
            Canvas.SetLeft(bottomHandle, container.Width / 2 - 5);
            Canvas.SetTop(bottomHandle, container.Height - 5);
            container.Children.Add(bottomHandle);
            handles.Add(bottomHandle);

            // Bottom Right
            var bottomRightHandle = CreateResizeHandle();
            Canvas.SetLeft(bottomRightHandle, container.Width - 5);
            Canvas.SetTop(bottomRightHandle, container.Height - 5);
            container.Children.Add(bottomRightHandle);
            handles.Add(bottomRightHandle);
        }

        /// <summary>
        /// Creates a resize handle rectangle for use in resizing controls.
        /// </summary>
        /// <returns></returns>
        private Rectangle CreateResizeHandle()
        {
            return new Rectangle
            {
                Width = 10,
                Height = 10,
                Fill = Brushes.White,
                Stroke = Brushes.Black,
                StrokeThickness = 1,
                Cursor = Cursors.SizeNWSE
            };
        }

        /// <summary>
        /// Removes resize handles from the specified container.
        /// </summary>
        /// <param name="container"></param>
        private void RemoveDialogResizeHandles(Canvas container)
        {
            if (_resizeHandles.TryGetValue(container, out var handles))
            {
                foreach (var handle in handles)
                {
                    container.Children.Remove(handle);
                }
                handles.Clear();
            }
        }

        /// <summary>
        /// Updates the positions of resize handles on the specified container.
        /// </summary>
        /// <param name="container"></param>
        private void UpdateDialogResizeHandles(Canvas container)
        {
            if (!_resizeHandles.TryGetValue(container, out var handles) || handles.Count < 3) return;

            var rightHandle = handles[0];
            Canvas.SetLeft(rightHandle, container.Width - 5);
            Canvas.SetTop(rightHandle, container.Height / 2 - 5);

            var bottomHandle = handles[1];
            Canvas.SetLeft(bottomHandle, container.Width / 2 - 5);
            Canvas.SetTop(bottomHandle, container.Height - 5);

            var bottomRightHandle = handles[2];
            Canvas.SetLeft(bottomRightHandle, container.Width - 5);
            Canvas.SetTop(bottomRightHandle, container.Height - 5);
        }

        /// <summary>
        /// Determines the resize direction based on mouse position relative to resize handles.
        /// </summary>
        /// <param name="container"></param>
        /// <param name="mousePos"></param>
        /// <returns></returns>
        private ResizeDirection GetResizeDirection(Canvas container, Point mousePos)
        {
            if (!_resizeHandles.TryGetValue(container, out var handles) || handles.Count < 3)
            {
                return ResizeDirection.None;
            }

            const double handleSize = 10;

            var rightHandle = handles[0];
            var rightPos = new Point(Canvas.GetLeft(rightHandle), Canvas.GetTop(rightHandle));
            if (IsPointInHandle(mousePos, rightPos, handleSize))
            {
                return ResizeDirection.Right;
            }

            var bottomHandle = handles[1];
            var bottomPos = new Point(Canvas.GetLeft(bottomHandle), Canvas.GetTop(bottomHandle));
            if (IsPointInHandle(mousePos, bottomPos, handleSize))
            {
                return ResizeDirection.Bottom;
            }

            var bottomRightHandle = handles[2];
            var bottomRightPos = new Point(Canvas.GetLeft(bottomRightHandle), Canvas.GetTop(bottomRightHandle));
            if (IsPointInHandle(mousePos, bottomRightPos, handleSize))
            {
                return ResizeDirection.BottomRight;
            }

            return ResizeDirection.None;
        }

        /// <summary>
        /// Verifys if a point is within a resize handle area.
        /// </summary>
        /// <param name="point"></param>
        /// <param name="handlePos"></param>
        /// <param name="handleSize"></param>
        /// <returns></returns>
        private bool IsPointInHandle(Point point, Point handlePos, double handleSize)
        {
            return point.X >= handlePos.X && point.X <= handlePos.X + handleSize &&
                   point.Y >= handlePos.Y && point.Y <= handlePos.Y + handleSize;
        }

        /// <summary>
        /// Determines if a given UIControl is considered a main background EDIT based on its type, name, and size.
        /// </summary>
        /// <param name="control"></param>
        /// <returns></returns>
        private bool IsMainBackgroundEdit(UIControl control)
        {
            if (!(control is UIEdit.Models.UIEditBox edit)) return false;

            var name = edit.Name?.ToLower() ?? "";
            if (name.Contains("bg") || name.Contains("background") || name.Contains("fundo"))
            {
                return true;
            }

            if (edit.Width > 200 && edit.Height > 200)
            {
                return true;
            }

            if (edit.Width > 300 || edit.Height > 300)
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Handles mouse move events on the canvas to update the cursor based on position for resizing.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CanvasOnMouseMove(object sender, MouseEventArgs e)
        {
            if (_isResizing || _isDragging) return;

            if (_dialogContainer != null && _selectedDialog != null)
            {
                var mousePos = e.GetPosition(_dialogContainer);
                var direction = GetResizeDirection(_dialogContainer, mousePos);

                switch (direction)
                {
                    case ResizeDirection.Right:
                        _canvas.Cursor = Cursors.SizeWE;
                        break;
                    case ResizeDirection.Bottom:
                        _canvas.Cursor = Cursors.SizeNS;
                        break;
                    case ResizeDirection.BottomRight:
                        _canvas.Cursor = Cursors.SizeNWSE;
                        break;
                    default:
                        _canvas.Cursor = Cursors.Arrow;
                        break;
                }
                return;
            }

            if (_selectedEditContainer != null && _selectedEditControl != null)
            {
                var mousePos = e.GetPosition(_selectedEditContainer);
                var direction = GetResizeDirection(_selectedEditContainer, mousePos);

                switch (direction)
                {
                    case ResizeDirection.Right:
                        _canvas.Cursor = Cursors.SizeWE;
                        break;
                    case ResizeDirection.Bottom:
                        _canvas.Cursor = Cursors.SizeNS;
                        break;
                    case ResizeDirection.BottomRight:
                        _canvas.Cursor = Cursors.SizeNWSE;
                        break;
                    default:
                        _canvas.Cursor = Cursors.Arrow;
                        break;
                }
                return;
            }

            var hitElement = e.OriginalSource as FrameworkElement;
            while (hitElement != null && !(hitElement is Canvas && hitElement.Tag is UIControl))
            {
                hitElement = hitElement.Parent as FrameworkElement;
            }

            if (hitElement is Canvas container && container.Tag is UIControl control && IsMainBackgroundEdit(control))
            {
                var mousePos = e.GetPosition(container);
                var direction = GetResizeDirection(container, mousePos);

                switch (direction)
                {
                    case ResizeDirection.Right:
                        _canvas.Cursor = Cursors.SizeWE;
                        break;
                    case ResizeDirection.Bottom:
                        _canvas.Cursor = Cursors.SizeNS;
                        break;
                    case ResizeDirection.BottomRight:
                        _canvas.Cursor = Cursors.SizeNWSE;
                        break;
                    default:
                        _canvas.Cursor = Cursors.Arrow;
                        break;
                }
            }
            else
            {
                _canvas.Cursor = Cursors.Arrow;
            }
        }

        /// <summary>
        /// Selects a control (UIControl) on the canvas, clearing previous selection and highlighting the new selection.
        /// </summary>
        /// <param name="container"></param>
        public void SelectControl(Canvas container)
        {
            if (container?.Tag is UIControl control)
            {
                ClearSelection();
                AddToSelection(container, control);
                OnSelectionChanged?.Invoke(control);
            }
        }
        #endregion
    }
}


