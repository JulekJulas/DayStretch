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
    public string typeOf;
    public string name;
    public string type;
    public bool reverse;
    public bool isGetter;
} 
[StaticConstructorOnStartup]
public static class ResultPatcher
{
    static string fullList = "[DayStretch]-(ResultPatch)\nResults Patched:\n";
    static string fullGetterList = "[DayStretch]-(ResultPatch)\nGetter Results Patched:\n";
    static int resultsPatched;

    static bool currentReverse = false;

    static bool logShown = false;
    static ResultPatcher()
    {
        foreach (ResultPatchDef def in DefDatabase<ResultPatchDef>.AllDefsListForReading)
        {
            ResultDefPatcher(def.typeOf, def.name, def.type, def.reverse, def.isGetter);
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

    static void ResultDefPatcher(string typeOf, string name, string numType, bool reverse, bool isGetter)
    {
        Type type = GenTypes.GetTypeInAnyAssembly(typeOf);
        if (type == null)
        {
            Log.Error($"[DayStretch]-(ResultPatch) Type '{typeOf}' not found in loaded assemblies; skipping.");
            return;
        }
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
                            var postfix = new HarmonyMethod(typeof(ResultPatcher).GetMethod(nameof(IntResultPostfix), BindingFlags.Static | BindingFlags.NonPublic)); harmony.Patch(getter, postfix: postfix);
                            break;
                        case "float":
                            var floatPostfix = new HarmonyMethod(typeof(ResultPatcher).GetMethod(nameof(FloatResultPostfix), BindingFlags.Static | BindingFlags.NonPublic)); harmony.Patch(getter, postfix: floatPostfix);
                            break;
                        case "long":
                            var longPostfix = new HarmonyMethod(typeof(ResultPatcher).GetMethod(nameof(LongResultPostfix), BindingFlags.Static | BindingFlags.NonPublic)); harmony.Patch(getter, postfix: longPostfix);
                            break;
                        case "short":
                            var shortPostfix = new HarmonyMethod(typeof(ResultPatcher).GetMethod(nameof(ShortResultPostfix), BindingFlags.Static | BindingFlags.NonPublic)); harmony.Patch(getter, postfix: shortPostfix);
                            break;
                        case "double":
                            var doublePostfix = new HarmonyMethod(typeof(ResultPatcher).GetMethod(nameof(DoubleResultPostfix), BindingFlags.Static | BindingFlags.NonPublic)); harmony.Patch(getter, postfix: doublePostfix);
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
                            var postfix = new HarmonyMethod(typeof(ResultPatcher).GetMethod(nameof(IntResultPostfix), BindingFlags.Static | BindingFlags.NonPublic)); harmony.Patch(method, postfix: postfix);
                            break;
                        case "float":
                            var floatPostfix = new HarmonyMethod(typeof(ResultPatcher).GetMethod(nameof(FloatResultPostfix), BindingFlags.Static | BindingFlags.NonPublic)); harmony.Patch(method, postfix: floatPostfix);
                            break;
                        case "long":
                            var longPostfix = new HarmonyMethod(typeof(ResultPatcher).GetMethod(nameof(LongResultPostfix), BindingFlags.Static | BindingFlags.NonPublic)); harmony.Patch(method, postfix: longPostfix);
                            break;
                        case "short":
                            var shortPostfix = new HarmonyMethod(typeof(ResultPatcher).GetMethod(nameof(ShortResultPostfix), BindingFlags.Static | BindingFlags.NonPublic)); harmony.Patch(method, postfix: shortPostfix);
                            break;
                        case "double":
                            var doublePostfix = new HarmonyMethod(typeof(ResultPatcher).GetMethod(nameof(DoubleResultPostfix), BindingFlags.Static | BindingFlags.NonPublic)); harmony.Patch(method, postfix: doublePostfix);
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
    static void IntResultPostfix(ref int __result)
    {
        if (currentReverse) __result = Mathf.RoundToInt(__result * (1f / Settings.Instance.TimeMultiplier));
        else __result = Mathf.RoundToInt(__result * Settings.Instance.TimeMultiplier);
    }
    static void FloatResultPostfix(ref float __result)
    {
        if (currentReverse) __result /= Settings.Instance.TimeMultiplier;
        else __result *= Settings.Instance.TimeMultiplier;
    }
    static void LongResultPostfix(ref long __result)
    {
        if (currentReverse) __result = (long)(__result * (1f / Settings.Instance.TimeMultiplier));
        else __result = (long)(__result * Settings.Instance.TimeMultiplier);
    }
    static void DoubleResultPostfix(ref double __result)
    {
        if (currentReverse) __result = (double)(__result * (1f / Settings.Instance.TimeMultiplier));
        else __result = (double)(__result * Settings.Instance.TimeMultiplier);
    }
    static void ShortResultPostfix(ref short __result)
    {
        if (currentReverse) __result = (short)(__result * (1f / Settings.Instance.TimeMultiplier));
        else __result = (short)(__result * Settings.Instance.TimeMultiplier);
    }
}