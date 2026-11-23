using System.Runtime.InteropServices;
using System.Windows.Threading;

namespace StardewSurfacePad.Services;

/// <summary>
/// Driverless Stardew Valley input backend using Win32 SendInput (WASD + Keyboard + Mouse Wheel/Cursor).
/// Works out of the box on any Windows Surface tablet without installing ViGEmBus.
/// </summary>
public sealed class KeyboardMouseInputService : IDisposable
{
    private const uint INPUT_MOUSE = 0;
    private const uint INPUT_KEYBOARD = 1;

    private const uint KEYEVENTF_EXTENDEDKEY = 0x0001;
    private const uint KEYEVENTF_KEYUP = 0x0002;
    private const uint MAPVK_VK_TO_VSC = 0;

    private const uint MOUSEEVENTF_MOVE = 0x0001;
    private const uint MOUSEEVENTF_WHEEL = 0x0800;
    private const int WHEEL_DELTA = 120;

    private const ushort VK_TAB = 0x09;
    private const ushort VK_ESCAPE = 0x1B;
    private const ushort VK_LEFT = 0x25;
    private const ushort VK_UP = 0x26;
    private const ushort VK_RIGHT = 0x27;
    private const ushort VK_DOWN = 0x28;
    private const ushort VK_A = 0x41;
    private const ushort VK_C = 0x43;
    private const ushort VK_D = 0x44;
    private const ushort VK_E = 0x45;
    private const ushort VK_F = 0x46;
    private const ushort VK_M = 0x4D;
    private const ushort VK_S = 0x53;
    private const ushort VK_W = 0x57;
    private const ushort VK_X = 0x58;

    private readonly HashSet<ushort> _heldKeys = [];
    private readonly DispatcherTimer _rightStickMouseTimer;
    private double _rightStickX;
    private double _rightStickY;
    private bool _disposed;

    public KeyboardMouseInputService()
    {
        _rightStickMouseTimer = new DispatcherTimer(DispatcherPriority.Input)
        {
            Interval = TimeSpan.FromMilliseconds(16)
        };
        _rightStickMouseTimer.Tick += RightStickMouseTimer_Tick;
    }

    public void SetButton(VirtualButton button, bool pressed)
    {
        if (_disposed) return;

        switch (button)
        {
            // Switch Pro A (Right, primary confirm/talk/harvest) -> Stardew Keyboard X
            case VirtualButton.A:
                SetKeyState(VK_X, pressed);
                break;
            // Switch Pro X (Top, use tool/attack) -> Stardew Keyboard C
            case VirtualButton.X:
                SetKeyState(VK_C, pressed);
                break;
            // Switch Pro B (Bottom, inventory/cancel) -> Stardew Keyboard E
            case VirtualButton.B:
                SetKeyState(VK_E, pressed);
                break;
            // Switch Pro Y (Left, map) -> Stardew Keyboard M
            case VirtualButton.Y:
                SetKeyState(VK_M, pressed);
                break;
            // Shoulder L / R -> Mouse Wheel scroll hotbar items
            case VirtualButton.L:
                if (pressed) SendMouseWheel(WHEEL_DELTA);
                break;
            case VirtualButton.R:
                if (pressed) SendMouseWheel(-WHEEL_DELTA);
                break;
            // Trigger ZL -> Tab (cycle hotbar row), ZR -> C (use tool/attack with right index finger)
            case VirtualButton.ZL:
                SetKeyState(VK_TAB, pressed);
                break;
            case VirtualButton.ZR:
                SetKeyState(VK_C, pressed);
                break;
            // System buttons: - -> F (Journal), + -> Escape (Game Menu / Cancel)
            case VirtualButton.Minus:
                SetKeyState(VK_F, pressed);
                break;
            case VirtualButton.Plus:
                SetKeyState(VK_ESCAPE, pressed);
                break;
        }
    }

    public void SetDPad(bool up, bool down, bool left, bool right)
    {
        if (_disposed) return;
        SetKeyState(VK_UP, up, extended: true);
        SetKeyState(VK_DOWN, down, extended: true);
        SetKeyState(VK_LEFT, left, extended: true);
        SetKeyState(VK_RIGHT, right, extended: true);
    }

    public void SetThumbstick(bool isLeft, double normX, double normY)
    {
        if (_disposed) return;

        if (isLeft)
        {
            const double threshold = 0.28;
            SetKeyState(VK_W, normY > threshold);
            SetKeyState(VK_S, normY < -threshold);
            SetKeyState(VK_A, normX < -threshold);
            SetKeyState(VK_D, normX > threshold);
        }
        else
        {
            _rightStickX = Math.Abs(normX) < 0.15 ? 0 : normX;
            _rightStickY = Math.Abs(normY) < 0.15 ? 0 : normY;

            if (_rightStickX == 0 && _rightStickY == 0)
            {
                _rightStickMouseTimer.Stop();
            }
            else if (!_rightStickMouseTimer.IsEnabled)
            {
                _rightStickMouseTimer.Start();
            }
        }
    }

    public void ResetAllInputs()
    {
        _rightStickX = 0;
        _rightStickY = 0;
        _rightStickMouseTimer.Stop();

        foreach (ushort vk in _heldKeys.ToArray())
        {
            bool extended = vk is VK_UP or VK_DOWN or VK_LEFT or VK_RIGHT;
            SendKeyInput(vk, down: false, extended);
        }
        _heldKeys.Clear();
    }

    private void RightStickMouseTimer_Tick(object? sender, EventArgs e)
    {
        const double maxPixelsPerTick = 14.0;
        int dx = (int)Math.Round(_rightStickX * maxPixelsPerTick);
        int dy = (int)Math.Round(-_rightStickY * maxPixelsPerTick); // screen +Y is down

        if (dx == 0 && dy == 0) return;

        var input = new INPUT
        {
            type = INPUT_MOUSE,
            U = new InputUnion
            {
                mi = new MOUSEINPUT
                {
                    dx = dx,
                    dy = dy,
                    dwFlags = MOUSEEVENTF_MOVE
                }
            }
        };
        SendInput(1, [input], Marshal.SizeOf<INPUT>());
    }

    private void SetKeyState(ushort vk, bool pressed, bool extended = false)
    {
        bool currentlyHeld = _heldKeys.Contains(vk);
        if (pressed == currentlyHeld) return;

        if (pressed)
        {
            _heldKeys.Add(vk);
            SendKeyInput(vk, down: true, extended);
        }
        else
        {
            _heldKeys.Remove(vk);
            SendKeyInput(vk, down: false, extended);
        }
    }

    private static void SendKeyInput(ushort vk, bool down, bool extended)
    {
        uint flags = down ? 0 : KEYEVENTF_KEYUP;
        if (extended) flags |= KEYEVENTF_EXTENDEDKEY;

        ushort scanCode = (ushort)MapVirtualKey(vk, MAPVK_VK_TO_VSC);

        var input = new INPUT
        {
            type = INPUT_KEYBOARD,
            U = new InputUnion
            {
                ki = new KEYBDINPUT
                {
                    wVk = vk,
                    wScan = scanCode,
                    dwFlags = flags
                }
            }
        };
        SendInput(1, [input], Marshal.SizeOf<INPUT>());
    }

    private static void SendMouseWheel(int delta)
    {
        var input = new INPUT
        {
            type = INPUT_MOUSE,
            U = new InputUnion
            {
                mi = new MOUSEINPUT
                {
                    mouseData = unchecked((uint)delta),
                    dwFlags = MOUSEEVENTF_WHEEL
                }
            }
        };
        SendInput(1, [input], Marshal.SizeOf<INPUT>());
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        ResetAllInputs();
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);

    [DllImport("user32.dll")]
    private static extern uint MapVirtualKey(uint uCode, uint uMapType);

    [StructLayout(LayoutKind.Sequential)]
    private struct INPUT
    {
        public uint type;
        public InputUnion U;
    }

    [StructLayout(LayoutKind.Explicit)]
    private struct InputUnion
    {
        [FieldOffset(0)] public MOUSEINPUT mi;
        [FieldOffset(0)] public KEYBDINPUT ki;
        [FieldOffset(0)] public HARDWAREINPUT hi;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MOUSEINPUT
    {
        public int dx;
        public int dy;
        public uint mouseData;
        public uint dwFlags;
        public uint time;
        public IntPtr dwExtraInfo;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct KEYBDINPUT
    {
        public ushort wVk;
        public ushort wScan;
        public uint dwFlags;
        public uint time;
        public IntPtr dwExtraInfo;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct HARDWAREINPUT
    {
        public uint uMsg;
        public ushort wParamL;
        public ushort wParamR;
    }
}
