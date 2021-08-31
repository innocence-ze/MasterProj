using LibNoise;
using LibNoise.Generator;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GrassGenerator : MonoBehaviour, IGenerator
{
    int seedOffset = 0;
    //perlin noise
    public float frequency = 1;
    public float lacunarity = 2;
    public float persistence = 0.5f;
    public int octaves = 4;
    //noise 2d
    public float size = 40;
    public float offset = 100;

    [Range(0,1)]
    public float noiseThreshold = 0.5f;
    [Range(0,90)]
    public float maxSteepness = 70;
    public float minHeight = 15;
    public float maxHeight = 40;

    public int density = 5;

    public List<Texture2D> grassTextures = new List<Texture2D>();

    TerrainData data;
    MyTerrainData myData;

    public void Clear()
    {
        seedOffset = 0;
        TerrainManager.Singleton.Data.detailPrototypes = null;
    }

    public void Generate()
    {
        var t = Time.realtimeSinceStartup;
        Clear();
        DetailPrototype[] detailPrototypes = new DetailPrototype[grassTextures.Count];
        for (int i = 0; i < grassTextures.Count; i++) detailPrototypes[i] = new DetailPrototype() { prototypeTexture = grassTextures[i], minHeight = 0.4f, maxHeight = 0.7f, };
        
        data = TerrainManager.Singleton.Data;
        myData = TerrainManager.Singleton.MyData;
        data.detailPrototypes = detailPrototypes;
        
        var noiseData = CreateNoiseData();

        for(int k = 0; k < data.detailPrototypes.Length; k++)
        {
            int[,] detailLayer = data.GetDetailLayer(0, 0, data.detailWidth, data.detailHeight, k);
            for (int i = 0; i < data.alphamapWidth; i++)
            {
                for (int j = 0; j < data.alphamapHeight; j++)
                {
                    float height = data.GetHeight(j, i);
                    float scaledX = 1.0f * j / data.heightmapResolution;
                    float scaledZ = 1.0f * i / data.heightmapResolution;
                    float angle = data.GetSteepness(scaledX, scaledZ);
                    if(angle <maxSteepness && height > minHeight && height < maxHeight && myData.type[i,j] == TerrainType.land && !IsNextRoad(i, j))
                    {
                        var d = (int)(noiseData[i, j] * density);
                        detailLayer[i, j] = d <= 0 ? density : d;

                    }
                    else
                    {
                        detailLayer[i, j] = 0;
                    }
                }
            }
            data.SetDetailLayer(0, 0, k, detailLayer);
        }

        Debug.Log(this.GetType().ToString() + (Time.realtimeSinceStartup - t));

    }

    bool IsNextRoad(int x, int z)
    {
        return myData.ContainRoadGrid(x, z) || myData.ContainRoadGrid(x, z + 1) || myData.ContainRoadGrid(x, z - 1) ||
            myData.ContainRoadGrid(x + 1, z) || myData.ContainRoadGrid(x - 1, z);
    }

    float[,] CreateNoiseData()
    {
        var noiseBuilder = new Noise2D
        (
            data.detailWidth,
            data.detailWidth,
            new Perlin
            {
                Seed = Utils.Seed,
                Frequency = frequency,
                Lacunarity = lacunarity,
                Persistence = persistence,
                OctaveCount = octaves,
            }
        );
        noiseBuilder.GeneratePlanar(offset, offset + size, offset, offset + size);
        return noiseBuilder.GetNormalizedData();
    }

}
