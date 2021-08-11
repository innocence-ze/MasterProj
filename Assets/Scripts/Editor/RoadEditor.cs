using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(RoadGenerator))]
public class RoadEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
        var gen = (RoadGenerator)target;
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
