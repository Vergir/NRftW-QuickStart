using HarmonyLib;
using Il2CppMoon.IntroSequence;

namespace QuickStart.Patches;

/// <summary>
/// The boot intro (the Moon Studios logo video) asks ShouldSkip every frame; the game's own version returns true once
/// Escape or a skip input is pressed. Answering "skipped" right away ends the video before it shows.
/// (PlayIntroSequence.SkipLogo is not called anywhere in the build, so patching it does nothing.)
/// </summary>
[HarmonyPatch(typeof(PlayIntroSequence), nameof(PlayIntroSequence.ShouldSkip))]
internal static class IntroSequenceShouldSkipPatch
{
    private static bool _logged;

    private static bool Prefix(ref bool hasSkipped, ref bool __result)
    {
        if (!Prefs.SkipVideos.Value) return true;
        if (!_logged)
        {
            _logged = true;
            QuickStartMod.Log.Msg("Skipping the intro video.");
        }
        hasSkipped = true;
        __result = true;
        return false;
    }
}
