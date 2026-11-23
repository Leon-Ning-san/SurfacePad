using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace StardewSurfacePad.Controls;

public partial class SwitchDPad : UserControl
{
    private int? _activeTouchId;
    private bool _mouseDragging;
    private (bool Up, bool Down, bool Left, bool Right) _currentState;

    public event EventHandler<(bool Up, bool Down, bool Left, bool Right)>? DirectionChanged;

    public SwitchDPad()
    {
        InitializeComponent();
        TouchInputHelper.DisableSystemTouchFeedback(this);
    }

    private void UpdateDirectionFromPoint(Point pt)
    {
        double cx = ActualWidth / 2.0;
        double cy = ActualHeight / 2.0;
        double dx = pt.X - cx;
        double dy = pt.Y - cy;
        double radius = Math.Min(ActualWidth, ActualHeight) / 2.0;
        double deadzone = radius * 0.16;

        double dist = Math.Sqrt(dx * dx + dy * dy);
        if (dist < deadzone)
        {
            ApplyState((false, false, false, false));
            return;
        }

        double nx = dx / dist;
        double ny = dy / dist;

        const double axisThreshold = 0.42; // allows crisp 4-way + clean diagonals
        bool up = ny < -axisThreshold;
        bool down = ny > axisThreshold;
        bool left = nx < -axisThreshold;
        bool right = nx > axisThreshold;

        ApplyState((up, down, left, right));
    }

    private void ApplyState((bool Up, bool Down, bool Left, bool Right) next)
    {
        if (next == _currentState) return;
        _currentState = next;

        UpHighlight.Opacity = next.Up ? 1.0 : 0.0;
        DownHighlight.Opacity = next.Down ? 1.0 : 0.0;
        LeftHighlight.Opacity = next.Left ? 1.0 : 0.0;
        RightHighlight.Opacity = next.Right ? 1.0 : 0.0;

        DirectionChanged?.Invoke(this, next);
    }

    protected override void OnTouchDown(TouchEventArgs e)
    {
        base.OnTouchDown(e);
        if (_activeTouchId is null)
        {
            _activeTouchId = e.TouchDevice.Id;
            CaptureTouch(e.TouchDevice);
            UpdateDirectionFromPoint(e.GetTouchPoint(this).Position);
        }
        e.Handled = true;
    }

    protected override void OnTouchMove(TouchEventArgs e)
    {
        base.OnTouchMove(e);
        if (_activeTouchId == e.TouchDevice.Id)
        {
            UpdateDirectionFromPoint(e.GetTouchPoint(this).Position);
            e.Handled = true;
        }
    }

    protected override void OnTouchUp(TouchEventArgs e)
    {
        base.OnTouchUp(e);
        if (_activeTouchId == e.TouchDevice.Id)
        {
            _activeTouchId = null;
            ReleaseTouchCapture(e.TouchDevice);
            ApplyState((false, false, false, false));
            e.Handled = true;
        }
    }

    protected override void OnLostTouchCapture(TouchEventArgs e)
    {
        base.OnLostTouchCapture(e);
        if (_activeTouchId == e.TouchDevice.Id)
        {
            _activeTouchId = null;
            ApplyState((false, false, false, false));
        }
    }

    protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
    {
        base.OnMouseLeftButtonDown(e);
        if (!TouchInputHelper.IsPureMouseOrPen(e)) return;

        _mouseDragging = true;
        CaptureMouse();
        UpdateDirectionFromPoint(e.GetPosition(this));
        e.Handled = true;
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);
        if (!TouchInputHelper.IsPureMouseOrPen(e)) return;

        if (_mouseDragging)
        {
            UpdateDirectionFromPoint(e.GetPosition(this));
            e.Handled = true;
        }
    }

    protected override void OnMouseLeftButtonUp(MouseButtonEventArgs e)
    {
        base.OnMouseLeftButtonUp(e);
        if (!TouchInputHelper.IsPureMouseOrPen(e)) return;

        if (_mouseDragging)
        {
            _mouseDragging = false;
            ReleaseMouseCapture();
            ApplyState((false, false, false, false));
            e.Handled = true;
        }
    }

    protected override void OnLostMouseCapture(MouseEventArgs e)
    {
        base.OnLostMouseCapture(e);
        if (_mouseDragging)
        {
            _mouseDragging = false;
            ApplyState((false, false, false, false));
        }
    }
}
