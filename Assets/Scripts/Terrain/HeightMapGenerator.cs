using LibNoise.Generator;
using LibNoise.Operator;
using LibNoise;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

[RequireComponent(typeof(TerrainManager))]
public class HeightMapGenerator : MonoBehaviour, IGenerator
{
    public int seed = 0;
    [Range(0,4)]
    public float mountainFrequency = 1;
    [Range(0, 4)]
    public float mountainLacunarity = 2;
    [Range(1,10)]
    public int mountainOctaves = 6;
    
    [Range(0,2)]
    public float flatScale = 0.125f;
    [Range(-1,1)]
    public float flatBias = -0.75f;
    [Range(0, 4)]
    public float flatFrequency = 2;

    [Range(0, 1)]
    public float combineFrequency = 0.5f;
    [Range(0, 1)]
    public float combinePersistence = 0.25f;

    [Range(0.5f, 2)]
    [SerializeField] float finalFrequency = 1;
    [Range(0, 1)]
    [SerializeField] float finalPower = 1f / 8;

    //offset表示噪声图的偏移量，size表示噪声图的大小
    [SerializeField] float xOffset = 0;
    [SerializeField] float zOffset = 0;
    [Range(1,16)]
    [SerializeField] float xSize = 8;
    [Range(1,16)]
    [SerializeField] float zSize = 8;

    public enum DistanceMode
    {
        Diagonal,
        Euclidean,
        Manhattan,
    }
    public bool useFallOff = true;
    public DistanceMode disMode = DistanceMode.Euclidean;
    public float disPower = 0.5f;
    public float falloffPower = 1;
    public float falloffRange = 10;

    float[,] heightData;
    int[,] seaDistance;

    public void Generate()
    {
        seaDistance = TerrainManager.Singleton.MyData.seaDistance;

        heightData = CreateOriginalHeightMap();
        Array.Copy(heightData, TerrainManager.Singleton.MyData.elevation,heightData.Length);

        if (useFallOff)
        {
            var falloffData = CreateFallOff();
            for (int i = 0; i < heightData.GetLength(0); i++)
            {
                for (int j = 0; j < heightData.GetLength(1); j++)
                {
                    heightData[i, j] *= 0.6f;
                    heightData[i, j] += 0.4f *falloffData[i, j];
                    heightData[i, j] = Mathf.Clamp01(heightData[i, j]);
                }
            }
            Debug.Log("have utilized fall off");
        }
        Debug.Log("height map has been generated completely");


        SetSeaDistance();
        Debug.Log("sea distance map has been generated completely");

        TerrainManager.Singleton.Data.SetHeights(0, 0, heightData);

    }

    float[,] CreateOriginalHeightMap()
    {
        var mountainTerrain = new RidgedMultifractal(mountainFrequency, mountainLacunarity, mountainOctaves, seed);


        var flatTerrain = new ScaleBias(flatScale, flatBias,
            new Billow
            {
                Seed = seed,
                Frequency = flatFrequency
            });

        var terrainType = new Perlin
        {
            Seed = seed,
            Frequency = 0.5,
            Persistence = 0.25
        };

        // Create the selector for turbulence
        var terrainSelector = new Select(flatTerrain, mountainTerrain, terrainType);
        terrainSelector.SetBounds(0, 1000);
        terrainSelector.FallOff = 0.125f;

        var finalTerrain = new Turbulence(new Perlin(seed), new Perlin(seed), new Perlin(seed), finalPower, terrainSelector)
        {
            Frequency = finalFrequency
        };

        //讲noise通过Noise2D传入MyData中
        var heightMapBuilder = new Noise2D(Utils.mapSize, Utils.mapSize, finalTerrain);
        heightMapBuilder.GeneratePlanar(xOffset, xOffset + xSize, zOffset, zOffset + zSize);
        heightMapBuilder.GetNormalizedData(out TerrainManager.Singleton.MyData.finalElevation);
        return TerrainManager.Singleton.MyData.finalElevation;
    }

    float[,] CreateFallOff()
    {
        var result = new float[Utils.mapSize, Utils.mapSize];
        for (int i = 0; i < Utils.mapSize; i++)
        {
            for (int j = 0; j < Utils.mapSize; j++)
            {
                float x = i / (float)Utils.mapSize * 2 - 1;
                float z = j / (float)Utils.mapSize * 2 - 1;
                float disValue = 0;
                switch (disMode)
                {
                    case DistanceMode.Diagonal:
                        disValue = Mathf.Max(Mathf.Abs(x), Mathf.Abs(z));
                        break;
                    case DistanceMode.Euclidean:
                        disValue = Mathf.Sqrt((x * x + z * z) * 0.5f);
                        break;
                    case DistanceMode.Manhattan:
                        disValue = (Mathf.Abs(x) + Mathf.Abs(z)) * 0.5f;
                        break;
                    default:
                        break;
                }
                disValue = Mathf.Pow(disValue, disPower);
                //result[i, j] = (1 - disValue) * 0.5f;
                result[i, j] = 1 - Mathf.Pow(disValue, falloffPower) / (Mathf.Pow(disValue, falloffPower) + Mathf.Pow(3 * (1 - disValue), falloffPower));
            }
        }
        return result;
    }


    void SetSeaDistance()
    {

        //x*10000+z
        Queue<int> nodeQueue = new Queue<int>();

        for(int i = 0; i < seaDistance.GetLength(0); i++)
        {
            for(int j = 0; j < seaDistance.GetLength(1); j++)
            {
                if (Mathf.Abs(heightData[i, j] * Utils.mapHeight - Utils.seaLevel) < 0.05f)
                {
                    Utils.maxSeaDistance = 0;
                    seaDistance[i, j] = 0;
                    nodeQueue.Enqueue(i * 10000 + j);
                    TerrainManager.Singleton.MyData.type[i, j] = TerrainType.land;
                }
                else
                {
                    seaDistance[i, j] = int.MinValue;
                }
            }
        }
        while (nodeQueue.Count > 0)
        {
            int number = nodeQueue.Dequeue();
            int x = number / 10000, z = number % 10000, value = seaDistance[x, z], x1, z1;

            x1 = x; z1 = z - 1;
            if (SetCurSeaDistance(x1, z1, value))
                nodeQueue.Enqueue(x1 * 10000 + z1);

            x1 = x; z1 = z + 1;
            if (SetCurSeaDistance(x1, z1, value))
                nodeQueue.Enqueue(x1 * 10000 + z1);

            x1 = x - 1; z1 = z;
            if (SetCurSeaDistance(x1, z1, value))
                nodeQueue.Enqueue(x1 * 10000 + z1);

            x1 = x + 1; z1 = z;
            if (SetCurSeaDistance(x1, z1, value))
                nodeQueue.Enqueue(x1 * 10000 + z1);
        }
    }

    bool SetCurSeaDistance(int x, int z, int value)
    {
        if (x < 0 || x >= Utils.mapSize || z < 0 || z >= Utils.mapSize)
        {
            return false;
        }
        if (seaDistance[x, z] == int.MinValue)
        {
            if (heightData[x, z] * Utils.mapHeight < Utils.seaLevel)
            {
                seaDistance[x, z] = -1;
                TerrainManager.Singleton.MyData.type[x, z] = TerrainType.sea;
            }
            else
            {
                seaDistance[x, z] = value + 1;
                TerrainManager.Singleton.MyData.type[x, z] = TerrainType.land;
                if (seaDistance[x, z] > Utils.maxSeaDistance)
                    Utils.maxSeaDistance = seaDistance[x, z];
            }
            return true;
        }
        else
            return false;
    }

    public void Clear()
    {
        TerrainManager.Singleton.MyData.Clear();
        Debug.Log("height map has been cleared");
    }

}
