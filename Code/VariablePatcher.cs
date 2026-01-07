using DayStretched;
using HarmonyLib;
using Microsoft.Win32;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Xml.Linq;
using UnityEngine;
using UnityEngine.Analytics;
using Verse;
using static UnityEngine.GraphicsBuffer;

public class VariablePatchDef : Def
{
    public string namespaceOf;
    public string typeOf;
    public string type;
    public string name;
    // optional
    public bool isReverse;
}
[StaticConstructorOnStartup]
public static class VariablePatcher
{
    static string fullList = "Variable Patched:\n";
    public static string loggerList = "";
    static int variablesPatched;
    public static Dictionary<string, bool> keyReverse = new Dictionary<string, bool>();


    static bool logShown = false;
    static VariablePatcher()
    {
        foreach (VariablePatchDef def in DefDatabase<VariablePatchDef>.AllDefsListForReading)
        {
            VariableDefPatcher(def.defName, def.namespaceOf, def.typeOf, def.type, def.isReverse, def.name);
        }
        if (!logShown) logShown = true;
        {
            loggerList += $"Variable Patcher:\nNumber of variables patched: {variablesPatched}\n\n{fullList}";
        }
    }

    static void VariableDefPatcher(string defName, string namespaceOf, string typeOf, string numType, bool reverse, string name)
    {
        if (namespaceOf == null) { Log.Error($"[DayStretch]-(VariablePatch) namespaceOf in {defName} is not filled in; skipping."); return; }
        if (typeOf == null) { Log.Error($"[DayStretch]-(VariablePatch) typeOf in {defName} is not filled in; skipping."); return; }
        if (numType == null) { Log.Error($"[DayStretch]-(VariablePatch) type in {defName} is not filled in; skipping."); return; }


        Type type = GenTypes.GetTypeInAnyAssembly($"{namespaceOf}.{typeOf}");
        if (type == null)
        {
            Log.Error($"[DayStretch]-(VariablePatch) Type '{typeOf}' not found in namespace '{namespaceOf}'; skipping.");
            return;
        }
        keyReverse.Add(namespaceOf + "." + typeOf + name, reverse);

        var harmony = new Harmony("com.julekjulas.variablepatch");
        foreach (var field in type.GetFields(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic))
        {
            if (!string.IsNullOrEmpty(name) && field.Name != name) continue;
            object target = null;
            if (!field.IsStatic)
            {
                if (type == typeof(CameraMapConfig))
                    target = Find.CameraDriver?.config;
            }
            try
            {
                switch (numType)
                {
                    case "int": int intValue = (int)field.GetValue(target); intValue = reverse ? Mathf.RoundToInt(intValue / Settings.Instance.TimeMultiplier) : Mathf.RoundToInt(intValue * Settings.Instance.TimeMultiplier); field.SetValue(target, intValue); break;
                    case "float": float floatValue = (float)field.GetValue(target); floatValue = reverse ? (floatValue / Settings.Instance.TimeMultiplier) : (floatValue * Settings.Instance.TimeMultiplier); field.SetValue(target, floatValue); break;
                    case "long": long longValue = (long)field.GetValue(target); longValue = reverse ? (long)(longValue / Settings.Instance.TimeMultiplier) : (long)(longValue * Settings.Instance.TimeMultiplier); field.SetValue(target, longValue); break;
                    case "short": short shortValue = (short)field.GetValue(target); shortValue = reverse ? (short)(shortValue / Settings.Instance.TimeMultiplier) : (short)(shortValue * Settings.Instance.TimeMultiplier); field.SetValue(target, shortValue); break;
                    case "double": double doubleValue = (double)field.GetValue(target); doubleValue = reverse ? (double)(doubleValue / Settings.Instance.TimeMultiplier) : (double)(doubleValue * Settings.Instance.TimeMultiplier); field.SetValue(target, doubleValue); break;
                    default: return;
                }
                fullList += $"{typeOf}.{name} ({numType}), \n";
                variablesPatched++;
            }
            catch (Exception e)
            {
                Log.Error($"[DayStretch]-(VariablePatch) {e} Variable not found.");
            }
        }
    }
}