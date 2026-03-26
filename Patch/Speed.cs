using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Vfx.Utilities;
using Quicker.Util;

namespace Quicker.Patch;

public static class Speed
{
    public static void Patch(Harmony harmony)
    {
        // Manual patch for SmoothDamp due to ref parameters in signature
        try
        {
            var smoothDampFloat = typeof(MathHelper).GetMethod(nameof(MathHelper.SmoothDamp),
            [
                typeof(float), typeof(float), typeof(float).MakeByRefType(), typeof(float), typeof(float), typeof(float)
            ]);
            if (smoothDampFloat != null)
                harmony.Patch(smoothDampFloat,
                    new HarmonyMethod(typeof(SmoothDampPatches).GetMethod(nameof(SmoothDampPatches.FloatPrefix))));

            var smoothDampVector2 = typeof(MathHelper).GetMethod(nameof(MathHelper.SmoothDamp),
            [
                typeof(Vector2), typeof(Vector2), typeof(Vector2).MakeByRefType(), typeof(float), typeof(float),
                typeof(float)
            ]);
            if (smoothDampVector2 != null)
                harmony.Patch(smoothDampVector2,
                    new HarmonyMethod(typeof(SmoothDampPatches).GetMethod(nameof(SmoothDampPatches.Vector2Prefix))));
        }
        catch (Exception ex)
        {
            Context.Log($"Failed to patch SmoothDamp: {ex.Message}");
        }
    }
}

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

public static class SmoothDampPatches
{
    public static bool FloatPrefix(float target, ref float currentVelocity, ref float __result)
    {
        if (!Context.IsInstantLerp) return true;
        __result = target;
        currentVelocity = 0;
        return false;
    }

    public static bool Vector2Prefix(Vector2 target, ref Vector2 currentVelocity, ref Vector2 __result)
    {
        if (!Context.IsInstantLerp) return true;
        __result = target;
        currentVelocity = Vector2.Zero;
        return false;
    }
}