using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Cards.Holders;
using MegaCrit.Sts2.Core.Nodes.Combat;

namespace Quicker.Patch;

[HarmonyPatch]
public class Patcha
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