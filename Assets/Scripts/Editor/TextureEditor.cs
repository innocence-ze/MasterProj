using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(TextureGenerator))]
public class TextureEditor : Editor
{
    string number;
    public override void OnInspectorGUI()
    {
        var gen = (TextureGenerator)target;

        if(DrawDefaultInspector())
        {

        }

        if (gen.textures != null)
        {

            for (int i = 0; i < gen.textures.Count; i++)
            {
                var t = gen.textures[i];
                t.terrainLayer = EditorGUILayout.ObjectField(i.ToString(), t.terrainLayer, typeof(TerrainLayer), false) as TerrainLayer;
                if (t.terrainLayer != null)
                {
                    EditorGUILayout.ObjectField(t.terrainLayer.name, t.terrainLayer.diffuseTexture, typeof(Texture2D), false);
                }
                t.heightCurve = EditorGUILayout.CurveField("Height Curve", t.heightCurve);
                t.angleCurve = EditorGUILayout.CurveField("Angle Curve", t.angleCurve);
                t.seaDisCurve = EditorGUILayout.CurveField("Sea Distance Curve", t.seaDisCurve);
            }

            if (GUILayout.Button("Add"))
            {
                gen.textures.Add();
            }

            EditorGUILayout.BeginHorizontal();
            {
                number = EditorGUILayout.TextField(number);
                if (GUILayout.Button("Remove At"))
                {
                    if (int.TryParse(number, out int texIndex) && texIndex >= 0 && texIndex < gen.textures.Count)
                    {
                        gen.textures.RemoveAt(texIndex);
                    }
                    else
                    {
                        EditorUtility.DisplayDialog("Error", "Please Input Correct Index", "OK");
                    }
                }
            }
            EditorGUILayout.EndHorizontal();
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
