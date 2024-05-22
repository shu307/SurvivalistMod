using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using HarmonyLib;
using UnityEngine;



public class Main
{
    // make sure DoPatching() is called at start either by
    // the mod loader or by your injector
    public static Harmony HarmonyInstance;
    public static void Load()
    {
#if DEBUG
        Harmony.DEBUG = true;
        FileLog.Reset();
#endif

        HarmonyInstance = new Harmony("shu307.iwanttrader");
        HarmonyInstance.PatchAll();
    }
    public static void Unload()
    {
        HarmonyInstance?.UnpatchAll();
    }
}

[HarmonyPatch(typeof(GameTerrain))]
[HarmonyPatch(nameof(GameTerrain.GenerateCommunity))] // if possible use nameof() here
class GenerateCommunityPatch
{
    static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        var found = 0;
        foreach (var instruction in instructions)
        {
            if (instruction.opcode == OpCodes.Stloc_S)
            {
                LocalBuilder localBuilder = (LocalBuilder)instruction.operand;

                if (localBuilder.LocalIndex == 28 || localBuilder.LocalIndex == 29)
                {
                    yield return new CodeInstruction(OpCodes.Ldc_I4_5);
                    yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(Math), nameof(Math.Max), new Type[] { typeof(int), typeof(int) }));
                    found++;
                }
#if DEBUG
                FileLog.Log($"found {found} --- this operand: {instruction.operand}");
#endif
            }
            yield return instruction;
        }

        Debug.Assert(found == 2);
    }
}

