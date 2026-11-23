using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace StardewSurfacePad.Controls;

public partial class SwitchPadButton : UserControl
{
    private readonly HashSet<int> _activeTouchIds = [];
    private bool _mousePressed;
    private bool _isPressed;

    public event EventHandler<bool>? PressedStateChanged;
    public event EventHandler? Tapped;

    public static readonly DependencyProperty LabelProperty =
        DependencyProperty.Register(nameof(Label), typeof(string), typeof(SwitchPadButton),
            new PropertyMetadata(string.Empty, OnVisualPropertyChanged));

    public static readonly DependencyProperty LabelFontSizeProperty =
        DependencyProperty.Register(nameof(LabelFontSize), typeof(double), typeof(SwitchPadButton),
            new PropertyMetadata(20.0, OnVisualPropertyChanged));

    public static readonly DependencyProperty ButtonCornerRadiusProperty =
        DependencyProperty.Register(nameof(ButtonCornerRadius), typeof(CornerRadius), typeof(SwitchPadButton),
            new PropertyMetadata(new CornerRadius(999), OnVisualPropertyChanged));

    public static readonly DependencyProperty NormalBackgroundProperty =
        DependencyProperty.Register(nameof(NormalBackground), typeof(Brush), typeof(SwitchPadButton),
            new PropertyMetadata(new SolidColorBrush(Color.FromArgb(0xA6, 0x24, 0x24, 0x24)), OnVisualPropertyChanged));

    public static readonly DependencyProperty PressedBackgroundProperty =
        DependencyProperty.Register(nameof(PressedBackground), typeof(Brush), typeof(SwitchPadButton),
            new PropertyMetadata(new SolidColorBrush(Color.FromArgb(0xD8, 0x4A, 0x4A, 0x4A))));

    public static readonly DependencyProperty AccentRingBrushProperty =
        DependencyProperty.Register(nameof(AccentRingBrush), typeof(Brush), typeof(SwitchPadButton),
            new PropertyMetadata(null, OnVisualPropertyChanged));

    public static readonly DependencyProperty IsCaptureButtonProperty =
        DependencyProperty.Register(nameof(IsCaptureButton), typeof(bool), typeof(SwitchPadButton),
            new PropertyMetadata(false, OnVisualPropertyChanged));

    public static readonly DependencyProperty IsToggledProperty =
        DependencyProperty.Register(nameof(IsToggled), typeof(bool), typeof(SwitchPadButton),
            new PropertyMetadata(false, OnVisualPropertyChanged));

    public string Label
    {
        get => (string)GetValue(LabelProperty);
        set => SetValue(LabelProperty, value);
    }

    public double LabelFontSize
    {
        get => (double)GetValue(LabelFontSizeProperty);
        set => SetValue(LabelFontSizeProperty, value);
    }

    public CornerRadius ButtonCornerRadius
    {
        get => (CornerRadius)GetValue(ButtonCornerRadiusProperty);
        set => SetValue(ButtonCornerRadiusProperty, value);
    }

    public Brush NormalBackground
    {
        get => (Brush)GetValue(NormalBackgroundProperty);
        set => SetValue(NormalBackgroundProperty, value);
    }

    public Brush PressedBackground
    {
        get => (Brush)GetValue(PressedBackgroundProperty);
        set => SetValue(PressedBackgroundProperty, value);
    }

    public Brush? AccentRingBrush
    {
        get => (Brush?)GetValue(AccentRingBrushProperty);
        set => SetValue(AccentRingBrushProperty, value);
    }

    public bool IsCaptureButton
    {
        get => (bool)GetValue(IsCaptureButtonProperty);
        set => SetValue(IsCaptureButtonProperty, value);
    }

    public bool IsToggled
    {
        get => (bool)GetValue(IsToggledProperty);
        set => SetValue(IsToggledProperty, value);
    }

    public SwitchPadButton()
    {
        InitializeComponent();
        TouchInputHelper.DisableSystemTouchFeedback(this);
        Loaded += (_, _) => ApplyVisualState();
    }

    private static void OnVisualPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is SwitchPadButton btn)
        {
            btn.ApplyVisualState();
        }
    }

    private void ApplyVisualState()
    {
        LabelText.Text = Label;
        LabelText.FontSize = LabelFontSize;
        LabelText.Visibility = IsCaptureButton ? Visibility.Collapsed : Visibility.Visible;
        CaptureIcon.Visibility = IsCaptureButton ? Visibility.Visible : Visibility.Collapsed;

        BodyBorder.CornerRadius = ButtonCornerRadius;
        OuterRing.CornerRadius = ButtonCornerRadius;

        if (AccentRingBrush is not null)
        {
            OuterRing.Visibility = Visibility.Visible;
            OuterRing.BorderBrush = AccentRingBrush;
            OuterRing.Opacity = _isPressed ? 1.0 : 0.72;
        }
        else if (IsToggled)
        {
            OuterRing.Visibility = Visibility.Visible;
            OuterRing.BorderBrush = new SolidColorBrush(Color.FromRgb(0xE6, 0x00, 0x12));
            OuterRing.Opacity = 0.95;
        }
        else
        {
            OuterRing.Visibility = Visibility.Collapsed;
        }

        BodyBorder.Background = (_isPressed || IsToggled) ? PressedBackground : NormalBackground;
        HighlightOverlay.Opacity = _isPressed ? 0.65 : 0.0;
        PressScale.ScaleX = _isPressed ? 0.92 : 1.0;
        PressScale.ScaleY = _isPressed ? 0.92 : 1.0;
    }

    private void UpdatePressedState()
    {
        bool next = _activeTouchIds.Count > 0 || _mousePressed;
        if (next == _isPressed) return;

        _isPressed = next;
        ApplyVisualState();
        PressedStateChanged?.Invoke(this, _isPressed);
        if (_isPressed)
        {
            Tapped?.Invoke(this, EventArgs.Empty);
        }
    }

    protected override void OnTouchDown(TouchEventArgs e)
    {
        base.OnTouchDown(e);
        _activeTouchIds.Add(e.TouchDevice.Id);
        CaptureTouch(e.TouchDevice);
        UpdatePressedState();
        e.Handled = true;
    }

    protected override void OnTouchUp(TouchEventArgs e)
    {
        base.OnTouchUp(e);
        _activeTouchIds.Remove(e.TouchDevice.Id);
        ReleaseTouchCapture(e.TouchDevice);
        UpdatePressedState();
        e.Handled = true;
    }

    protected override void OnLostTouchCapture(TouchEventArgs e)
    {
        base.OnLostTouchCapture(e);
        _activeTouchIds.Remove(e.TouchDevice.Id);
        UpdatePressedState();
    }

    protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
    {
        base.OnMouseLeftButtonDown(e);
        if (!TouchInputHelper.IsPureMouseOrPen(e)) return;

        _mousePressed = true;
        CaptureMouse();
        UpdatePressedState();
        e.Handled = true;
    }

    protected override void OnMouseLeftButtonUp(MouseButtonEventArgs e)
    {
        base.OnMouseLeftButtonUp(e);
        if (!TouchInputHelper.IsPureMouseOrPen(e)) return;

        _mousePressed = false;
        ReleaseMouseCapture();
        UpdatePressedState();
        e.Handled = true;
    }

    protected override void OnLostMouseCapture(MouseEventArgs e)
    {
        base.OnLostMouseCapture(e);
        _mousePressed = false;
        UpdatePressedState();
    }
}
