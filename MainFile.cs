using System.Reflection;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Modding;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Cards.Holders;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Debug;
using MegaCrit.Sts2.Core.Nodes.Vfx.Utilities;
using Logger = MegaCrit.Sts2.Core.Logging.Logger;

namespace Quicker;

[ModInitializer(nameof(Initialize))]
public partial class MainFile : Node
{
    public const string ModId = "Quicker"; //At the moment, this is used only for the Logger and harmony names.
    private static Logger Logger { get; } = new(ModId, LogType.Generic);

    public static float DeltaMultiplier { get; set; } = 2.0f;
    public static bool IsDeltaMultiplied { get; set; } = true;
    public static bool IsInstantLerp { get; set; } = true;

    public static void Initialize()
    {
        Harmony harmony = new(ModId);

        // Auto patch with attributes (for NGame)
        harmony.PatchAll(Assembly.GetExecutingAssembly());

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
            Logger.Error($"Failed to patch SmoothDamp: {ex.Message}");
        }

        Log("Initialized");
    }

    public static void Log(string message, LogLevel level = LogLevel.Info, int skipFrames = 1)
    {
        Logger.LogMessage(level, $"[{ModId}] {message}", skipFrames);

        try
        {
            var console = NDevConsole.Instance;
            var outputBufferField =
                typeof(NDevConsole).GetField("_outputBuffer", BindingFlags.NonPublic | BindingFlags.Instance);
            if (outputBufferField?.GetValue(console) is not RichTextLabel outputBuffer) return;
            outputBuffer.Text += $"[color=#00ffff][{ModId}][/color] {message}";
            outputBuffer.Text += "\n";
        }
        catch
        {
            // Console might not be initialized yet
        }
    }
}

[HarmonyPatch(typeof(NHitStop), "SetTimeScale")]
public static class HitStopPatch
{
    public static void Prefix(ref float timeScale)
    {
        if (MainFile.IsDeltaMultiplied && timeScale >= 1.0f)
        {
            timeScale = MainFile.DeltaMultiplier;
        }
    }
}

[HarmonyPatch(typeof(NGame), "_Ready")]
public static class NGameReadyPatch
{
    public static void Postfix()
    {
        if (MainFile.IsDeltaMultiplied) Engine.TimeScale = MainFile.DeltaMultiplier;
    }
}

[HarmonyPatch(typeof(NGame), "_Input")]
public static class NGameInputPatch
{
    public static void Postfix(InputEvent inputEvent)
    {
        if (inputEvent is not InputEventKey { Pressed: true } keyEvent || keyEvent.Echo) return;

        var oldScale = Engine.TimeScale;

        switch (keyEvent.Keycode)
        {
            case Key.F8:
                MainFile.IsDeltaMultiplied = !MainFile.IsDeltaMultiplied;
                Engine.TimeScale = MainFile.IsDeltaMultiplied ? MainFile.DeltaMultiplier : 1.0;
                MainFile.Log(
                    $"Speed Multiplier Toggled: {MainFile.IsDeltaMultiplied} (Scale: {oldScale:F1} -> {Engine.TimeScale:F1})");
                break;
            case Key.F9:
                MainFile.IsInstantLerp = !MainFile.IsInstantLerp;
                MainFile.Log($"Instant Lerp Toggled: {MainFile.IsInstantLerp}");
                break;
            case Key.Bracketright:
                MainFile.DeltaMultiplier += 0.5f;
                if (MainFile.IsDeltaMultiplied) Engine.TimeScale = MainFile.DeltaMultiplier;
                MainFile.Log($"Speed Multiplier Increased: {oldScale:F1} -> {Engine.TimeScale:F1}");
                break;
            case Key.Bracketleft:
                MainFile.DeltaMultiplier = Math.Max(0.5f, MainFile.DeltaMultiplier - 0.5f);
                if (MainFile.IsDeltaMultiplied) Engine.TimeScale = MainFile.DeltaMultiplier;
                MainFile.Log($"Speed Multiplier Decreased: {oldScale:F1} -> {Engine.TimeScale:F1}");
                break;
        }
    }
}

public static class SmoothDampPatches
{
    public static bool FloatPrefix(float target, ref float currentVelocity, ref float __result)
    {
        if (!MainFile.IsInstantLerp) return true;
        __result = target;
        currentVelocity = 0;
        return false;
    }

    public static bool Vector2Prefix(Vector2 target, ref Vector2 currentVelocity, ref Vector2 __result)
    {
        if (!MainFile.IsInstantLerp) return true;
        __result = target;
        currentVelocity = Vector2.Zero;
        return false;
    }
}

[HarmonyPatch]
public class Patch
{
    [HarmonyPrefix]
    [HarmonyPatch(typeof(NMouseCardPlay), "TargetSelection")]
    private static bool TargetSelection(NMouseCardPlay __instance, TargetMode targetMode, ref Task __result)
    {
        var card = AccessTools.PropertyGetter(typeof(NCardPlay), "Card")?.Invoke(__instance, null) as CardModel;

        if (!IsAutoPlayable(card)) return true;

        // manually set _target if type == AnyEnemy
        if (card is { TargetType: TargetType.AnyEnemy })
        {
            var target = card.CombatState?.HittableEnemies[0];
            if (target is null) return true;
            AccessTools.Field(typeof(NMouseCardPlay), "_target").SetValue(__instance, target);
        }

        // MegaCrit sets _target for All other types in IsAutoPlayable()
        __result = Task.CompletedTask;
        return false;
    }


    [HarmonyPrefix]
    [HarmonyPatch(typeof(NMouseCardPlay), "IsCardInPlayZone")]
    private static bool IsCardInPlayZone(ref bool __result)
    {
        __result = true;
        return false;
    }


    [HarmonyPatch(typeof(NPlayerHand), "StartCardPlay")]
    private static void StartCardPlay(NHandCardHolder holder, ref bool startedViaShortcut)
    {
        if (IsAutoPlayable(holder.CardModel)) startedViaShortcut = true;
    }


    public static bool IsAutoPlayable(CardModel? card)
    {
        if (card?.CombatState == null) return false;

        return card.TargetType switch
        {
            TargetType.None or TargetType.Self or TargetType.AllEnemies or TargetType.RandomEnemy => true,
            TargetType.AnyEnemy => card.CombatState.HittableEnemies.Count == 1,
            _ => false
        };
    }
}