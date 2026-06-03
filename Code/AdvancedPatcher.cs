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
using System.Xml.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using Verse;
using static UnityEngine.GraphicsBuffer;

public class AdvancedPatchDef : Def
{
    public string namespaceOf; //namespace
    public string typeOf; // class
    public string name; // method or property name
    public string type; // num OR result, delta, call
                        // optional
    public List<double> values; // must be filled in unless isCall is used
    public bool isReverse; // by default the value is multiplied, if isReverse is true it is divided instead
    public bool isGetter; // by default methods are used, if isGetter is true it uses property getters instead
    public string callName; // the name of the method in call
    public int skipResults; // skips x amount of results
    public int parametersLength;
    public double customModifier;
    public bool isPrefix;
}


namespace DayStretch
{
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
        public static Dictionary<string, bool> keyReverse = new Dictionary<string, bool>();

        public static Dictionary<string, double[]> wrongValues = new Dictionary<string, double[]>();

        public static Dictionary<string, List<double>> targetNumbers = new Dictionary<string, List<double>>();

        public static int amountofWrongValues = 0;

        static int numbersPatched = 0;
        static string fullList = "Methods Patched:\n";
        static string fullGetterList = "Getters Patched:\n";
        public static string loggerList = "";

        static string resFullList = "Method Results Patched:\n";
        static string resFullGetterList = "Getter Results Patched:\n";
        public static string resLoggerList = "";
        static int resultsPatched;


        static AdvancedPatcher()
        {
            foreach (AdvancedPatchDef def in DefDatabase<AdvancedPatchDef>.AllDefsListForReading)
            {
                AdvancedDefPatcher(def);
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
                resLoggerList += $"Result Patcher:\nNumber of results patched: {resultsPatched}\n\n{resFullList}\n\n{resFullGetterList}";
            }
        }

        static void AdvancedDefPatcher(AdvancedPatchDef def)
        {
            string[] numericalTypes = new string[] { "int", "float", "long", "short", "double" };
            string[] otherTypes = new string[] { "call", "result", "delta" };
            bool isNumericalType = numericalTypes.Contains(def.type);
            bool isOtherType = otherTypes.Contains(def.type);
            if (def.namespaceOf == null) { Log.Error($"[DayStretch]-(AdvancedPatch) namespaceOf in {def.defName} is not filled in; skipping."); return; }
            if (def.typeOf == null) { Log.Error($"[DayStretch]-(AdvancedPatch) typeOf in {def.defName} is not filled in; skipping."); return; }
            if (def.name == null) { Log.Error($"[DayStretch]-(AdvancedPatch) name in {def.defName} is not filled in; skipping."); return; }
            if (def.type == null || !isNumericalType && !isOtherType) { Log.Error($"[DayStretch]-(AdvancedPatch) {def.typeOf} has an invalid type or is null, input: {def.type}"); return; }
            // yeah i know i could have done something fancier but is it REALLY needed?
            bool parametersLengthFilled = def.parametersLength != 0;
            bool customModifierFilled = def.customModifier != 0f;

            var harmony = new Harmony("com.julekjulas.advancedpatch");

            Type type = GenTypes.GetTypeInAnyAssembly($"{def.namespaceOf}.{def.typeOf}");

            if (type == null) { Log.Error($"[DayStretch]-(AdvancedPatch) Type '{def.typeOf}' not found in namespace '{def.namespaceOf}'; skipping."); return; }

            if (isNumericalType)
            {
                string fullDictionaryEntry = $"{def.namespaceOf}.{def.typeOf}";
                if (def.isGetter) fullDictionaryEntry += "get_";
                fullDictionaryEntry += $"{def.name}:{def.type}";
                double reverse = def.isReverse ? -1 : 1;
                List<double> curValues = new List<double> { reverse, def.customModifier, def.skipResults };
                foreach (double val in def.values) { curValues.Add(val); }
                targetNumbers.Add(fullDictionaryEntry, curValues);
                if (def.isGetter)
                {
                    foreach (var prop in type.GetProperties(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic))
                    {
                        if (!string.IsNullOrEmpty(def.name) && prop.Name != def.name) continue;
                        var getter = prop.GetGetMethod(true);
                        if (getter == null) continue;
                        if (getter.GetParameters().Length != def.parametersLength && def.parametersLength != 0) continue;
                        if (getter.IsAbstract || getter.IsGenericMethodDefinition) continue;
                        try
                        {
                            HarmonyMethod transpiler;
                            switch (def.type)
                            { // why didnt i do object stuff? well i dont wanna
                                // seriously just sending more data by actually just naming them different things is just more convenient leave me alone
                                case "int": transpiler = new HarmonyMethod(typeof(AdvancedPatcher).GetMethod(nameof(TranspileIntVariables), BindingFlags.Static | BindingFlags.NonPublic)); break;
                                case "float": transpiler = new HarmonyMethod(typeof(AdvancedPatcher).GetMethod(nameof(TranspileFloatVariables), BindingFlags.Static | BindingFlags.NonPublic)); break;
                                case "long": transpiler = new HarmonyMethod(typeof(AdvancedPatcher).GetMethod(nameof(TranspileLongVariables), BindingFlags.Static | BindingFlags.NonPublic)); break;
                                case "short": transpiler = new HarmonyMethod(typeof(AdvancedPatcher).GetMethod(nameof(TranspileIntVariables), BindingFlags.Static | BindingFlags.NonPublic)); break;
                                case "double": transpiler = new HarmonyMethod(typeof(AdvancedPatcher).GetMethod(nameof(TranspileDoubleVariables), BindingFlags.Static | BindingFlags.NonPublic)); break;
                                default: return;
                            }
                            harmony.Patch(getter, transpiler: transpiler);
                            fullGetterList += $"{def.typeOf}.{prop.Name} ({def.type}), \n";
                        }
                        catch (Exception e)
                        {
                            Log.Error($"[DayStretch]-(AdvancedPatch) Failed patching getter {def.typeOf}.{prop.Name}: {e}");
                        }
                    }
                }
                else
                {
                    foreach (var method in type.GetMethods(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic))
                    {
                        if (method.IsAbstract || method.IsGenericMethodDefinition) continue;
                        if (method.GetParameters().Length != def.parametersLength && def.parametersLength != 0) continue;
                        if (!string.IsNullOrEmpty(def.name) && method.Name != def.name) continue;
                        try
                        {
                            HarmonyMethod transpiler;
                            switch (def.type)
                            {
                                case "int": transpiler = new HarmonyMethod(typeof(AdvancedPatcher).GetMethod(nameof(TranspileIntVariables), BindingFlags.Static | BindingFlags.NonPublic)); break;
                                case "float": transpiler = new HarmonyMethod(typeof(AdvancedPatcher).GetMethod(nameof(TranspileFloatVariables), BindingFlags.Static | BindingFlags.NonPublic)); break;
                                case "long": transpiler = new HarmonyMethod(typeof(AdvancedPatcher).GetMethod(nameof(TranspileLongVariables), BindingFlags.Static | BindingFlags.NonPublic)); break;
                                case "short": transpiler = new HarmonyMethod(typeof(AdvancedPatcher).GetMethod(nameof(TranspileIntVariables), BindingFlags.Static | BindingFlags.NonPublic)); break;
                                case "double": transpiler = new HarmonyMethod(typeof(AdvancedPatcher).GetMethod(nameof(TranspileDoubleVariables), BindingFlags.Static | BindingFlags.NonPublic)); break;
                                default: return;
                            }
                            harmony.Patch(method, transpiler: transpiler);
                            fullList += $"{def.typeOf}.{method.Name} ({def.type}), \n";
                        }
                        catch (Exception e)
                        {
                            Log.Error($"[DayStretch]-(AdvancedPatch) Failed Patching {def.typeOf}. {e}");
                            return;
                        }
                    }
                }
            }
            else
            {
            }

        }
        public static void DoCall(AdvancedPatchDef def)
        {
            Type type = GenTypes.GetTypeInAnyAssembly($"{def.namespaceOf}.{def.typeOf}");
            var harmony = new Harmony("com.julekjulas.callpatch");
            if (def.callName == null) { Log.Error($"[DayStretch]-(AdvancedPatch) callName in {def.defName} is not filled in despite using isCall; skipping."); return; }
            calls.Add(def.namespaceOf + "." + def.typeOf + def.name + ":call", new string[] { def.callName, def.isReverse.ToString(), def.type });

            foreach (var method in type.GetMethods(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic))
            {
                if (method.IsAbstract || method.IsGenericMethodDefinition) continue;
                if (!string.IsNullOrEmpty(def.name) && method.Name != def.name) continue;
                try
                {
                    var transpiler = new HarmonyMethod(typeof(AdvancedPatcher).GetMethod(nameof(TranspileCall), BindingFlags.Static | BindingFlags.NonPublic)); harmony.Patch(method, transpiler: transpiler);
                    fullList += $"{def.typeOf}.{method.Name} ({def.type}), \n";
                }
                catch (Exception e)
                {
                    Log.Error($"[DayStretch]-(AdvancedPatch) Failed Patching {def.typeOf}. {e}");
                    return;
                }
            }
        }
        public static void DoResult(AdvancedPatchDef def)
        {
            var harmony = new Harmony("com.julekjulas.resultpatch");
            Type type = GenTypes.GetTypeInAnyAssembly($"{def.namespaceOf}.{def.typeOf}");
            if (type == null)
            {
                Log.Error($"[DayStretch]-(ResultPatch) Type '{def.typeOf}' not found in namespace '{def.namespaceOf}'; skipping.");
                return;
            }
            if (def.isGetter)
            {
                foreach (var prop in type.GetProperties(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic))
                {
                    if (!string.IsNullOrEmpty(def.name) && prop.Name != def.name) continue;

                    var getter = prop.GetGetMethod(true);
                    if (getter == null) continue;
                    if (getter.GetParameters().Length != def.parametersLength) continue;
                    if (getter.IsAbstract || getter.IsGenericMethodDefinition) continue;

                    try
                    {
                        if (def.isPrefix)
                        {
                            var prefix = new HarmonyMethod(typeof(AdvancedPatcher).GetMethod(nameof(ResultPrefix), BindingFlags.Static | BindingFlags.NonPublic)); harmony.Patch(getter, prefix: prefix);
                        }
                        else
                        {
                            var postfix = new HarmonyMethod(typeof(AdvancedPatcher).GetMethod(nameof(ResultPostfix), BindingFlags.Static | BindingFlags.NonPublic)); harmony.Patch(getter, postfix: postfix);
                        }
                        resFullGetterList += $"{def.typeOf}.{prop.Name} \n";
                        resultsPatched++;
                    }
                    catch (Exception e)
                    {
                        Log.Error($"[DayStretch]-(ResultPatch) Failed patching getter {def.typeOf}.{prop.Name}: {e}");
                    }// TODO do the logger bit
                }
                return;
            }
            else
            {
                foreach (var method in type.GetMethods(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic))
                {

                    if (method.IsAbstract || method.IsGenericMethodDefinition) continue;
                    if (!string.IsNullOrEmpty(def.name) && method.Name != def.name) continue;
                    if (method.GetParameters().Length != def.parametersLength) continue;
                    try
                    {
                        if (def.isPrefix)
                        {
                            var prefix = new HarmonyMethod(typeof(AdvancedPatcher).GetMethod(nameof(ResultPrefix), BindingFlags.Static | BindingFlags.NonPublic)); harmony.Patch(method, prefix: prefix);
                        }
                        else
                        {
                            var postfix = new HarmonyMethod(typeof(AdvancedPatcher).GetMethod(nameof(ResultPostfix), BindingFlags.Static | BindingFlags.NonPublic)); harmony.Patch(method, postfix: postfix);
                        }
                        fullList += $"{def.typeOf}.{method.Name} \n";
                        resultsPatched++;
                    }
                    catch (Exception e)
                    {
                        Log.Error($"[DayStretch]-(ResultPatch) {e} Result not found.");
                    }
                }
            }
        }




        static IEnumerable<CodeInstruction> TranspileIntVariables(IEnumerable<CodeInstruction> instructions, MethodBase type)
        {
            string typeOf = type.DeclaringType.ToString();
            string name = type.Name.ToString();
            string dictKey = typeOf + name + ":int"; // the :int is literally the only reason its not all one big IEnumerable, yes really, I didnt do that in result patcher cuz you dont need as much info
            targetNumbers.TryGetValue(dictKey, out List<double> values);
            bool reverse = false;
            double customModifier = 0; bool customModifierFilled = false;
            if (values[0] == 1) { reverse = true; }
            if (values[1] != 0) { customModifier = values[1]; customModifierFilled = true; } //TODO do customModifier, for now i cant be bothered
            int skipResults = (int)values[2];
            values.RemoveRange(0, 3);
            foreach (var instr in instructions)
                {
                    if ((instr.opcode == OpCodes.Ldc_I4 || instr.opcode == OpCodes.Ldc_I4_S) && instr.operand is int val)
                    { 
                        if (values.Any(x => Math.Abs(x - val) <= 0.0001)) { if (skipResults == 0) { if (reverse) { instr.operand = (int)(val / Settings.Instance.TimeMultiplier); } else { instr.operand = (int)(val * Settings.Instance.TimeMultiplier); } } else skipResults--; }
                    }
                    yield return instr;
                }
        }
        static IEnumerable<CodeInstruction> TranspileFloatVariables(IEnumerable<CodeInstruction> instructions, MethodBase type)
        {
            string typeOf = type.DeclaringType.ToString();
            string name = type.Name.ToString();
            string dictKey = typeOf + name + ":float"; // like geniuinely if anyone has a better idea how to do this please tell me, this is bizzare
            targetNumbers.TryGetValue(dictKey, out List<double> values);
            bool reverse = false;
            double customModifier = 0; bool customModifierFilled = false;
            if (values[0] == 1) { reverse = true; }
            if (values[1] != 0) { customModifier = values[1]; customModifierFilled = true; } //TODO do customModifier, for now i cant be bothered
            int skipResults = (int)values[2];
            values.RemoveRange(0, 3);
            foreach (var instr in instructions)
            {
                if ((instr.opcode == OpCodes.Ldc_R4) && instr.operand is float val)
                {
                    if (values.Any(x => Math.Abs(x - val) <= 0.0001)) { if (skipResults == 0) { if (reverse) { instr.operand = val / Settings.Instance.TimeMultiplier; } else { instr.operand = val * Settings.Instance.TimeMultiplier; } } else skipResults--; }
                }
                yield return instr;
            }

        }
        static IEnumerable<CodeInstruction> TranspileLongVariables(IEnumerable<CodeInstruction> instructions, MethodBase type)
        {
            string typeOf = type.DeclaringType.ToString();
            string name = type.Name.ToString();
            string dictKey = typeOf + name + ":long";
            targetNumbers.TryGetValue(dictKey, out List<double> values);
            bool reverse = false;
            double customModifier = 0; bool customModifierFilled = false;
            if (values[0] == 1) { reverse = true; }
            if (values[1] != 0) { customModifier = values[1]; customModifierFilled = true; } //TODO do customModifier, for now i cant be bothered
            int skipResults = (int)values[2];
            values.RemoveRange(0, 3);
            foreach (var instr in instructions)
            {
                if ((instr.opcode == OpCodes.Ldc_I8) && instr.operand is long val)
                {
                    if (values.Any(x => Math.Abs(x - val) <= 0.0001)) { if (skipResults == 0) { if (reverse) { instr.operand = (long)(val / Settings.Instance.TimeMultiplier); } else { instr.operand = (long)(val * Settings.Instance.TimeMultiplier); } } else skipResults--;      }
                }
                yield return instr;
            }
        }
        static IEnumerable<CodeInstruction> TranspileDoubleVariables(IEnumerable<CodeInstruction> instructions, MethodBase type)
        {
            string typeOf = type.DeclaringType.ToString();
            string name = type.Name.ToString();
            string dictKey = typeOf + name + ":double"; // what double? of course
            targetNumbers.TryGetValue(dictKey, out List<double> values);
            bool reverse = false;
            double customModifier = 0; bool customModifierFilled = false;
            if (values[0] == 1) { reverse = true; }
            if (values[1] != 0) { customModifier = values[1]; customModifierFilled = true; } //TODO do customModifier, for now i cant be bothered
            int skipResults = (int)values[2];
            values.RemoveRange(0, 3);
            foreach (var instr in instructions)
            {
                if ((instr.opcode == OpCodes.Ldc_R8) && instr.operand is double val)
                {
                    if (values.Any(x => Math.Abs(x - val) <= 0.0001)) { if (skipResults == 0) { if (reverse) { instr.operand = val / Settings.Instance.TimeMultiplier; } else { instr.operand = val * Settings.Instance.TimeMultiplier; } } else skipResults--; }
                }
                yield return instr;
            }
        }

        static IEnumerable<CodeInstruction> TranspileCall(IEnumerable<CodeInstruction> instructions, MethodBase type)
        {
            string typeOf = type.DeclaringType.ToString(); // old ahh code does it even work? i think so?
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
        static bool ReverseCheck(MethodBase type) // get the bool
        {
            string typeOf = type.DeclaringType.ToString();
            string name = type.Name.ToString();
            string dictKey = typeOf + name;
            keyReverse.TryGetValue(dictKey, out bool currentReverse);
            return currentReverse;
        }
        static void ResultPostfix(ref object __result, MethodBase __originalMethod)
        {
            dynamic result = __result;
            bool currentReverse = ReverseCheck(__originalMethod);
            if (currentReverse) __result = result / Settings.Instance.TimeMultiplier;
            else __result = result * Settings.Instance.TimeMultiplier;
        }
        static bool ResultPrefix(ref object __result, MethodBase __originalMethod)
        {
            dynamic result = __result;
            bool currentReverse = ReverseCheck(__originalMethod);
            if (currentReverse) __result = result / Settings.Instance.TimeMultiplier;
            else __result = result * Settings.Instance.TimeMultiplier;
            return false;
        }
    }
}
