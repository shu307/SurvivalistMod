using HarmonyLib;
using System;
using System.Collections.Generic;


static class BetterGiveAllPatchers
{
    static int foundStrengthBonus = 0;
    static Dictionary<LiquidPrototype, float> foundLiquidAmountPairs = new Dictionary<LiquidPrototype, float>();
    static Dictionary<EquipmentPrototype, int> foundEquipmentAmountPairs = new Dictionary<EquipmentPrototype, int>();

    static Dictionary<EquipmentPrototype, int> wantGiveCombinableAmountPairs = new Dictionary<EquipmentPrototype, int>();

    public static void TransferAllPrefix(out bool __state, InventoryBehaviour fromInventory)
    {
        __state = false;

        if (fromInventory.Carrier is Character character && Session.Instance.GetPlayerControllingCharacter(character) != null)
        {
            Main.HarmonyInstance.Patch(Main.IsIncludedInTakeAllOriginal, prefix: new HarmonyMethod(Main.IsIncludedInTakeAllPrefix));
            Main.HarmonyInstance.Patch(Main.GetMaxTransferrableToOriginal, postfix: new HarmonyMethod(Main.GetMaxTransferrableToPostfix));

            __state = true;
        }
    }
    public static void TransferAllPostfix(bool __state)
    {
        if (__state)
        {
            Main.HarmonyInstance.Unpatch(Main.IsIncludedInTakeAllOriginal, Main.IsIncludedInTakeAllPrefix);
            Main.HarmonyInstance.Unpatch(Main.GetMaxTransferrableToOriginal, Main.GetMaxTransferrableToPostfix);
            foundStrengthBonus = 0;
            foundLiquidAmountPairs.Clear();
            foundEquipmentAmountPairs.Clear();
            wantGiveCombinableAmountPairs.Clear();
        }
    }

    public static bool IsIncludedInTakeAllPrefix(ref bool __result, Equipment __instance, TileObject carrier)
    {
        var shouldCallOriginal = true;

        var equipment = __instance;
        var character = (Character)carrier;

        // always keep strength bonus items
        if ((equipment.GetPrototype().SkillBonusType == SkillType.Strength) && (equipment.GetPrototype().SkillBonus > 0))
        {
            List<SkillEffect> skillEffects = (Util.AmIOnMainThread() ? Character.SkillEffects : Character.SkillEffectsOnThread);
            character.GetSkillEffects(skillEffects);

            int unlimitedLevel = character.Skillset.GetLevel(SkillType.Strength);
            int capLevel = character.Skillset.GetCap(SkillType.Strength);
            for (int i = 0; i < skillEffects.Count; i++)
            {
                bool flag = skillEffects[i].SkillType == SkillType.Strength;
                if (flag)
                {
                    unlimitedLevel += skillEffects[i].Effect;
                }
            }

            var wantGiveAmount = 0;
            var equipmentType = equipment.GetPrototype();

            while (wantGiveAmount < equipment.GetAmount())
            {
                foundStrengthBonus += equipmentType.SkillBonus;
                if ((unlimitedLevel - foundStrengthBonus) < capLevel)
                {
                    break;
                }
                wantGiveAmount += 1;
            }

            if (wantGiveAmount == 0)
            {
                __result = false;
                shouldCallOriginal = false;
            }
            else if (equipment.CanBeCombined())
            {
                wantGiveCombinableAmountPairs[equipmentType] = wantGiveAmount;
            }
        }
        // always keep armor and pack
        else if ((equipment.GetClothingType() != ClothingType.Invalid) && (character.Clothes[(int)equipment.GetClothingType()] == equipment))
        {
            __result = false;
            shouldCallOriginal = false;
        }
        // matching policy mode
        else if (!InputFunctionManager.Instance.IsPressed(InputFunction.CommandModeFastScroll, true, 0))
        {
            var equipmentType = equipment.GetPrototype();
            var liquidType = equipment.GetLiquidContentsType();
            var targetAmount = character.GetTargetAmountToCarryIncludingAmmo(equipmentType, liquidType, equipment.InfectedWith);

            // keep at least 1 if it's equipped
            if (character.EquippedItem == equipment)
            {
                targetAmount = (liquidType != null) ? equipment.GetLiquidContentsAmount() : Math.Max(1f, targetAmount);
            }

            // has policy
            if (targetAmount > 0f)
            {
                // has liquid
                if (liquidType != null)
                {
                    // cache found amount
                    var foundAmountExceptThis = foundLiquidAmountPairs.ContainsKey(liquidType) ? foundLiquidAmountPairs[liquidType] : 0f;
                    // increase with this amount
                    foundLiquidAmountPairs[liquidType] = foundAmountExceptThis + equipment.GetLiquidContentsAmount();
                    // keep this, allow overflow
                    if (foundAmountExceptThis < targetAmount)
                    {
                        __result = false;
                        shouldCallOriginal = false;
                    }
                }
                // no liquid
                else
                {
                    // can be combined
                    if (equipment.CanBeCombined())
                    {
                        if (equipment.GetAmount() <= (int)targetAmount)
                        {
                            __result = false;
                            shouldCallOriginal = false;
                        }
                        else
                        {
                            wantGiveCombinableAmountPairs[equipmentType] = equipment.GetAmount() - (int)targetAmount;
                        }
                    }
                    // can not be combined
                    else
                    {
                        var foundAmountExceptThis = foundEquipmentAmountPairs.ContainsKey(equipmentType) ? foundEquipmentAmountPairs[equipmentType] : 0;
                        foundEquipmentAmountPairs[equipmentType] = foundAmountExceptThis + equipment.GetAmount();
                        if (foundAmountExceptThis < (int)targetAmount)
                        {
                            __result = false;
                            shouldCallOriginal = false;
                        }
                    }
                }
            }
        }

        return shouldCallOriginal;
    }

    public static void GetMaxTransferrableToPostfix(ref int __result, Equipment __instance)
    {
        var equipmentType = __instance.GetPrototype();

        if (wantGiveCombinableAmountPairs.ContainsKey(equipmentType))
        {
            __result = Math.Min(__result, wantGiveCombinableAmountPairs[equipmentType]);
        }
    }


}
