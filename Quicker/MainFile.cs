using System.Reflection;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Modding;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Debug;
using Logger = MegaCrit.Sts2.Core.Logging.Logger;

namespace Quicker;

[ModInitializer(nameof(Initialize))]
public partial class MainFile : Node
{
    private const string ModId = "Quicker";
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
    public static bool FloatPrefix(float target, ref float currentVelocity, ref float result)
    {
        if (!MainFile.IsInstantLerp) return true;
        result = target;
        currentVelocity = 0;
        return false;
    }

    public static bool Vector2Prefix(Vector2 target, ref Vector2 currentVelocity, ref Vector2 result)
    {
        if (!MainFile.IsInstantLerp) return true;
        result = target;
        currentVelocity = Vector2.Zero;
        return false;
    }
}