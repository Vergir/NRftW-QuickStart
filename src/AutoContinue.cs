using Il2CppMoon.Forsaken;
using Il2CppMoon.TheLoop;
using UnityEngine;

namespace QuickStart;

/// <summary>
/// Presses the main menu's Continue button once per game launch, as soon as the game enables it.
/// Continue = the game's own "last character in its last realm" (MainMenuSelectScreen.OnContinueButtonPressed:
/// TryGetLastPlayedRealmForCharacter + ClientFlowAPI.RequestSessionJoin; it falls back to the menu itself on failure).
/// </summary>
internal static class AutoContinue
{
    private static bool _armed;
    private static float _menuSeenAt = -1f, _readySince = -1f;

    public static bool Armed => _armed;

    /// <summary>Arm at mod load, unless the mod was loaded into a running game (a hot reload, or a DLL added later).</summary>
    public static void Init()
    {
        _menuSeenAt = _readySince = -1f;
        _armed = true;
        // Set by MelonLoader HotReload 1.1+ while it loads a melon after startup: "reload" or "new"; null at game start.
        var loadedLate = System.AppDomain.CurrentDomain.GetData("HotReload.LoadingLate") as string;
        if (!Prefs.SkipMainMenu.Value)
            Disarm("SkipMainMenu is off");
        else if (loadedLate != null)
            Disarm(loadedLate == "reload" ? "hot reload" : "loaded into a running game");
        // Older HotReload builds set no flag: tell from the game's state.
        else if (ViewFrame.s_activeViewFrame != null)
            Disarm("the game is already in a realm");
        else if (Object.FindObjectOfType<MainMenuSelectScreen>() != null)
            Disarm("the main menu is already up");
    }

    /// <summary>Postfix of MainMenuSelectScreen.OnExitState: the player went elsewhere (character creation, settings, a modal),
    /// so Continue must not fire when they come back.</summary>
    public static void OnSelectScreenExit()
    {
        if (_menuSeenAt >= 0f) Disarm("the main menu screen was left before Continue was available");
    }

    public static void Disarm(string reason)
    {
        if (!_armed) return;
        _armed = false;
        QuickStartMod.Log.Msg("Staying in the main menu: " + reason + ".");
    }

    /// <summary>Every frame, from mod load until it fires or gives up: the stay key cancels.</summary>
    public static void Update()
    {
        if (!_armed) return;
        var key = Prefs.GetStayKey();
        if (key != KeyCode.None && (Input.GetKey(key) || NativeKeys.IsDown(key)))
            Disarm(key + " was held");
    }

    /// <summary>Postfix of MainMenuSelectScreen.OnUpdateActiveScreen: runs each frame while the menu is the active screen.</summary>
    public static void OnSelectScreenUpdate(MainMenuSelectScreen screen)
    {
        Update();   // the stay key, also when the game's update runs before the mod's this frame
        if (!_armed) return;
        float now = Time.unscaledTime;
        if (_menuSeenAt < 0f)
        {
            _menuSeenAt = now;
            QuickStartMod.Log.Msg("Main menu is up, waiting for Continue.");
        }

        var button = screen.ContinueButton;
        bool ready = button != null && button.interactable && button.gameObject.activeInHierarchy && !screen.m_isJoiningRealm;
        if (!ready)
        {
            _readySince = -1f;
            if (now - _menuSeenAt > Prefs.Timeout.Value)
                Disarm($"Continue was not available within {Prefs.Timeout.Value:0.#} s (no character yet?)");
            return;
        }

        if (_readySince < 0f) _readySince = now;
        if (now - _readySince < Prefs.Delay.Value) return;

        _armed = false;
        QuickStartMod.Log.Msg($"Pressing Continue ({now - _menuSeenAt:0.00} s after the main menu appeared).");
        screen.OnContinueButtonPressed();
    }
}
