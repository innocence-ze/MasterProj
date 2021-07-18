using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(GrassGenerator))]
public class GrassEditor: Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
        var gen = (GrassGenerator)target;
        if (GUILayout.Button("Generate"))
        {
            gen.Generate();
        }
        if (GUILayout.Button("Clear"))
        {
            gen.Clear();
        }
    }
}
