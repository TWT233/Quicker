using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes;
using Quicker.Util;

namespace Quicker.Patch;

[HarmonyPatch(typeof(NGame), "_Input")]
public static class Hotkey
{
    public static void Postfix(InputEvent inputEvent)
    {
        if (inputEvent is not InputEventKey { Pressed: true } keyEvent || keyEvent.Echo) return;

        var oldScale = Engine.TimeScale;

        switch (keyEvent.Keycode)
        {
            case Key.F8:
                Context.IsDeltaMultiplied = !Context.IsDeltaMultiplied;
                Engine.TimeScale = Context.IsDeltaMultiplied ? Context.DeltaMultiplier : 1.0;
                Context.Log(
                    $"Speed Multiplier Toggled: {Context.IsDeltaMultiplied} (Scale: {oldScale:F1} -> {Engine.TimeScale:F1})");
                break;
            case Key.F9:
                Context.IsInstantLerp = !Context.IsInstantLerp;
                Context.Log($"Instant Lerp Toggled: {Context.IsInstantLerp}");
                break;
            case Key.Bracketright:
                Context.DeltaMultiplier += 0.5f;
                if (Context.IsDeltaMultiplied) Engine.TimeScale = Context.DeltaMultiplier;
                Context.Log($"Speed Multiplier Increased: {oldScale:F1} -> {Engine.TimeScale:F1}");
                break;
            case Key.Bracketleft:
                Context.DeltaMultiplier = Math.Max(0.5f, Context.DeltaMultiplier - 0.5f);
                if (Context.IsDeltaMultiplied) Engine.TimeScale = Context.DeltaMultiplier;
                Context.Log($"Speed Multiplier Decreased: {oldScale:F1} -> {Engine.TimeScale:F1}");
                break;
        }
    }
}