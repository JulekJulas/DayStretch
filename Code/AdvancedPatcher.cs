using DayStretched;
using HarmonyLib;
using Microsoft.Win32;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
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
    static bool logShown = false;
    public static Dictionary<string, int[]> scaledInts = new Dictionary<string, int[]>();
    public static Dictionary<string, float[]> scaledFloats = new Dictionary<string, float[]>();
    public static Dictionary<string, long[]> scaledLongs = new Dictionary<string, long[]>();
    public static Dictionary<string, short[]> scaledShorts = new Dictionary<string, short[]>();
    public static Dictionary<string, double[]> scaledDoubles = new Dictionary<string, double[]>();
    public static Dictionary<string, double[]> wrongValues = new Dictionary<string, double[]>();
    public static int amountofWrongValues = 0;

    static int numbersPatched = 0;

    static AdvancedPatcher()
    {
        foreach (AdvancedPatchDef def in DefDatabase<AdvancedPatchDef>.AllDefsListForReading)
        {
            AdvancedDefPatcher(def.typeOf, def.name, def.type, def.value, def.secondValue, def.thirdValue, def.reverse, def.isGetter);
        }
        // makes so the log only shows the amount of numbers patched exactly one time
        if (!logShown)
        {
            if (wrongValues.Count > 0)
            {
                foreach (string key in wrongValues.Keys)
                {
                    if (wrongValues[key][0] != 0) Log.Error($"[DayStretch]-(AdvancedPatch) Value {wrongValues[key][0]} not found in {key}");
                    if (wrongValues[key][1] != 0) Log.Error($"[DayStretch]-(AdvancedPatch) Value {wrongValues[key][1]} not found in {key}");
                    if (wrongValues[key][2] != 0) Log.Error($"[DayStretch]-(AdvancedPatch) Value {wrongValues[key][2]} not found in {key}");
                }
            }
            Log.Message($"[DayStretch]-(AdvancedPatch) Patched {numbersPatched} variables");
            logShown = true;
        }
    }

    static void AdvancedDefPatcher(string typeOf, string name, string numType, double value, double secondValue, double thirdValue, bool reverse, bool isGetter)
    {

        double scaledValue = 0; double secondScaledValue = 0; double thirdScaledValue = 0;

        int scaledInt = 0; int secondScaledInt = 0; int thirdScaledInt = 0;

        float scaledFloat = 0; float secondScaledFloat = 0; float thirdScaledFloat = 0;

        long scaledLong = 0; long secondScaledLong = 0; long thirdScaledLong = 0;

        if (reverse)
        {
            scaledValue = (double)(value * (1f / Settings.Instance.TimeMultiplier));
            if (secondValue != 0d) secondScaledValue = (double)(secondValue * (1f / Settings.Instance.TimeMultiplier));
            if (thirdValue != 0d) thirdScaledValue = (double)(thirdValue * (1f / Settings.Instance.TimeMultiplier));
        }
        else
        {
            scaledValue = (double)(value * Settings.Instance.TimeMultiplier);
            if (secondValue != 0d) secondScaledValue = (double)(secondValue * Settings.Instance.TimeMultiplier);
            if (thirdValue != 0d) thirdScaledValue = (double)(thirdValue * Settings.Instance.TimeMultiplier);
        }
        switch (numType)
        {
            case "int":
                scaledInt = (int)(scaledValue);
                if (secondValue != 0d) secondScaledInt = (int)(secondScaledValue);
                if (thirdValue != 0d) thirdScaledInt = (int)(thirdScaledValue);
                scaledInts.Add(isGetter ? (typeOf + "get_" + name) : (typeOf + name), new int[] { scaledInt, secondScaledInt, thirdScaledInt, (int)value, (int)secondValue, (int)thirdValue });
                break;
            case "float":
                scaledFloat = (float)(scaledValue);
                if (secondValue != 0d) secondScaledFloat = (float)(secondScaledValue);
                if (thirdValue != 0d) thirdScaledFloat = (float)(thirdScaledValue);
                scaledFloats.Add(isGetter ? (typeOf + "get_" + name) : (typeOf + name), new float[] { scaledFloat, secondScaledFloat, thirdScaledFloat, (float)value, (float)secondValue, (float)thirdValue });
                break;
            case "long":
                scaledLong = (long)(scaledValue);
                if (secondValue != 0d) secondScaledLong = (long)(secondScaledValue);
                if (thirdValue != 0d) thirdScaledLong = (long)(thirdScaledValue);
                scaledLongs.Add(isGetter ? (typeOf + "get_" + name) : (typeOf + name), new long[] { scaledLong, secondScaledLong, thirdScaledLong, (long)value, (long)secondValue, (long)thirdValue });
                break;
            case "short": // just in case if someone inputs it
                scaledInt = (int)(scaledValue);
                if (secondValue != 0d) secondScaledInt = (int)(secondScaledValue);
                if (thirdValue != 0d) thirdScaledInt = (int)(thirdScaledValue);
                scaledShorts.Add(isGetter ? (typeOf + "get_" + name) : (typeOf + name), new short[] { (short)scaledInt, (short)secondScaledInt, (short)thirdScaledInt, (short)value, (short)secondValue, (short)thirdValue });
                break; // just goes to ints as it prob should
            case "double":
                scaledDoubles.Add(isGetter ? (typeOf + "get_" + name) : (typeOf + name), new double[] { scaledValue, secondScaledValue, thirdScaledValue, value, secondValue, thirdValue }); // just values since its already a double
                break;
            default:
                Log.Error($"[DayStretch]-(AdvancedPatch) {typeOf} has an invalid type, input: {numType}");
                return;
        }
        Type type = GenTypes.GetTypeInAnyAssembly(typeOf);
        if (type == null)
        {
            Log.Error($"[DayStretch]-(AdvancedPatch) Type '{typeOf}' not found in loaded assemblies; skipping.");
            return;
        }

        var harmony = new Harmony("com.julekjulas.advancedpatch");
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
        else
        {
            foreach (var method in type.GetMethods(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic))
            {
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
                    Log.Message($"[DayStretch]-(AdvancedPatch) Patched {typeOf}");
                }
                catch (Exception e)
                {
                    Log.Error($"[DayStretch]-(AdvancedPatch) Failed Patching {typeOf}. {e}");
                    return;
                }
            }
        }
    }

    static IEnumerable<CodeInstruction> TranspileIntVariables(IEnumerable<CodeInstruction> instructions, MethodBase type)
    {
        string typeOf = type.DeclaringType.ToString();
        typeOf = typeOf.Split('.').Last(); // gotta remove the namespace 
        string name = type.Name.ToString();
        string dictKey = typeOf + name; // i think this is the best way to do it
        scaledInts.TryGetValue(dictKey, out int[] values);
        int scaledValue = values[0]; int secondScaledValue = values[1]; int thirdScaledValue = values[2];

        int value = values[3]; int secondValue = values[4]; int thirdValue = values[5];

        bool secondVariablePresent = secondValue != 0; // checks if variables are used
        bool thirdVariablePresent = thirdValue != 0;

        bool variablePatched = false; bool secondVariablePatched = false; bool thirdVariablePatched = false;

        foreach (var instr in instructions)
        {
            if ((instr.opcode == OpCodes.Ldc_I4 || instr.opcode == OpCodes.Ldc_I4_S) && instr.operand is int val)
            {
                if (val == value) { instr.operand = scaledValue; variablePatched = true; }
                else if ((val == secondValue) && (secondVariablePresent)) { instr.operand = secondScaledValue; secondVariablePatched = true; }
                else if ((val == thirdValue) && (thirdVariablePresent)) { instr.operand = thirdScaledValue; thirdVariablePatched = true; }
            }
            yield return instr;
        }
        bool incorrectSecondValue = (secondVariablePresent && !secondVariablePatched);
        bool incorrectThirdValue = (thirdVariablePresent && !thirdVariablePatched);
        if (!variablePatched || incorrectSecondValue || incorrectThirdValue) // problem detected
        {
            wrongValues.Add(dictKey, new double[] { value, secondValue, thirdValue });
            if (variablePatched) wrongValues[dictKey][0] = 0; // really bad code, i know i can do it better but i just dont wanna
            if (secondVariablePatched) wrongValues[dictKey][1] = 0;
            if (thirdVariablePatched) wrongValues[dictKey][2] = 0;
        }
        
       
    }
    static IEnumerable<CodeInstruction> TranspileFloatVariables(IEnumerable<CodeInstruction> instructions, MethodBase type)
    {
        string typeOf = type.DeclaringType.ToString();
        typeOf = typeOf.Split('.').Last();
        string name = type.Name.ToString();
        string dictKey = typeOf + name;
        scaledFloats.TryGetValue(dictKey, out float[] values);
        float scaledValue = values[0]; float secondScaledValue = values[1]; float thirdScaledValue = values[2];

        float value = values[3]; float secondValue = values[4]; float thirdValue = values[5];

        bool secondVariablePresent = secondValue != 0; // checks if variables are used
        bool thirdVariablePresent = thirdValue != 0;

        bool variablePatched = false; bool secondVariablePatched = false; bool thirdVariablePatched = false;
        foreach (var instr in instructions)
        {
            if ((instr.opcode == OpCodes.Ldc_R4) && instr.operand is float val)
            {
                if (Mathf.Approximately(val, value)) { instr.operand = scaledValue; variablePatched = true; }
                else if (Mathf.Approximately(val, secondValue) && (secondVariablePresent)) { instr.operand = secondScaledValue; secondVariablePatched = true; }
                else if (Mathf.Approximately(val, thirdValue) && (thirdVariablePresent)) { instr.operand = thirdScaledValue; thirdVariablePatched = true; }
            }
            yield return instr;
        }
        bool incorrectSecondValue = (secondVariablePresent && !secondVariablePatched);
        bool incorrectThirdValue = (thirdVariablePresent && !thirdVariablePatched);
        if (!variablePatched || incorrectSecondValue || incorrectThirdValue) // problem detected
        {
            wrongValues.Add(dictKey, new double[] { value, secondValue, thirdValue });
            if (variablePatched) wrongValues[dictKey][0] = 0; // really bad code, i know i can do it better but i just dont wanna
            if (secondVariablePatched) wrongValues[dictKey][1] = 0;
            if (thirdVariablePatched) wrongValues[dictKey][2] = 0;
        }
    }
    static IEnumerable<CodeInstruction> TranspileLongVariables(IEnumerable<CodeInstruction> instructions, MethodBase type)
    {
        string typeOf = type.DeclaringType.ToString();
        typeOf = typeOf.Split('.').Last();
        string name = type.Name.ToString();
        string dictKey = typeOf + name;
        scaledLongs.TryGetValue(dictKey, out long[] values);
        long scaledValue = values[0]; long secondScaledValue = values[1]; long thirdScaledValue = values[2];

        long value = values[3]; long secondValue = values[4]; long thirdValue = values[5];

        bool secondVariablePresent = secondValue != 0; // checks if variables are used
        bool thirdVariablePresent = thirdValue != 0;

        bool variablePatched = false; bool secondVariablePatched = false; bool thirdVariablePatched = false;
        foreach (var instr in instructions)
        {
            if ((instr.opcode == OpCodes.Ldc_I8) && instr.operand is long val)
            {
                if (val == value) { instr.operand = scaledValue; variablePatched = true; }
                else if ((val == secondValue) && (secondVariablePresent)) { instr.operand = secondScaledValue; secondVariablePatched = true; }
                else if ((val == thirdValue) && (thirdVariablePresent)) { instr.operand = thirdScaledValue; thirdVariablePatched = true; }
            }
            yield return instr;
        }
        bool incorrectSecondValue = (secondVariablePresent && !secondVariablePatched);
        bool incorrectThirdValue = (thirdVariablePresent && !thirdVariablePatched);
        if (!variablePatched || incorrectSecondValue || incorrectThirdValue) // problem detected
        {
            wrongValues.Add(dictKey, new double[] { value, secondValue, thirdValue });
            if (variablePatched) wrongValues[dictKey][0] = 0; // really bad code, i know i can do it better but i just dont wanna
            if (secondVariablePatched) wrongValues[dictKey][1] = 0;
            if (thirdVariablePatched) wrongValues[dictKey][2] = 0;
        }
    }
    static IEnumerable<CodeInstruction> TranspileDoubleVariables(IEnumerable<CodeInstruction> instructions, MethodBase type)
    {
        string typeOf = type.DeclaringType.ToString();
        typeOf = typeOf.Split('.').Last();
        string name = type.Name.ToString();
        string dictKey = typeOf + name;
        scaledDoubles.TryGetValue(dictKey, out double[] values);
        double scaledValue = values[0]; double secondScaledValue = values[1]; double thirdScaledValue = values[2];

        double value = values[3]; double secondValue = values[4]; double thirdValue = values[5];

        bool secondVariablePresent = secondValue != 0; // checks if variables are used
        bool thirdVariablePresent = thirdValue != 0;

        bool variablePatched = false; bool secondVariablePatched = false; bool thirdVariablePatched = false;
        foreach (var instr in instructions)
        {
            if ((instr.opcode == OpCodes.Ldc_R8) && instr.operand is double val)
            {
                if (Math.Abs(val - value) < 0.0001) { instr.operand = scaledValue; variablePatched = true; }
                else if ((Math.Abs(val - secondValue) < 0.0001) && (secondVariablePresent)) { instr.operand = secondScaledValue; secondVariablePatched = true; }
                else if ((Math.Abs(val - thirdValue) < 0.0001) && (thirdVariablePresent)) { instr.operand = thirdScaledValue; thirdVariablePatched = true; }
            }
            yield return instr;
        }
        bool incorrectSecondValue = (secondVariablePresent && !secondVariablePatched);
        bool incorrectThirdValue = (thirdVariablePresent && !thirdVariablePatched);
        if (!variablePatched || incorrectSecondValue || incorrectThirdValue) // problem detected
        {
            wrongValues.Add(dictKey, new double[] { value, secondValue, thirdValue });
            if (variablePatched) wrongValues[dictKey][0] = 0; // really bad code, i know i can do it better but i just dont wanna
            if (secondVariablePatched) wrongValues[dictKey][1] = 0;
            if (thirdVariablePatched) wrongValues[dictKey][2] = 0;
        }
    }
}