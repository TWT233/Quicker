using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Vfx.Utilities;
using Quicker.Util;

namespace Quicker.Patch;

[HarmonyPatch]
public class Speed
{
    // SetTimeScale
    // some battle scene action would change game speed
    // fix by this patch
    [HarmonyPrefix]
    [HarmonyPatch(typeof(NHitStop), "SetTimeScale")]
    private static void SetTimeScale(NHitStop __instance, ref float timeScale)
    {
        if (Context.IsDeltaMultiplied && timeScale >= 1.0f) timeScale = Context.DeltaMultiplier;
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(NGame), "_Ready")]
    private static void NGameReady(NGame __instance)
    {
        if (Context.IsDeltaMultiplied) Engine.TimeScale = Context.DeltaMultiplier;
    }
}