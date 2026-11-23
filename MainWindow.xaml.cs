using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using StardewSurfacePad.Controls;
using StardewSurfacePad.Models;
using StardewSurfacePad.Services;

namespace StardewSurfacePad;

public partial class MainWindow : Window
{
    private static readonly Brush TransparentHitTestBrush = new SolidColorBrush(Color.FromArgb(1, 0, 0, 0));

    private readonly VirtualGamepadService _gamepad = new();
    private readonly LayoutStorageService _storage = new();
    private readonly TouchMouseBlocker _touchMouseBlocker = new();
    private PadLayoutConfig _layout = new();
    private bool _isEditMode;
    private bool _isInitializingUi;

    public MainWindow()
    {
        _isInitializingUi = true;
        InitializeComponent();
        TouchInputHelper.DisableSystemTouchFeedback(this);

        SourceInitialized += MainWindow_SourceInitialized;
        Loaded += MainWindow_Loaded;
        Closed += MainWindow_Closed;

        WireGamepadControls();
        WireClusterDragPersistence();
    }

    private void MainWindow_SourceInitialized(object? sender, EventArgs e)
    {
        IntPtr hwnd = new WindowInteropHelper(this).Handle;
        NativeMethods.ApplyNoActivateOverlayStyle(hwnd);

        var source = HwndSource.FromHwnd(hwnd);
        source?.AddHook(WndProcHook);
    }

    private static IntPtr WndProcHook(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == NativeMethods.WM_MOUSEACTIVATE)
        {
            handled = true;
            return new IntPtr(NativeMethods.MA_NOACTIVATE);
        }
        return IntPtr.Zero;
    }

    private void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        double width = ActualWidth > 0 ? ActualWidth : SystemParameters.PrimaryScreenWidth;
        double height = ActualHeight > 0 ? ActualHeight : SystemParameters.PrimaryScreenHeight;

        _layout = _storage.LoadOrDefault(width, height);
        ApplyLayoutToUi();

        _touchMouseBlocker.Install();
        UpdateTouchMouseBlockingState();

        _gamepad.Initialize(preferViGEm: true);
        RefreshConnectionStatus();
    }

    private void MainWindow_Closed(object? sender, EventArgs e)
    {
        _touchMouseBlocker.Dispose();
        _gamepad.ResetAllInputs();
        _gamepad.Dispose();
    }

    private void WireGamepadControls()
    {
        // Nintendo Switch Pro right diamond: Top=X, Left=Y, Right=A (Confirm), Bottom=B (Cancel)
        BindButton(BtnA, VirtualButton.A);
        BindButton(BtnB, VirtualButton.B);
        BindButton(BtnX, VirtualButton.X);
        BindButton(BtnY, VirtualButton.Y);

        // Curved shoulder & trigger buttons
        BindButton(BtnL, VirtualButton.L);
        BindButton(BtnZL, VirtualButton.ZL);
        BindButton(BtnR, VirtualButton.R);
        BindButton(BtnZR, VirtualButton.ZR);

        // System function buttons: - (Minus -> Back), + (Plus -> Start)
        BindButton(BtnMinus, VirtualButton.Minus);
        BindButton(BtnPlus, VirtualButton.Plus);

        // Capture button toggles Edit / Lock mode
        BtnCapture.Tapped += (_, _) => SetEditMode(!_isEditMode);

        // Thumbsticks & D-Pad
        LeftStick.AxisChanged += (_, axis) => _gamepad.SetThumbstick(isLeft: true, axis.X, axis.Y);
        RightStick.AxisChanged += (_, axis) => _gamepad.SetThumbstick(isLeft: false, axis.X, axis.Y);
        DPad.DirectionChanged += (_, dir) => _gamepad.SetDPad(dir.Up, dir.Down, dir.Left, dir.Right);
    }

    private void BindButton(SwitchPadButton buttonControl, VirtualButton target)
    {
        buttonControl.PressedStateChanged += (_, pressed) => _gamepad.SetButton(target, pressed);
    }

    private void WireClusterDragPersistence()
    {
        BindClusterDrag(LeftShouldersCluster, pos => _layout.LeftShoulders = pos);
        BindClusterDrag(RightShouldersCluster, pos => _layout.RightShoulders = pos);
        BindClusterDrag(SystemBarCluster, pos => _layout.SystemBar = pos);
        BindClusterDrag(LeftStickCluster, pos => _layout.LeftStick = pos);
        BindClusterDrag(DPadCluster, pos => _layout.DPad = pos);
        BindClusterDrag(FaceButtonsCluster, pos => _layout.FaceButtons = pos);
        BindClusterDrag(RightStickCluster, pos => _layout.RightStick = pos);
    }

    private void BindClusterDrag(PadClusterContainer cluster, Action<ElementPosition> assign)
    {
        cluster.DragCompleted += (_, pt) =>
        {
            assign(new ElementPosition { X = pt.X, Y = pt.Y });
            _storage.Save(_layout);
        };
    }

    private void ApplyLayoutToUi()
    {
        _isInitializingUi = true;
        try
        {
            PadCanvas.Opacity = _layout.GlobalOpacity;
            OpacitySlider.Value = _layout.GlobalOpacity;
            OpacityValueText.Text = $"{Math.Round(_layout.GlobalOpacity * 100)}%";

            ScaleSlider.Value = _layout.ControlScale;
            ScaleValueText.Text = $"{Math.Round(_layout.ControlScale * 100)}%";

            ChkBlockTouchMouse.IsChecked = _layout.BlockTouchMouse;

            ApplyClusterTransform(LeftShouldersCluster, _layout.LeftShoulders);
            ApplyClusterTransform(RightShouldersCluster, _layout.RightShoulders);
            ApplyClusterTransform(SystemBarCluster, _layout.SystemBar);
            ApplyClusterTransform(LeftStickCluster, _layout.LeftStick);
            ApplyClusterTransform(DPadCluster, _layout.DPad);
            ApplyClusterTransform(FaceButtonsCluster, _layout.FaceButtons);
            ApplyClusterTransform(RightStickCluster, _layout.RightStick);
        }
        finally
        {
            _isInitializingUi = false;
        }
    }

    private void ApplyClusterTransform(PadClusterContainer cluster, ElementPosition pos)
    {
        Canvas.SetLeft(cluster, pos.X);
        Canvas.SetTop(cluster, pos.Y);
        cluster.ControlScale = _layout.ControlScale;
    }

    private void SetEditMode(bool editMode)
    {
        _isEditMode = editMode;
        if (_isEditMode)
        {
            _gamepad.ResetAllInputs();
        }

        BtnCapture.IsToggled = _isEditMode;
        EditToolbar.Visibility = _isEditMode ? Visibility.Visible : Visibility.Collapsed;
        UpdateTouchMouseBlockingState();

        foreach (PadClusterContainer cluster in GetAllClusters())
        {
            cluster.IsEditMode = _isEditMode;
        }
    }

    private IEnumerable<PadClusterContainer> GetAllClusters()
    {
        yield return LeftShouldersCluster;
        yield return RightShouldersCluster;
        yield return SystemBarCluster;
        yield return LeftStickCluster;
        yield return DPadCluster;
        yield return FaceButtonsCluster;
        yield return RightStickCluster;
    }

    private void UpdateTouchMouseBlockingState()
    {
        bool shouldBlock = !_isEditMode && _layout.BlockTouchMouse;
        _touchMouseBlocker.IsBlocking = shouldBlock;
        PadCanvas.Background = shouldBlock ? TransparentHitTestBrush : null;
    }

    private void ChkBlockTouchMouse_Changed(object sender, RoutedEventArgs e)
    {
        if (!IsLoaded || _isInitializingUi) return;

        _layout.BlockTouchMouse = ChkBlockTouchMouse.IsChecked == true;
        UpdateTouchMouseBlockingState();
        _storage.Save(_layout);
    }

    protected override void OnTouchDown(TouchEventArgs e)
    {
        base.OnTouchDown(e);
        if (!_isEditMode && _layout.BlockTouchMouse)
        {
            e.Handled = true;
        }
    }

    protected override void OnTouchMove(TouchEventArgs e)
    {
        base.OnTouchMove(e);
        if (!_isEditMode && _layout.BlockTouchMouse)
        {
            e.Handled = true;
        }
    }

    protected override void OnTouchUp(TouchEventArgs e)
    {
        base.OnTouchUp(e);
        if (!_isEditMode && _layout.BlockTouchMouse)
        {
            e.Handled = true;
        }
    }

    private void RefreshConnectionStatus()
    {
        StatusText.Text = _gamepad.StatusMessage;
        BtnToggleBackend.Content = _gamepad.ActiveBackend == ActiveInputBackend.ViGEmXbox360
            ? "切回免驱动键鼠"
            : "检测 ViGEm 驱动";
    }

    private void BtnLockLayout_Click(object sender, RoutedEventArgs e)
    {
        _storage.Save(_layout);
        SetEditMode(false);
    }

    private void BtnToggleBackend_Click(object sender, RoutedEventArgs e)
    {
        _gamepad.ResetAllInputs();
        bool wantViGEm = _gamepad.ActiveBackend == ActiveInputBackend.DriverlessKeyboardMouse;
        _gamepad.Initialize(preferViGEm: wantViGEm);
        RefreshConnectionStatus();
    }

    private void OpacitySlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (!IsLoaded || _isInitializingUi || PadCanvas is null || OpacityValueText is null) return;

        _layout.GlobalOpacity = Math.Round(e.NewValue, 2);
        PadCanvas.Opacity = _layout.GlobalOpacity;
        OpacityValueText.Text = $"{Math.Round(_layout.GlobalOpacity * 100)}%";
        _storage.Save(_layout);
    }

    private void ScaleSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (!IsLoaded || _isInitializingUi || ScaleValueText is null) return;

        _layout.ControlScale = Math.Round(e.NewValue, 2);
        ScaleValueText.Text = $"{Math.Round(_layout.ControlScale * 100)}%";
        foreach (PadClusterContainer cluster in GetAllClusters())
        {
            cluster.ControlScale = _layout.ControlScale;
        }
        _storage.Save(_layout);
    }

    private void BtnResetLayout_Click(object sender, RoutedEventArgs e)
    {
        double width = ActualWidth > 0 ? ActualWidth : SystemParameters.PrimaryScreenWidth;
        double height = ActualHeight > 0 ? ActualHeight : SystemParameters.PrimaryScreenHeight;
        _layout = PadLayoutConfig.CreateDefault(width, height);
        ApplyLayoutToUi();
        _storage.Save(_layout);
    }

    private void BtnExitApp_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}
