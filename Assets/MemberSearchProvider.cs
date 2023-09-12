using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using JetBrains.Annotations;
using Unity.QuickSearch;
using UnityEditor;
using UnityEditor.ShortcutManagement;
using UnityEngine;
using Object = UnityEngine.Object;

public static class MemberSearchProvider 
{
	private const string                               providerId  = "member";
	private const string                               displayName = "Member";
	private const string                               filterId    = "\\";
	private static        QueryEngine<Component>   queryEngine;

	[UsedImplicitly, SearchItemProvider]
	static SearchProvider CreateProvider()
	{
		return new SearchProvider(providerId, displayName)
		{
			filterId = filterId,
			showDetails = true,
			showDetailsOptions = ShowDetailsOptions.Inspector | ShowDetailsOptions.Actions,
			fetchItems = (context, items, provider) =>
			{
				var splits = context.searchQuery.Split(' ');
				var blankSearch = string.IsNullOrWhiteSpace(context.searchQuery);
				items.AddRange(Selection.gameObjects
					               .SelectMany(x => x.GetComponentsInChildren<Component>())
					               .Select(x=>(component:x, path:GetPath(x)))
					               .Select(x=>(component:x.component, path:x.path, score:GetScore(x.component, x.path,splits)))
					               .Where(x=> blankSearch || x.score>=0)
					               .Select(x => provider.CreateItem(context, 
					                                                x.component.GetInstanceID().ToString(), 
					                                                -x.score,
					                                                $"{x.component.GetType().Name}", 
					                                                x.path, 
					                                                GetIconForObject(x.component),
					                                                x.component)));
				return null;
			},
			toObject = (item, type) => item.data as Object,
			startDrag = (item, context) => StartDrag(item, context),

		};
	}
	
	static int GetScore(Component _component, string _path, string[] _splits)
	{
		int score = -1;
		const int typeMatch = 10;
		const int pathMatch = 1;
		foreach (var s in _splits)
		{
			if (string.IsNullOrWhiteSpace(s))
				continue;
			
			if (_component.GetType().Name.IndexOf(s, StringComparison.OrdinalIgnoreCase) >= 0)
			{
				score = Mathf.Max(0, score);
				score += typeMatch * s.Length;
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

	[SearchActionsProvider,UsedImplicitly]
	static IEnumerable<SearchAction> CreateActionHandlers()
	{
		return new[]
		{
			new SearchAction(providerId, "open", null, "Open asset...", OpenItem)
		};
	}
	
	[UsedImplicitly, Shortcut("Help/Quick Search/Quick Search",KeyCode.Backslash, ShortcutModifiers.Alt)]
	private static void PopQuickSearch()
	{
		if (Selection.activeGameObject)
		{
			// Open Search with only the "Asset" provider enabled.
			var qs = QuickSearch.OpenWithContextualProvider(new[]
			{
				providerId
			});	
			qs.SetSearchText(" ");
		} 
	}
	
	static void OpenItem(SearchItem _obj)
	{
		var ty = typeof(EditorApplication).Assembly.GetType("UnityEditor.PropertyEditor");
		var mi = ty.GetMethod("OpenPropertyEditor", BindingFlags.NonPublic | BindingFlags.Static);
		mi.Invoke(null, new object[]{_obj.data as Object, true});
	}

	static Texture2D GetIconForObject(Object forObject)
	{
		if (forObject == null)
			return null;

		if (forObject is ScriptableObject || forObject is MonoBehaviour || forObject is GameObject || forObject is MonoScript)
		{
			var ty = typeof(EditorGUIUtility);
			var mi = ty.GetMethod("GetIconForObject", BindingFlags.NonPublic | BindingFlags.Static);
			var icon = mi.Invoke(null, new object[] { forObject }) as Texture2D;
			if (forObject is MonoBehaviour && !icon)
				return EditorGUIUtility.FindTexture("cs Script Icon");
		}

		return (Texture2D)EditorGUIUtility.ObjectContent(forObject, typeof(Mesh)).image;
	}
}
