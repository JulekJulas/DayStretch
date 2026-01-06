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
using UnityEngine.Analytics;
using Verse;

public class ResultPatchDef : Def
{
    public string namespaceOf;
    public string typeOf;
    public string name;
    public string type;
    // optional
    public bool isReverse;
    public bool isGetter;
    public bool isPrefix;
} 
[StaticConstructorOnStartup]
public static class ResultPatcher
{
    static string fullList = "Method Results Patched:\n";
    static string fullGetterList = "Getter Results Patched:\n";
    public static string loggerList = "";
    static int resultsPatched;
    public static Dictionary<string, bool> keyReverse = new Dictionary<string, bool>();


    static bool logShown = false;
    static ResultPatcher()
    {
        foreach (ResultPatchDef def in DefDatabase<ResultPatchDef>.AllDefsListForReading)
        {
            ResultDefPatcher(def.defName, def.namespaceOf, def.typeOf, def.name, def.type, def.isReverse, def.isGetter, def.isPrefix);
        }
        if (!logShown) logShown = true;
        {
            loggerList += $"Result Patcher:\nNumber of results patched: {resultsPatched}\n\n{fullList}\n\n{fullGetterList}";
        }
    }

    static void ResultDefPatcher(string defName, string namespaceOf, string typeOf, string name, string numType, bool reverse, bool isGetter, bool isPrefix)
    {
        if (namespaceOf == null) { Log.Error($"[DayStretch]-(ResultPatch) namespaceOf in {defName} is not filled in; skipping."); return; }
        if (typeOf == null) { Log.Error($"[DayStretch]-(ResultPatch) typeOf in {defName} is not filled in; skipping."); return; }
        if (name == null) { Log.Error($"[DayStretch]-(ResultPatch) name in {defName} is not filled in; skipping."); return; }
        if (numType == null) { Log.Error($"[DayStretch]-(ResultPatch) type in {defName} is not filled in; skipping."); return; }


        Type type = GenTypes.GetTypeInAnyAssembly($"{namespaceOf}.{typeOf}");
        if (type == null)
        {
            Log.Error($"[DayStretch]-(ResultPatch) Type '{typeOf}' not found in namespace '{namespaceOf}'; skipping.");
            return;
        }
        if (type.Method(name) == null && isGetter == false)
        {
            Log.Error($"[DayStretch]-(ResultPatch) Method '{name}' not found in class '{typeOf}' in namespace '{namespaceOf}'; skipping.");
            return;
        }
        if (type.Property(name) == null && isGetter == true) // thanks vs autocomplete
        {
            Log.Error($"[DayStretch]-(ResultPatch) Property '{name}' not found in class '{typeOf}' in namespace '{namespaceOf}'; skipping.");
            return;
        }
        keyReverse.Add(isGetter ? (namespaceOf + "." + typeOf + "get_" + name) : (namespaceOf + "." + typeOf + name),  reverse);






        var harmony = new Harmony("com.julekjulas.resultpatch");
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
                    if (isPrefix)
                    {
                        switch (numType)
                        {
                            case "int": var prefix = new HarmonyMethod(typeof(ResultPatcher).GetMethod(nameof(IntResultPrefix), BindingFlags.Static | BindingFlags.NonPublic)); harmony.Patch(getter, prefix: prefix); break;
                            case "float": var floatPrefix = new HarmonyMethod(typeof(ResultPatcher).GetMethod(nameof(FloatResultPrefix), BindingFlags.Static | BindingFlags.NonPublic)); harmony.Patch(getter, prefix: floatPrefix); break;
                            case "long": var longPrefix = new HarmonyMethod(typeof(ResultPatcher).GetMethod(nameof(LongResultPrefix), BindingFlags.Static | BindingFlags.NonPublic)); harmony.Patch(getter, prefix: longPrefix); break;
                            case "short": var shortPrefix = new HarmonyMethod(typeof(ResultPatcher).GetMethod(nameof(ShortResultPrefix), BindingFlags.Static | BindingFlags.NonPublic)); harmony.Patch(getter, prefix: shortPrefix); break;
                            case "double": var doublePrefix = new HarmonyMethod(typeof(ResultPatcher).GetMethod(nameof(DoubleResultPrefix), BindingFlags.Static | BindingFlags.NonPublic)); harmony.Patch(getter, prefix: doublePrefix); break;
                            default: return;
                        }
                    }
                    else
                    {
                        switch (numType)
                        {
                            case "int": var postfix = new HarmonyMethod(typeof(ResultPatcher).GetMethod(nameof(IntResultPostfix), BindingFlags.Static | BindingFlags.NonPublic)); harmony.Patch(getter, postfix: postfix); break;
                            case "float": var floatPostfix = new HarmonyMethod(typeof(ResultPatcher).GetMethod(nameof(FloatResultPostfix), BindingFlags.Static | BindingFlags.NonPublic)); harmony.Patch(getter, postfix: floatPostfix); break;
                            case "long": var longPostfix = new HarmonyMethod(typeof(ResultPatcher).GetMethod(nameof(LongResultPostfix), BindingFlags.Static | BindingFlags.NonPublic)); harmony.Patch(getter, postfix: longPostfix); break;
                            case "short": var shortPostfix = new HarmonyMethod(typeof(ResultPatcher).GetMethod(nameof(ShortResultPostfix), BindingFlags.Static | BindingFlags.NonPublic)); harmony.Patch(getter, postfix: shortPostfix); break;
                            case "double": var doublePostfix = new HarmonyMethod(typeof(ResultPatcher).GetMethod(nameof(DoubleResultPostfix), BindingFlags.Static | BindingFlags.NonPublic)); harmony.Patch(getter, postfix: doublePostfix); break;
                            default: return;
                        }
                    }
                    fullGetterList += $"{typeOf}.{prop.Name} ({numType}), \n";
                    resultsPatched++;
                }
                catch (Exception e)
                {
                    Log.Error($"[DayStretch]-(ResultPatch) Failed patching getter {typeOf}.{prop.Name}: {e}");
                }
            }
            return;
        }
        else
        {
            foreach (var method in type.GetMethods(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic))
            {

                if (method.IsAbstract || method.IsGenericMethodDefinition) continue;
                if (!string.IsNullOrEmpty(name) && method.Name != name) continue;
                try
                {
                    if (isPrefix)
                    {
                        switch (numType)
                        {
                            case "int": var prefix = new HarmonyMethod(typeof(ResultPatcher).GetMethod(nameof(IntResultPrefix), BindingFlags.Static | BindingFlags.NonPublic)); harmony.Patch(method, prefix: prefix); break;
                            case "float": var floatPrefix = new HarmonyMethod(typeof(ResultPatcher).GetMethod(nameof(FloatResultPrefix), BindingFlags.Static | BindingFlags.NonPublic)); harmony.Patch(method, prefix: floatPrefix); break;
                            case "long": var longPrefix = new HarmonyMethod(typeof(ResultPatcher).GetMethod(nameof(LongResultPrefix), BindingFlags.Static | BindingFlags.NonPublic)); harmony.Patch(method, prefix: longPrefix); break;
                            case "short": var shortPrefix = new HarmonyMethod(typeof(ResultPatcher).GetMethod(nameof(ShortResultPrefix), BindingFlags.Static | BindingFlags.NonPublic)); harmony.Patch(method, prefix: shortPrefix); break;
                            case "double": var doublePrefix = new HarmonyMethod(typeof(ResultPatcher).GetMethod(nameof(DoubleResultPrefix), BindingFlags.Static | BindingFlags.NonPublic)); harmony.Patch(method, prefix: doublePrefix); break;
                            default: return;
                        }
                    }
                    else
                    {
                        switch (numType)
                        {
                            case "int": var postfix = new HarmonyMethod(typeof(ResultPatcher).GetMethod(nameof(IntResultPostfix), BindingFlags.Static | BindingFlags.NonPublic)); harmony.Patch(method, postfix: postfix); break;
                            case "float": var floatPostfix = new HarmonyMethod(typeof(ResultPatcher).GetMethod(nameof(FloatResultPostfix), BindingFlags.Static | BindingFlags.NonPublic)); harmony.Patch(method, postfix: floatPostfix); break;
                            case "long": var longPostfix = new HarmonyMethod(typeof(ResultPatcher).GetMethod(nameof(LongResultPostfix), BindingFlags.Static | BindingFlags.NonPublic)); harmony.Patch(method, postfix: longPostfix); break;
                            case "short": var shortPostfix = new HarmonyMethod(typeof(ResultPatcher).GetMethod(nameof(ShortResultPostfix), BindingFlags.Static | BindingFlags.NonPublic)); harmony.Patch(method, postfix: shortPostfix); break;
                            case "double": var doublePostfix = new HarmonyMethod(typeof(ResultPatcher).GetMethod(nameof(DoubleResultPostfix), BindingFlags.Static | BindingFlags.NonPublic)); harmony.Patch(method, postfix: doublePostfix); break;
                            default: return;
                        }
                    }
                    fullList += $"{typeOf}.{method.Name} ({numType}), \n";
                    resultsPatched++;
                }
                catch (Exception e)
                {
                    Log.Error($"[DayStretch]-(ResultPatch) {e} Result not found.");
                }
            }
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
    static void IntResultPostfix(ref int __result, MethodBase __originalMethod)
    {
        bool currentReverse = ReverseCheck(__originalMethod); // i think its a pretty neat way to do it
        if (currentReverse) __result = Mathf.RoundToInt(__result / Settings.Instance.TimeMultiplier);
        else __result = Mathf.RoundToInt(__result * Settings.Instance.TimeMultiplier);
    }
    static void FloatResultPostfix(ref float __result, MethodBase __originalMethod)
    {
        bool currentReverse = ReverseCheck(__originalMethod);
        if (currentReverse) __result /= Settings.Instance.TimeMultiplier;
        else __result *= Settings.Instance.TimeMultiplier;
    }
    static void LongResultPostfix(ref long __result, MethodBase __originalMethod)
    {
        bool currentReverse = ReverseCheck(__originalMethod);
        if (currentReverse) __result = (long)(__result / Settings.Instance.TimeMultiplier);
        else __result = (long)(__result * Settings.Instance.TimeMultiplier);
    }
    static void DoubleResultPostfix(ref double __result, MethodBase __originalMethod)
    {
        bool currentReverse = ReverseCheck(__originalMethod);
        if (currentReverse) __result = (double)(__result / Settings.Instance.TimeMultiplier);
        else __result = (double)(__result * Settings.Instance.TimeMultiplier);
    }
    static void ShortResultPostfix(ref short __result, MethodBase __originalMethod)
    {
        bool currentReverse = ReverseCheck(__originalMethod); 
        if (currentReverse) __result = (short)(__result / Settings.Instance.TimeMultiplier);
        else __result = (short)(__result * Settings.Instance.TimeMultiplier);
    }




    static bool IntResultPrefix(ref int __result, MethodBase __originalMethod)
    {
        bool currentReverse = ReverseCheck(__originalMethod); // i think its a pretty neat way to do it
        if (currentReverse) __result = Mathf.RoundToInt(__result / Settings.Instance.TimeMultiplier);
        else __result = Mathf.RoundToInt(__result * Settings.Instance.TimeMultiplier);
        return false;
    }
    static bool FloatResultPrefix(ref float __result, MethodBase __originalMethod)
    {
        bool currentReverse = ReverseCheck(__originalMethod);
        if (currentReverse) __result /= Settings.Instance.TimeMultiplier;
        else __result *= Settings.Instance.TimeMultiplier;
        return false;
    }
    static bool LongResultPrefix(ref long __result, MethodBase __originalMethod)
    {
        bool currentReverse = ReverseCheck(__originalMethod);
        if (currentReverse) __result = (long)(__result / Settings.Instance.TimeMultiplier);
        else __result = (long)(__result * Settings.Instance.TimeMultiplier);
        return false;
    }
    static bool DoubleResultPrefix(ref double __result, MethodBase __originalMethod)
    {
        bool currentReverse = ReverseCheck(__originalMethod);
        if (currentReverse) __result = (double)(__result / Settings.Instance.TimeMultiplier);
        else __result = (double)(__result * Settings.Instance.TimeMultiplier);
        return false;
    }
    static bool ShortResultPrefix(ref short __result, MethodBase __originalMethod)
    {
        bool currentReverse = ReverseCheck(__originalMethod);
        if (currentReverse) __result = (short)(__result / Settings.Instance.TimeMultiplier);
        else __result = (short)(__result * Settings.Instance.TimeMultiplier);
        return false;
    }





}