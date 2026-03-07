using System.Reflection;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Modding;
using MegaCrit.Sts2.Core.Nodes;

namespace Quicker;

[ModInitializer(nameof(Initialize))]
public partial class MainFile : Node
{
    private const string ModId = "Quicker";
    private static MegaCrit.Sts2.Core.Logging.Logger Logger { get; } = new(ModId, MegaCrit.Sts2.Core.Logging.LogType.Generic);

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
                [typeof(float), typeof(float), typeof(float).MakeByRefType(), typeof(float), typeof(float), typeof(float)
                ]);
            if (smoothDampFloat != null)
            {
                harmony.Patch(smoothDampFloat, prefix: new HarmonyMethod(typeof(SmoothDampPatches).GetMethod(nameof(SmoothDampPatches.FloatPrefix))));
            }

            var smoothDampVector2 = typeof(MathHelper).GetMethod(nameof(MathHelper.SmoothDamp),
                [typeof(Vector2), typeof(Vector2), typeof(Vector2).MakeByRefType(), typeof(float), typeof(float), typeof(float)
                ]);
            if (smoothDampVector2 != null)
            {
                harmony.Patch(smoothDampVector2, prefix: new HarmonyMethod(typeof(SmoothDampPatches).GetMethod(nameof(SmoothDampPatches.Vector2Prefix))));
            }
        }
        catch (Exception ex)
        {
            Logger.Error($"Failed to patch SmoothDamp: {ex.Message}");
        }
        
        Log("Quicker Initialized");
    }

    public static void Log(string message)
    {
        Logger.Info(message);
        try
        {
            var console = MegaCrit.Sts2.Core.Nodes.Debug.NDevConsole.Instance;
            var outputBufferField = typeof(MegaCrit.Sts2.Core.Nodes.Debug.NDevConsole).GetField("_outputBuffer", BindingFlags.NonPublic | BindingFlags.Instance);
            if (outputBufferField?.GetValue(console) is RichTextLabel outputBuffer)
            {
                outputBuffer.Text += $"[color=#00ffff][Quicker][/color] {message}\n";
            }
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
        if (MainFile.IsDeltaMultiplied)
        {
            Engine.TimeScale = MainFile.DeltaMultiplier;
        }
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
                MainFile.Log($"Speed Multiplier Toggled: {MainFile.IsDeltaMultiplied} (Scale: {oldScale:F1} -> {Engine.TimeScale:F1})");
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
