using System;
using System.Collections.Generic;


static class BetterGiveAllPatchers
{
    //static int debugCount;
    static bool isGivingAll = false;

    static bool isMatchingPolicy = false;

    static int cachedLevel = -1;
    static int cachedCap = -1;

    static int foundStrengthBonus = 0;
    static Dictionary<LiquidPrototype, float> foundLiquidAmountPairs = new Dictionary<LiquidPrototype, float>();
    static Dictionary<EquipmentPrototype, int> foundEquipmentAmountPairs = new Dictionary<EquipmentPrototype, int>();

    static Dictionary<EquipmentPrototype, int> wantGiveCombinableAmountPairs = new Dictionary<EquipmentPrototype, int>();

    public static void TransferAllPrefix(InventoryBehaviour fromInventory)
    {
        if (fromInventory.Carrier is Character character && Session.Instance.GetPlayerControllingCharacter(character) != null)
        {
            isGivingAll = true;

            //debugCount++;
            //FileLog.Log(debugCount.ToString());

            // initialize
            isMatchingPolicy = !InputFunctionManager.Instance.IsPressed(InputFunction.CommandModeFastScroll, false, 0);

            cachedLevel = -1;
            cachedCap = -1;

            foundStrengthBonus = 0;
            foundLiquidAmountPairs.Clear();
            foundEquipmentAmountPairs.Clear();

            wantGiveCombinableAmountPairs.Clear();

        }
    }
    public static void TransferAllPostfix()
    {
        if (isGivingAll)
        {
            isGivingAll = false;
        }
    }


    public static bool IsIncludedInTakeAllPrefix(ref bool __result, Equipment __instance, TileObject carrier)
    {

        var shouldCallOriginal = true;

        if (!isGivingAll)
        {
            return shouldCallOriginal;
        }

        var character = (Character)carrier;
        var equipment = __instance;
        var equipmentType = equipment.GetPrototype();
        var liquidType = equipment.GetLiquidContentsType();

        var amountToKeep = 0f;

        // always keep necessary strength bonus items
        if ((equipmentType.SkillBonusType == SkillType.Strength) && (equipmentType.SkillBonus > 0))
        {
            if (cachedLevel == -1)
            {
                List<SkillEffect> skillEffects = (Util.AmIOnMainThread() ? Character.SkillEffects : Character.SkillEffectsOnThread);
                character.GetSkillEffects(skillEffects);

                cachedLevel = character.Skillset.GetLevel(SkillType.Strength);
                cachedCap = character.Skillset.GetCap(SkillType.Strength);

                for (int i = 0; i < skillEffects.Count; i++)
                {
                    bool flag = skillEffects[i].SkillType == SkillType.Strength;
                    if (flag)
                    {
                        cachedLevel += skillEffects[i].Effect;
                    }
                }
            }

            amountToKeep = equipment.GetAmount();
            while (amountToKeep > 0f)
            {
                foundStrengthBonus += equipmentType.SkillBonus;
                if ((cachedLevel - foundStrengthBonus) < cachedCap)
                {
                    break;
                }
                amountToKeep -= 1f;
            }
        }
        // always keep wearing items: armor and pack ..
        else if ((equipment.GetClothingType() != ClothingType.Invalid) && (character.Clothes[(int)equipment.GetClothingType()] == equipment))
        {
            amountToKeep = 1f;
        }
        // always keep 1 at least if it's equipped
        else if (character.EquippedItem == equipment)
        {
            amountToKeep = (liquidType != null) ? equipment.GetLiquidContentsAmount() : 1f;
        }



        // get max one as targetAmount if is matching policy
        if (isMatchingPolicy)
        {
            amountToKeep = Math.Max(amountToKeep, character.GetTargetAmountToCarryIncludingAmmo(equipmentType, liquidType, equipment.InfectedWith));
        }

        if (amountToKeep > 0f)
        {
            if (liquidType != null)
            {
                var foundAmountExceptThis = foundLiquidAmountPairs.ContainsKey(liquidType) ? foundLiquidAmountPairs[liquidType] : 0f;
                // keep this, allow overflow
                if (foundAmountExceptThis < amountToKeep)
                {
                    __result = false;
                    shouldCallOriginal = false;
                }
                // increase found amount with this amount
                foundLiquidAmountPairs[liquidType] = foundAmountExceptThis + equipment.GetLiquidContentsAmount();
            }
            else
            {
                if (equipment.CanBeCombined())
                {
                    if (equipment.GetAmount() > (int)amountToKeep)
                    {
                        // cache want give amount
                        wantGiveCombinableAmountPairs[equipmentType] = equipment.GetAmount() - (int)amountToKeep;
                    }
                    else
                    {
                        // less than target, give none
                        __result = false;
                        shouldCallOriginal = false;
                    }
                }
                // can not be combined
                else
                {
                    var foundAmountExceptThis = foundEquipmentAmountPairs.ContainsKey(equipmentType) ? foundEquipmentAmountPairs[equipmentType] : 0;
                    if (foundAmountExceptThis < (int)amountToKeep)
                    {
                        __result = false;
                        shouldCallOriginal = false;
                    }
                    // increase found amount
                    foundEquipmentAmountPairs[equipmentType] = foundAmountExceptThis + equipment.GetAmount();
                }
            }

        }

        return shouldCallOriginal;
    }

    public static void GetMaxTransferrableToPostfix(ref int __result, Equipment __instance)
    {
        if (!isGivingAll)
        {
            return;
        }

        var equipmentType = __instance.GetPrototype();

        if (wantGiveCombinableAmountPairs.ContainsKey(equipmentType))
        {
            __result = Math.Min(__result, wantGiveCombinableAmountPairs[equipmentType]);
        }
    }


}
