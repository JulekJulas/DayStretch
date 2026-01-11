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
using System.Security.Cryptography;
using UnityEngine;
using UnityEngine.UIElements;
using Verse;

public class AdvancedPatchDef : Def
{
    public string namespaceOf; //namespace
    public string typeOf; // class
    public string name; // method or property name
    public string type; // int, float, long, short, double
    // optional
    public double value; // must be filled in unless isCall is used
    public double secondValue; // optional, cant be used if skip results is used
    public double thirdValue; // same as above
    public bool isReverse; // by default the value is multiplied, if isReverse is true it is divided instead
    public bool isGetter; // by default methods are used, if isGetter is true it uses property getters instead
    public bool isCall; // if true, AdvancedPatcher will try to find the method used and add multiplication/division to the call
    public string callName; // the name of the method in call
    public int skipResults; // skips x amount of results
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
    public static Dictionary<string, string[]> calls = new Dictionary<string, string[]>();

    public static Dictionary<string, double[]> wrongValues = new Dictionary<string, double[]>();

    public static int amountofWrongValues = 0;

    static int numbersPatched = 0;
    static string fullList = "Methods Patched:\n";
    static string fullGetterList = "Getters Patched:\n";
    public static string loggerList = "";

    static AdvancedPatcher()
    {
        foreach (AdvancedPatchDef def in DefDatabase<AdvancedPatchDef>.AllDefsListForReading)
        {
            AdvancedDefPatcher(def.defName, def.namespaceOf, def.typeOf, def.name, def.type, def.value, def.secondValue, def.thirdValue, def.isReverse, def.isGetter, def.isCall, def.callName, def.skipResults);
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

            loggerList += $"Advanced Patcher:\nNumber of variables patched: {numbersPatched}\n\n{fullList}\n\n{fullGetterList}";
        }
    }

    static void AdvancedDefPatcher(string defName, string namespaceOf, string typeOf, string name, string numType, double value, double secondValue, double thirdValue, bool reverse, bool isGetter, bool isCall, string callName, int skipResults)
    {
        string[] validTypes = new string[] { "int", "float", "long", "short", "double" };
        // really compact checks for null values
        if (namespaceOf == null) { Log.Error($"[DayStretch]-(AdvancedPatch) namespaceOf in {defName} is not filled in; skipping."); return; }
        if (typeOf == null) { Log.Error($"[DayStretch]-(AdvancedPatch) typeOf in {defName} is not filled in; skipping."); return; }
        if (name == null) { Log.Error($"[DayStretch]-(AdvancedPatch) name in {defName} is not filled in; skipping."); return; }
        if (numType == null || !validTypes.Contains(numType)) { Log.Error($"[DayStretch]-(AdvancedPatch) {typeOf} has an invalid type or is null, input: {numType}"); return; }





        // my habit of overcompacting things will be the death of me one day    

        var harmony = new Harmony("com.julekjulas.advancedpatch");

        Type type = GenTypes.GetTypeInAnyAssembly($"{namespaceOf}.{typeOf}");

        if (type == null) { Log.Error($"[DayStretch]-(AdvancedPatch) Type '{typeOf}' not found in namespace '{namespaceOf}'; skipping."); return; }
        if (type.Method(name) == null && isGetter == false) { Log.Error($"[DayStretch]-(AdvancedPatch) Method '{name}' not found in class '{typeOf}' in namespace '{namespaceOf}'; skipping."); return; }
        if (type.Property(name) == null && isGetter == true) { Log.Error($"[DayStretch]-(AdvancedPatch) Property '{name}' not found in class '{typeOf}' in namespace '{namespaceOf}'; skipping."); return; }

        if (isCall) // since there is no value we have to do patch it now
        {
            if (callName == null) { Log.Error($"[DayStretch]-(AdvancedPatch) callName in {defName} is not filled in despite using isCall; skipping."); return; }
            calls.Add(namespaceOf + "." + typeOf + name + ":call", new string[] { callName, reverse.ToString(), numType });

            foreach (var method in type.GetMethods(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic))
            {
                if (method.IsAbstract || method.IsGenericMethodDefinition) continue;
                if (!string.IsNullOrEmpty(name) && method.Name != name) continue;
                try
                {
                    var transpiler = new HarmonyMethod(typeof(AdvancedPatcher).GetMethod(nameof(TranspileCall), BindingFlags.Static | BindingFlags.NonPublic)); harmony.Patch(method, transpiler: transpiler);
                    fullList += $"{typeOf}.{method.Name} ({numType}), \n";
                }
                catch (Exception e)
                {
                    Log.Error($"[DayStretch]-(AdvancedPatch) Failed Patching {typeOf}. {e}");
                    return;
                }
            }
        }
        else
        {
            double scaledValue = 0; double secondScaledValue = 0; double thirdScaledValue = 0;

            int scaledInt = 0; int secondScaledInt = 0; int thirdScaledInt = 0;

            float scaledFloat = 0; float secondScaledFloat = 0; float thirdScaledFloat = 0;

            long scaledLong = 0; long secondScaledLong = 0; long thirdScaledLong = 0;

            bool secondValuePresent = secondValue != 0d;
            bool thirdValuePresent = thirdValue != 0d;
            if (value == 0d) { Log.Error($"[DayStretch]-(AdvancedPatch) value in {defName} is not filled in; skipping."); return; }
            if (reverse)
            {
                scaledValue = (double)(value * (1f / Settings.Instance.TimeMultiplier));
                if (secondValuePresent) secondScaledValue = (double)(secondValue / Settings.Instance.TimeMultiplier);
                if (thirdValuePresent) thirdScaledValue = (double)(thirdValue / Settings.Instance.TimeMultiplier);
            }
            else
            {
                scaledValue = (double)(value * Settings.Instance.TimeMultiplier);
                if (secondValuePresent) secondScaledValue = (double)(secondValue * Settings.Instance.TimeMultiplier);
                if (thirdValuePresent) thirdScaledValue = (double)(thirdValue * Settings.Instance.TimeMultiplier);
            }
            switch (numType)
            {
                case "int": // it looks scary but its just because im dumb and could have done this better
                    scaledInt = (int)(scaledValue);
                    if (secondValuePresent) secondScaledInt = (int)(secondScaledValue);
                    if (thirdValuePresent) thirdScaledInt = (int)(thirdScaledValue);
                    scaledInts.Add(isGetter ? (namespaceOf + "." + typeOf + "get_" + name + ":int") : (namespaceOf + "." + typeOf + name + ":int"), new int[] { scaledInt, secondScaledInt, thirdScaledInt, (int)value, (int)secondValue, (int)thirdValue, skipResults });
                    break;
                case "float":
                    scaledFloat = (float)(scaledValue);
                    if (secondValuePresent) secondScaledFloat = (float)(secondScaledValue);
                    if (thirdValuePresent) thirdScaledFloat = (float)(thirdScaledValue);
                    scaledFloats.Add(isGetter ? (namespaceOf + "." + typeOf + "get_" + name + ":float") : (namespaceOf + "." + typeOf + name + ":float"), new float[] { scaledFloat, secondScaledFloat, thirdScaledFloat, (float)value, (float)secondValue, (float)thirdValue, (float)skipResults });
                    break;
                case "long":
                    scaledLong = (long)(scaledValue);
                    if (secondValuePresent) secondScaledLong = (long)(secondScaledValue);
                    if (thirdValuePresent) thirdScaledLong = (long)(thirdScaledValue);
                    scaledLongs.Add(isGetter ? (namespaceOf + "." + typeOf + "get_" + name + ":long") : (namespaceOf + "." + typeOf + name + ":long"), new long[] { scaledLong, secondScaledLong, thirdScaledLong, (long)value, (long)secondValue, (long)thirdValue, (long)skipResults });
                    break;
                case "short": // just in case if someone inputs it
                    scaledInt = (int)(scaledValue);
                    if (secondValuePresent) secondScaledInt = (int)(secondScaledValue);
                    if (thirdValuePresent) thirdScaledInt = (int)(thirdScaledValue);
                    scaledShorts.Add(isGetter ? (namespaceOf + "." + typeOf + "get_" + name + ":int") : (namespaceOf + "." + typeOf + name + ":int"), new short[] { (short)scaledInt, (short)secondScaledInt, (short)thirdScaledInt, (short)value, (short)secondValue, (short)thirdValue, (short)skipResults });
                    break; // just goes to ints as it prob should
                case "double":
                    scaledDoubles.Add(isGetter ? (namespaceOf + "." + typeOf + "get_" + name + ":double") : (namespaceOf + "." + typeOf + name + ":double"), new double[] { scaledValue, secondScaledValue, thirdScaledValue, value, secondValue, thirdValue, skipResults }); // just values since its already a double
                    break;
                default:
                    Log.Error($"[DayStretch]-(AdvancedPatch) {typeOf} has an invalid type, input: {numType}");
                    return;
            }


            // extra checks



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
                        HarmonyMethod transpiler;
                        switch (numType)
                        {
                            case "int": transpiler = new HarmonyMethod(typeof(AdvancedPatcher).GetMethod(nameof( TranspileIntVariables), BindingFlags.Static | BindingFlags.NonPublic)); break;
                            case "float": transpiler = new HarmonyMethod(typeof(AdvancedPatcher).GetMethod(nameof(TranspileFloatVariables), BindingFlags.Static | BindingFlags.NonPublic)); break;
                            case "long": transpiler = new HarmonyMethod(typeof(AdvancedPatcher).GetMethod(nameof(TranspileLongVariables), BindingFlags.Static | BindingFlags.NonPublic)); break;
                            case "short": transpiler = new HarmonyMethod(typeof(AdvancedPatcher).GetMethod(nameof(TranspileIntVariables), BindingFlags.Static | BindingFlags.NonPublic)); break;
                            case "double": transpiler = new HarmonyMethod(typeof(AdvancedPatcher).GetMethod(nameof(TranspileDoubleVariables), BindingFlags.Static | BindingFlags.NonPublic)); break;
                            default: return;
                        }
                        harmony.Patch(getter, transpiler: transpiler);
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
                        HarmonyMethod transpiler;
                        switch (numType)
                        {
                            case "int": transpiler = new HarmonyMethod(typeof(AdvancedPatcher).GetMethod(nameof(TranspileIntVariables), BindingFlags.Static | BindingFlags.NonPublic)); break;
                            case "float": transpiler = new HarmonyMethod(typeof(AdvancedPatcher).GetMethod(nameof(TranspileFloatVariables), BindingFlags.Static | BindingFlags.NonPublic)); break;
                            case "long": transpiler = new HarmonyMethod(typeof(AdvancedPatcher).GetMethod(nameof(TranspileLongVariables), BindingFlags.Static | BindingFlags.NonPublic)); break;
                            case "short": transpiler = new HarmonyMethod(typeof(AdvancedPatcher).GetMethod(nameof(TranspileIntVariables), BindingFlags.Static | BindingFlags.NonPublic)); break;
                            case "double": transpiler = new HarmonyMethod(typeof(AdvancedPatcher).GetMethod(nameof(TranspileDoubleVariables), BindingFlags.Static | BindingFlags.NonPublic)); break;
                            default: return;
                        }
                        harmony.Patch(method, transpiler: transpiler);
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
    }





    static IEnumerable<CodeInstruction> TranspileIntVariables(IEnumerable<CodeInstruction> instructions, MethodBase type)
    {
        string typeOf = type.DeclaringType.ToString();
        string name = type.Name.ToString();
        string dictKey = typeOf + name + ":int";
        scaledInts.TryGetValue(dictKey, out int[] values);
        int scaledValue = values[0]; int secondScaledValue = values[1]; int thirdScaledValue = values[2];

        int value = values[3]; int secondValue = values[4]; int thirdValue = values[5]; int skipResults = (int)values[6];

        bool secondVariablePresent = secondValue != 0; // checks if variables are used
        bool thirdVariablePresent = thirdValue != 0;

        bool variablePatched = false; bool secondVariablePatched = false; bool thirdVariablePatched = false;
        foreach (var instr in instructions)
        {   
            if ((instr.opcode == OpCodes.Ldc_I4 || instr.opcode == OpCodes.Ldc_I4_S) && instr.operand is int val)
            {
                if (val == value) { if (skipResults == 0) { instr.operand = scaledValue; variablePatched = true; numbersPatched++; } else skipResults--; }
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
        string dictKey = typeOf + name + ":float";
        scaledFloats.TryGetValue(dictKey, out float[] values);
        float scaledValue = values[0]; float secondScaledValue = values[1]; float thirdScaledValue = values[2];

        float value = values[3]; float secondValue = values[4]; float thirdValue = values[5]; int skipResults = (int)values[6];

        bool secondVariablePresent = secondValue != 0; // checks if variables are used
        bool thirdVariablePresent = thirdValue != 0;

        bool variablePatched = false; bool secondVariablePatched = false; bool thirdVariablePatched = false;
        foreach (var instr in instructions)
        {
            if ((instr.opcode == OpCodes.Ldc_R4) && instr.operand is float val)
            {
                if (Mathf.Approximately(val, value)) { if (skipResults == 0) { instr.operand = scaledValue; variablePatched = true; numbersPatched++; } else skipResults--; }
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
        string dictKey = typeOf + name + ":long";
        scaledLongs.TryGetValue(dictKey, out long[] values);
        long scaledValue = values[0]; long secondScaledValue = values[1]; long thirdScaledValue = values[2];

        long value = values[3]; long secondValue = values[4]; long thirdValue = values[5]; int skipResults = (int)values[6];

        bool secondVariablePresent = secondValue != 0; // checks if variables are used
        bool thirdVariablePresent = thirdValue != 0;

        bool variablePatched = false; bool secondVariablePatched = false; bool thirdVariablePatched = false;
        foreach (var instr in instructions)
        {
            if ((instr.opcode == OpCodes.Ldc_I8) && instr.operand is long val)
            {
                if (val == value) { if (skipResults == 0) { instr.operand = scaledValue; variablePatched = true; numbersPatched++; } else skipResults--; }
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
        string dictKey = typeOf + name + ":double";
        scaledDoubles.TryGetValue(dictKey, out double[] values);
        double scaledValue = values[0]; double secondScaledValue = values[1]; double thirdScaledValue = values[2];

        double value = values[3]; double secondValue = values[4]; double thirdValue = values[5]; int skipResults = (int)values[6];

        bool secondVariablePresent = secondValue != 0; // checks if variables are used
        bool thirdVariablePresent = thirdValue != 0;

        bool variablePatched = false; bool secondVariablePatched = false; bool thirdVariablePatched = false;
        foreach (var instr in instructions)
        {
            if ((instr.opcode == OpCodes.Ldc_R8) && instr.operand is double val)
            {
                if (Math.Abs(val - value) < 0.0001) { if (skipResults == 0) { instr.operand = scaledValue; variablePatched = true; numbersPatched++; } else skipResults--; }
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

    static IEnumerable<CodeInstruction> TranspileCall(IEnumerable<CodeInstruction> instructions, MethodBase type)
    {
        string typeOf = type.DeclaringType.ToString();
        string name = type.Name.ToString();
        string dictKey = typeOf + name + ":call";
        calls.TryGetValue(dictKey, out string[] strings);
        string callName = strings[0]; bool reverse = Convert.ToBoolean(strings[1]); string numType = strings[2];
        MethodInfo targetMethod = null;


        switch (numType)
        {
            case "int": targetMethod = AccessTools.Method(typeof(GenText), callName, new Type[] { typeof(int) }); break;
            case "float": targetMethod = AccessTools.Method(typeof(GenText), callName, new Type[] { typeof(float) }); break;
            case "long": targetMethod = AccessTools.Method(typeof(GenText), callName, new Type[] { typeof(long) }); break;
            case "short": targetMethod = AccessTools.Method(typeof(GenText), callName, new Type[] { typeof(short) }); break;
            case "double": targetMethod = AccessTools.Method(typeof(GenText), callName, new Type[] { typeof(double) }); break;
        }
        bool callPatched = false;
        FieldInfo instanceField = AccessTools.Field(typeof(Settings), nameof(Settings.Instance));
        FieldInfo timeMultiplierField = AccessTools.Field(typeof(DayStretch), nameof(DayStretch.TimeMultiplier));

        foreach (var instr in instructions)
        {
            if (instr.Calls(targetMethod))
            {
                yield return new CodeInstruction(OpCodes.Ldsfld, instanceField);
                yield return new CodeInstruction(OpCodes.Ldfld, timeMultiplierField);
                callPatched = true;
                if (reverse) yield return new CodeInstruction(OpCodes.Div);
                else yield return new CodeInstruction(OpCodes.Mul);
            }
            yield return instr;
        }
        if (callPatched == false)
        {
            wrongValues.Add(dictKey, new double[] { 1, 0, 0 }); // just a dummy value to show its not patched, yup thanks vs autocomplete
        }
    }








}