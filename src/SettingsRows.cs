using System;
using System.Collections.Generic;
using Il2CppInterop.Runtime;
using Il2CppMoon.Forsaken;
using MelonLoader;
using UnityEngine;

namespace QuickStart;

/// <summary>
/// Our checkboxes at the end of Options > Gameplay, after a divider (same recipe as the other mods of this workspace).
/// The game's own toggles need a PlayerSetting&lt;bool&gt; built on a ref-returning delegate, which a mod cannot supply;
/// AddKeyboardAndMouseSchemeToggleItem makes the same toggle row from a plain Action&lt;bool&gt;.
/// The stay key is a button row turned into a "press a key" row by KeyCapture.
/// </summary>
internal static class SettingsRows
{
    public const string Prefix = "QS_";
    private const PlayerSettingCategory Category = PlayerSettingCategory.Gameplay;
    // Alternate1: the only scheme style that does not arm the game's "preview keyboard scheme" button while hovered.
    private const KeyboardAndMouseStyle ToggleStyle = KeyboardAndMouseStyle.Alternate1;
    public const string StayKeyId = "QS_StayKey";
    private const string SpacerId = "QS_Spacer", VideosId = "QS_SkipVideos", MainMenuId = "QS_SkipMainMenu";
    private static readonly string[] AllIds = { SpacerId, VideosId, MainMenuId, StayKeyId };

    private static readonly Dictionary<string, LocalizedMessage> _messages = new Dictionary<string, LocalizedMessage>();

    /// <summary>Add the rows to every settings screen that already exists (after a hot reload).</summary>
    public static void AddToLiveScreens()
    {
        foreach (var s in Resources.FindObjectsOfTypeAll<SettingsScreen>())
            if (s != null && s.m_gameplayTab != null) AddTo(s.m_gameplayTab.m_controls);
    }

    public static void AddTo(SettingsScreenControls? controls)
    {
        if (controls == null) { QuickStartMod.Log.Warning("GameplaySettingsTab.m_controls is null"); return; }
        var content = GameplayContent(controls);
        if (content == null) { QuickStartMod.Log.Warning("Gameplay tab has no content root yet"); return; }

        ForgetRows(controls, oursToo: false);
        if (content.Find(VideosId) != null) return;
        RemoveRegistryEntries(controls);

        AddSpacer(controls, content);
        AddToggle(controls, content, VideosId, "Skip Videos on Launch",
            "Skip the logo video when the game starts (Quick Start).",
            Prefs.SkipVideos);
        AddToggle(controls, content, MainMenuId, "Skip Main Menu on Launch",
            MainMenuDescription(),
            Prefs.SkipMainMenu);
        AddStayKeyRow(controls, content);
        QuickStartMod.Log.Msg("Added Quick Start rows to Options > Gameplay");
    }

    /// <summary>A button row (like Reset Tutorials) that captures a key; KeyCapture adds the value label on the right.</summary>
    private static void AddStayKeyRow(SettingsScreenControls controls, RectTransform content)
    {
        ButtonSettingsItemGUI? row = null;
        Action onClick = () => { if (row != null) KeyCapture.Start(row); };
        int before = content.childCount;
        row = controls.AddButtonItem(StayKeyId, Category, Msg(StayKeyId, "Hold to Stay in Main Menu"), onClick,
            Msg(StayKeyId + "_Desc", "The key to hold while the game loads to stay in the main menu for that launch. " +
                                     "Select it and press the new key. Esc cancels, Backspace sets none (Quick Start)."));
        NameNewRow(content, before, StayKeyId);
        if (row == null) { QuickStartMod.Log.Warning("Stay key row: AddButtonItem returned nothing"); return; }
        KeyCapture.Adopt(row, content.Find(MainMenuId)?.Find("rightContent"));
    }

    /// <summary>The Skip Main Menu row's description names the stay key; rewrite it in place after a change.</summary>
    public static void RefreshMainMenuDescription()
    {
        if (_messages.TryGetValue(MainMenuId + "_Desc", out var desc) && desc != null) SetText(desc, MainMenuDescription());
    }

    private static string MainMenuDescription()
    {
        string text = "When the game starts, continue with your last character in their last realm, like pressing Continue.";
        var key = Prefs.GetStayKey();
        if (key != KeyCode.None) text += $" Hold {Prefs.StayKeyLabel(key)} while the game loads to stay in the main menu.";
        return text + " (Quick Start)";
    }

    /// <summary>Hot reload / unload: destroy our rows on every live settings screen and free their registry keys.</summary>
    public static void RemoveAll()
    {
        KeyCapture.Forget();
        foreach (var s in Resources.FindObjectsOfTypeAll<SettingsScreen>())
        {
            var controls = s != null && s.m_gameplayTab != null ? s.m_gameplayTab.m_controls : null;
            if (controls == null) continue;
            ForgetRows(controls, oursToo: true);
            var content = GameplayContent(controls);
            if (content != null)
                for (int i = content.childCount - 1; i >= 0; i--)
                {
                    var child = content.GetChild(i);
                    if (child != null && child.name.StartsWith(Prefix)) UnityEngine.Object.DestroyImmediate(child.gameObject);
                }
            RemoveRegistryEntries(controls);
        }
    }

    /// <summary>Drop the controls' references to destroyed rows (or, with oursToo, to ours): the cached selected / modal-previous
    /// element, and the dropdown list Esc walks (IsAnyDropDownOpen): a destroyed dropdown there makes Esc stop closing the screen.
    /// 1.0.0 test builds added a dropdown row; its destroyed copies are pruned here too.</summary>
    private static void ForgetRows(SettingsScreenControls controls, bool oursToo)
    {
        if (IsDeadOrOurs(controls.m_cachedSelectedItemGUI, oursToo)) controls.m_cachedSelectedItemGUI = null;
        if (IsDeadOrOurs(controls.m_modalPreviousElement, oursToo)) controls.m_modalPreviousElement = null;
        var dropdowns = controls.m_actualDropDownInstances;
        if (dropdowns != null)
            for (int i = dropdowns.Count - 1; i >= 0; i--)
                if (IsDeadOrOurs(dropdowns[i], oursToo) || (dropdowns[i] != null && dropdowns[i].gameObject.name.StartsWith(Prefix)))
                    dropdowns.RemoveAt(i);
    }

    private static bool IsDeadOrOurs(SettingsItemGUIBase? item, bool oursToo)
    {
        if (item is null) return false;                       // no reference at all
        if (item == null) return true;                        // Unity-destroyed object
        return oursToo && item.gameObject.name.StartsWith(Prefix);
    }

    private static RectTransform? GameplayContent(SettingsScreenControls controls)
    {
        var roots = controls.m_nameToContentRoot;
        if (roots == null || !roots.ContainsKey(Category)) return null;
        return roots[Category];
    }

    private static void RemoveRegistryEntries(SettingsScreenControls controls)
    {
        if (controls.m_categoryToContentToItem == null || !controls.m_categoryToContentToItem.ContainsKey(Category)) return;
        var items = controls.m_categoryToContentToItem[Category];
        if (items == null) return;
        foreach (var id in AllIds) items.Remove(id);
    }

    private static void AddToggle(SettingsScreenControls controls, RectTransform content, string id, string name, string desc,
        MelonPreferences_Entry<bool> pref)
    {
        Action<bool> onChanged = v => { pref.Value = v; MelonPreferences.Save(); };
        int before = content.childCount;
        controls.AddKeyboardAndMouseSchemeToggleItem(Category, Msg(id, name), pref.Value, onChanged, ToggleStyle, Msg(id + "_Desc", desc), false);
        NameNewRow(content, before, id);
    }

    /// <summary>The same empty divider row the game uses between its own groups.</summary>
    private static void AddSpacer(SettingsScreenControls controls, RectTransform content)
    {
        int before = content.childCount;
        controls.AddDividerItem(Category, SpacerId);
        NameNewRow(content, before, SpacerId);
        if (content.childCount > before)
        {
            var row = content.GetChild(content.childCount - 1).GetComponent<SettingsItemGUIBase>();
            if (row != null && row.SettingLabel != null) row.SettingLabel.text = "";
        }
    }

    private static void NameNewRow(RectTransform content, int before, string id)
    {
        if (content.childCount > before) content.GetChild(content.childCount - 1).name = id;
    }

    /// <summary>A LocalizedMessage is a ScriptableObject holding one string per language; fill every language with the same text.</summary>
    private static LocalizedMessage Msg(string id, string text)
    {
        if (_messages.TryGetValue(id, out var cached) && cached != null) return cached;
        var so = ScriptableObject.CreateInstance(Il2CppType.Of<LocalizedMessage>());
        var m = so.Cast<LocalizedMessage>();
        m.name = id;
        m.Id = id;
        SetText(m, text);
        m.hideFlags = HideFlags.HideAndDontSave;
        _messages[id] = m;
        return m;
    }

    private static void SetText(LocalizedMessage m, string text)
    {
        m.English = text; m.French = text; m.Italian = text; m.German = text; m.Spanish = text;
        m.BrazilianPortuguese = text; m.TraditionalChinese = text; m.SimplifiedChinese = text;
        m.Korean = text; m.Russian = text; m.Japanese = text; m.Polish = text;
    }
}
