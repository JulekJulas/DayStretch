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

public class DeltaPatchDef : Def
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
public static class DeltaPatcher
{
    static string fullList = "Method Deltas Patched:\n";
    static string fullGetterList = "Getter Deltas Patched:\n";
    public static string loggerList = "";
    static int deltasPatched;
    public static Dictionary<string, bool> keyReverse = new Dictionary<string, bool>();


    static bool logShown = false;
    static DeltaPatcher()
    {
        foreach (DeltaPatchDef def in DefDatabase<DeltaPatchDef>.AllDefsListForReading)
        {
            DeltaDefPatcher(def.defName, def.namespaceOf, def.typeOf, def.name, def.type, def.isReverse, def.isGetter, def.isPrefix);
        }
        if (!logShown) logShown = true;
        {
            loggerList += $"Delta Patcher:\nNumber of deltas patched: {deltasPatched}\n\n{fullList}\n\n{fullGetterList}";
        }
    }

    static void DeltaDefPatcher(string defName, string namespaceOf, string typeOf, string name, string numType, bool reverse, bool isGetter, bool isPrefix)
    {
        if (namespaceOf == null) { Log.Error($"[DayStretch]-(DeltaPatch) namespaceOf in {defName} is not filled in; skipping."); return; }
        if (typeOf == null) { Log.Error($"[DayStretch]-(DeltaPatch) typeOf in {defName} is not filled in; skipping."); return; }
        if (name == null) { Log.Error($"[DayStretch]-(DeltaPatch) name in {defName} is not filled in; skipping."); return; }
        if (numType == null) { Log.Error($"[DayStretch]-(DeltaPatch) type in {defName} is not filled in; skipping."); return; }


        Type type = GenTypes.GetTypeInAnyAssembly($"{namespaceOf}.{typeOf}");
        if (type == null)
        {
            Log.Error($"[DayStretch]-(DeltaPatch) Type '{typeOf}' not found in namespace '{namespaceOf}'; skipping.");
            return;
        }
        if (type.Method(name) == null && isGetter == false)
        {
            Log.Error($"[DayStretch]-(DeltaPatch) Method '{name}' not found in class '{typeOf}' in namespace '{namespaceOf}'; skipping.");
            return;
        }
        if (type.Property(name) == null && isGetter == true) // thanks vs autocomplete
        {
            Log.Error($"[DayStretch]-(DeltaPatch) Property '{name}' not found in class '{typeOf}' in namespace '{namespaceOf}'; skipping.");
            return;
        }
        keyReverse.Add(isGetter ? (namespaceOf + "." + typeOf + "get_" + name) : (namespaceOf + "." + typeOf + name), reverse);






        var harmony = new Harmony("com.julekjulas.deltapatch");
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
                            case "int": var prefix = new HarmonyMethod(typeof(DeltaPatcher).GetMethod(nameof(IntDeltaPrefix), BindingFlags.Static | BindingFlags.NonPublic)); harmony.Patch(getter, prefix: prefix); break;
                            case "float": var floatPrefix = new HarmonyMethod(typeof(DeltaPatcher).GetMethod(nameof(FloatDeltaPrefix), BindingFlags.Static | BindingFlags.NonPublic)); harmony.Patch(getter, prefix: floatPrefix); break;
                            case "long": var longPrefix = new HarmonyMethod(typeof(DeltaPatcher).GetMethod(nameof(LongDeltaPrefix), BindingFlags.Static | BindingFlags.NonPublic)); harmony.Patch(getter, prefix: longPrefix); break;
                            case "short": var shortPrefix = new HarmonyMethod(typeof(DeltaPatcher).GetMethod(nameof(ShortDeltaPrefix), BindingFlags.Static | BindingFlags.NonPublic)); harmony.Patch(getter, prefix: shortPrefix); break;
                            case "double": var doublePrefix = new HarmonyMethod(typeof(DeltaPatcher).GetMethod(nameof(DoubleDeltaPrefix), BindingFlags.Static | BindingFlags.NonPublic)); harmony.Patch(getter, prefix: doublePrefix); break;
                            default: return;
                        }
                    }
                    else
                    {
                        switch (numType)
                        {
                            case "int": var postfix = new HarmonyMethod(typeof(DeltaPatcher).GetMethod(nameof(IntDeltaPostfix), BindingFlags.Static | BindingFlags.NonPublic)); harmony.Patch(getter, postfix: postfix); break;
                            case "float": var floatPostfix = new HarmonyMethod(typeof(DeltaPatcher).GetMethod(nameof(FloatDeltaPostfix), BindingFlags.Static | BindingFlags.NonPublic)); harmony.Patch(getter, postfix: floatPostfix); break;
                            case "long": var longPostfix = new HarmonyMethod(typeof(DeltaPatcher).GetMethod(nameof(LongDeltaPostfix), BindingFlags.Static | BindingFlags.NonPublic)); harmony.Patch(getter, postfix: longPostfix); break;
                            case "short": var shortPostfix = new HarmonyMethod(typeof(DeltaPatcher).GetMethod(nameof(ShortDeltaPostfix), BindingFlags.Static | BindingFlags.NonPublic)); harmony.Patch(getter, postfix: shortPostfix); break;
                            case "double": var doublePostfix = new HarmonyMethod(typeof(DeltaPatcher).GetMethod(nameof(DoubleDeltaPostfix), BindingFlags.Static | BindingFlags.NonPublic)); harmony.Patch(getter, postfix: doublePostfix); break;
                            default: return;
                        }
                    }
                    fullGetterList += $"{typeOf}.{prop.Name} ({numType}), \n";
                    deltasPatched++;
                }
                catch (Exception e)
                {
                    Log.Error($"[DayStretch]-(DeltaPatch) Failed patching getter {typeOf}.{prop.Name}: {e}");
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
                            case "int": var prefix = new HarmonyMethod(typeof(DeltaPatcher).GetMethod(nameof(IntDeltaPrefix), BindingFlags.Static | BindingFlags.NonPublic)); harmony.Patch(method, postfix: prefix); break;
                            case "float": var floatPrefix = new HarmonyMethod(typeof(DeltaPatcher).GetMethod(nameof(FloatDeltaPrefix), BindingFlags.Static | BindingFlags.NonPublic)); harmony.Patch(method, prefix: floatPrefix); break;
                            case "long": var longPrefix = new HarmonyMethod(typeof(DeltaPatcher).GetMethod(nameof(LongDeltaPrefix), BindingFlags.Static | BindingFlags.NonPublic)); harmony.Patch(method, prefix: longPrefix); break;
                            case "short": var shortPrefix = new HarmonyMethod(typeof(DeltaPatcher).GetMethod(nameof(ShortDeltaPrefix), BindingFlags.Static | BindingFlags.NonPublic)); harmony.Patch(method, prefix: shortPrefix); break;
                            case "double": var doublePrefix = new HarmonyMethod(typeof(DeltaPatcher).GetMethod(nameof(DoubleDeltaPrefix), BindingFlags.Static | BindingFlags.NonPublic)); harmony.Patch(method, prefix: doublePrefix); break;
                            default: return;
                        }
                    }
                    else
                    {
                        switch (numType)
                        {
                            case "int": var postfix = new HarmonyMethod(typeof(DeltaPatcher).GetMethod(nameof(IntDeltaPostfix), BindingFlags.Static | BindingFlags.NonPublic)); harmony.Patch(method, postfix: postfix); break;
                            case "float": var floatPostfix = new HarmonyMethod(typeof(DeltaPatcher).GetMethod(nameof(FloatDeltaPostfix), BindingFlags.Static | BindingFlags.NonPublic)); harmony.Patch(method, postfix: floatPostfix); break;
                            case "long": var longPostfix = new HarmonyMethod(typeof(DeltaPatcher).GetMethod(nameof(LongDeltaPostfix), BindingFlags.Static | BindingFlags.NonPublic)); harmony.Patch(method, postfix: longPostfix); break;
                            case "short": var shortPostfix = new HarmonyMethod(typeof(DeltaPatcher).GetMethod(nameof(ShortDeltaPostfix), BindingFlags.Static | BindingFlags.NonPublic)); harmony.Patch(method, postfix: shortPostfix); break;
                            case "double": var doublePostfix = new HarmonyMethod(typeof(DeltaPatcher).GetMethod(nameof(DoubleDeltaPostfix), BindingFlags.Static | BindingFlags.NonPublic)); harmony.Patch(method, postfix: doublePostfix); break;
                            default: return;
                        }
                    }

                    fullList += $"{typeOf}.{method.Name} ({numType}), \n";
                    deltasPatched++;
                }
                catch (Exception e)
                {
                    Log.Error($"[DayStretch]-(DeltaPatch) {e} Delta not found.");
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
    static void IntDeltaPostfix(ref int delta, MethodBase __originalMethod)
    {
        bool currentReverse = ReverseCheck(__originalMethod); // i think its a pretty neat way to do it
        if (currentReverse) delta = Mathf.RoundToInt(delta / Settings.Instance.TimeMultiplier);
        else delta = Mathf.RoundToInt(delta * Settings.Instance.TimeMultiplier);
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
        if (currentReverse) delta = (long)(delta / Settings.Instance.TimeMultiplier);
        else delta = (long)(delta * Settings.Instance.TimeMultiplier);
    }
    static void DoubleDeltaPostfix(ref double delta, MethodBase __originalMethod)
    {
        bool currentReverse = ReverseCheck(__originalMethod);
        if (currentReverse) delta = (double)(delta / Settings.Instance.TimeMultiplier);
        else delta = (double)(delta * Settings.Instance.TimeMultiplier);
    }
    static void ShortDeltaPostfix(ref short delta, MethodBase __originalMethod)
    {
        bool currentReverse = ReverseCheck(__originalMethod);
        if (currentReverse) delta = (short)(delta / Settings.Instance.TimeMultiplier);
        else delta = (short)(delta * Settings.Instance.TimeMultiplier);
    }




    static void IntDeltaPrefix(ref int delta, MethodBase __originalMethod)
    {
        bool currentReverse = ReverseCheck(__originalMethod); // i think its a pretty neat way to do it
        if (currentReverse) delta = Mathf.RoundToInt(delta / Settings.Instance.TimeMultiplier);
        else delta = Mathf.RoundToInt(delta * Settings.Instance.TimeMultiplier);
    }
    static void FloatDeltaPrefix(ref float delta, MethodBase __originalMethod)
    {
        bool currentReverse = ReverseCheck(__originalMethod);
        if (currentReverse) delta /= Settings.Instance.TimeMultiplier;
        else delta *= Settings.Instance.TimeMultiplier;
    }
    static void LongDeltaPrefix(ref long delta, MethodBase __originalMethod)
    {
        bool currentReverse = ReverseCheck(__originalMethod);
        if (currentReverse) delta = (long)(delta / Settings.Instance.TimeMultiplier);
        else delta = (long)(delta * Settings.Instance.TimeMultiplier);
    }
    static void DoubleDeltaPrefix(ref double delta, MethodBase __originalMethod)
    {
        bool currentReverse = ReverseCheck(__originalMethod);
        if (currentReverse) delta = (double)(delta / Settings.Instance.TimeMultiplier);
        else delta = (double)(delta * Settings.Instance.TimeMultiplier);
    }
    static void ShortDeltaPrefix(ref short delta, MethodBase __originalMethod)
    {
        bool currentReverse = ReverseCheck(__originalMethod);
        if (currentReverse) delta = (short)(delta / Settings.Instance.TimeMultiplier);
        else delta = (short)(delta * Settings.Instance.TimeMultiplier);
    }





}