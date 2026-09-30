using System;
using System.Runtime.InteropServices;
using UnityEngine;

namespace QuickStart;

/// <summary>
/// The physical key state from Windows (GetAsyncKeyState; Wine / Proton implement it too). Unity's Input.GetKey only sees
/// keys whose key-down reached the focused game window, so a key held since before the window existed reads as up.
/// </summary>
internal static class NativeKeys
{
    [DllImport("user32.dll")]
    private static extern short GetAsyncKeyState(int vKey);

    private static bool _unavailable;

    /// <summary>True when the key is down; false when it is up, has no Windows key code, or user32 is not there.</summary>
    public static bool IsDown(KeyCode key)
    {
        if (_unavailable) return false;
        int vk = VirtualKey(key);
        if (vk == 0) return false;
        try { return (GetAsyncKeyState(vk) & 0x8000) != 0; }
        catch (Exception e) when (e is DllNotFoundException || e is EntryPointNotFoundException)
        {
            _unavailable = true;
            QuickStartMod.Log.Warning("GetAsyncKeyState is not available, only keys pressed in the game window count: " + e.Message);
            return false;
        }
    }

    /// <summary>Windows virtual-key code for a Unity KeyCode; 0 for keys without one (gamepad buttons).</summary>
    private static int VirtualKey(KeyCode key)
    {
        if (key >= KeyCode.A && key <= KeyCode.Z) return 0x41 + (key - KeyCode.A);
        if (key >= KeyCode.Alpha0 && key <= KeyCode.Alpha9) return 0x30 + (key - KeyCode.Alpha0);
        if (key >= KeyCode.Keypad0 && key <= KeyCode.Keypad9) return 0x60 + (key - KeyCode.Keypad0);
        if (key >= KeyCode.F1 && key <= KeyCode.F15) return 0x70 + (key - KeyCode.F1);
        switch (key)
        {
            case KeyCode.LeftShift: return 0xA0;
            case KeyCode.RightShift: return 0xA1;
            case KeyCode.LeftControl: return 0xA2;
            case KeyCode.RightControl: return 0xA3;
            case KeyCode.LeftAlt: return 0xA4;
            case KeyCode.RightAlt: return 0xA5;
            case KeyCode.LeftWindows: return 0x5B;
            case KeyCode.RightWindows: return 0x5C;
            case KeyCode.Space: return 0x20;
            case KeyCode.Tab: return 0x09;
            case KeyCode.Return: return 0x0D;
            case KeyCode.KeypadEnter: return 0x0D;
            case KeyCode.Backspace: return 0x08;
            case KeyCode.Escape: return 0x1B;
            case KeyCode.CapsLock: return 0x14;
            case KeyCode.Insert: return 0x2D;
            case KeyCode.Delete: return 0x2E;
            case KeyCode.Home: return 0x24;
            case KeyCode.End: return 0x23;
            case KeyCode.PageUp: return 0x21;
            case KeyCode.PageDown: return 0x22;
            case KeyCode.LeftArrow: return 0x25;
            case KeyCode.UpArrow: return 0x26;
            case KeyCode.RightArrow: return 0x27;
            case KeyCode.DownArrow: return 0x28;
            case KeyCode.KeypadPlus: return 0x6B;
            case KeyCode.KeypadMinus: return 0x6D;
            case KeyCode.KeypadMultiply: return 0x6A;
            case KeyCode.KeypadDivide: return 0x6F;
            case KeyCode.KeypadPeriod: return 0x6E;
            case KeyCode.BackQuote: return 0xC0;
            case KeyCode.Minus: return 0xBD;
            case KeyCode.Equals: return 0xBB;
            case KeyCode.LeftBracket: return 0xDB;
            case KeyCode.RightBracket: return 0xDD;
            case KeyCode.Backslash: return 0xDC;
            case KeyCode.Semicolon: return 0xBA;
            case KeyCode.Quote: return 0xDE;
            case KeyCode.Comma: return 0xBC;
            case KeyCode.Period: return 0xBE;
            case KeyCode.Slash: return 0xBF;
            case KeyCode.Mouse0: return 0x01;
            case KeyCode.Mouse1: return 0x02;
            case KeyCode.Mouse2: return 0x04;
            case KeyCode.Mouse3: return 0x05;
            case KeyCode.Mouse4: return 0x06;
            default: return 0;
        }
    }
}
