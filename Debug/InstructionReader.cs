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

public class InstructionReadDef : Def
{
    public string namespaceOf;
    public string typeOf;
    public string name;
}

[StaticConstructorOnStartup]
public static class InstructionReader
{

    static bool logShown = false;

    public static int amountofWrongValues = 0;
    static string fullList = "[DayStretch]-(InstructionReader)\nInstructions:\n";

    static InstructionReader()
    {
        foreach (InstructionReadDef def in DefDatabase<InstructionReadDef>.AllDefsListForReading)
        {
            InstructionDefReader(def.defName, def.namespaceOf, def.typeOf, def.name);
        }
        // makes so the log only shows the amount of numbers patched exactly one time
        if (!logShown)
        {
            logShown = true;

            Log.Message(fullList + "\n\n\n\n");
        }
    }

    static void InstructionDefReader(string defName, string namespaceOf, string typeOf, string name)
    {
        // really compact checks for null values
        if (namespaceOf == null) { Log.Error($"[DayStretch]-(InstructionReader) namespaceOf in {defName} is not filled in; skipping."); return; }
        if (typeOf == null) { Log.Error($"[DayStretch]-(InstructionReader) typeOf in {defName} is not filled in; skipping."); return; }
        if (name == null) { Log.Error($"[DayStretch]-(InstructionReader) name in {defName} is not filled in; skipping."); return; }
        // my habit of overcompacting things will be the death of me one day


        Type type = GenTypes.GetTypeInAnyAssembly($"{namespaceOf}.{typeOf}");



        if (type == null)
        {
            Log.Error($"[DayStretch]-(InstructionReader) Type '{typeOf}' not found in namespace '{namespaceOf}'; skipping.");
            return;
        }

        var harmony = new Harmony("com.julekjulas.instructionreader");
        foreach (var method in type.GetMethods(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic))
        {
            if (method.IsAbstract || method.IsGenericMethodDefinition) continue;
            if (!string.IsNullOrEmpty(name) && method.Name != name) continue;
            var transpiler = new HarmonyMethod(typeof(InstructionReader).GetMethod(nameof(ReadInstructions), BindingFlags.Static | BindingFlags.NonPublic)); harmony.Patch(method, transpiler: transpiler);
        }
    }

    static IEnumerable<CodeInstruction> ReadInstructions(IEnumerable<CodeInstruction> instructions, MethodBase type)
    {
        fullList += $"Current Method: {type.DeclaringType}.{type.Name}\n";
        foreach (var instr in instructions)
        {
            fullList += instr.ToString() + "\n";
            yield return instr;
        }
        fullList += "\n\n\n\n\n\n";
    }
}