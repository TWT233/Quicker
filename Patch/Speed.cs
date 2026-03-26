using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Vfx.Utilities;
using Quicker.Util;

namespace Quicker.Patch;

[HarmonyPatch(typeof(NHitStop), "SetTimeScale")]
public static class HitStopPatch
{
    public static void Prefix(ref float timeScale)
    {
        if (Context.IsDeltaMultiplied && timeScale >= 1.0f) timeScale = Context.DeltaMultiplier;
    }
}

[HarmonyPatch(typeof(NGame), "_Ready")]
public static class NGameReadyPatch
{
    public static void Postfix()
    {
        if (Context.IsDeltaMultiplied) Engine.TimeScale = Context.DeltaMultiplier;
    }
}