#if true
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using UnityEditor;
using UnityEditor.SceneManagement;
#if UNITY_2021_1_OR_NEWER
using UnityEditor.Search;
using UnityEngine.Search;
#else
using System.Reflection;
using UnityEditor.Experimental.SceneManagement;
using Unity.QuickSearch;
#endif
using UnityEditor.ShortcutManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace ComponentSearch
{
	public static class ComponentSearchProvider
	{
		private const string componentProviderId           = "components";
		private const string componentInChildrenProviderId = "componentsInChildren";

		[SearchItemProvider]
		static SearchProvider CreateProviderSingle() => BuildProvider(false);

		[SearchItemProvider]
		static SearchProvider CreateProviderWithChildren() => BuildProvider(true);

		static SearchProvider BuildProvider(bool _includeChildren)
		{
			(var providerId, var displayName, var filterId) = _includeChildren 
				? (componentInChildrenProviderId, "Components In Children", "\\\\:") 
				: (componentProviderId,"Coponents", "\\:");
			
			return new SearchProvider(providerId, displayName)
			{
				filterId = filterId,
				showDetails = true,
				isExplicitProvider = true,
				showDetailsOptions = ShowDetailsOptions.Inspector | ShowDetailsOptions.Actions,
				fetchItems = (context, items, provider) => FetchItemsRoutine(context, items, provider, _includeChildren),
				toObject = (item, _) => item.data as Object,
				startDrag = (item, context) => StartDrag(item, context),
				fetchThumbnail = (item, _) => GetIconForObject(item.data as Object),
				trackSelection = (item, _) => EditorGUIUtility.PingObject(item.data as Object),
#if UNITY_2021_1_OR_NEWER
			actions = {	new SearchAction(componentProviderId, "open", null, "Open asset...", OpenItem) }
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
			var path = $":{_component.GetType().Name}";
			while (current != null)
			{
				path = $"/{current.name}{path}";
				current = current.parent;
			}
			return path;
		}

		private static IEnumerable FetchItemsRoutine(SearchContext _context, 
                 List<SearchItem> _items,
                 SearchProvider _provider,
                 bool _includeChildren)
		{
			var splits = _context.searchQuery.Split(' ');
			var blankSearch = string.IsNullOrWhiteSpace(_context.searchQuery);

			var process = GetSelectedGameObjects()
				.SelectMany(x => _includeChildren ? x.GetComponentsInChildren<Component>(true) : x.GetComponents<Component>())
				.Distinct()
				.Where(x => x)
				.Select(x => (component: x, path: GetPath(x)))
				.Select(x => (x.component, x.path, score: blankSearch ? 0 : GetScore(x.component, x.path, splits)))
				.Where(x => x.score >= 0)
				.Select(x => _provider.CreateItem(_context,
				                                  x.component.GetInstanceID().ToString(),
				                                  -x.score,
				                                  $"{x.component.GetType().Name}",
				                                  x.path,
				                                  null,
				                                  x.component));
			foreach (var item in process)
			{
				_items.Add(item);
				yield return null;
			}
		}
		static GameObject[] GetSelectedGameObjects()
		{
			return Selection.gameObjects.Length > 0 ? Selection.gameObjects
				: PrefabStageUtility.GetCurrentPrefabStage() ? new[]
				{
					PrefabStageUtility.GetCurrentPrefabStage().prefabContentsRoot
				}
				: new GameObject[0];
		}

		private static void StartDrag(SearchItem item, SearchContext context)
		{
			DragAndDrop.PrepareStartDrag();
			DragAndDrop.objectReferences = new[]
			{
				item.data as Object
			};
			DragAndDrop.StartDrag(item.label);
		}

#if !UNITY_2021_1_OR_NEWER
		[SearchActionsProvider]
		static IEnumerable<SearchAction> CreateActionHandlers()
		{
			return new[] { new SearchAction(componentProviderId, "open", null, "Open asset...", OpenItem),
				new SearchAction(componentInChildrenProviderId, "open", null, "Open asset...", OpenItem)};
		}
#endif

		[Shortcut("Help/Quick Search/Components", KeyCode.Backslash, ShortcutModifiers.Alt)]
		private static void QuickSearchMembers() => OpenQuickSearch(false);

		[Shortcut("Help/Quick Search/Components in children", KeyCode.Backslash, ShortcutModifiers.Alt | ShortcutModifiers.Shift)]
		private static void QuickSearchChildren() => OpenQuickSearch(true);

		private static void OpenQuickSearch(bool _includeChildren)
		{
			if (Selection.activeGameObject || PrefabStageUtility.GetCurrentPrefabStage())
			{
			#if UNITY_2021_1_OR_NEWER
				var context = new SearchContext(new[] { BuildProvider(_includeChildren) }, " ", 
					SearchFlags.Sorted|SearchFlags.NoIndexing);
				var state = new SearchViewState(context, 
					SearchViewFlags.OpenInspectorPreview|SearchViewFlags.DisableBuilderModeToggle|SearchViewFlags.ListView);
				state.position.width = 900;
				state.position.height = 550;
				state.title = !_includeChildren ? "Components" : "Component in children";
				SearchService.ShowWindow(state);
			#else
				// Open Search with only the "Asset" provider enabled.
				var qs = QuickSearch.OpenWithContextualProvider(new[]
				{
					_includeChildren ? componentInChildrenProviderId : componentProviderId
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
			mi.Invoke(null, new object[]
			{
				_obj.data as Object, true
			});
#endif
		}

		static Texture2D GetIconForObject(Object forObject)
		{
			if (forObject == null)
				return null;

			if (forObject is ScriptableObject || forObject is MonoBehaviour 
				|| forObject is GameObject || forObject is MonoScript)
			{
				
#if UNITY_2021_2_OR_NEWER
			var icon = EditorGUIUtility.GetIconForObject(forObject);
#else
				var mi = typeof(EditorGUIUtility).GetMethod("GetIconForObject", BindingFlags.NonPublic | BindingFlags.Static);
				var icon = mi.Invoke(null, new object[] { forObject }) as Texture2D;
#endif
				if (icon)
					return icon;
				
				if (forObject is MonoBehaviour)
					return EditorGUIUtility.IconContent(EditorGUIUtility.isProSkin 
						? "d_cs Script Icon" : "cs Script Icon").image as Texture2D;
			}

			return (Texture2D)EditorGUIUtility.ObjectContent(forObject, forObject.GetType()).image;
		}
		
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
				if (GUILayout.Button(new GUIContent("Search Components...", "Search components in this gameobject and it's children (alt+\\)"), GUILayout.ExpandWidth(false)))
				{
					var prev = Selection.objects;
					Selection.objects = _obj.targets;
					OpenQuickSearch(false);
					Selection.objects = prev;
				}
				if (GUILayout.Button(new GUIContent("Search Components in children...", "Search components in this gameobject and it's children (alt+shift+\\)"), GUILayout.ExpandWidth(false)))
				{
					var prev = Selection.objects;
					Selection.objects = _obj.targets;
					OpenQuickSearch(true);
					Selection.objects = prev;
				}
			}
		}
	}
}
#endif