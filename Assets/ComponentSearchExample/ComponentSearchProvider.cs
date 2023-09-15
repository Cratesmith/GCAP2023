using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using JetBrains.Annotations;
using UnityEditor;
#if UNITY_2021_1_OR_NEWER
using UnityEditor.Search;
using UnityEngine.Search;
using UnityEditor.SceneManagement;
#else
using UnityEditor.Experimental.SceneManagement;
using Unity.QuickSearch;
#endif
using UnityEditor.ShortcutManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace ComponentSearchExample
{
	public static class ComponentSearchProvider
	{
		private const string providerId = "components";

		[UsedImplicitly, SearchItemProvider]
		static SearchProvider CreateProviderSingle()
		{
			return new SearchProvider(providerId, "Coponents")
			{
				filterId = "\\:",
				showDetails = true,
				isExplicitProvider = true,
				showDetailsOptions = ShowDetailsOptions.Inspector | ShowDetailsOptions.Actions,
				fetchItems = (context, items, provider) => FetchItemsRoutine(context, items, provider),
				toObject = (item, type) => item.data as Object,
				startDrag = (item, context) => StartDrag(item),
				fetchThumbnail = (item, context) => GetIconForObject(item.data as Object),
				trackSelection = (_item, _context) => EditorGUIUtility.PingObject(_item.data as Object),
	#if UNITY_2021_1_OR_NEWER
				actions = {	new SearchAction(providerId, "open", null, "Open asset...", OpenItem) }
	#endif
			};
		}
		
		private static IEnumerable FetchItemsRoutine(SearchContext _context, List<SearchItem> _items, SearchProvider _provider)
		{
			var splits = _context.searchQuery.Split(' ');
			var blankSearch = string.IsNullOrWhiteSpace(_context.searchQuery);

			var process = Selection.gameObjects
				.SelectMany(x=>x.GetComponents<Component>())
				.Distinct()
				.Where(x => x)
				.Select(x => (component:x, score: blankSearch ? 0 : GetScore(x, splits)))
				.Where(x => x.score >= 0)
				.Select(x => _provider.CreateItem(_context,
												  x.component.GetInstanceID().ToString(),
												  -x.score,
												  $"{x.component.GetType().Name}",
												  $"{x.component.name}:{x.component.GetType().Name}",
												  null,
												  x.component));
			foreach (var item in process)
			{
				_items.Add(item);
				yield return null;
			}
		}

		static int GetScore(Component _component, string[] _splits)
		{
			int score = -1;
			const int typeMatch = 5;
			const int nameMatch = 1;
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
			}
			return score;
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
				var icon = mi.Invoke(null, new object[]
				{
					forObject
				}) as Texture2D;
				if (forObject is MonoBehaviour && !icon)
					return EditorGUIUtility.FindTexture("cs Script Icon");
#endif
			}
			return (Texture2D)EditorGUIUtility.ObjectContent(forObject, typeof(Mesh)).image;
		}

		private static void StartDrag(SearchItem item)
		{
			DragAndDrop.PrepareStartDrag();
			DragAndDrop.objectReferences = new[]
			{
				item.data as Object
			};
			DragAndDrop.StartDrag(item.label);
		}

		static void OpenItem(SearchItem _obj)
		{
#if UNITY_2021_1_OR_NEWER
			EditorApplication.delayCall += () => EditorUtility.OpenPropertyEditor(_obj.data as Object);
#else
			var ty = typeof(EditorApplication).Assembly.GetType("UnityEditor.PropertyEditor");
			var mi = ty.GetMethod("OpenPropertyEditor", BindingFlags.NonPublic | BindingFlags.Static);
			mi.Invoke(null, new object[] { _obj.data as Object, true });
#endif
		}
#if !UNITY_2021_1_OR_NEWER
		[SearchActionsProvider, UsedImplicitly]
		static IEnumerable<SearchAction> CreateActionHandlers() 
			=> new[] { new SearchAction(providerId, 
				"open", 
				null,
				"Open asset...",
				OpenItem)
			};
#endif

		[UsedImplicitly, Shortcut("Help/Quick Search/Components", KeyCode.Backslash, ShortcutModifiers.Alt)]
		private static void QuickSearchMembers()
		{
			if (Selection.activeGameObject || PrefabStageUtility.GetCurrentPrefabStage())
			{
			#if UNITY_2021_1_OR_NEWER
				var context = new SearchContext(new[] { CreateProvider(_includeChildren) }, " ", 
												SearchFlags.Sorted
												|SearchFlags.NoIndexing
												|SearchFlags.Synchronous);
				var state = new SearchViewState(context, SearchViewFlags.OpenInspectorPreview
															| SearchViewFlags.DisableBuilderModeToggle
															| SearchViewFlags.ListView);
				state.position.width = 900;
				state.position.height = 550;
				state.windowTitle = new GUIContent(!_includeChildren ? "Components" : "Component in children");
				var qs = SearchService.ShowWindow(state);
			#else
				// Open Search with only the "Asset" provider enabled.
				var qs = QuickSearch.OpenWithContextualProvider(new[]
				{
					providerId
				});

				qs.SetSearchText(" ");
			#endif
			}
		}
		
		[InitializeOnLoadMethod]
		static void InitializeOnLoad()
		{
			EditorApplication.delayCall += () => Editor.finishedDefaultHeaderGUI += DrawHeaderGUI;
		}

		static void DrawHeaderGUI(Editor _obj)
		{
			if (!(_obj.target is GameObject))
				return;

			using (new GUILayout.HorizontalScope())
			{
				GUILayout.FlexibleSpace();
				if (GUILayout.Button(new GUIContent("Search Components...", 
												"Search components in this gameobject and it's children (alt+\\)"), 
												GUILayout.ExpandWidth(false)))
				{
					var prev = Selection.objects;
					Selection.objects = _obj.targets;
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
							providerId
						});

						qs.SetSearchText(" ");
			#endif
					}
					Selection.objects = prev;
				}
			}
		}
	}
}