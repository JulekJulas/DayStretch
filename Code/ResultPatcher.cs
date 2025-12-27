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
    public bool isReverse;
    public bool isGetter;
    public bool isDelta;
} 
[StaticConstructorOnStartup]
public static class ResultPatcher
{
    static string fullList = "[DayStretch]-(ResultPatch)\nResults Patched:\n";
    static string fullGetterList = "[DayStretch]-(ResultPatch)\nGetter Results Patched:\n";
    static int resultsPatched;
    public static Dictionary<string, bool> keyReverse = new Dictionary<string, bool>();


    static bool logShown = false;
    static ResultPatcher()
    {
        foreach (ResultPatchDef def in DefDatabase<ResultPatchDef>.AllDefsListForReading)
        {
            ResultDefPatcher(def.defName, def.namespaceOf, def.typeOf, def.name, def.type, def.isReverse, def.isGetter, def.isDelta);
        }
        if (!logShown) logShown = true;
        {
            fullList += "\n\n\n\n";
            fullGetterList += "\n\n\n\n";
            Log.Message(fullList);
            Log.Message(fullGetterList);

            Log.Message($"[DayStretch]-(ResultPatch) Patched {resultsPatched} results");
        }
    }

    static void ResultDefPatcher(string defName, string namespaceOf, string typeOf, string name, string numType, bool reverse, bool isGetter, bool isDelta)
    {
        Type type = GenTypes.GetTypeInAnyAssembly($"{namespaceOf}.{typeOf}");
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
                    switch (numType)
                    {
                        case "int":
                            var postfix = (isDelta ? (new HarmonyMethod(typeof(ResultPatcher).GetMethod(nameof(IntDeltaPostfix), BindingFlags.Static | BindingFlags.NonPublic))) : (new HarmonyMethod(typeof(ResultPatcher).GetMethod(nameof(IntResultPostfix), BindingFlags.Static | BindingFlags.NonPublic))));
                            harmony.Patch(getter, postfix: postfix);
                            break;
                        case "float":
                            var floatPostfix = (isDelta ? (new HarmonyMethod(typeof(ResultPatcher).GetMethod(nameof(FloatDeltaPostfix), BindingFlags.Static | BindingFlags.NonPublic))) : (new HarmonyMethod(typeof(ResultPatcher).GetMethod(nameof(FloatResultPostfix), BindingFlags.Static | BindingFlags.NonPublic))));
                            harmony.Patch(getter, postfix: floatPostfix);
                            break;
                        case "long":
                            var longPostfix = (isDelta ? (new HarmonyMethod(typeof(ResultPatcher).GetMethod(nameof(LongDeltaPostfix), BindingFlags.Static | BindingFlags.NonPublic))) : (new HarmonyMethod(typeof(ResultPatcher).GetMethod(nameof(LongResultPostfix), BindingFlags.Static | BindingFlags.NonPublic))));
                            harmony.Patch(getter, postfix: longPostfix);
                            break;
                        case "short":
                            var shortPostfix = (isDelta ? (new HarmonyMethod(typeof(ResultPatcher).GetMethod(nameof(ShortDeltaPostfix), BindingFlags.Static | BindingFlags.NonPublic))) : (new HarmonyMethod(typeof(ResultPatcher).GetMethod(nameof(ShortResultPostfix), BindingFlags.Static | BindingFlags.NonPublic))));
                            harmony.Patch(getter, postfix: shortPostfix);
                            break;
                        case "double":
                            var doublePostfix = (isDelta ? (new HarmonyMethod(typeof(ResultPatcher).GetMethod(nameof(DoubleDeltaPostfix), BindingFlags.Static | BindingFlags.NonPublic))) : (new HarmonyMethod(typeof(ResultPatcher).GetMethod(nameof(DoubleResultPostfix), BindingFlags.Static | BindingFlags.NonPublic))));
                            harmony.Patch(getter, postfix: doublePostfix);
                            break;
                        default:
                            return;
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
                    switch (numType)
                    {
                        case "int":
                            var postfix = (isDelta ? (new HarmonyMethod(typeof(ResultPatcher).GetMethod(nameof(IntDeltaPostfix), BindingFlags.Static | BindingFlags.NonPublic))) : (new HarmonyMethod(typeof(ResultPatcher).GetMethod(nameof(IntResultPostfix), BindingFlags.Static | BindingFlags.NonPublic))));
                            harmony.Patch(method, postfix: postfix);
                            break;
                        case "float":
                            var floatPostfix = (isDelta ? (new HarmonyMethod(typeof(ResultPatcher).GetMethod(nameof(FloatDeltaPostfix), BindingFlags.Static | BindingFlags.NonPublic))) : (new HarmonyMethod(typeof(ResultPatcher).GetMethod(nameof(FloatResultPostfix), BindingFlags.Static | BindingFlags.NonPublic))));
                            harmony.Patch(method, postfix: floatPostfix);
                            break;
                        case "long":
                            var longPostfix = (isDelta ? (new HarmonyMethod(typeof(ResultPatcher).GetMethod(nameof(LongDeltaPostfix), BindingFlags.Static | BindingFlags.NonPublic))) : (new HarmonyMethod(typeof(ResultPatcher).GetMethod(nameof(LongResultPostfix), BindingFlags.Static | BindingFlags.NonPublic))));
                            harmony.Patch(method, postfix: longPostfix);
                            break;
                        case "short":
                            var shortPostfix = (isDelta ? (new HarmonyMethod(typeof(ResultPatcher).GetMethod(nameof(ShortDeltaPostfix), BindingFlags.Static | BindingFlags.NonPublic))) : (new HarmonyMethod(typeof(ResultPatcher).GetMethod(nameof(ShortResultPostfix), BindingFlags.Static | BindingFlags.NonPublic))));
                            harmony.Patch(method, postfix: shortPostfix);
                            break;
                        case "double":
                            var doublePostfix = (isDelta ? (new HarmonyMethod(typeof(ResultPatcher).GetMethod(nameof(DoubleDeltaPostfix), BindingFlags.Static | BindingFlags.NonPublic))) : (new HarmonyMethod(typeof(ResultPatcher).GetMethod(nameof(DoubleResultPostfix), BindingFlags.Static | BindingFlags.NonPublic))));
                            harmony.Patch(method, postfix: doublePostfix);
                            break;
                        default:
                            return;
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
        if (currentReverse) __result = Mathf.RoundToInt(__result * (1f / Settings.Instance.TimeMultiplier));
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
        if (currentReverse) __result = (long)(__result * (1f / Settings.Instance.TimeMultiplier));
        else __result = (long)(__result * Settings.Instance.TimeMultiplier);
    }
    static void DoubleResultPostfix(ref double __result, MethodBase __originalMethod)
    {
        bool currentReverse = ReverseCheck(__originalMethod);
        if (currentReverse) __result = (double)(__result * (1f / Settings.Instance.TimeMultiplier));
        else __result = (double)(__result * Settings.Instance.TimeMultiplier);
    }
    static void ShortResultPostfix(ref short __result, MethodBase __originalMethod)
    {
        bool currentReverse = ReverseCheck(__originalMethod); 
        if (currentReverse) __result = (short)(__result * (1f / Settings.Instance.TimeMultiplier));
        else __result = (short)(__result * Settings.Instance.TimeMultiplier);
    }


    // delta patches

    static void IntDeltaPostfix(ref int delta, MethodBase __originalMethod)
    {
        bool currentReverse = ReverseCheck(__originalMethod);
        if (currentReverse) delta = (int)(delta * (1f / Settings.Instance.TimeMultiplier));
        else delta = (int)(delta * Settings.Instance.TimeMultiplier);
    }
    static void FloatDeltaPostfix(ref float delta, MethodBase __originalMethod)
    {
        bool currentReverse = ReverseCheck(__originalMethod);
        if (currentReverse) delta /= Settings.Instance.TimeMultiplier;
        else delta *= Settings.Instance.TimeMultiplier;
    }
    static void LongDeltaPostfix(ref long delta, MethodBase __originalMethod)
    {
        bool currentReverse = ReverseCheck(__originalMethod);
        if (currentReverse) delta = (long)(delta * (1f / Settings.Instance.TimeMultiplier));
        else delta = (long)(delta * Settings.Instance.TimeMultiplier);
    }
    static void DoubleDeltaPostfix(ref double delta, MethodBase __originalMethod)
    {
        bool currentReverse = ReverseCheck(__originalMethod);
        if (currentReverse) delta = (double)(delta * (1f / Settings.Instance.TimeMultiplier));
        else delta = (double)(delta * Settings.Instance.TimeMultiplier);
    }
    static void ShortDeltaPostfix(ref short delta, MethodBase __originalMethod)
    {
        bool currentReverse = ReverseCheck(__originalMethod);
        if (currentReverse) delta = (short)(delta * (1f / Settings.Instance.TimeMultiplier));
        else delta = (short)(delta * Settings.Instance.TimeMultiplier);
    }


}