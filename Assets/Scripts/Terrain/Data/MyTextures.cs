using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System;

[CreateAssetMenu(fileName = "TerrainTextures",menuName ="TerrainGenerator/Textures")]
public class MyTextures : ScriptableObject
{
    [SerializeField]
    List<MyTexture> textures = new List<MyTexture>();

    public void Add()
    {
        textures.Add(new MyTexture());
    }

    public void Clear()
    {
        textures.Clear();
    }

    public int Count { get => textures.Count; }

    public void RemoveAt(int index)
    {
        textures.RemoveAt(index);
    }

    public MyTexture this[int index]
    {
        get
        {
            return textures[index];
        }
        set
        {
            if(index < 0 || index >= textures.Count)
            {
                throw new IndexOutOfRangeException("Please input correct index");
            }
            textures[index] = value;
        }
    }







    [Serializable]
    public class MyTexture
    {
        public TerrainLayer terrainLayer;
        public AnimationCurve heightCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
        public AnimationCurve angleCurve = AnimationCurve.EaseInOut(0, 1, 1, 0);
        public AnimationCurve seaDisCurve = AnimationCurve.Linear(0, 1, 1, 1);
        public float GetWeight(float h, float a, float s)
        {
            return heightCurve.Evaluate(h) * angleCurve.Evaluate(a) * seaDisCurve.Evaluate(s);
        }
    }
}


