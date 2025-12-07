using DayStretched;
using HarmonyLib;
using Microsoft.Win32;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;
using Verse;

public class AdvancedPatchDef : Def
{
    public string typeOf;
    public string name;
    public bool isInt;
    public float value;
    public float secondValue;
    public float thirdValue;
    public bool reverse;
    public bool isGetter;
}

[StaticConstructorOnStartup]
public static class AdvancedPatcher
{
    static bool currentIsInt;
    static bool currentReverse;
    static bool logShown = false;

    static float currentValue;
    static float currentSecondValue;
    static float currentThirdValue;

    static int currentScaledInt;
    static int currentSecondScaledInt;
    static int currentThirdScaledInt;

    static float currentScaledFloat;
    static float currentSecondScaledFloat;
    static float currentThirdScaledFloat;

    static int numbersPatched;

    static AdvancedPatcher()
    {
        foreach (AdvancedPatchDef def in DefDatabase<AdvancedPatchDef>.AllDefsListForReading)
        {
            AdvancedDefPatcher(def.typeOf, def.name, def.isInt, def.value, def.secondValue, def.thirdValue, def.reverse, def.isGetter);
        }
        // makes so the log only shows the amount of numbers patched exactly one time
        if (!logShown)
        {
            logShown = true;
            Log.Message($"[DayStretch]-(AdvancedPatch) Patched {numbersPatched} variables");
        }
    }

    static void AdvancedDefPatcher(string typeOf, string name, bool isInt, float value, float secondValue, float thirdValue, bool reverse, bool isGetter)
    {
        currentIsInt = isInt;
        currentValue = value;
        currentSecondValue = secondValue;
        currentThirdValue = thirdValue;
        currentReverse = reverse;
        if (isInt == true)
        {
            if (reverse == true)
            {
                currentScaledInt = Mathf.RoundToInt(value * (1f / Settings.Instance.TimeMultiplier));
                if (secondValue != 0f) 
                {
                    currentSecondScaledInt = Mathf.RoundToInt(secondValue * (1f / Settings.Instance.TimeMultiplier));
                }
                if (thirdValue != 0f)
                {
                    currentThirdScaledInt = Mathf.RoundToInt(thirdValue * (1f / Settings.Instance.TimeMultiplier));
                }
            }
            else
            {
                currentScaledInt = Mathf.RoundToInt(value * Settings.Instance.TimeMultiplier);
                if (secondValue != 0f)
                {
                    currentSecondScaledInt = Mathf.RoundToInt(secondValue * Settings.Instance.TimeMultiplier);
                }
                if (thirdValue != 0f)
                {
                    currentThirdScaledInt = Mathf.RoundToInt(thirdValue * Settings.Instance.TimeMultiplier);
                }

            }
        }
        else
        {
            if (reverse == true)
            {
                currentScaledFloat = (value * (1f / Settings.Instance.TimeMultiplier));
                if (secondValue != 0f)
                {
                    currentSecondScaledFloat = (secondValue * (1f / Settings.Instance.TimeMultiplier));
                }
                if (thirdValue != 0f)
                {
                    currentThirdScaledFloat = (thirdValue * (1f / Settings.Instance.TimeMultiplier));
                }
            }
            else
            {
                currentScaledFloat = (value * Settings.Instance.TimeMultiplier);
                if (secondValue != 0f)
                {
                    currentSecondScaledFloat = (secondValue * Settings.Instance.TimeMultiplier);
                }
                if (secondValue != 0f)
                {
                    currentThirdScaledFloat = (thirdValue * Settings.Instance.TimeMultiplier);
                }
            }
        }
        // making string a type so .GetMethods doesnt scream
        Type type = GenTypes.GetTypeInAnyAssembly(typeOf);
        // i wanted to include this in the top code but i dont think i can?
        // correct me if i'm wrong though
        if (type == null)
        {
            Log.Error($"[DayStretch]-(AdvancedPatch) Type '{typeOf}' not found in loaded assemblies; skipping.");
            return;
        }

        var harmony = new Harmony("com.julekjulas.advancedpatch");
        foreach (var method in type.GetMethods(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic))
        {
            if (isGetter)
            {
                foreach (var prop in type.GetProperties(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic))
                {
                    if (!string.IsNullOrEmpty(name) && prop.Name != name) continue;
                    var getter = prop.GetGetMethod(true);
                    if (getter == null) continue;
                    if (getter.IsAbstract || getter.IsGenericMethodDefinition) continue;
                    try
                    {
                        var transpiler = new HarmonyMethod(typeof(AdvancedPatcher).GetMethod(isInt ? nameof(TranspileIntVariables) : nameof(TranspileFloatVariables), BindingFlags.Static | BindingFlags.NonPublic));
                        harmony.Patch(getter, transpiler: transpiler);
                        numbersPatched++;
                        Log.Message($"[DayStretch]-(AdvancedPatch) Patched getter {typeOf}.{prop.Name}");
                    }
                    catch (Exception e)
                    {
                        Log.Error($"[DayStretch]-(AdvancedPatch) Failed patching getter {typeOf}.{prop.Name}: {e}");
                    }
                }
            }
            if (method.IsAbstract || method.IsGenericMethodDefinition) continue;
            if (!string.IsNullOrEmpty(name) && method.Name != name) continue;
            try
            {
                var transpiler = new HarmonyMethod(typeof(AdvancedPatcher).GetMethod(isInt ? nameof(TranspileIntVariables) : nameof(TranspileFloatVariables), BindingFlags.Static | BindingFlags.NonPublic));
                harmony.Patch(method, transpiler: transpiler);
                numbersPatched++;
            }
            catch (Exception)
            {
                if (Mathf.Approximately(secondValue, -1))
                {
                    Log.Error($"[DayStretch]-(AdvancedPatch) Variable {value} not found in {typeOf} and {name}.");
                }
                else
                {
                    Log.Error($"[DayStretch]-(AdvancedPatch) Variable {value} nor variable {secondValue} not found in {typeOf} and {name}.");
                }
            }
            numbersPatched++;
            Log.Message($"[DayStretch]-(AdvancedPatch) Patched {typeOf}");
        }

    }

    static IEnumerable<CodeInstruction> TranspileIntVariables(IEnumerable<CodeInstruction> instructions)
    {
        foreach (var instr in instructions)
        {
            if ((instr.opcode == OpCodes.Ldc_I4 || instr.opcode == OpCodes.Ldc_I4_S) && instr.operand is int val)
            {
                if (val == (int)currentValue) instr.operand = currentScaledInt;
                else if ((val == (int)currentSecondValue) && (currentSecondValue != 0)) instr.operand = currentSecondScaledInt;
                else if ((val == (int)currentThirdValue) && (currentThirdValue != 0)) instr.operand = currentThirdScaledInt;
            }
            yield return instr;
        }
    }
    static IEnumerable<CodeInstruction> TranspileFloatVariables(IEnumerable<CodeInstruction> instructions)
    {
        foreach (var instr in instructions)
        {
            if ((instr.opcode == OpCodes.Ldc_R4) && instr.operand is float val)
            {
                if (val == (float)currentValue) instr.operand = currentScaledFloat;
                else if ((val == (float)currentSecondValue) && (currentSecondValue != 0)) instr.operand = currentSecondScaledFloat;
                else if ((val == (float)currentThirdValue) && (currentThirdValue != 0)) instr.operand = currentThirdScaledFloat;
            }
            yield return instr;
        }
    }
}   