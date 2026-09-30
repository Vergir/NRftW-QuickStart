using HarmonyLib;
using Il2CppMoon.Forsaken;

namespace QuickStart.Patches;

[HarmonyPatch(typeof(MainMenuSelectScreen), nameof(MainMenuSelectScreen.OnUpdateActiveScreen))]
internal static class MainMenuSelectScreenUpdatePatch
{
    private static void Postfix(MainMenuSelectScreen __instance) => AutoContinue.OnSelectScreenUpdate(__instance);
}

[HarmonyPatch(typeof(MainMenuSelectScreen), nameof(MainMenuSelectScreen.OnExitState))]
internal static class MainMenuSelectScreenExitPatch
{
    private static void Postfix() => AutoContinue.OnSelectScreenExit();
}
