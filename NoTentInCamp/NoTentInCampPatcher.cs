using System.Collections.Generic;
using HarmonyLib;



public class Main
{
    // make sure DoPatching() is called at start either by
    // the mod loader or by your injector
    public static Harmony HarmonyInstance;
    public static void Load()
    {
        HarmonyInstance = new Harmony("shu307.notentincamp");
        HarmonyInstance.PatchAll();
    }
    public static void Unload()
    {
        HarmonyInstance?.UnpatchAll();
    }
}

[HarmonyPatch(typeof(PropPrototype))]
[HarmonyPatch(nameof(PropPrototype.GetAccomodationTypes))] // if possible use nameof() here
class GetAccomodationTypesPatch
{

    static void Postfix(ref List<PropPrototype> __result)
    {

        var stack = new Stack<PropPrototype>(__result.Count);
        foreach (var type in __result)
        {
            if (type.NativeName.Contains("Tent"))
            {
                stack.Push(type);
            }
        }

        while (stack.Count > 0)
        {
            __result.Remove(stack.Pop());

        }

    }
}

