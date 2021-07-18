using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TextureGenerator : MonoBehaviour, IGenerator
{
    public MyTextures textures;
    public void Clear()
    {
        TerrainManager.Singleton.heightMapGenerated = false;
        textures.Clear();
    }

    public void Generate()
    {
        if (!TerrainManager.Singleton.heightMapGenerated)
        {
            Debug.LogWarning("please generate height map first");
            return;
        }

        if(textures == null)
        {
            Debug.LogError("Texture list is not setted");
            return;
        }

        RemoveUselessLayers();

        if (textures.Count == 0)
            return;

        SetTerrainLayer();

        var data = TerrainManager.Singleton.Data;
        var myData = TerrainManager.Singleton.MyData;


        float[,,] textureMap = new float[data.alphamapWidth, data.alphamapHeight, data.alphamapLayers];
        float[,] textureSum = new float[data.alphamapWidth, data.alphamapHeight];
        for(int i = 0; i < data.alphamapWidth; i++)
        {
            for(int j = 0; j < data.alphamapHeight; j++)
            {
                float height = data.GetHeight(j, i);
                float scaledHeight = height / data.size.y;
                float scaledX = 1.0f * j / data.heightmapResolution;
                float scaledZ = 1.0f * i / data.heightmapResolution;
                float scaledAngle = data.GetSteepness(scaledX, scaledZ) / 90.0f;
                for(int k = 0; k < data.alphamapLayers; k++)
                {
                    textureMap[i, j, k] = textures[k].GetWeight(scaledHeight, scaledAngle, Mathf.Clamp01((1.0f * myData.waterDistance[j, i]) / Utils.maxOceanDis));
                    textureSum[i, j] += textureMap[i, j, k];
                }
                //normalized
                for(int k = 0; k < data.alphamapLayers; k++)
                {
                    textureMap[i, j, k] /= textureSum[i, j];
                }
            }
        }

        data.SetAlphamaps(0, 0, textureMap);
    }


    void SetTerrainLayer()
    {
        TerrainLayer[] terrainLayers = new TerrainLayer[textures.Count];
        for (int i = 0; i < textures.Count; i++)
        {
            terrainLayers[i] = textures[i].terrainLayer;
        }

        TerrainManager.Singleton.Data.terrainLayers = terrainLayers;
    }

    void RemoveUselessLayers()
    {
        for (int i = textures.Count - 1; i >= 0; i--)
        {
            if (textures[i].terrainLayer == null)
            {
                textures.RemoveAt(i);
            }
        }
    }

}
