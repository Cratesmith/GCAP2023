using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace URLHook
{
	public static class AssetUrlHeaderGUI
	{
		const string urlPrefix  = "com.unity3d.kharma://selectguid/?";
		
		[InitializeOnLoadMethod]
		public static void InitializeOnLoad()
		{
			Editor.finishedDefaultHeaderGUI += DrawHeaderGUI;
			OpenURLPatch.Listeners.Add(OnURL);
		}
		
		static bool OnURL(string _url)
		{
			if (!_url.StartsWith(urlPrefix, StringComparison.OrdinalIgnoreCase))
				return false;

			var guid = _url.Substring(urlPrefix.Length);
			var path = AssetDatabase.GUIDToAssetPath(guid);
			if (string.IsNullOrEmpty(path))
				Debug.LogError($"Link error: asset with guid:{guid}");
			else
			{
				var asset = AssetDatabase.LoadAssetAtPath<Object>(path);
				Selection.activeObject = asset;
				EditorGUIUtility.PingObject(asset);
			}
			return true;
		}

		static void DrawHeaderGUI(Editor _editor)
		{
			if (_editor.targets.Length != 1)
				return;

			var path = AssetDatabase.GetAssetPath(_editor.target);
			if (string.IsNullOrEmpty(path))
				return;

			var guid = AssetDatabase.AssetPathToGUID(path);

			GUILayout.BeginHorizontal();

			using (new EditorGUI.DisabledScope(true))
				EditorGUILayout.TextField("GUID", guid);
			
			if (GUILayout.Button("Copy Link", GUILayout.ExpandWidth(false)))
				GUIUtility.systemCopyBuffer = $"{urlPrefix}{guid}";
			
			GUILayout.EndHorizontal();
		}
	}
}
