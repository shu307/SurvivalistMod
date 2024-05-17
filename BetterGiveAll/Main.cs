using HarmonyLib;
using System;
using UnityEngine;
using System.Reflection;



public class Main
{
    public static Harmony HarmonyInstance;
    public static MethodInfo IsIncludedInTakeAllOriginal;
    public static MethodInfo IsIncludedInTakeAllPrefix;
    public static MethodInfo GetMaxTransferrableToOriginal;
    public static MethodInfo GetMaxTransferrableToPostfix;


    public static void Load()
    {
        HarmonyInstance = new Harmony("shu307.bettergiveall");

        var transferAllOriginal = AccessTools.Method(typeof(InfoPage), "TransferAll", new Type[] { typeof(InventoryBehaviour), typeof(InventoryBehaviour), typeof(bool), typeof(InputFrame) });
        var transferAllPrefix = typeof(BetterGiveAllPatchers).GetMethod(nameof(BetterGiveAllPatchers.TransferAllPrefix));
        var transferAllPostfix = typeof(BetterGiveAllPatchers).GetMethod(nameof(BetterGiveAllPatchers.TransferAllPostfix));

        IsIncludedInTakeAllOriginal = typeof(Equipment).GetMethod(nameof(Equipment.IsIncludedInTakeAll), new Type[] { typeof(TileObject) });
        IsIncludedInTakeAllPrefix = typeof(BetterGiveAllPatchers).GetMethod(nameof(BetterGiveAllPatchers.IsIncludedInTakeAllPrefix));

        GetMaxTransferrableToOriginal = typeof(Equipment).GetMethod(nameof(Equipment.GetMaxTransferrableTo), new Type[] { typeof(TileObject) });
        GetMaxTransferrableToPostfix = typeof(BetterGiveAllPatchers).GetMethod(nameof(BetterGiveAllPatchers.GetMaxTransferrableToPostfix));

        HarmonyInstance.Patch(transferAllOriginal, prefix: new HarmonyMethod(transferAllPrefix), postfix: new HarmonyMethod(transferAllPostfix));

    }
    public static void Unload()
    {
        HarmonyInstance?.UnpatchAll();
    }




}








