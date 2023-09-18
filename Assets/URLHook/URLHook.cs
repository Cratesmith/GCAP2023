using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using UnityEditor;
using UnityEngine;

namespace URLHook
{
    public static class Patcher
    {
        [InitializeOnLoadMethod]
        static void InitializeOnLoad()
        {
            var harmony = new Harmony("com.cratesmith.urlhook");
            harmony.PatchAll(); // apply all HarmonyPatches in this assembly 
        }
    }

    [HarmonyPatch]
    public static class OpenURLPatch
    {
        public static List<Func<string, bool>> Listeners { get; } = new List<Func<string, bool>>();   

        static bool Prefix(ref string url)
        {
            foreach (var listener in Listeners)
            {
                if (listener?.Invoke(url) == true)
                    return false; // don't run original method
            }
            return true; // run original method
        }
        
        static MethodBase TargetMethod()
        {
            var typeName = "UnityEditor.PackageManager.UI.PackageManagerWindow";
            var methodName = "OpenURL";
            return AccessTools.Method($"{typeName}:{methodName}");
        }
    }
}