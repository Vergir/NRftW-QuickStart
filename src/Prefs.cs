using System;
using System.Text.RegularExpressions;
using MelonLoader;
using UnityEngine;

namespace QuickStart;

/// <summary>User settings, stored in UserData/MelonPreferences.cfg under [QuickStart].
/// SkipVideos and SkipMainMenu are also checkboxes in Options > Gameplay, StayKey a "press a key" row there.</summary>
internal static class Prefs
{
    public static MelonPreferences_Entry<bool> Enabled = null!;
    public static MelonPreferences_Entry<bool> SkipVideos = null!;
    public static MelonPreferences_Entry<bool> SkipMainMenu = null!;
    public static MelonPreferences_Entry<string> StayKey = null!;
    public static MelonPreferences_Entry<float> Delay = null!;
    public static MelonPreferences_Entry<float> Timeout = null!;
    public static MelonPreferences_Entry<bool> AddSettingsRows = null!;

    public static void Init()
    {
        var cat = MelonPreferences.CreateCategory("QuickStart", "Quick Start");
        Enabled = cat.CreateEntry("Enabled", true, description: "Master switch.");
        SkipVideos = cat.CreateEntry("SkipVideos", true,
            description: "Skip the logo video when the game starts. Also a checkbox in Options > Gameplay.");
        SkipMainMenu = cat.CreateEntry("SkipMainMenu", true,
            description: "Press Continue on the main menu once per launch (last character, last realm). Also a checkbox in Options > Gameplay.");
        StayKey = cat.CreateEntry("StayKey", "LeftShift",
            description: "Hold this key while the game starts to stay in the main menu. A Unity KeyCode name (LeftShift, LeftControl, Space, ...); empty = no key. Also a row in Options > Gameplay: click it and press a key.");
        Delay = cat.CreateEntry("Delay", 0f,
            description: "Seconds to wait on the main menu, once Continue is available, before pressing it.");
        Timeout = cat.CreateEntry("Timeout", 60f,
            description: "Give up if Continue is still unavailable this many seconds after the main menu appears (no character yet, save data not loading). " +
                         "Counted from the menu appearing, not from the game starting.");
        AddSettingsRows = cat.CreateEntry("AddSettingsRows", true,
            description: "Add this mod's rows to Options > Gameplay.");
    }

    /// <summary>The stay key, or None when the setting is empty or not a KeyCode name.</summary>
    public static KeyCode GetStayKey()
    {
        string name = StayKey.Value?.Trim() ?? "";
        return name.Length > 0 && Enum.TryParse(name, true, out KeyCode key) ? key : KeyCode.None;
    }

    public static void SetStayKey(KeyCode key)
    {
        StayKey.Value = key == KeyCode.None ? "" : key.ToString();
        MelonPreferences.Save();
    }

    /// <summary>The stay key for display: "Left Shift", "Left Ctrl", "5", "Gamepad A"; other KeyCodes spaced out (PageUp -> "Page Up").</summary>
    public static string StayKeyLabel(KeyCode key)
    {
        switch (key)
        {
            case KeyCode.None: return "None";
            case KeyCode.LeftControl: return "Left Ctrl";
            case KeyCode.RightControl: return "Right Ctrl";
            case KeyCode.Return: return "Enter";
            case KeyCode.KeypadEnter: return "Keypad Enter";
        }
        if (key >= KeyCode.Alpha0 && key <= KeyCode.Alpha9) return ((int)(key - KeyCode.Alpha0)).ToString();
        if (key >= KeyCode.JoystickButton0 && key <= KeyCode.JoystickButton19)
        {
            // Unity's XInput numbering on Windows.
            string[] xbox = { "A", "B", "X", "Y", "LB", "RB", "View", "Menu", "Left Stick", "Right Stick" };
            int i = key - KeyCode.JoystickButton0;
            return "Gamepad " + (i < xbox.Length ? xbox[i] : "Button " + i);
        }
        return Regex.Replace(key.ToString(), "(?<=[a-z])(?=[A-Z0-9])", " ");
    }
}
