using Nefarius.ViGEm.Client;
using Nefarius.ViGEm.Client.Exceptions;
using Nefarius.ViGEm.Client.Targets;
using Nefarius.ViGEm.Client.Targets.Xbox360;

namespace StardewSurfacePad.Services;

public enum VirtualButton
{
    A,
    B,
    X,
    Y,
    L,
    R,
    ZL,
    ZR,
    Minus,
    Plus,
    L3,
    R3,
    DPadUp,
    DPadDown,
    DPadLeft,
    DPadRight
}

public enum ActiveInputBackend
{
    DriverlessKeyboardMouse,
    ViGEmXbox360
}

/// <summary>
/// Unified input backend: uses ViGEmBus Xbox 360 emulation when available,
/// and seamlessly falls back to zero-driver Win32 SendInput keyboard/mouse mapping.
/// </summary>
public sealed class VirtualGamepadService : IDisposable
{
    private readonly object _sync = new();
    private readonly KeyboardMouseInputService _kmBackend = new();
    private ViGEmClient? _client;
    private IXbox360Controller? _controller;
    private bool _disposed;

    public bool IsViGEmConnected { get; private set; }
    public ActiveInputBackend ActiveBackend { get; private set; } = ActiveInputBackend.DriverlessKeyboardMouse;
    public string StatusMessage { get; private set; } = "免驱动直连模式（即开即用，无需安装驱动）";

    public bool Initialize(bool preferViGEm = true)
    {
        lock (_sync)
        {
            if (_disposed) return false;

            if (!preferViGEm)
            {
                DisconnectViGEm();
                ActiveBackend = ActiveInputBackend.DriverlessKeyboardMouse;
                StatusMessage = "免驱动直连模式（即开即用，无需安装驱动）";
                return true;
            }

            if (IsViGEmConnected && _controller is not null)
            {
                ActiveBackend = ActiveInputBackend.ViGEmXbox360;
                StatusMessage = "ViGEm 虚拟手柄模式（XInput 已连接）";
                return true;
            }

            try
            {
                _client = new ViGEmClient();
                _controller = _client.CreateXbox360Controller();
                _controller.AutoSubmitReport = false;
                _controller.Connect();

                IsViGEmConnected = true;
                ActiveBackend = ActiveInputBackend.ViGEmXbox360;
                StatusMessage = "ViGEm 虚拟手柄模式（XInput 已连接）";
                return true;
            }
            catch (VigemBusNotFoundException)
            {
                DisconnectViGEm();
                ActiveBackend = ActiveInputBackend.DriverlessKeyboardMouse;
                StatusMessage = "免驱动直连模式（即开即用，无需安装驱动）";
                return false;
            }
            catch
            {
                DisconnectViGEm();
                ActiveBackend = ActiveInputBackend.DriverlessKeyboardMouse;
                StatusMessage = "免驱动直连模式（即开即用，无需安装驱动）";
                return false;
            }
        }
    }

    public void SetButton(VirtualButton button, bool pressed)
    {
        lock (_sync)
        {
            if (_disposed) return;

            if (ActiveBackend == ActiveInputBackend.DriverlessKeyboardMouse)
            {
                _kmBackend.SetButton(button, pressed);
                return;
            }

            if (!IsViGEmConnected || _controller is null) return;
            SetButtonUnsafe(button, pressed);
            _controller.SubmitReport();
        }
    }

    public void SetDPad(bool up, bool down, bool left, bool right)
    {
        lock (_sync)
        {
            if (_disposed) return;

            if (ActiveBackend == ActiveInputBackend.DriverlessKeyboardMouse)
            {
                _kmBackend.SetDPad(up, down, left, right);
                return;
            }

            if (!IsViGEmConnected || _controller is null) return;
            _controller.SetButtonState(Xbox360Button.Up, up);
            _controller.SetButtonState(Xbox360Button.Down, down);
            _controller.SetButtonState(Xbox360Button.Left, left);
            _controller.SetButtonState(Xbox360Button.Right, right);
            _controller.SubmitReport();
        }
    }

    /// <summary>
    /// Updates thumbstick axes. Inputs are normalized in [-1, 1] with +Y pointing UP.
    /// </summary>
    public void SetThumbstick(bool isLeft, double normX, double normY)
    {
        lock (_sync)
        {
            if (_disposed) return;

            if (ActiveBackend == ActiveInputBackend.DriverlessKeyboardMouse)
            {
                _kmBackend.SetThumbstick(isLeft, normX, normY);
                return;
            }

            if (!IsViGEmConnected || _controller is null) return;

            short rawX = ToAxisShort(normX);
            short rawY = ToAxisShort(normY);

            if (isLeft)
            {
                _controller.SetAxisValue(Xbox360Axis.LeftThumbX, rawX);
                _controller.SetAxisValue(Xbox360Axis.LeftThumbY, rawY);
            }
            else
            {
                _controller.SetAxisValue(Xbox360Axis.RightThumbX, rawX);
                _controller.SetAxisValue(Xbox360Axis.RightThumbY, rawY);
            }

            _controller.SubmitReport();
        }
    }

    public void ResetAllInputs()
    {
        lock (_sync)
        {
            _kmBackend.ResetAllInputs();

            if (!IsViGEmConnected || _controller is null) return;

            foreach (VirtualButton btn in Enum.GetValues<VirtualButton>())
            {
                SetButtonUnsafe(btn, false);
            }

            _controller.SetAxisValue(Xbox360Axis.LeftThumbX, 0);
            _controller.SetAxisValue(Xbox360Axis.LeftThumbY, 0);
            _controller.SetAxisValue(Xbox360Axis.RightThumbX, 0);
            _controller.SetAxisValue(Xbox360Axis.RightThumbY, 0);
            _controller.SubmitReport();
        }
    }

    private void SetButtonUnsafe(VirtualButton button, bool pressed)
    {
        if (_controller is null) return;
        switch (button)
        {
            case VirtualButton.A: _controller.SetButtonState(Xbox360Button.A, pressed); break;
            case VirtualButton.B: _controller.SetButtonState(Xbox360Button.B, pressed); break;
            case VirtualButton.X: _controller.SetButtonState(Xbox360Button.X, pressed); break;
            case VirtualButton.Y: _controller.SetButtonState(Xbox360Button.Y, pressed); break;
            case VirtualButton.L: _controller.SetButtonState(Xbox360Button.LeftShoulder, pressed); break;
            case VirtualButton.R: _controller.SetButtonState(Xbox360Button.RightShoulder, pressed); break;
            case VirtualButton.ZL: _controller.SetSliderValue(Xbox360Slider.LeftTrigger, pressed ? byte.MaxValue : byte.MinValue); break;
            case VirtualButton.ZR: _controller.SetSliderValue(Xbox360Slider.RightTrigger, pressed ? byte.MaxValue : byte.MinValue); break;
            case VirtualButton.Minus: _controller.SetButtonState(Xbox360Button.Back, pressed); break;
            case VirtualButton.Plus: _controller.SetButtonState(Xbox360Button.Start, pressed); break;
            case VirtualButton.L3: _controller.SetButtonState(Xbox360Button.LeftThumb, pressed); break;
            case VirtualButton.R3: _controller.SetButtonState(Xbox360Button.RightThumb, pressed); break;
            case VirtualButton.DPadUp: _controller.SetButtonState(Xbox360Button.Up, pressed); break;
            case VirtualButton.DPadDown: _controller.SetButtonState(Xbox360Button.Down, pressed); break;
            case VirtualButton.DPadLeft: _controller.SetButtonState(Xbox360Button.Left, pressed); break;
            case VirtualButton.DPadRight: _controller.SetButtonState(Xbox360Button.Right, pressed); break;
        }
    }

    private static short ToAxisShort(double value)
    {
        double clamped = Math.Clamp(value, -1.0, 1.0);
        if (Math.Abs(clamped) < 0.08) return 0;
        return clamped >= 0
            ? (short)Math.Round(clamped * 32767.0)
            : (short)Math.Round(clamped * 32768.0);
    }

    private void DisconnectViGEm()
    {
        IsViGEmConnected = false;
        try { _controller?.Disconnect(); } catch { }
        _controller = null;
        try { _client?.Dispose(); } catch { }
        _client = null;
    }

    public void Dispose()
    {
        lock (_sync)
        {
            if (_disposed) return;
            _disposed = true;
            _kmBackend.Dispose();
            DisconnectViGEm();
        }
    }
}
