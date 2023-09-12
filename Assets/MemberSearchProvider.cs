using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using Unity.QuickSearch;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

public static class MemberSearchProvider 
{
	private const string                               type        = "member";
	private const string                               displayName = "Member";
	private const string                               filterId    = "\\";
	private static        QueryEngine<Component>   queryEngine;

	[SearchItemProvider]
	static SearchProvider CreateProvider()
	{
		return new SearchProvider(type, displayName)
		{
			priority = 10,
			filterId = filterId,
			showDetails = true,
			showDetailsOptions = ShowDetailsOptions.Inspector | ShowDetailsOptions.Description | ShowDetailsOptions.Actions | ShowDetailsOptions.Preview,

			fetchItems = (context, items, provider) =>
			{
				var splits = context.searchQuery.Split(' ');
				items.AddRange(Selection.gameObjects
					               .SelectMany(x => x.GetComponentsInChildren<Component>())
					               .Select(x=>(component:x, path:GetPath(x)))
					               .Where(x=>splits.Any(y=>x.path.IndexOf(y, StringComparison.CurrentCultureIgnoreCase)>=0))
					               .Select(x => provider.CreateItem(context, x.component.GetInstanceID().ToString(), x.GetType().Name, x.path, GetIconForObject(x.component), x.component)));
				return null;
			},
			fetchThumbnail = (_item, _context) =>  _item.data is Object o ? GetIconForObject(o):null,
			toObject = (item, type) => item.data as Object,
		};
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

	[SearchActionsProvider]
	static IEnumerable<SearchAction> CreateActionHandlers()
	{
		return new[]
		{
			new SearchAction(type, "open", null, "Open asset...", OpenItem)
		};
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
		{						
			return null;
		}

		if (forObject is ScriptableObject || forObject is MonoBehaviour || forObject is GameObject || forObject is MonoScript)
		{
			var ty = typeof(EditorGUIUtility);
			var mi = ty.GetMethod("GetIconForObject", BindingFlags.NonPublic | BindingFlags.Static);
			return mi.Invoke(null, new object[] { forObject }) as Texture2D;			
		}

		return (Texture2D)EditorGUIUtility.ObjectContent(forObject, typeof(Mesh)).image;
	}
}
