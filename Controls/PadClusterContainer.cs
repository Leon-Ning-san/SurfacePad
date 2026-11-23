using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace StardewSurfacePad.Controls;

/// <summary>
/// Draggable edit-mode container for each Switch Pro control cluster.
/// Inherits from Grid (without its own XAML NameScope) so named child controls remain accessible in MainWindow.
/// </summary>
public sealed class PadClusterContainer : Grid
{
    private readonly ScaleTransform _scaleTransform = new(1, 1);
    private readonly Border _editOverlay;
    private readonly TextBlock _titleText;

    private int? _dragTouchId;
    private bool _mouseDragging;
    private Point _dragStartOnCanvas;
    private Point _initialCanvasPos;

    public event EventHandler<Point>? DragCompleted;

    public static readonly DependencyProperty ClusterTitleProperty =
        DependencyProperty.Register(nameof(ClusterTitle), typeof(string), typeof(PadClusterContainer),
            new PropertyMetadata(string.Empty, (d, _) => ((PadClusterContainer)d)._titleText.Text = ((PadClusterContainer)d).ClusterTitle));

    public static readonly DependencyProperty IsEditModeProperty =
        DependencyProperty.Register(nameof(IsEditMode), typeof(bool), typeof(PadClusterContainer),
            new PropertyMetadata(false, (d, _) => ((PadClusterContainer)d).UpdateEditOverlay()));

    public static readonly DependencyProperty ControlScaleProperty =
        DependencyProperty.Register(nameof(ControlScale), typeof(double), typeof(PadClusterContainer),
            new PropertyMetadata(1.0, (d, _) =>
            {
                var c = (PadClusterContainer)d;
                c._scaleTransform.ScaleX = c.ControlScale;
                c._scaleTransform.ScaleY = c.ControlScale;
            }));

    public string ClusterTitle
    {
        get => (string)GetValue(ClusterTitleProperty);
        set => SetValue(ClusterTitleProperty, value);
    }

    public bool IsEditMode
    {
        get => (bool)GetValue(IsEditModeProperty);
        set => SetValue(IsEditModeProperty, value);
    }

    public double ControlScale
    {
        get => (double)GetValue(ControlScaleProperty);
        set => SetValue(ControlScaleProperty, value);
    }

    public PadClusterContainer()
    {
        IsManipulationEnabled = false;
        RenderTransformOrigin = new Point(0.5, 0.5);
        RenderTransform = _scaleTransform;
        TouchInputHelper.DisableSystemTouchFeedback(this);

        _titleText = new TextBlock
        {
            Foreground = Brushes.White,
            FontSize = 10.5,
            FontWeight = FontWeights.SemiBold
        };

        var badge = new Border
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Top,
            Margin = new Thickness(0, -10, 0, 0),
            Padding = new Thickness(7, 1, 7, 1),
            CornerRadius = new CornerRadius(6),
            Background = new SolidColorBrush(Color.FromArgb(0xE6, 0xE6, 0x00, 0x12)),
            Child = _titleText
        };

        _editOverlay = new Border
        {
            Visibility = Visibility.Collapsed,
            Background = new SolidColorBrush(Color.FromArgb(0x2A, 0x14, 0x14, 0x14)),
            BorderBrush = new SolidColorBrush(Color.FromArgb(0xCC, 0xE6, 0x00, 0x12)),
            BorderThickness = new Thickness(1.6),
            CornerRadius = new CornerRadius(14),
            Cursor = Cursors.SizeAll,
            Child = badge
        };
        SetZIndex(_editOverlay, 999);

        Loaded += (_, _) =>
        {
            if (!Children.Contains(_editOverlay))
            {
                Children.Add(_editOverlay);
            }
            UpdateEditOverlay();
        };
    }

    private void UpdateEditOverlay()
    {
        _editOverlay.Visibility = IsEditMode ? Visibility.Visible : Visibility.Collapsed;
        foreach (UIElement child in Children)
        {
            if (!ReferenceEquals(child, _editOverlay))
            {
                child.IsHitTestVisible = !IsEditMode;
            }
        }
    }

    private Canvas? ParentCanvas => Parent as Canvas;

    private void BeginDrag(Point canvasPoint)
    {
        _dragStartOnCanvas = canvasPoint;
        double left = Canvas.GetLeft(this);
        double top = Canvas.GetTop(this);
        _initialCanvasPos = new Point(double.IsNaN(left) ? 0 : left, double.IsNaN(top) ? 0 : top);
    }

    private void MoveDrag(Point canvasPoint)
    {
        if (ParentCanvas is null) return;

        double nextX = _initialCanvasPos.X + (canvasPoint.X - _dragStartOnCanvas.X);
        double nextY = _initialCanvasPos.Y + (canvasPoint.Y - _dragStartOnCanvas.Y);

        double maxX = Math.Max(0, ParentCanvas.ActualWidth - ActualWidth);
        double maxY = Math.Max(0, ParentCanvas.ActualHeight - ActualHeight);

        nextX = Math.Clamp(nextX, 0, maxX);
        nextY = Math.Clamp(nextY, 0, maxY);

        Canvas.SetLeft(this, nextX);
        Canvas.SetTop(this, nextY);
    }

    private void EndDrag()
    {
        double left = Canvas.GetLeft(this);
        double top = Canvas.GetTop(this);
        DragCompleted?.Invoke(this, new Point(double.IsNaN(left) ? 0 : left, double.IsNaN(top) ? 0 : top));
    }

    protected override void OnPreviewTouchDown(TouchEventArgs e)
    {
        if (!IsEditMode || ParentCanvas is null)
        {
            base.OnPreviewTouchDown(e);
            return;
        }

        if (_dragTouchId is null)
        {
            _dragTouchId = e.TouchDevice.Id;
            CaptureTouch(e.TouchDevice);
            BeginDrag(e.GetTouchPoint(ParentCanvas).Position);
        }
        e.Handled = true;
    }

    protected override void OnPreviewTouchMove(TouchEventArgs e)
    {
        if (!IsEditMode || ParentCanvas is null || _dragTouchId != e.TouchDevice.Id)
        {
            base.OnPreviewTouchMove(e);
            return;
        }

        MoveDrag(e.GetTouchPoint(ParentCanvas).Position);
        e.Handled = true;
    }

    protected override void OnPreviewTouchUp(TouchEventArgs e)
    {
        if (_dragTouchId != e.TouchDevice.Id)
        {
            base.OnPreviewTouchUp(e);
            return;
        }

        _dragTouchId = null;
        ReleaseTouchCapture(e.TouchDevice);
        EndDrag();
        e.Handled = true;
    }

    protected override void OnPreviewMouseLeftButtonDown(MouseButtonEventArgs e)
    {
        if (!IsEditMode || ParentCanvas is null || !TouchInputHelper.IsPureMouseOrPen(e))
        {
            base.OnPreviewMouseLeftButtonDown(e);
            return;
        }

        _mouseDragging = true;
        CaptureMouse();
        BeginDrag(e.GetPosition(ParentCanvas));
        e.Handled = true;
    }

    protected override void OnPreviewMouseMove(MouseEventArgs e)
    {
        if (!_mouseDragging || ParentCanvas is null || !TouchInputHelper.IsPureMouseOrPen(e))
        {
            base.OnPreviewMouseMove(e);
            return;
        }

        MoveDrag(e.GetPosition(ParentCanvas));
        e.Handled = true;
    }

    protected override void OnPreviewMouseLeftButtonUp(MouseButtonEventArgs e)
    {
        if (!_mouseDragging || !TouchInputHelper.IsPureMouseOrPen(e))
        {
            base.OnPreviewMouseLeftButtonUp(e);
            return;
        }

        _mouseDragging = false;
        ReleaseMouseCapture();
        EndDrag();
        e.Handled = true;
    }
}
