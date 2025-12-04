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
    public bool reverse;
}

[StaticConstructorOnStartup]
public static class AdvancedPatcher
{
    static bool currentIsInt;
    static float currentValue;
    static float currentSecondValue;
    static bool currentReverse;
    static int currentScaledInt;
    static int currentSecondScaledInt;
    static float currentScaledFloat;
    static float currentSecondScaledFloat;
    static int numbersPatched;
    static bool logShown = false;
    static AdvancedPatcher()
    {
        foreach (AdvancedPatchDef def in DefDatabase<AdvancedPatchDef>.AllDefsListForReading)
        {
            AdvancedDefPatcher(def.typeOf, def.name, def.isInt, def.value, def.secondValue, def.reverse);
        }
        // makes so the log only shows the amount of numbers patched exactly one time
        if (!logShown)
        {
            logShown = true;
            Log.Message($"[DayStretch] Patched {numbersPatched} variables");
        }
    }

    static void AdvancedDefPatcher(string typeOf, string name, bool isInt, float value, float secondValue, bool reverse)
    {
        currentIsInt = isInt;
        currentValue = value;
        currentSecondValue = secondValue;
        currentReverse = reverse;
        if (isInt == true)
        {
            if (reverse == true)
            {
                int scaledTime = Mathf.RoundToInt(value * (1f / Settings.Instance.TimeMultiplier));
                currentScaledInt = scaledTime;
                if (Mathf.Approximately(secondValue, -1) is false) 
                {
                    int secondScaledTime = Mathf.RoundToInt(secondValue * (1f / Settings.Instance.TimeMultiplier));
                    currentSecondScaledInt = scaledTime;
                }
            }
            else
            {
                int scaledTime = Mathf.RoundToInt(value * Settings.Instance.TimeMultiplier);
                currentScaledInt = scaledTime;
                if (Mathf.Approximately(secondValue, -1) is false)
                {
                    int secondScaledTime = Mathf.RoundToInt(secondValue * Settings.Instance.TimeMultiplier);
                    currentSecondScaledInt = secondScaledTime;
                }
            }
        }
        else
        {
            if (reverse == true)
            {
                float scaledTime = (value * (1f / Settings.Instance.TimeMultiplier));
                currentScaledFloat = scaledTime;
                if (Mathf.Approximately(secondValue, -1) is false)
                {
                    float secondScaledTime = (secondValue * (1f / Settings.Instance.TimeMultiplier));
                    currentSecondScaledFloat = scaledTime;
                }
            }
            else
            {
                float scaledTime = (value * Settings.Instance.TimeMultiplier);
                currentScaledFloat = scaledTime;
                if (Mathf.Approximately(secondValue, -1) is false)
                {
                    float secondScaledTime = (secondValue * Settings.Instance.TimeMultiplier);
                    currentSecondScaledFloat = scaledTime;
                }
            }
        }// is there any point in making this a method?


        // making string a type so .GetMethods doesnt scream
        Type type = AppDomain.CurrentDomain.GetAssemblies().SelectMany(a =>
        {
            try
            {
                return a.GetTypes();
            }
            catch
            {
                return Type.EmptyTypes;
            }
        }).FirstOrDefault(t => t.FullName == typeOf || t.Name == typeOf);
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
            if (method.IsAbstract || method.IsGenericMethodDefinition) continue;
            if (!string.IsNullOrEmpty(name) && method.Name != name) continue;
            try
            {
                var transpiler = new HarmonyMethod(typeof(AdvancedPatcher).GetMethod(nameof(TranspileVariables), BindingFlags.Static | BindingFlags.NonPublic));
                harmony.Patch(method, transpiler: transpiler);
            }
            catch (Exception)
            {
                if (Mathf.Approximately(secondValue, -1))
                {
                    Log.Error($"[DayStretch] Variable {value} not found in {typeOf} and {name}.");
                }
                else
                {
                    Log.Error($"[DayStretch] Variable {value} nor variable {secondValue} not found in {typeOf} and {name}.");
                }
            }
            numbersPatched++;
            Log.Message($"Patched {typeOf}");
        }

    }
    static IEnumerable<CodeInstruction> TranspileVariables(IEnumerable<CodeInstruction> instructions)
    {
        if (currentIsInt == true)
        {
            if (Mathf.Approximately(currentSecondValue, -1))
            {
                foreach (var instr in instructions)
                {
                    if (instr.opcode == OpCodes.Ldc_I4 && instr.operand is int val && val == (int)currentValue)
                    {
                        instr.operand = currentScaledInt;
                    }
                    yield return instr;
                }
            }
            else
            {
                foreach (var instr in instructions)
                {
                    if (instr.opcode == OpCodes.Ldc_I4 && instr.operand is int val && val == currentValue)
                    {
                        instr.operand = currentScaledInt;
                    }
                    else if (instr.opcode == OpCodes.Ldc_I4 && instr.operand is int f && Mathf.Approximately(f, currentSecondValue))
                    {
                        instr.operand = currentSecondScaledInt;
                    }
                    yield return instr;
                }
            }
        }
        else
        {
            if (Mathf.Approximately(currentSecondValue, -1))
            {
                foreach (var instr in instructions)
                {
                    if (instr.opcode == OpCodes.Ldc_R4 && instr.operand is float val && val == currentValue)
                    {
                        instr.operand = currentScaledFloat;
                    }
                    yield return instr;
                }
            }
            else
            {
                foreach (var instr in instructions)
                {
                    if (instr.opcode == OpCodes.Ldc_R4 && instr.operand is float val && val == currentValue)
                    {
                        instr.operand = currentScaledFloat;
                    }
                    else if (instr.opcode == OpCodes.Ldc_R4 && instr.operand is float f && Mathf.Approximately(f, currentSecondValue))
                    {
                        instr.operand = currentSecondScaledFloat;
                    }
                    yield return instr;
                }
            }
        }
    }
}   