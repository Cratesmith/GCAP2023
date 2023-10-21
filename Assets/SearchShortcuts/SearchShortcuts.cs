using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.ShortcutManagement;
using UnityEngine;

public static class SearchShortcuts
{
	[InitializeOnLoadMethod]
	public static void InitializeOnLoad()
	{
		EditorApplication.delayCall += DoRebind;
	}

	static void DoRebind()
	{
		if (ShortcutManager.instance.IsProfileReadOnly(ShortcutManager.instance.activeProfileId))
			return;

#if UNITY_2021_1_OR_NEWER
		RebindIfMissing("Help/Search/Assets",
		                new KeyCombination(KeyCode.P, ShortcutModifiers.Alt | ShortcutModifiers.Shift));

		RebindIfMissing("Help/Search/Hierarchy",
		                new KeyCombination(KeyCode.H, ShortcutModifiers.Alt | ShortcutModifiers.Shift));

		RebindIfMissing("Help/Search/Menu",
		                new KeyCombination(KeyCode.M, ShortcutModifiers.Alt | ShortcutModifiers.Shift));
#else
		RebindIfMissing("Help/Quick Search/Assets", 
		                new KeyCombination(KeyCode.P, ShortcutModifiers.Alt | ShortcutModifiers.Shift));

		RebindIfMissing("Help/Quick Search/Scene", 
		                new KeyCombination(KeyCode.H, ShortcutModifiers.Alt | ShortcutModifiers.Shift));

		RebindIfMissing("Help/Quick Search/Menu",
		                new KeyCombination(KeyCode.M, ShortcutModifiers.Alt | ShortcutModifiers.Shift));
#endif
	}

	static void RebindIfMissing(string _bindingId, KeyCombination _keyCombination)
	{
		var binding = ShortcutManager.instance.GetShortcutBinding(_bindingId);
		if (binding.keyCombinationSequence.Count() == 0)
		{
			Debug.Log($"Rebinding {_bindingId} to {_keyCombination.ToString()}");
			var kcQsAssets = new ShortcutBinding(_keyCombination);
			ShortcutManager.instance.RebindShortcut(_bindingId, kcQsAssets);
		}
	}
}
