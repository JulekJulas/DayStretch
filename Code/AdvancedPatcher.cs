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
    public string type;
    public double value;
    public double secondValue;
    public double thirdValue;
    public bool reverse;
    public bool isGetter;
}

[StaticConstructorOnStartup]
public static class AdvancedPatcher
{
    static string currentNumType;
    static bool currentReverse;
    static bool logShown = false;

    static double currentValue;
    static double currentSecondValue;
    static double currentThirdValue;

    static double currentScaledValue;
    static double currentSecondScaledValue;
    static double currentThirdScaledValue;

    static int currentScaledInt;
    static int currentSecondScaledInt;
    static int currentThirdScaledInt;

    static float currentScaledFloat;
    static float currentSecondScaledFloat;
    static float currentThirdScaledFloat;

    static long currentScaledLong;
    static long currentSecondScaledLong;
    static long currentThirdScaledLong;

    static int numbersPatched;

    static AdvancedPatcher()
    {
        foreach (AdvancedPatchDef def in DefDatabase<AdvancedPatchDef>.AllDefsListForReading)
        {
            AdvancedDefPatcher(def.typeOf, def.name, def.type, def.value, def.secondValue, def.thirdValue, def.reverse, def.isGetter);
        }
        // makes so the log only shows the amount of numbers patched exactly one time
        if (!logShown) logShown = true;
        {
            Log.Message($"[DayStretch]-(AdvancedPatch) Patched {numbersPatched} variables");
        }
    }

    static void AdvancedDefPatcher(string typeOf, string name, string numType, double value, double secondValue, double thirdValue, bool reverse, bool isGetter)
    {
        currentNumType = numType;
        currentValue = value;
        currentSecondValue = secondValue;
        currentThirdValue = thirdValue;
        currentReverse = reverse;

        int numberOfValues = 1;
        if (currentSecondValue != 0) numberOfValues++;
        if (currentThirdValue != 0) numberOfValues++;

        if (reverse)
        {
            currentScaledValue = (double)(value * (1f / Settings.Instance.TimeMultiplier));
            if (secondValue != 0d) currentSecondScaledValue = (double)(secondValue * (1f / Settings.Instance.TimeMultiplier));
            if (thirdValue != 0d) currentThirdScaledValue = (double)(thirdValue * (1f / Settings.Instance.TimeMultiplier));
        }
        else
        {
            currentScaledValue = (double)(value * Settings.Instance.TimeMultiplier);
            if (secondValue != 0d) currentSecondScaledValue = (double)(secondValue * Settings.Instance.TimeMultiplier);
            if (thirdValue != 0d) currentThirdScaledValue = (double)(thirdValue * Settings.Instance.TimeMultiplier);
        }
        switch (numType)
        {
            case "int":
                currentScaledInt = (int)(currentScaledValue);
                if (secondValue != 0d) currentSecondScaledInt = (int)(currentSecondScaledValue);
                if (thirdValue != 0d) currentThirdScaledInt = (int)(currentThirdScaledValue);
                break;
            case "float":
                currentScaledFloat = (float)(currentScaledValue);
                if (secondValue != 0d) currentSecondScaledFloat = (float)(currentSecondScaledValue);
                if (thirdValue != 0d) currentThirdScaledFloat = (float)(currentThirdScaledValue);
                break;
            case "long":
            currentScaledLong = (long)(currentScaledValue);
                if (secondValue != 0d) currentSecondScaledLong = (long)(currentSecondScaledValue);
                if (thirdValue != 0d) currentThirdScaledLong = (long)(currentThirdScaledValue);
                break;
            case "short": // just in case if someone inputs it
                currentScaledInt = (int)(currentScaledValue);
                if (secondValue != 0d) currentSecondScaledInt = (int)(currentSecondScaledValue);
                if (thirdValue != 0d) currentThirdScaledInt = (int)(currentThirdScaledValue);
                break; // just goes to ints as it prob should
            case "double":
                break;
            default:
                Log.Error($"[DayStretch]-(AdvancedPatch) {typeOf} has an invalid type, input: {numType}");
                return;// i understand switch statements now guys
                // this is pretty sweet
        }
        Type type = GenTypes.GetTypeInAnyAssembly(typeOf);
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
                        switch (numType)
                        {
                            case "int":
                                var transpiler = new HarmonyMethod(typeof(AdvancedPatcher).GetMethod(nameof(TranspileIntVariables), BindingFlags.Static | BindingFlags.NonPublic)); harmony.Patch(getter, transpiler: transpiler);
                                break;
                            case "float":
                                var floatTranspiler = new HarmonyMethod(typeof(AdvancedPatcher).GetMethod(nameof(TranspileFloatVariables), BindingFlags.Static | BindingFlags.NonPublic)); harmony.Patch(getter, transpiler: floatTranspiler);
                                break;
                            case "long":
                                var longTranspiler = new HarmonyMethod(typeof(AdvancedPatcher).GetMethod(nameof(TranspileLongVariables), BindingFlags.Static | BindingFlags.NonPublic)); harmony.Patch(getter, transpiler: longTranspiler);
                                break;
                            case "short":
                                var shortTranspiler = new HarmonyMethod(typeof(AdvancedPatcher).GetMethod(nameof(TranspileIntVariables), BindingFlags.Static | BindingFlags.NonPublic)); harmony.Patch(getter, transpiler: shortTranspiler);
                                break;
                            case "double":
                                var doubleTranspiler = new HarmonyMethod(typeof(AdvancedPatcher).GetMethod(nameof(TranspileDoubleVariables), BindingFlags.Static | BindingFlags.NonPublic)); harmony.Patch(getter, transpiler: doubleTranspiler);
                                break;
                            default:
                                return;// juuust in case 
                        }
                        numbersPatched++;
                        Log.Message($"[DayStretch]-(AdvancedPatch) Patched getter {typeOf}.{prop.Name} of value type {numType}");
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
                        switch (numType)
                        {
                            case "int":
                                var transpiler = new HarmonyMethod(typeof(AdvancedPatcher).GetMethod(nameof(TranspileIntVariables), BindingFlags.Static | BindingFlags.NonPublic)); harmony.Patch(method, transpiler: transpiler);
                                break;
                            case "float":
                                var floatTranspiler = new HarmonyMethod(typeof(AdvancedPatcher).GetMethod(nameof(TranspileFloatVariables), BindingFlags.Static | BindingFlags.NonPublic)); harmony.Patch(method, transpiler: floatTranspiler);
                                break;
                            case "long":
                                var longTranspiler = new HarmonyMethod(typeof(AdvancedPatcher).GetMethod(nameof(TranspileLongVariables), BindingFlags.Static | BindingFlags.NonPublic)); harmony.Patch(method, transpiler: longTranspiler);
                                break;
                            case "short":
                                var shortTranspiler = new HarmonyMethod(typeof(AdvancedPatcher).GetMethod(nameof(TranspileIntVariables), BindingFlags.Static | BindingFlags.NonPublic)); harmony.Patch(method, transpiler: shortTranspiler);
                                break; // pretty sure i can just do I4 for shorts
                            case "double":
                                var doubleTranspiler = new HarmonyMethod(typeof(AdvancedPatcher).GetMethod(nameof(TranspileDoubleVariables), BindingFlags.Static | BindingFlags.NonPublic)); harmony.Patch(method, transpiler: doubleTranspiler);
                                break;
                            default:
                                return;// juuust in case even though it should have stopped before
                        }
                numbersPatched++;
            }
            catch (Exception e)
            {
                Log.Error($"[DayStretch]-(AdvancedPatch) Failed Patching {typeOf}. {e}");
                return;
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
    static IEnumerable<CodeInstruction> TranspileLongVariables(IEnumerable<CodeInstruction> instructions)
    {
        foreach (var instr in instructions)
        {
            if ((instr.opcode == OpCodes.Ldc_I8) && instr.operand is long val)
            {
                if (val == (long)currentValue) instr.operand = currentScaledLong;
                else if ((val == (long)currentSecondValue) && (currentSecondValue != 0)) instr.operand = currentSecondScaledLong;
                else if ((val == (long)currentThirdValue) && (currentThirdValue != 0)) instr.operand = currentThirdScaledLong;
            }
            yield return instr;
        }
    }
    static IEnumerable<CodeInstruction> TranspileDoubleVariables(IEnumerable<CodeInstruction> instructions)
    {
        foreach (var instr in instructions)
        {
            if ((instr.opcode == OpCodes.Ldc_R8) && instr.operand is double val)
            {
                if (val == (double)currentValue) instr.operand = currentScaledValue; // just value since its already a double
                else if ((val == (double)currentSecondValue) && (currentSecondValue != 0)) instr.operand = currentSecondScaledValue;
                else if ((val == (double)currentThirdValue) && (currentThirdValue != 0)) instr.operand = currentThirdScaledValue;
            }
            yield return instr;
        }
    }
}   