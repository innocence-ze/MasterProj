using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TextureGenerator : MonoBehaviour, IGenerator
{
    public List<MyTexture> textures = new List<MyTexture>();
    public void Clear()
    {
        textures.Clear();
    }

    public void Generate()
    {
        if(textures == null)
        {
            throw new NullReferenceException("Texture list is not setted");
        }

        TerrainLayer[] terrainLayers = new TerrainLayer[textures.Count];
        for(int i = 0; i < textures.Count; i++)
        {
            terrainLayers[i] = textures[i].terrainLayer;
        }

        var data = TerrainManager.Singleton.data;
        var myData = TerrainManager.Singleton.myData;
        data.terrainLayers = terrainLayers;

        float[,,] textureMap = new float[data.alphamapWidth, data.alphamapHeight, data.alphamapLayers];
        for(int i = 0; i < data.alphamapWidth; i++)
        {
            for(int j = 0; j < data.alphamapHeight; j++)
            {
                float height = data.GetHeight(i, j);
                float scaledHeight = height / data.size.y;
                float scaledX = 1.0f * i / data.heightmapResolution;
                float scaledZ = 1.0f * j / data.heightmapResolution;
                float scaledAngle = data.GetSteepness(scaledX, scaledZ) / 90.0f;
                for(int k = 0; k < data.alphamapLayers; k++)
                {
                    textureMap[i, j, k] = textures[i].GetWeight(scaledHeight, scaledAngle, 1.0f * myData.seaDistance[i, j] / Utils.maxSeaDistance);
                }
            }
        }

        data.SetAlphamaps(0, 0, textureMap);
    }






    public class MyTexture
    {
        public TerrainLayer terrainLayer;
        public AnimationCurve heightCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
        public AnimationCurve angleCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
        public AnimationCurve seaDisCurve = AnimationCurve.Linear(0, 1, 1, 1);
        public float GetWeight(float h, float a, float s)
        {
            return heightCurve.Evaluate(h) * angleCurve.Evaluate(a) * seaDisCurve.Evaluate(s);
        }

        
    }
}
