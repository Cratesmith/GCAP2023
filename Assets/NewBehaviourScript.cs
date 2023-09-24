using System.Reflection;
using UnityEditor;
using UnityEngine;

public class NewBehaviourScript : MonoBehaviour
{
    public int noRole;
    public int programmerRole;
    public int artRole;
    public int designRole;
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