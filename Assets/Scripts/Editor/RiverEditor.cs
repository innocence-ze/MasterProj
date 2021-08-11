using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(RiverGenerator))]
public class RiverEditor : Editor
{
    public override void OnInspectorGUI()
    {
        var gen = (RiverGenerator)target;

        if (DrawDefaultInspector())
        {

        }

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
