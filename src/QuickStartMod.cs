using System;
using MelonLoader;
using QuickStart;

[assembly: MelonInfo(typeof(QuickStartMod), "Quick Start", "1.0.0", "vergir")]
[assembly: MelonGame("Moon Studios", "NoRestForTheWicked")]
// Patches are applied below, only when enabled (MelonLoader would otherwise apply them all by itself).
[assembly: HarmonyDontPatchAll]

namespace QuickStart;

/// <summary>Entry point: skips the intro video and continues with the last played character as soon as the main menu allows it.</summary>
public class QuickStartMod : MelonMod
{
    public static QuickStartMod Instance { get; private set; } = null!;
    public static MelonLogger.Instance Log => Instance.LoggerInstance;

    public override void OnInitializeMelon()
    {
        Instance = this;
        Prefs.Init();
        if (!Prefs.Enabled.Value)
        {
            LoggerInstance.Msg("Disabled via preferences.");
            return;
        }
        AutoContinue.Init();
        LoggerInstance.Msg($"Loaded at frame {UnityEngine.Time.frameCount}, {UnityEngine.Time.realtimeSinceStartup:0.0} s after start; " +
                           (AutoContinue.Armed ? "will press Continue on the main menu." : "not armed."));
        HarmonyInstance.PatchAll(typeof(QuickStartMod).Assembly);

        // After a hot reload the settings screens already exist; the Initialize postfix will not run for them.
        if (Prefs.AddSettingsRows.Value)
        {
            try { SettingsRows.AddToLiveScreens(); }
            catch (Exception e) { LoggerInstance.Warning("Adding rows to live settings screens: " + e.Message); }
        }
    }

    /// <summary>Unload / hot reload: take our rows back off the game's settings screens.</summary>
    public override void OnDeinitializeMelon()
    {
        try { SettingsRows.RemoveAll(); }
        catch (Exception e) { LoggerInstance.Warning("SettingsRows.RemoveAll: " + e.Message); }
    }

    public override void OnUpdate()
    {
        AutoContinue.Update();
        KeyCapture.Update();
    }
}
