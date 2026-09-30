using System;
using System.Collections.Generic;
using Il2CppMoon.Forsaken;
using Il2CppTMPro;
using UnityEngine;

namespace QuickStart;

/// <summary>
/// The "Hold to Stay in Main Menu" row: a button row (like Reset Tutorials) with the current key shown on the right, in a
/// copy of a toggle row's On/Off label. Clicking it (mouse, Enter, gamepad A) waits for the next key press, like the
/// game's own key binding screen: that key becomes the stay key, Esc cancels, Backspace / Delete clears it.
/// A button row, not a dropdown: SettingsScreenControls tracks dropdowns for Esc handling, which a mod row must stay out of.
/// </summary>
internal static class KeyCapture
{
    private const float CaptureSeconds = 8f;
    private const string PromptText = "Press a key...";
    private const string ValueName = "QS_StayKeyValue";

    private static readonly List<TMP_Text> _values = new List<TMP_Text>();
    private static TMP_Text? _capturing;
    private static int _startFrame;
    private static float _until;
    private static KeyCode[]? _candidates;

    /// <summary>Give a freshly added button row a value label on the right: a copy of a toggle row's "rightContent" block
    /// (same anchors in both rows) without its checkbox, so the key has the On/Off label's font, size and baseline, and ends where the checkboxes end.</summary>
    public static void Adopt(ButtonSettingsItemGUI row, Transform? toggleRightContent)
    {
        var template = toggleRightContent?.Find("onOffLabel")?.GetComponent<TMP_Text>();
        if (toggleRightContent == null || template == null) { QuickStartMod.Log.Warning("Stay key row: no On/Off label to copy, the key is not shown"); return; }
        var block = UnityEngine.Object.Instantiate(toggleRightContent.gameObject, row.transform, false);
        block.name = ValueName;
        for (int i = block.transform.childCount - 1; i >= 0; i--)
        {
            var child = block.transform.GetChild(i);
            if (child.name != "onOffLabel") UnityEngine.Object.DestroyImmediate(child.gameObject);
        }
        var text = block.transform.Find("onOffLabel").GetComponent<TMP_Text>();
        // LocalizedText would put "On" back on every language refresh.
        foreach (var c in text.GetComponents<Component>())
            if (c.GetIl2CppType().Name == "LocalizedText") UnityEngine.Object.DestroyImmediate(c);
        // Fixed at the On/Off size; wider box on the left for "Press a key..." (the right edge stays: pivot is right).
        text.enableAutoSizing = false;
        text.fontSize = template.fontSize;
        var rt = text.rectTransform;
        rt.sizeDelta = new Vector2(Mathf.Max(rt.sizeDelta.x, 330f), rt.sizeDelta.y);
        // Right edge on the checkboxes' right edge (the Toggle sits at the block's right edge), not on the On/Off text.
        rt.anchoredPosition = new Vector2(0f, rt.anchoredPosition.y);
        text.raycastTarget = false;
        _values.RemoveAll(v => v == null);
        _values.Add(text);
        text.text = Prefs.StayKeyLabel(Prefs.GetStayKey());
    }

    public static void Forget() { _values.Clear(); _capturing = null; }

    /// <summary>The row's click / A button: start listening, on the value label of that row.</summary>
    public static void Start(ButtonSettingsItemGUI row)
    {
        if (_capturing != null) return;
        var value = row.transform.Find(ValueName + "/onOffLabel")?.GetComponent<TMP_Text>();
        if (value == null) return;
        _capturing = value;
        _startFrame = Time.frameCount;   // the key or click that started the capture is still down this frame
        _until = Time.unscaledTime + CaptureSeconds;
        value.text = PromptText;
    }

    /// <summary>Every frame from the mod's OnUpdate.</summary>
    public static void Update()
    {
        var value = _capturing;
        if (value is null) return;
        if (value == null || !value.gameObject.activeInHierarchy || Time.unscaledTime > _until) { Finish(null); return; }
        if (Time.frameCount <= _startFrame) return;

        foreach (var key in Candidates())
        {
            if (!Input.GetKeyDown(key)) continue;
            if (key == KeyCode.Escape) Finish(null);
            else if (key == KeyCode.Backspace || key == KeyCode.Delete) Finish(KeyCode.None);
            else Finish(key);
            return;
        }
    }

    private static void Finish(KeyCode? key)
    {
        _capturing = null;
        if (key.HasValue)
        {
            Prefs.SetStayKey(key.Value);
            QuickStartMod.Log.Msg($"Stay key set to {(key.Value == KeyCode.None ? "none" : key.Value.ToString())}.");
            SettingsRows.RefreshMainMenuDescription();
        }
        string label = Prefs.StayKeyLabel(Prefs.GetStayKey());
        _values.RemoveAll(v => v == null);
        foreach (var v in _values) v.text = label;
    }

    /// <summary>Keyboard keys and gamepad buttons; not mouse buttons (the click that starts the capture is one).</summary>
    private static KeyCode[] Candidates()
    {
        if (_candidates != null) return _candidates;
        var list = new List<KeyCode>();
        foreach (KeyCode k in Enum.GetValues(typeof(KeyCode)))
        {
            if (k == KeyCode.None || (k >= KeyCode.Mouse0 && k <= KeyCode.Mouse6)) continue;
            // Generic gamepad buttons only; JoystickNButtonM duplicates them per pad.
            if (k > KeyCode.JoystickButton19) continue;
            if (!list.Contains(k)) list.Add(k);
        }
        return _candidates = list.ToArray();
    }
}
