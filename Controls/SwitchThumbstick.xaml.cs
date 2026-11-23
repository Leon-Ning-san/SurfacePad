using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace StardewSurfacePad.Controls;

public partial class SwitchThumbstick : UserControl
{
    private int? _activeTouchId;
    private bool _mouseDragging;

    public event EventHandler<(double X, double Y)>? AxisChanged;

    public static readonly DependencyProperty CaptionProperty =
        DependencyProperty.Register(nameof(Caption), typeof(string), typeof(SwitchThumbstick),
            new PropertyMetadata("L", OnCaptionChanged));

    public string Caption
    {
        get => (string)GetValue(CaptionProperty);
        set => SetValue(CaptionProperty, value);
    }

    public SwitchThumbstick()
    {
        InitializeComponent();
        TouchInputHelper.DisableSystemTouchFeedback(this);
        Loaded += (_, _) => StickLabel.Text = Caption;
    }

    private static void OnCaptionChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is SwitchThumbstick stick)
        {
            stick.StickLabel.Text = stick.Caption;
        }
    }

    private void UpdateStickFromPoint(Point pt)
    {
        double centerX = ActualWidth / 2.0;
        double centerY = ActualHeight / 2.0;
        double maxRadius = Math.Max(20.0, (Math.Min(ActualWidth, ActualHeight) - ThumbCap.Width) / 2.0);

        double dx = pt.X - centerX;
        double dy = pt.Y - centerY;
        double dist = Math.Sqrt(dx * dx + dy * dy);

        if (dist > maxRadius && dist > 0.001)
        {
            double scale = maxRadius / dist;
            dx *= scale;
            dy *= scale;
        }

        ThumbTransform.X = dx;
        ThumbTransform.Y = dy;
        CapBody.Stroke = new SolidColorBrush(Color.FromArgb(0xFF, 0xF0, 0xF0, 0xF0));

        double normX = Math.Clamp(dx / maxRadius, -1.0, 1.0);
        double normY = Math.Clamp(-dy / maxRadius, -1.0, 1.0); // +Y is UP in XInput
        AxisChanged?.Invoke(this, (normX, normY));
    }

    private void ResetStick()
    {
        ThumbTransform.X = 0;
        ThumbTransform.Y = 0;
        CapBody.Stroke = new SolidColorBrush(Color.FromArgb(0xC0, 0xD2, 0xD2, 0xD2));
        AxisChanged?.Invoke(this, (0.0, 0.0));
    }

    protected override void OnTouchDown(TouchEventArgs e)
    {
        base.OnTouchDown(e);
        if (_activeTouchId is null)
        {
            _activeTouchId = e.TouchDevice.Id;
            CaptureTouch(e.TouchDevice);
            UpdateStickFromPoint(e.GetTouchPoint(this).Position);
        }
        e.Handled = true;
    }

    protected override void OnTouchMove(TouchEventArgs e)
    {
        base.OnTouchMove(e);
        if (_activeTouchId == e.TouchDevice.Id)
        {
            UpdateStickFromPoint(e.GetTouchPoint(this).Position);
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
            ResetStick();
            e.Handled = true;
        }
    }

    protected override void OnLostTouchCapture(TouchEventArgs e)
    {
        base.OnLostTouchCapture(e);
        if (_activeTouchId == e.TouchDevice.Id)
        {
            _activeTouchId = null;
            ResetStick();
        }
    }

    protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
    {
        base.OnMouseLeftButtonDown(e);
        if (!TouchInputHelper.IsPureMouseOrPen(e)) return;

        _mouseDragging = true;
        CaptureMouse();
        UpdateStickFromPoint(e.GetPosition(this));
        e.Handled = true;
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);
        if (!TouchInputHelper.IsPureMouseOrPen(e)) return;

        if (_mouseDragging)
        {
            UpdateStickFromPoint(e.GetPosition(this));
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
            ResetStick();
            e.Handled = true;
        }
    }

    protected override void OnLostMouseCapture(MouseEventArgs e)
    {
        base.OnLostMouseCapture(e);
        if (_mouseDragging)
        {
            _mouseDragging = false;
            ResetStick();
        }
    }
}
