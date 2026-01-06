using DayStretched;
using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;


public class StringPatchDef : Def
{
    public string namespaceOf;
    public string typeOf;
    public string name;
    public string originalText;
    public string newText;
}

[StaticConstructorOnStartup]
public static class StringPatcher
{

    static bool logShown = false;
    public static Dictionary<string, string[]> scaledStrings = new Dictionary<string, string[]>();
    public static Dictionary<string, string[]> wrongStrings = new Dictionary<string, string[]>();

    public static int amountofWrongStrings = 0;

    static int stringsPatched = 0;
    static string fullList = "Methods Patched:\n";
    public static string loggerList = "";

    static StringPatcher()
    {
        foreach (StringPatchDef def in DefDatabase<StringPatchDef>.AllDefsListForReading)
        {
            StringDefPatcher(def.defName, def.namespaceOf, def.typeOf, def.name, def.originalText, def.newText);
        }
        // makes so the log only shows the amount of numbers patched exactly one time
        if (!logShown)
        {
            logShown = true;
            if (wrongStrings.Count > 0)
            {
                foreach (string key in wrongStrings.Keys)
                {
                    Log.Error($"[DayStretch]-(StringPatch) String {wrongStrings[key][0]} not found in {key}");
                }
            }
            loggerList += $"String Patcher:\nNumber of strings patched: {stringsPatched}\n\n{fullList}";
        }
    }
    static void StringDefPatcher(string defName, string namespaceOf, string typeOf, string name, string originalText, string newText)
    {
        if (namespaceOf == null) { Log.Error($"[DayStretch]-(StringPatch) namespaceOf in {defName} is not filled in; skipping."); return; }
        if (typeOf == null) { Log.Error($"[DayStretch]-(StringPatch) typeOf in {defName} is not filled in; skipping."); return; }
        if (name == null) { Log.Error($"[DayStretch]-(StringPatch) name in {defName} is not filled in; skipping."); return; }
        if (originalText == null) { Log.Error($"[DayStretch]-(StringPatch) original text in {defName} is not filled in; skipping."); return; }
        if (newText == null) { Log.Error($"[DayStretch]-(StringPatch) new text in {defName} is not filled in; skipping."); return; }

        float TimeMultiplier = Settings.Instance.TimeMultiplier;

        scaledStrings.Add(namespaceOf + "." + typeOf + name, new string[] { originalText, newText });



        Type type = GenTypes.GetTypeInAnyAssembly($"{namespaceOf}.{typeOf}");



        // extra checks

        if (type == null)
        {
            Log.Error($"[DayStretch]-(StringPatch) Type '{typeOf}' not found in namespace '{namespaceOf}'; skipping.");
            return;
        }
        if (type.Method(name) == null)
        {
            Log.Error($"[DayStretch]-(StringPatch) Method '{name}' not found in class '{typeOf}' in namespace '{namespaceOf}'; skipping.");
            return;
        }

        var harmony = new Harmony("com.julekjulas.stringpatch");

        foreach (var method in type.GetMethods(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic))
        {
            if (method.IsAbstract || method.IsGenericMethodDefinition) continue;
            if (!string.IsNullOrEmpty(name) && method.Name != name) continue;
            try
            {
                var transpiler = new HarmonyMethod(typeof(StringPatcher).GetMethod(nameof(TranspileString), BindingFlags.Static | BindingFlags.NonPublic)); harmony.Patch(method, transpiler: transpiler);
                fullList += $"{typeOf}.{method.Name}, \n";
            }
            catch (Exception e)
            {
                Log.Error($"[DayStretch]-(StringPatch) Failed Patching {namespaceOf}.{typeOf}.{name} {e}");
                return;
            }
        }
    }

    static IEnumerable<CodeInstruction> TranspileString(IEnumerable<CodeInstruction> instructions, MethodBase type)
    {
        string typeOf = type.DeclaringType.ToString();
        string name = type.Name.ToString();
        string dictKey = typeOf + name;
        scaledStrings.TryGetValue(dictKey, out string[] text);
        string originalText = text[0];
        string newText = text[1];

        bool stringPatched = false;

        foreach (var instr in instructions)
        {
            if ((instr.opcode == OpCodes.Ldstr) && instr.operand is string localText)
            {
                if (localText == originalText) { instr.operand = newText; stringPatched = true; stringsPatched++; }
            }
            yield return instr;
        }
        if (stringPatched == false)
        {
            wrongStrings.Add(dictKey, new string[] { originalText, newText });
        }
    }
}
