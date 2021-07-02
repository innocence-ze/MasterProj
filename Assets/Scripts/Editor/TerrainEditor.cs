using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(TerrainManager))]
public class TerrainEditor : Editor
{
    public override void OnInspectorGUI()
    {

        var gen = (TerrainManager)target;
        if (GUILayout.Button("Update"))
        {
            gen.UpdateTerrain();
        }
    }
}
