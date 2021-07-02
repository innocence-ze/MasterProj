using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(HeightMapGenerator))]
public class HeightMapEditor : Editor
{
    public override void OnInspectorGUI()
    {
        var gen = (HeightMapGenerator)target;

        if (DrawDefaultInspector())
        {
            //auto update
            // gen.Generate();
            
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
