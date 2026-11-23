using System.Diagnostics;
using System.Runtime.InteropServices;

namespace StardewSurfacePad.Services;

/// <summary>
/// Installs a lightweight low-level mouse hook (WH_MOUSE_LL) to suppress Windows
/// touch-to-mouse cursor jumps and clicks while Play Mode is active.
/// Real physical mice, touchpads, and Right Stick SendInput (dwExtraInfo == 0) are unaffected.
/// </summary>
public sealed class TouchMouseBlocker : IDisposable
{
    private const int WH_MOUSE_LL = 14;
    private const long MI_WP_TOUCH_SIGNATURE = 0xFF515780L;
    private const long MI_WP_TOUCH_MASK = 0xFFFFFF80L;

    private delegate IntPtr LowLevelMouseProc(int nCode, IntPtr wParam, IntPtr lParam);

    private readonly LowLevelMouseProc _hookProc;
    private IntPtr _hookHandle;
    private bool _disposed;

    public bool IsBlocking { get; set; }

    public TouchMouseBlocker()
    {
        _hookProc = HookCallback;
    }

    public void Install()
    {
        if (_disposed || _hookHandle != IntPtr.Zero) return;

        using Process currentProcess = Process.GetCurrentProcess();
        using ProcessModule? mainModule = currentProcess.MainModule;
        IntPtr moduleHandle = mainModule is not null ? GetModuleHandle(mainModule.ModuleName) : IntPtr.Zero;
        _hookHandle = SetWindowsHookEx(WH_MOUSE_LL, _hookProc, moduleHandle, 0);
    }

    private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode >= 0 && IsBlocking)
        {
            var hookStruct = Marshal.PtrToStructure<MSLLHOOKSTRUCT>(lParam);
            long extra = hookStruct.dwExtraInfo.ToInt64();
            if ((extra & MI_WP_TOUCH_MASK) == MI_WP_TOUCH_SIGNATURE)
            {
                // Swallow Windows touch-synthesized mouse move/click so finger taps
                // do not hijack the mouse cursor from the Right Stick.
                return new IntPtr(1);
            }
        }

        return CallNextHookEx(_hookHandle, nCode, wParam, lParam);
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        IsBlocking = false;

        if (_hookHandle != IntPtr.Zero)
        {
            UnhookWindowsHookEx(_hookHandle);
            _hookHandle = IntPtr.Zero;
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct POINT
    {
        public int x;
        public int y;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MSLLHOOKSTRUCT
    {
        public POINT pt;
        public uint mouseData;
        public uint flags;
        public uint time;
        public IntPtr dwExtraInfo;
    }

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelMouseProc lpfn, IntPtr hMod, uint dwThreadId);

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool UnhookWindowsHookEx(IntPtr hhk);

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

    [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr GetModuleHandle(string? lpModuleName);
}
