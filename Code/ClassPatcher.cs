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

/*public class ClassPatchDef : Def
{
    public string typeOf;
    public string type;
    public double value;
    public bool reverse;
    public string exceptions;
}

[StaticConstructorOnStartup]
public static class ClassPatcher
{
    static string currentNumType;
    static bool currentReverse;
    static string currentExceptions;
    static bool logShown = false;

    static double currentValue;

    static double currentScaledValue;

    static int currentScaledInt;

    static float currentScaledFloat;

    static long currentScaledLong;

    static int classesPatched;

    static ClassPatcher()
    {
        foreach (ClassPatchDef def in DefDatabase<ClassPatchDef>.AllDefsListForReading)
        {
            ClassDefPatcher(def.typeOf, def.type, def.value, def.reverse, def.exceptions);
        }
        // makes so the log only shows the amount of numbers patched exactly one time
        if (!logShown) logShown = true;
        {
            Log.Message($"[DayStretch]-(ClassPatch) Patched {classesPatched} classes");
        }
    }

    static void ClassDefPatcher(string typeOf, string numType, double value, bool reverse, string exceptions)
    {
        currentNumType = numType;
        currentValue = value;
        currentReverse = reverse;
        currentExceptions = exceptions;
        // apply the multiplier
        if (reverse) currentScaledValue = (double)(value * (1f / Settings.Instance.TimeMultiplier));
        else currentScaledValue = (double)(value * Settings.Instance.TimeMultiplier);

        switch (numType)
        { //convert 
            case "int": currentScaledInt = (int)(currentScaledValue); break;
            case "float": currentScaledFloat = (float)(currentScaledValue); break;
            case "long": currentScaledLong = (long)(currentScaledValue); break;
            case "short": currentScaledInt = (int)(currentScaledValue); break;
            case "double": break;
            default:
                Log.Error($"[DayStretch]-(ClassPatch) {typeOf} has an invalid type, input: {numType}");
                return;
        }
        var type = AccessTools.TypeByName(typeOf);
        if (type == null)
        {
            Log.Error($"[DayStretch]-(ClassPatch)Could not find {typeOf} type to patch.");
            return;
        }
        var harmony = new Harmony("com.julekjulas.classpatch");
        foreach (var method in type.GetMethods(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic))
        {
            if (method.IsAbstract || method.IsGenericMethodDefinition || method.DeclaringType != type) continue;

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
                classesPatched++;
            }
            catch (Exception e)
            {
                Log.Error($"[DayStretch]-(ClassPatch) Failed Patching {typeOf}. {e}");
                return;
            }
            classesPatched++;
            Log.Message($"[DayStretch]-(ClassPatch) Patched {typeOf}");
        }

    }

    static IEnumerable<CodeInstruction> TranspileIntVariables(IEnumerable<CodeInstruction> instructions)
    {
        foreach (var instr in instructions)
        {
            if ((instr.opcode == OpCodes.Ldc_I4 || instr.opcode == OpCodes.Ldc_I4_S) && instr.operand is int val)
            {
                if (val == (int)currentValue) instr.operand = currentScaledInt;
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
            }
            yield return instr;
        }
    }
}*/