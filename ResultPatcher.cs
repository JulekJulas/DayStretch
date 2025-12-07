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

public class ResultPatchDef : Def
{
    public string typeOf;
    public string name;
    public bool isInt;
    public bool reverse;
    public bool isGetter;
} // mostly just a heavily edited copy of Advanced patcher, i did want to add this functionality to it but honestly, this will be simpler and maybe more performant
// note: it was much simpler and its much easier for the user to use
[StaticConstructorOnStartup]
public static class ResultPatcher
{
    static int resultsPatched;
    static bool logShown = false;
    static ResultPatcher()
    {
        foreach (ResultPatchDef def in DefDatabase<ResultPatchDef>.AllDefsListForReading)
        {
            ResultDefPatcher(def.typeOf, def.name, def.isInt, def.reverse, def.isGetter);
        }
        if (!logShown)
        {
            logShown = true;
            Log.Message($"[DayStretch]-(ResultPatch) Patched {resultsPatched} results");
        }
    }

    static void ResultDefPatcher(string typeOf, string name, bool isInt, bool reverse, bool isGetter)
    {


        // making the string a type so .GetMethods doesnt scream
        Type type = GenTypes.GetTypeInAnyAssembly(typeOf);
        // i wanted to include this in the top code but i dont think i can?
        // correct me if i'm wrong though
        if (type == null)
        {
            Log.Error($"[DayStretch]-(ResultPatch) Type '{typeOf}' not found in loaded assemblies; skipping.");
            return;
        }


        var harmony = new Harmony("com.julekjulas.resultpatch");

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
                        if (reverse)
                        {
                            var postfix = new HarmonyMethod(typeof(ResultPatcher).GetMethod(isInt ? nameof(ReverseIntResultPostfix) : nameof(ReverseFloatResultPostfix), BindingFlags.Static | BindingFlags.NonPublic));
                            harmony.Patch(getter, postfix: postfix);
                        }
                        else
                        {
                            var postfix = new HarmonyMethod(typeof(ResultPatcher).GetMethod(isInt ? nameof(IntResultPostfix) : nameof(FloatResultPostfix), BindingFlags.Static | BindingFlags.NonPublic));
                            harmony.Patch(getter, postfix: postfix);
                        }
                        resultsPatched++;
                        Log.Message($"[DayStretch]-(ResultPatch) Patched getter {typeOf}.{prop.Name}");
                    }
                    catch (Exception e)
                    {
                        Log.Error($"[DayStretch]-(ResultPatch) Failed patching getter {typeOf}.{prop.Name}: {e}");
                    }
                }
                return;
            }
            if (method.IsAbstract || method.IsGenericMethodDefinition) continue;
            if (!string.IsNullOrEmpty(name) && method.Name != name) continue;
            try
            {
                if (reverse)
                {
                    var postfix = new HarmonyMethod(typeof(ResultPatcher).GetMethod(isInt ? nameof(ReverseIntResultPostfix) : nameof(ReverseFloatResultPostfix), BindingFlags.Static | BindingFlags.NonPublic));
                    harmony.Patch(method, postfix: postfix);
                }
                else
                {
                    var postfix = new HarmonyMethod(typeof(ResultPatcher).GetMethod(isInt ? nameof(IntResultPostfix) : nameof(FloatResultPostfix), BindingFlags.Static | BindingFlags.NonPublic));
                    harmony.Patch(method, postfix: postfix);
                }
                resultsPatched++;
                Log.Message($"Patched {typeOf}");
            }
            catch (Exception e)
            {
                Log.Error($"[DayStretch]-(ResultPatch) {e} Result not found.");
            }
        }
    }
    static void IntResultPostfix(ref int __result)
    {
        __result = Mathf.RoundToInt(__result * Settings.Instance.TimeMultiplier);
    }
    static void ReverseIntResultPostfix(ref int __result)
    {
        __result = Mathf.RoundToInt(__result * (1f / Settings.Instance.TimeMultiplier));
    }
    static void FloatResultPostfix(ref float __result)
    {
        __result *= Settings.Instance.TimeMultiplier;
    }
    static void ReverseFloatResultPostfix(ref float __result)
    {
        __result /= Settings.Instance.TimeMultiplier;
    }
}