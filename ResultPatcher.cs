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
} // mostly just a heavily edited copy of Advanced patcher, i did want to add this functionality to it but honestly, this will be simpler and maybe more performant
// note: it was much simpler and its much easier for the user to use
[StaticConstructorOnStartup]
public static class ResultPatcher
{
    static bool currentReverse;
    static int resultsPatched;
    static bool logShown = false;
    static ResultPatcher()
    {
        foreach (ResultPatchDef def in DefDatabase<ResultPatchDef>.AllDefsListForReading)
        {
            ResultDefPatcher(def.typeOf, def.name, def.isInt, def.reverse);
        }
        if (!logShown)
        {
            logShown = true;
            Log.Message($"[DayStretch] Patched {resultsPatched} results");
        }
    }

    static void ResultDefPatcher(string typeOf, string name, bool isInt, bool reverse)
    {
        currentReverse = reverse;

        // making string a type so .GetMethods doesnt scream
        Type type = AppDomain.CurrentDomain.GetAssemblies().SelectMany(a =>
        {
            try
            {
                return a.GetTypes();
            }
            catch
            {
                return Type.EmptyTypes;
            }
        }).FirstOrDefault(t => t.FullName == typeOf || t.Name == typeOf);
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
            if (method.IsAbstract || method.IsGenericMethodDefinition) continue;
            if (!string.IsNullOrEmpty(name) && method.Name != name) continue;
            if (isInt == true)
            {
                try
                {
                    var postfix = new HarmonyMethod(typeof(ResultPatcher).GetMethod(nameof(IntResultPostfix), BindingFlags.Static | BindingFlags.NonPublic));
                    harmony.Patch(method, postfix: postfix);
                }
                catch (Exception)
                {
                    Log.Error($"[DayStretch] Result not found.");
                }
                resultsPatched++;

            }
            else
            {
                try
                {
                    var postfix = new HarmonyMethod(typeof(ResultPatcher).GetMethod(nameof(FloatResultPostfix), BindingFlags.Static | BindingFlags.NonPublic));
                    harmony.Patch(method, postfix: postfix);
                }
                catch (Exception)
                {
                    Log.Error($"[DayStretch] Result not found.");
                }
                resultsPatched++;
            }


        }

    }
    static void IntResultPostfix(ref int __result)
    {
        if (currentReverse == true)
        {
            __result = Mathf.RoundToInt(__result * (1f / Settings.Instance.TimeMultiplier));
        }
        else
        {
            __result = Mathf.RoundToInt(__result * Settings.Instance.TimeMultiplier);
        }
    }
    static void FloatResultPostfix(ref float __result)
    {
        if (currentReverse == true)
        {
            __result /= Settings.Instance.TimeMultiplier;
        }
        else
        {
            __result *= Settings.Instance.TimeMultiplier;
        }
    }
}