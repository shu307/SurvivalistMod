using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using UnityEngine;


static class BetterGiveAllPatchers
{
#if DEBUG
    static int debugCount;
#endif
    static bool isGivingAll = false;
    static bool isMatchingPolicy = false;

    static int cachedLevel = -1;
    static int cachedCap = -1;

    static int foundStrengthBonus = 0;
    static Dictionary<LiquidPrototype, float> foundLiquidAmountPairs = new Dictionary<LiquidPrototype, float>();
    static Dictionary<EquipmentPrototype, int> foundEquipmentAmountPairs = new Dictionary<EquipmentPrototype, int>();

    // should be initialized for every item
    static int cachedWantGiveAmount = 0;

    public static void TransferAllPrefix(InventoryBehaviour fromInventory)
    {
        isGivingAll = false;

        if (fromInventory.Carrier is Character character && Session.Instance.GetPlayerControllingCharacter(character) != null)
        {
#if DEBUG
            debugCount++;
            FileLog.Log(debugCount.ToString());
#endif
            // initialize
            isGivingAll = true;
            isMatchingPolicy = !InputFunctionManager.Instance.IsPressed(InputFunction.CommandModeFastScroll, false, 0);

            cachedLevel = -1;
            cachedCap = -1;

            foundStrengthBonus = 0;
            foundLiquidAmountPairs.Clear();
            foundEquipmentAmountPairs.Clear();
        }
    }

    static bool NewIsIncludedInTakeAll(Equipment equipment, TileObject carrier)
    {
        if (!isGivingAll)
        {
            return equipment.IsIncludedInTakeAll(carrier);
        }

        cachedWantGiveAmount = 0;

        var character = (Character)carrier;
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

        var shouldCallOriginal = true;

        if (amountToKeep > 0f)
        {
            if (liquidType != null)
            {
                var foundAmountExceptThis = foundLiquidAmountPairs.ContainsKey(liquidType) ? foundLiquidAmountPairs[liquidType] : 0f;
                // keep this, allow overflow
                if (foundAmountExceptThis < amountToKeep)
                {
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
                        cachedWantGiveAmount = equipment.GetAmount() - (int)amountToKeep;
                    }
                    else
                    {
                        // less than target, give none
                        shouldCallOriginal = false;
                    }
                }
                // can not be combined
                else
                {
                    var foundAmountExceptThis = foundEquipmentAmountPairs.ContainsKey(equipmentType) ? foundEquipmentAmountPairs[equipmentType] : 0;
                    if (foundAmountExceptThis < (int)amountToKeep)
                    {
                        shouldCallOriginal = false;
                    }
                    // increase found amount
                    foundEquipmentAmountPairs[equipmentType] = foundAmountExceptThis + equipment.GetAmount();
                }
            }

        }

        return shouldCallOriginal ? equipment.IsIncludedInTakeAll(carrier) : false;
    }

    static int NewGetAmount(Equipment equipment)
    {
        return (isGivingAll && cachedWantGiveAmount > 0) ? Math.Min(cachedWantGiveAmount, equipment.GetAmount()) : equipment.GetAmount();
    }

    public static IEnumerable<CodeInstruction> TransferAllTranspiler(IEnumerable<CodeInstruction> instructions)
    {
        var foundGetAmountIndex = -1;
        var foundIsIncludedInTakeAllIndex = -1;

        var codes = new List<CodeInstruction>(instructions);
        for (var i = 0; i < codes.Count; i++)
        {
            if (codes[i].Calls(AccessTools.Method(typeof(Equipment), nameof(Equipment.GetAmount))))
            {
                foundGetAmountIndex = i;
                codes[i] = new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(BetterGiveAllPatchers), nameof(NewGetAmount)));

            }
            else if (codes[i].Calls(AccessTools.Method(typeof(Equipment), nameof(Equipment.IsIncludedInTakeAll), new Type[] { typeof(TileObject) })))
            {
                foundIsIncludedInTakeAllIndex = i;
                codes[i] = new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(BetterGiveAllPatchers), nameof(NewIsIncludedInTakeAll)));
            }
        }

        Debug.Assert(foundGetAmountIndex > -1 && foundIsIncludedInTakeAllIndex > -1);
#if DEBUG
        FileLog.Log($"foundGetAmountIndex: {foundGetAmountIndex}, foundIsIncludedInTakeAllIndex: {foundIsIncludedInTakeAllIndex}");
#endif

        return codes.AsEnumerable();
    }


}
