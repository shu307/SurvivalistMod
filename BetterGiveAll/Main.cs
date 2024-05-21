using HarmonyLib;
using System;
using UnityEngine;
using System.Reflection;



public class Main
{
    public static Harmony HarmonyInstance;

    public static void Load()
    {
        //Harmony.DEBUG = true;
        HarmonyInstance = new Harmony("shu307.bettergiveall");

        var originalInfoTransferAll = AccessTools.Method(typeof(InfoPage), "TransferAll", new Type[] { typeof(InventoryBehaviour), typeof(InventoryBehaviour), typeof(bool), typeof(InputFrame) });
        var prefixInfoTransferAll = typeof(BetterGiveAllPatchers).GetMethod(nameof(BetterGiveAllPatchers.TransferAllPrefix));
        var postfixInfoTransferAll = typeof(BetterGiveAllPatchers).GetMethod(nameof(BetterGiveAllPatchers.TransferAllPostfix));

        var originalInfoIsIncludedInTakeAll = typeof(Equipment).GetMethod(nameof(Equipment.IsIncludedInTakeAll), new Type[] { typeof(TileObject) });
        var prefixInfoIsIncludedInTakeAll = typeof(BetterGiveAllPatchers).GetMethod(nameof(BetterGiveAllPatchers.IsIncludedInTakeAllPrefix));

        var originalInfoGetMaxTransferrableTo = typeof(Equipment).GetMethod(nameof(Equipment.GetMaxTransferrableTo), new Type[] { typeof(TileObject) });
        var postfixInfoGetMaxTransferrableTo = typeof(BetterGiveAllPatchers).GetMethod(nameof(BetterGiveAllPatchers.GetMaxTransferrableToPostfix));

        HarmonyInstance.Patch(originalInfoTransferAll, prefix: new HarmonyMethod(prefixInfoTransferAll), postfix: new HarmonyMethod(postfixInfoTransferAll));
        HarmonyInstance.Patch(originalInfoIsIncludedInTakeAll, prefix: new HarmonyMethod(prefixInfoIsIncludedInTakeAll));
        HarmonyInstance.Patch(originalInfoGetMaxTransferrableTo, postfix: new HarmonyMethod(postfixInfoGetMaxTransferrableTo));


    }
    public static void Unload()
    {
        HarmonyInstance?.UnpatchAll();
    }

}








