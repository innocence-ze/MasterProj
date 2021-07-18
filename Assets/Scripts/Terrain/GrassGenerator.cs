using LibNoise;
using LibNoise.Generator;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(TerrainManager))]
public class GrassGenerator : MonoBehaviour, IGenerator
{
    public int seed = 0;
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
    [Range(0,50)]
    public float minHeight = Utils.seaLevel;
    [Range(0, 50)]
    public float maxHeight = Utils.mapHeight;

    public int density;

    public List<Texture2D> grassTextures = new List<Texture2D>();

    TerrainData data;
    MyTerrainData myData;

    private void OnValidate()
    {
        if (maxHeight > Utils.mapHeight)
            maxHeight = Utils.mapHeight;
        if (maxHeight < Utils.seaLevel)
            maxHeight = Utils.seaLevel;

        if (minHeight < Utils.seaLevel)
            minHeight = Utils.seaLevel;
        if (minHeight > maxHeight)
            minHeight = maxHeight;
    }

    public void Clear()
    {
        TerrainManager.Singleton.heightMapGenerated = false;
        TerrainManager.Singleton.Data.detailPrototypes = null;
    }

    public void Generate()
    {
        if (!TerrainManager.Singleton.heightMapGenerated)
        {
            Debug.LogWarning("please generate height map first");
            return;
        }

        DetailPrototype[] detailPrototypes = new DetailPrototype[grassTextures.Count];
        for (int i = 0; i < grassTextures.Count; i++) detailPrototypes[i] = new DetailPrototype() { prototypeTexture = grassTextures[i], minHeight = 0.5f, maxHeight = 1, };
        
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
                    if(noiseData[j,i] > noiseThreshold && angle <maxSteepness && height > minHeight && height < maxHeight)
                    {
                        var d = density + Random.Range(-(density % 5 + 1), density % 5 + 1);
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
        Debug.Log("grass generated");


    }

    float[,] CreateNoiseData()
    {
        var noiseBuilder = new Noise2D
        (
            data.detailWidth,
            data.detailWidth,
            new Perlin
            {
                Seed = seed,
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
