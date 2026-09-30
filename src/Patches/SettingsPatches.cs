using HarmonyLib;
using Il2CppMoon.Forsaken;

namespace QuickStart.Patches;

/// <summary>Adds our rows to the Gameplay tab. The game builds settings screens when a scene loads, not on menu open.</summary>
[HarmonyPatch(typeof(GameplaySettingsTab), nameof(GameplaySettingsTab.Initialize))]
internal static class GameplaySettingsTabInitializePatch
{
    private static void Postfix(GameplaySettingsTab __instance)
    {
        if (!Prefs.AddSettingsRows.Value) return;
        try { SettingsRows.AddTo(__instance.m_controls); }
        catch (System.Exception e) { QuickStartMod.Log.Error("Adding settings rows failed: " + e); }
    }
}

