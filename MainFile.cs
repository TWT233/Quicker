using System.Reflection;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Modding;
using Quicker.Patch;
using Quicker.Util;

namespace Quicker;

[ModInitializer(nameof(Initialize))]
public partial class MainFile : Node
{
    public static void Initialize()
    {
        Harmony harmony = new(Context.ModId);

        // Auto patch with attributes (for NGame)
        harmony.PatchAll(Assembly.GetExecutingAssembly());

        Speed.Patch(harmony);

        Context.Log("Initialized");
    }
}