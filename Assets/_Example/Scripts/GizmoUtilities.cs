using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class GizmoUtilities 
{
	public static void DrawPrefab(GameObject prefab, Transform transform, Vector3 localPosition, Quaternion localRotation, Vector3 localScale)
	{
		if(transform==null || prefab == null || Application.isPlaying)
		{
			return;
		}

		var prev = Gizmos.matrix;
		foreach(var r in prefab.GetComponentsInChildren<MeshFilter>())
		{
			Gizmos.matrix = r.transform.parent!=null 
							? transform.localToWorldMatrix * r.transform.localToWorldMatrix
							: transform.localToWorldMatrix;		
	
											
			Gizmos.DrawMesh(r.sharedMesh, localPosition, localRotation, localScale);
		}
		Gizmos.matrix = prev;
	}
}
