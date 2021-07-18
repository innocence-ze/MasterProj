using LibNoise;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(TerrainManager))]
public class TreeGenerator : MonoBehaviour,IGenerator
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

    [Range(0, 1)]
    public float noiseThreshold = 0.5f;
    [Range(0, 90)]
    public float maxSteepness = 70;
    [Range(0, 50)]
    public float minHeight = Utils.seaLevel;
    [Range(0, 50)]
    public float maxHeight = Utils.mapHeight;

    public int density;

    public List<GameObject> treePrefabs = new List<GameObject>();

    TerrainData data;
    MyTerrainData myData;

    public void Clear()
    {
        TerrainManager.Singleton.Data.treeInstances = new TreeInstance[0];
    }

    public void Generate()
    {
        if (!TerrainManager.Singleton.heightMapGenerated)
        {
            Debug.LogWarning("please generate height map first");
            return;
        }

        TreePrototype[] treePrototypes = new TreePrototype[treePrefabs.Count];
        for (int i = 0; i < treePrefabs.Count; i++) treePrototypes[i] = new TreePrototype() { prefab = treePrefabs[i], bendFactor = 1, };

        data = TerrainManager.Singleton.Data;
        myData = TerrainManager.Singleton.MyData;
        data.treePrototypes = treePrototypes;

        var noiseData = CreateNoiseData();

        var treePos = new List<Vector3>();

        for (int i = 0; i < data.alphamapWidth; i++)
        {
            for (int j = 0; j < data.alphamapHeight; j++)
            {
                float height = data.GetHeight(j, i);
                float scaledHeight = height / data.size.y;
                float scaledX = 1.0f * j / data.heightmapResolution;
                float scaledZ = 1.0f * i / data.heightmapResolution;
                float angle = data.GetSteepness(scaledX, scaledZ);
                if (noiseData[j, i] > noiseThreshold && angle < maxSteepness && height > minHeight && height < maxHeight && Random.Range(0,1f) > 0.5f)
                {
                    treePos.Add(new Vector3(scaledX, scaledHeight, scaledZ));
                }
            }
        }

        TreeInstance[] treeInstances = new TreeInstance[treePos.Count];
        for(int i = 0; i < treeInstances.Length; i++)
        {
            treeInstances[i].position = treePos[i];
            treeInstances[i].prototypeIndex = Random.Range(0, treePrefabs.Count);
            treeInstances[i].color = Color.white;
            treeInstances[i].lightmapColor = Color.white;
            var s = Random.Range(0.75f, 1.5f);
            treeInstances[i].heightScale = s;
            treeInstances[i].widthScale = s;
            treeInstances[i].rotation = 0;
        }
        data.treeInstances = treeInstances;

    }

    float[,] CreateNoiseData()
    {
        var noiseBuilder = new Noise2D
        (
            data.detailWidth,
            data.detailWidth,
            new LibNoise.Generator.Perlin
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
