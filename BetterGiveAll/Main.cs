using HarmonyLib;
using System;
using UnityEngine;
using System.Reflection;



public class Main
{
    public static Harmony HarmonyInstance;

    public static void Load()
    {
#if DEBUG
        Harmony.DEBUG = true;
        FileLog.Reset();
#endif
        HarmonyInstance = new Harmony("shu307.bettergiveall");

        var originalInfoTransferAll = AccessTools.Method(typeof(InfoPage), "TransferAll", new Type[] { typeof(InventoryBehaviour), typeof(InventoryBehaviour), typeof(bool), typeof(InputFrame) });
        var prefixInfoTransferAll = typeof(BetterGiveAllPatchers).GetMethod(nameof(BetterGiveAllPatchers.TransferAllPrefix));
        var transpilerInfoTransferAll = typeof(BetterGiveAllPatchers).GetMethod(nameof(BetterGiveAllPatchers.TransferAllTranspiler));

        HarmonyInstance.Patch(originalInfoTransferAll, prefix: new HarmonyMethod(prefixInfoTransferAll), transpiler: new HarmonyMethod(transpilerInfoTransferAll));
    }
    public static void Unload()
    {
        HarmonyInstance?.UnpatchAll();
    }

}








