using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using UnityEditor;
using UnityEngine;

namespace InsepctorFilter
{
	public static class Patcher
	{
		static Harmony s_Harmony;
		
		public static void PatchIfNeeded()
		{
			if (s_Harmony != null)
				return;
			s_Harmony = new Harmony("com.cratesmith.inspectorfilter");
			s_Harmony.PatchAll();
		}
	}

[HarmonyPatch(typeof(EditorGUILayout), 
              nameof(EditorGUILayout.PropertyField),
              new []{typeof(SerializedProperty),
	              typeof(GUIContent),
	              typeof(bool),
	              typeof(GUILayoutOption[])})]
public static class PropertyFieldPatch
{
	static bool Prefix(SerializedProperty property,
	                          ref GUIContent label,
	                          bool includeChildren,
	                          GUILayoutOption[] options,
	                          ref bool __result)
	{
		if (!Active)
			return true;
		
		var iter = property.Copy();
		var end = property.GetEndProperty();
		do
		{
			if (InspectorFilterGUI.Filter(property.displayName))
			{
				return true;
			}
			
		} while (iter.NextVisible(true) 
		         && !SerializedProperty.EqualContents(iter, end));
		
		__result = false;
		return false;
	}
	public static bool Active { get; set; }
}
	
[HarmonyPatch]
public static class OnInspectorGUIPatch
{
	static bool Prefix(Editor __instance)
	{
		if (__instance.target is AssetImporter)
			return true;
		
		var iter = __instance.serializedObject.GetIterator();
 		iter.Next(true);
 		while (iter.NextVisible(true))
 		{
	        if (InspectorFilterGUI.Filter(iter.displayName))
	        {
		        PropertyFieldPatch.Active = true;
		        return true;
	        }
 		}
        return false;
	}

	static void Postfix()
	{
		PropertyFieldPatch.Active = false;
	}

	static IEnumerable<MethodBase> TargetMethods()
	{ 
		return AccessTools.AllTypes() 
 			.Where(type => !type.IsAbstract && typeof(Editor).IsAssignableFrom(type))
 			.Select(type => AccessTools.Method(type,nameof(Editor.OnInspectorGUI)));
	}
}
	
public static class InspectorFilterGUI
{
	[InitializeOnLoadMethod]
	static void InitializeOnLoad()
	{
		EditorApplication.delayCall += () => Editor.finishedDefaultHeaderGUI += DrawHeaderGUI;
	}
	
	static MethodInfo s_ToolbarSearchField;
	private static string SearchField(string value, params GUILayoutOption[] options)
	{
		if (s_ToolbarSearchField == null)
		{
			s_ToolbarSearchField = AccessTools.Method(typeof(EditorGUILayout), "ToolbarSearchField",
											new [] {typeof(string), typeof(GUILayoutOption[])});
		}

		return s_ToolbarSearchField.Invoke(null, new[] { value, (object)options }) as string;
	}
	
	static void DrawHeaderGUI(Editor _obj)
	{
		if (!_obj.target || _obj.target is AssetImporter)
			return;
		
		Patcher.PatchIfNeeded();
		
		FilterText = EditorGUILayout.TextField(FilterText);
		SearchSplits = FilterText.Split(' ');
	}

	public static bool Filter(string _string)
	{
		if (string.IsNullOrWhiteSpace(FilterText))
			return true;
		
		foreach (var split in InspectorFilterGUI.SearchSplits)
		{
			if (split.Length == 0) continue;
			if (_string.IndexOf(split, StringComparison.OrdinalIgnoreCase) >= 0)
				return true;
		}
		return false;
	}
	
	public static string[] SearchSplits { get; set; } = new string[0];

	public static string FilterText { get; private set; } = "";
}
}
