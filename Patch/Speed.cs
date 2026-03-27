using System.Reflection;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Helpers;
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
    private static void SetTimeScale(NHitStop __instance, float timeScale)
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

[HarmonyPatch]
public static class SmoothDampFloatPatch
{
    public static MethodBase TargetMethod()
    {
        return typeof(MathHelper).GetMethod(
            nameof(MathHelper.SmoothDamp),
            BindingFlags.Public | BindingFlags.Static,
            null,
            [typeof(float), typeof(float), typeof(float).MakeByRefType(), typeof(float), typeof(float), typeof(float)],
            null
        )!;
    }

    [HarmonyPrefix]
    public static bool Prefix(
        float current,
        float target,
        ref float currentVelocity,
        float smoothTime,
        float deltaTime,
        float maxSpeed,
        ref float __result)
    {
        if (!Context.IsDeltaMultiplied || Context.DeltaMultiplier <= 1.0f) return true;
        __result = target;
        currentVelocity = 0;
        return false;
    }
}

[HarmonyPatch]
public static class SmoothDampVector2Patch
{
    public static MethodBase TargetMethod()
    {
        return typeof(MathHelper).GetMethod(
            nameof(MathHelper.SmoothDamp),
            BindingFlags.Public | BindingFlags.Static,
            null,
            [
                typeof(Vector2),
                typeof(Vector2),
                typeof(Vector2).MakeByRefType(),
                typeof(float),
                typeof(float),
                typeof(float)
            ],
            null
        )!;
    }

    [HarmonyPrefix]
    public static bool Prefix(
        Vector2 current,
        Vector2 target,
        ref Vector2 currentVelocity,
        float smoothTime,
        float deltaTime,
        float maxSpeed,
        ref Vector2 __result)
    {
        if (!Context.IsDeltaMultiplied || Context.DeltaMultiplier <= 1.0f) return true;
        __result = target;
        currentVelocity = Vector2.Zero;
        return false;
    }
}