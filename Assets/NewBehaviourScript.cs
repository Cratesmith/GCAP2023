using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using Object = UnityEngine.Object;

[Flags]
public enum Roles
{
    None   = 0,  
    Code   = 1<<0,
    Design = 1<<1,
    Art    = 1<<2,
}


[AttributeUsage(AttributeTargets.Class|AttributeTargets.Field)]
public class RoleAttribute : PropertyAttribute
{
    public readonly Roles role;
    public RoleAttribute(Roles _role) => role = _role;
}

public static class RoleHeaderGUI
{
    public static Roles activeRoles {
        get => (Roles)EditorPrefs.GetInt("ActiveRoles", -1);
        set => EditorPrefs.SetInt("ActiveRoles", (int)value);
    }

    public static Dictionary<Type, Roles> RoleClasses { get; } = new Dictionary<Type, Roles>
    {
        {typeof(Rigidbody), Roles.Code|Roles.Design},
        {typeof(TerrainCollider), Roles.Code|Roles.Design},
        {typeof(SphereCollider), Roles.Code|Roles.Design},
        {typeof(MeshCollider), Roles.Code|Roles.Design},
        {typeof(BoxCollider), Roles.Code|Roles.Design},
    };

    static Object[] resetToSelection;

    [InitializeOnLoadMethod]
    public static void InitializeOnLoad()
    {
        Editor.finishedDefaultHeaderGUI += OnFinishedDefaultHeaderGUI;
        Selection.selectionChanged += OnSelectionChanged;
    }
    
    static void OnFinishedDefaultHeaderGUI(Editor _editor)
    {
        GameObject go = _editor.target as GameObject;
        if (go && !go.scene.IsValid())
            return;

        EditorGUI.BeginChangeCheck();
        activeRoles = (Roles)EditorGUILayout.EnumFlagsField("Role",activeRoles);
        if (EditorGUI.EndChangeCheck())
        {
            if (SetComponentsCollapsedByRole(go))
            {
                resetToSelection = Selection.objects;
                Selection.objects = null;
            }
        }
    }
    
    static bool SetComponentsCollapsedByRole(GameObject go)
    {
        if (!go)
            return false;
        
        var needsRefresh = false;
        foreach (var c in go.GetComponents<Component>())
        {
            Roles role;
            if (!RoleClasses.TryGetValue(c.GetType(), out role))
            {
                var attrib = c.GetType().GetCustomAttribute<RoleAttribute>();
                if (attrib == null)
                    continue;
                role = attrib.role;
            }

            var isExpanded = (role & activeRoles) != 0;
            if (InternalEditorUtility.GetIsInspectorExpanded(c) != isExpanded)
            {
                InternalEditorUtility.SetIsInspectorExpanded(c, isExpanded);
                needsRefresh = true;
            }
        }
        return needsRefresh;
    }
    
    static void OnSelectionChanged()
    {
        if (resetToSelection != null)
        {
            EditorApplication.delayCall += ()=>
            {
                Selection.objects = resetToSelection;
                resetToSelection = null;
            };
            return;
        }
        
        SetComponentsCollapsedByRole(Selection.activeGameObject); 
    }
}

[CustomPropertyDrawer(typeof(RoleAttribute), true)]
public class RoleDrawer : PropertyDrawer
{
    public RoleAttribute roleAttribute => (RoleAttribute)attribute;
    
    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        return (roleAttribute.role & RoleHeaderGUI.activeRoles) != 0
            ? base.GetPropertyHeight(property, label)
            : 2f;
    }

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        if ((roleAttribute.role & RoleHeaderGUI.activeRoles) == 0)
        {
            GUI.Box(position, new GUIContent("", $"[{roleAttribute.role.ToString()}] {label.text} {label.tooltip}"), GUIStyle.none);
            EditorGUI.DrawRect(position, Color.grey);
            return;
        }

        label.tooltip = $"[{roleAttribute.role.ToString()}] {label.tooltip}";
        EditorGUI.PropertyField(position, property, label);
    }
}

//[Role(Roles.Code)]
public class NewBehaviourScript : MonoBehaviour
{
    public                      int noRole;
    [Role(Roles.Code)]   public int programmerRole;
    [Role(Roles.Art)]    public int artRole;
    [Role(Roles.Design)] public int designRole;
    public NewBehaviourScript reference;

    [CustomEditor(typeof(NewBehaviourScript))]
    public class Inspector : Editor
    {
        public override void OnInspectorGUI()
        {
            if (GUILayout.Button("Test"))
            {
                var mi = typeof(EditorApplication).Assembly.GetType("UnityEditor.PropertyEditor").GetMethod("OpenPropertyEditor", BindingFlags.NonPublic|BindingFlags.Static);
                mi.Invoke(null, new object[]{target, true});
            }

            base.OnInspectorGUI();
        }
    }
}