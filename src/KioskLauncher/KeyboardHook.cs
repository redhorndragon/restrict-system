using System.Runtime.InteropServices;

namespace KioskLauncher;

public sealed class KeyboardHook : IDisposable
{
    private const int WH_KEYBOARD_LL = 13;
    private const int VK_LWIN = 0x5B;
    private const int VK_RWIN = 0x5C;
    private const int VK_TAB = 0x09;
    private const int VK_ESCAPE = 0x1B;
    private const int VK_F4 = 0x73;
    private const int VK_MENU = 0x12;
    private const int VK_CONTROL = 0x11;
    private const int VK_SHIFT = 0x10;

    private readonly HookProc _proc;
    private readonly IntPtr _hookId;
    private bool _disposed;

    public KeyboardHook()
    {
        _proc = HookCallback;
        _hookId = SetHook(_proc);
    }

    private static IntPtr SetHook(HookProc proc)
    {
        using var process = System.Diagnostics.Process.GetCurrentProcess();
        using var module = process.MainModule;
        return SetWindowsHookEx(WH_KEYBOARD_LL, proc, GetModuleHandle(module?.ModuleName ?? string.Empty), 0);
    }

    private static IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode >= 0)
        {
            var vkCode = Marshal.ReadInt32(lParam);
            var alt = (GetKeyState(VK_MENU) & 0x8000) != 0;
            var ctrl = (GetKeyState(VK_CONTROL) & 0x8000) != 0;
            var shift = (GetKeyState(VK_SHIFT) & 0x8000) != 0;

            var blocked =
                vkCode == VK_LWIN || vkCode == VK_RWIN ||
                (alt && vkCode == VK_TAB) ||
                (ctrl && vkCode == VK_ESCAPE) ||
                (ctrl && shift && vkCode == VK_ESCAPE) ||
                (alt && vkCode == VK_F4);

            if (blocked) return (IntPtr)1;
        }
        return CallNextHookEx(IntPtr.Zero, nCode, wParam, lParam);
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        UnhookWindowsHookEx(_hookId);
    }

    private delegate IntPtr HookProc(int nCode, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr SetWindowsHookEx(int idHook, HookProc lpfn, IntPtr hMod, uint dwThreadId);

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool UnhookWindowsHookEx(IntPtr hhk);

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

    [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr GetModuleHandle(string lpModuleName);

    [DllImport("user32.dll")]
    private static extern short GetKeyState(int nVirtKey);
}
