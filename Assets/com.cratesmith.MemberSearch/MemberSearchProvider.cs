using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using JetBrains.Annotations;
using UnityEditor;
using UnityEditor.Experimental.SceneManagement;
using UnityEditor.SceneManagement;
#if UNITY_2021_1_OR_NEWER
using UnityEditor.Search;
using UnityEngine.Search;
#else
using Unity.QuickSearch;
#endif
using UnityEditor.ShortcutManagement;
using UnityEngine;
using Object = UnityEngine.Object;


public static class MemberSearchProvider 
{
	private const string providerId  = "components";
	private const string displayName = "Coponents";
	private const string filterId    = "\\:";

	private const string displayNameWithChildren = "Components In Children";
	private const string providerWithChildrenId = "componentsInChildren";
	private const string filterWithChildrenId   = "\\\\:";

	[InitializeOnLoadMethod]
	static void InitializeOnLoad()
	{
		Editor.finishedDefaultHeaderGUI += DrawHeaderGUI;
	}
	
	static void DrawHeaderGUI(Editor _obj)
	{
		if (!(_obj.target is GameObject))
			return;

		using (new GUILayout.HorizontalScope())
		{
			GUILayout.FlexibleSpace();
			if (GUILayout.Button(new GUIContent("Search Components...","Search components in this gameobject and it's children (alt+\\)"), GUILayout.ExpandWidth(false)))
			{
				var prev = Selection.objects; 
				Selection.objects = _obj.targets;
				OpenQuickSearch(false);
				Selection.objects = prev;
			}
			if (GUILayout.Button(new GUIContent("Search Components in children...","Search components in this gameobject and it's children (alt+shift+\\)"), GUILayout.ExpandWidth(false)))
			{
				var prev = Selection.objects; 
				Selection.objects = _obj.targets;
				OpenQuickSearch(true);
				Selection.objects = prev;
			}
		}
	}

	[UsedImplicitly, SearchItemProvider]
	static SearchProvider CreateProviderSingle()
		=> CreateProvider(false);

	[UsedImplicitly, SearchItemProvider]
	static SearchProvider CreateProviderWithChildren()
		=> CreateProvider(true);
	
	static SearchProvider CreateProvider(bool _includeChildren)
	{
		return new SearchProvider(_includeChildren ? providerWithChildrenId: providerId,
		                          _includeChildren ? displayNameWithChildren:displayName)
		{
			filterId = _includeChildren ? filterWithChildrenId:filterId,
			showDetails = true,
			isExplicitProvider = true,
			showDetailsOptions = ShowDetailsOptions.Inspector | ShowDetailsOptions.Actions,
			fetchItems = (context, items, provider) =>
			{
				IEnumerable _FetchRoutine()
				{
					var splits = context.searchQuery.Split(' ');
					var blankSearch = string.IsNullOrWhiteSpace(context.searchQuery);
					var source = Selection.gameObjects.Length > 0
						? Selection.gameObjects
						: PrefabStageUtility.GetCurrentPrefabStage() 
							? new[] {PrefabStageUtility.GetCurrentPrefabStage().prefabContentsRoot}
							:new GameObject[0];
					
					var process = source
						.SelectMany(x => _includeChildren ? x.GetComponentsInChildren<Component>(true) : x.GetComponents<Component>())
						.Distinct()
						.Where(x => x)
						.Select(x => (component: x, path: GetPath(x)))
						.Select(x => (x.component, x.path, score: GetScore(x.component, x.path, splits)))
						.Where(x => blankSearch || x.score >= 0)
						.Select(x => provider.CreateItem(context,
						                                 x.component.GetInstanceID().ToString(),
						                                 -x.score,
						                                 $"{x.component.GetType().Name}",
						                                 x.path,
						                                 null,
						                                 x.component));
					foreach (var item in process)
					{
						items.Add(item);
						yield return null;
					}
				}
				return _FetchRoutine();
			},
			toObject = (item, type) => item.data as Object,
			startDrag = (item, context) => StartDrag(item, context),
			fetchThumbnail = (item, context) => GetIconForObject(item.data as Object),
			trackSelection = (_item, _context) => EditorGUIUtility.PingObject(_item.data as Object), 
#if UNITY_2021_1_OR_NEWER			
			actions = {	new SearchAction(providerId, "open", null, "Open asset...", OpenItem) }
#endif
		};
	}
	
	static int GetScore(Component _component, string _path, string[] _splits)
	{
		int score = -1;
		const int typeMatch = 10;
		const int nameMatch = 2;
		const int pathMatch = 1;
		foreach (var s in _splits)
		{
			if (string.IsNullOrWhiteSpace(s))
				continue;
			var type = _component.GetType();
			while (type != null)
			{
				if (type.Name.IndexOf(s, StringComparison.OrdinalIgnoreCase) >= 0)
				{
					score = Mathf.Max(0, score);
					score += typeMatch * s.Length;
					break;
				}
				type = type.BaseType;
			}
			
			if (_component.name.IndexOf(s, StringComparison.OrdinalIgnoreCase) >= 0)
			{
				score = Mathf.Max(0, score);
				score += nameMatch * s.Length;
			}
			
			if (_path.IndexOf(s, StringComparison.OrdinalIgnoreCase) >= 0)
			{
				score = Mathf.Max(0, score);
				score += pathMatch * s.Length;
			}
		}
		return score;
	}

	static string GetPath(Component _component)
	{
		if (!_component)
			return "";
		var current = _component.transform;
		var path = $":{current.GetType().Name}";
		while (current != null)
		{
			path = $"/{current.name}{path}";
			current = current.parent;
		}
		return path;
	}
	
	private static void StartDrag(SearchItem item, SearchContext context)
	{
		DragAndDrop.PrepareStartDrag();
		DragAndDrop.objectReferences = new[]{item.data as Object};
		DragAndDrop.StartDrag(item.label);
	}

#if !UNITY_2021_1_OR_NEWER
	[SearchActionsProvider,UsedImplicitly]
	static IEnumerable<SearchAction> CreateActionHandlers()
	{
		return new[]
		{
			new SearchAction(providerId, "open", null, "Open asset...", OpenItem)
		};
	}
#endif

	[UsedImplicitly, Shortcut("Help/Quick Search/Components", KeyCode.Backslash, ShortcutModifiers.Alt)]
	private static void QuickSearchMembers() => OpenQuickSearch(false);
	
	[UsedImplicitly, Shortcut("Help/Quick Search/Components in children", KeyCode.Backslash, ShortcutModifiers.Alt|ShortcutModifiers.Shift)]
	private static void QuickSearchChildren() => OpenQuickSearch(true);
	
	private static void OpenQuickSearch(bool _includeChildren)
	{
		if (Selection.activeGameObject || PrefabStageUtility.GetCurrentPrefabStage())
		{
			#if UNITY_2021_1_OR_NEWER
			var context = new SearchContext(new[] { CreateProvider(_includeChildren) }, " ", SearchFlags.Sorted|SearchFlags.NoIndexing|SearchFlags.Synchronous);
			var state = new SearchViewState(context, SearchViewFlags.OpenInspectorPreview|SearchViewFlags.DisableBuilderModeToggle|SearchViewFlags.ListView);
			state.position.width = 900;
			state.position.height = 550;
			state.windowTitle = new GUIContent(!_includeChildren ? "Components" : "Component in children");
			var qs = SearchService.ShowWindow(state);
			#else
			// Open Search with only the "Asset" provider enabled.
			var qs = QuickSearch.OpenWithContextualProvider(new[]
			{
				_includeChildren? providerWithChildrenId:providerId
			});	

			qs.SetSearchText(" ");
			#endif
		} 
	}
	
	static void OpenItem(SearchItem _obj)
	{
#if UNITY_2021_1_OR_NEWER
		EditorApplication.delayCall += () => EditorUtility.OpenPropertyEditor(_obj.data as Object);
#else 
		var ty = typeof(EditorApplication).Assembly.GetType("UnityEditor.PropertyEditor");
		var mi = ty.GetMethod("OpenPropertyEditor", BindingFlags.NonPublic | BindingFlags.Static);
		mi.Invoke(null, new object[]{_obj.data as Object, true});
#endif
	}

	static Texture2D GetIconForObject(Object forObject)
	{
		if (forObject == null)
			return null;
		
		if (forObject is ScriptableObject || forObject is MonoBehaviour || forObject is GameObject || forObject is MonoScript)
		{
#if UNITY_2021_2_OR_NEWER
			var icon = EditorGUIUtility.GetIconForObject(forObject);
			if (forObject is MonoBehaviour && !icon)
				return EditorGUIUtility.IconContent(EditorGUIUtility.isProSkin ? "d_cs Script Icon" : "cs Script Icon").image as Texture2D;
#else
			var ty = typeof(EditorGUIUtility);
			var mi = ty.GetMethod("GetIconForObject", BindingFlags.NonPublic | BindingFlags.Static);
			var icon = mi.Invoke(null, new object[] { forObject }) as Texture2D;
			if (forObject is MonoBehaviour && !icon)
				return EditorGUIUtility.FindTexture("cs Script Icon");
#endif
		}

		return (Texture2D)EditorGUIUtility.ObjectContent(forObject, typeof(Mesh)).image;
	}
}
