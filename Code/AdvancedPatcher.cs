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
    public string namespaceOf;
    public string typeOf;
    public string name;
    public string type;
    public double value;
    // optional
    public double secondValue;
    public double thirdValue;
    public bool isReverse;
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
    static string fullList = "[DayStretch]-(AdvancedPatch)\nVariables Patched:\n";
    static string fullGetterList = "[DayStretch]-(AdvancedPatch)\nGetter Variables Patched:\n";

    static AdvancedPatcher()
    {
        foreach (AdvancedPatchDef def in DefDatabase<AdvancedPatchDef>.AllDefsListForReading)
        {
            AdvancedDefPatcher(def.defName, def.namespaceOf, def.typeOf, def.name, def.type, def.value, def.secondValue, def.thirdValue, def.isReverse, def.isGetter);
        }
        // makes so the log only shows the amount of numbers patched exactly one time
        if (!logShown)
        {
            logShown = true;
            if (wrongValues.Count > 0)
            {
                foreach (string key in wrongValues.Keys)
                {
                    if (wrongValues[key][0] != 0) Log.Error($"[DayStretch]-(AdvancedPatch) Value {wrongValues[key][0]} not found in {key}");
                    if (wrongValues[key][1] != 0) Log.Error($"[DayStretch]-(AdvancedPatch) Value {wrongValues[key][1]} not found in {key}");
                    if (wrongValues[key][2] != 0) Log.Error($"[DayStretch]-(AdvancedPatch) Value {wrongValues[key][2]} not found in {key}");
                }
                Log.Warning("[DayStretch]-(AdvancedPatch) Do note: Advanced Patcher not patching certain variables even though they are in the source code may suggest it is edited by something else.");
            }
            Log.Message($"[DayStretch]-(AdvancedPatch) Patched {numbersPatched} variables");

            fullList += "\n\n\n\n";
            fullGetterList += "\n\n\n\n";
            Log.Message(fullList);
            Log.Message(fullGetterList);


            int chance = UnityEngine.Random.Range(1, 101);
            if (chance == 1)
            {
                Log.Message("[DayStretch]-(AdvancedPatch) Patch and fix till its done.");
            }
        }
    }

    static void AdvancedDefPatcher(string defName, string namespaceOf, string typeOf, string name, string numType, double value, double secondValue, double thirdValue, bool reverse, bool isGetter)
    {
        // really compact checks for null values
        if (namespaceOf == null) { Log.Error($"[DayStretch]-(AdvancedPatch) namespaceOf in {defName} is not filled in; skipping."); return; }
        if (typeOf == null) { Log.Error($"[DayStretch]-(AdvancedPatch) typeOf in {defName} is not filled in; skipping."); return; }
        if (name == null) { Log.Error($"[DayStretch]-(AdvancedPatch) name in {defName} is not filled in; skipping."); return; }
        if (numType == null) { Log.Error($"[DayStretch]-(AdvancedPatch) type in {defName} is not filled in; skipping."); return; }
        if (value == 0d) { Log.Error($"[DayStretch]-(AdvancedPatch) value in {defName} is not filled in; skipping."); return; }
        // my habit of overcompacting things will be the death of me one day

        double scaledValue = 0; double secondScaledValue = 0; double thirdScaledValue = 0;

        int scaledInt = 0; int secondScaledInt = 0; int thirdScaledInt = 0;

        float scaledFloat = 0; float secondScaledFloat = 0; float thirdScaledFloat = 0;

        long scaledLong = 0; long secondScaledLong = 0; long thirdScaledLong = 0;


        if (reverse)
        {
            scaledValue = (double)(value * (1f / Settings.Instance.TimeMultiplier));
            if (secondValue != 0d) secondScaledValue = (double)(secondValue / Settings.Instance.TimeMultiplier);
            if (thirdValue != 0d) thirdScaledValue = (double)(thirdValue / Settings.Instance.TimeMultiplier);
        }
        else
        {
            scaledValue = (double)(value * Settings.Instance.TimeMultiplier);
            if (secondValue != 0d) secondScaledValue = (double)(secondValue * Settings.Instance.TimeMultiplier);
            if (thirdValue != 0d) thirdScaledValue = (double)(thirdValue * Settings.Instance.TimeMultiplier);
        }
        switch (numType)
        {
            case "int": // it looks scary but its just because im dumb and could have done this better
                scaledInt = (int)(scaledValue);
                if (secondValue != 0d) secondScaledInt = (int)(secondScaledValue);
                if (thirdValue != 0d) thirdScaledInt = (int)(thirdScaledValue);
                scaledInts.Add(isGetter ? (namespaceOf + "." + typeOf + "get_" + name) : (namespaceOf + "." + typeOf + name), new int[] { scaledInt, secondScaledInt, thirdScaledInt, (int)value, (int)secondValue, (int)thirdValue });
                break;
            case "float":
                scaledFloat = (float)(scaledValue);
                if (secondValue != 0d) secondScaledFloat = (float)(secondScaledValue);
                if (thirdValue != 0d) thirdScaledFloat = (float)(thirdScaledValue);
                scaledFloats.Add(isGetter ? (namespaceOf + "." + typeOf + "get_" + name) : (namespaceOf + "." + typeOf + name), new float[] { scaledFloat, secondScaledFloat, thirdScaledFloat, (float)value, (float)secondValue, (float)thirdValue });
                break;
            case "long":
                scaledLong = (long)(scaledValue);
                if (secondValue != 0d) secondScaledLong = (long)(secondScaledValue);
                if (thirdValue != 0d) thirdScaledLong = (long)(thirdScaledValue);
                scaledLongs.Add(isGetter ? (namespaceOf + "." + typeOf + "get_" + name) : (namespaceOf + "." + typeOf + name), new long[] { scaledLong, secondScaledLong, thirdScaledLong, (long)value, (long)secondValue, (long)thirdValue });
                break;
            case "short": // just in case if someone inputs it
                scaledInt = (int)(scaledValue);
                if (secondValue != 0d) secondScaledInt = (int)(secondScaledValue);
                if (thirdValue != 0d) thirdScaledInt = (int)(thirdScaledValue);
                scaledShorts.Add(isGetter ? (namespaceOf + "." + typeOf + "get_" + name) : (namespaceOf + "." + typeOf + name), new short[] { (short)scaledInt, (short)secondScaledInt, (short)thirdScaledInt, (short)value, (short)secondValue, (short)thirdValue });
                break; // just goes to ints as it prob should
            case "double":
                scaledDoubles.Add(isGetter ? (namespaceOf + "." + typeOf + "get_" + name) : (namespaceOf + "." + typeOf + name), new double[] { scaledValue, secondScaledValue, thirdScaledValue, value, secondValue, thirdValue }); // just values since its already a double
                break;
            default:
                Log.Error($"[DayStretch]-(AdvancedPatch) {typeOf} has an invalid type, input: {numType}");
                return;
        }
        Type type = GenTypes.GetTypeInAnyAssembly($"{namespaceOf}.{typeOf}");


        // extra checks

        if (type == null)
        {
            Log.Error($"[DayStretch]-(AdvancedPatch) Type '{typeOf}' not found in namespace '{namespaceOf}'; skipping.");
            return;
        }
        if (type.Method(name) == null && isGetter == false)
        {
            Log.Error($"[DayStretch]-(AdvancedPatch) Method '{name}' not found in class '{typeOf}' in namespace '{namespaceOf}'; skipping.");
            return;
        }
        if (type.Property(name) == null && isGetter == true) // thanks vs autocomplete
        {
            Log.Error($"[DayStretch]-(AdvancedPatch) Property '{name}' not found in class '{typeOf}' in namespace '{namespaceOf}'; skipping.");
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
                    fullGetterList += $"{typeOf}.{prop.Name} ({numType}), \n";
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
                    fullList += $"{typeOf}.{method.Name} ({numType}), \n";
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
        string name = type.Name.ToString();
        string dictKey = typeOf + name; // i think this is the best way to do it hint: its not
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
                if (val == value) { instr.operand = scaledValue; variablePatched = true; numbersPatched++; }
                else if ((val == secondValue) && (secondVariablePresent)) { instr.operand = secondScaledValue; secondVariablePatched = true; numbersPatched++; }
                else if ((val == thirdValue) && (thirdVariablePresent)) { instr.operand = thirdScaledValue; thirdVariablePatched = true; numbersPatched++; }
            }
            yield return instr;
        }
        bool incorrectSecondValue = (secondVariablePresent && !secondVariablePatched);
        bool incorrectThirdValue = (thirdVariablePresent && !thirdVariablePatched);
        if (!variablePatched || incorrectSecondValue || incorrectThirdValue) // problem detected
        {
            wrongValues.Add(dictKey, new double[] { value, secondValue, thirdValue });
            if (variablePatched) wrongValues[dictKey][0] = 0 ; // really bad code, i know i can do it better but i just dont wanna
            if (secondVariablePatched) wrongValues[dictKey][1] = 0; // on a second thought the code i originally wanted to make would probably be way more messy
            if (thirdVariablePatched) wrongValues[dictKey][2] = 0; // its quite neat actually
        } 
    }
    static IEnumerable<CodeInstruction> TranspileFloatVariables(IEnumerable<CodeInstruction> instructions, MethodBase type)
    {
        string typeOf = type.DeclaringType.ToString();
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
                if (Mathf.Approximately(val, value)) { instr.operand = scaledValue; variablePatched = true; numbersPatched++; }
                else if (Mathf.Approximately(val, secondValue) && (secondVariablePresent)) { instr.operand = secondScaledValue; secondVariablePatched = true; numbersPatched++; }
                else if (Mathf.Approximately(val, thirdValue) && (thirdVariablePresent)) { instr.operand = thirdScaledValue; thirdVariablePatched = true; numbersPatched++; }
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
                if (val == value) { instr.operand = scaledValue; variablePatched = true; numbersPatched++; }
                else if ((val == secondValue) && (secondVariablePresent)) { instr.operand = secondScaledValue; secondVariablePatched = true; numbersPatched++; }
                else if ((val == thirdValue) && (thirdVariablePresent)) { instr.operand = thirdScaledValue; thirdVariablePatched = true; numbersPatched++; }
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
                if (Math.Abs(val - value) < 0.0001) { instr.operand = scaledValue; variablePatched = true; numbersPatched++; }
                else if ((Math.Abs(val - secondValue) < 0.0001) && (secondVariablePresent)) { instr.operand = secondScaledValue; secondVariablePatched = true; numbersPatched++; }
                else if ((Math.Abs(val - thirdValue) < 0.0001) && (thirdVariablePresent)) { instr.operand = thirdScaledValue; thirdVariablePatched = true; numbersPatched++; }
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