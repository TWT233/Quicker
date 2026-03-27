using System.Reflection;
using Godot;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Nodes.Debug;
using Logger = MegaCrit.Sts2.Core.Logging.Logger;

namespace Quicker.Util;

public static class Context
{
    public const string ModId = "Quicker";

    public static float DeltaMultiplier { get; set; } = 2.0f;
    public static bool IsDeltaMultiplied { get; set; } = true;

    private static Logger L { get; } = new(ModId, LogType.Generic);

    public static void Log(string message, LogLevel level = LogLevel.Info, int skipFrames = 1)
    {
        L.LogMessage(level, $"[{ModId}] {message}", skipFrames);

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