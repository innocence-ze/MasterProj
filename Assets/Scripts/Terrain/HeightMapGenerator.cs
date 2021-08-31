using LibNoise.Generator;
using LibNoise.Operator;
using LibNoise;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class HeightMapGenerator : MonoBehaviour, IGenerator
{
    [Header("NoisePara")]
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

    [Header("NoiseMapPara")]
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
    [Header("fallOff")]
    public bool useFallOff = true;
    public DistanceMode disMode = DistanceMode.Euclidean;
    public float disPower = 0.5f;
    public float falloffPower = 1;
    public float falloffRange = 10;

    public bool useCombineOcean = true;

    MyTerrainData myData;
    TerrainData data;
    float[,] heightData;
    int[,] oceanDis;
    readonly HashSet<Vector2Int> originalOceanSet = new HashSet<Vector2Int>();


    public void Generate()
    {
        var t = Time.realtimeSinceStartup;
        Clear();
        myData = TerrainManager.Singleton.MyData;
        data = TerrainManager.Singleton.Data;
        oceanDis = myData.waterDistance;

        myData.finalElevation = CreateOriginalHeightMap();
        heightData = myData.finalElevation;

        var falloffData = CreateFallOff();

        if (useFallOff)
        {
            for (int i = 0; i < heightData.GetLength(0); i++)
            {
                for (int j = 0; j < heightData.GetLength(1); j++)
                {
                    heightData[i, j] *= 0.6f;
                    heightData[i, j] += 0.4f * falloffData[i, j];
                    heightData[i, j] = Mathf.Clamp01(heightData[i, j]);
                    if (heightData[i, j] * Utils.mapHeight <= Utils.seaLevel)
                        originalOceanSet.Add(new Vector2Int(i, j));
                }
            }

        }

        if (useCombineOcean)
        {

            FindOcean();

            SetOceanDistance();

            SetMainLand();
        }
        data.SetHeights(0, 0, heightData);
        Debug.Log(this.GetType().ToString() + (Time.realtimeSinceStartup - t));
    }

    float[,] CreateOriginalHeightMap()
    {
        var mountainTerrain = new RidgedMultifractal(mountainFrequency, mountainLacunarity, mountainOctaves, Utils.Seed);


        var flatTerrain = new ScaleBias(flatScale, flatBias,
            new Billow
            {
                Seed = Utils.Seed,
                Frequency = flatFrequency
            });

        var terrainType = new Perlin
        {
            Seed = Utils.Seed,
            Frequency = 0.5,
            Persistence = 0.25
        };

        // Create the selector for turbulence
        var terrainSelector = new Select(flatTerrain, mountainTerrain, terrainType);
        terrainSelector.SetBounds(0, 1000);
        terrainSelector.FallOff = 0.125f;

        var finalTerrain = new Turbulence(new Perlin(Utils.Seed), new Perlin(Utils.Seed), new Perlin(Utils.Seed), finalPower, terrainSelector)
        {
            Frequency = finalFrequency
        };

        var heightMapBuilder = new Noise2D(Utils.mapSize, Utils.mapSize, finalTerrain);
        heightMapBuilder.GeneratePlanar(xOffset, xOffset + xSize, zOffset, zOffset + zSize);
        return heightMapBuilder.GetNormalizedData();
        
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


    void FindOcean()
    {
        int x = heightData.GetLength(0), z = heightData.GetLength(1);
        var tempQueue = new Queue<Vector2Int>();
        for(int i = 0; i < x - 1; i++)
        {
            Vector2Int v1 = new Vector2Int(i, 0), v2 = new Vector2Int(x - 1 - i, z - 1),
                       v3 = new Vector2Int(0, z - 1 - i), v4 = new Vector2Int(x - 1, i);
            SetCurOcean(v1, tempQueue);
            SetCurOcean(v2, tempQueue);
            SetCurOcean(v3, tempQueue);
            SetCurOcean(v4, tempQueue);
        }

        while (tempQueue.Count > 0)
        {
            var peekVec = tempQueue.Dequeue();
            myData.AddOceanGrid(peekVec);
            SetCurOcean(peekVec + Vector2Int.right, tempQueue);
            SetCurOcean(peekVec + Vector2Int.left, tempQueue);
            SetCurOcean(peekVec + Vector2Int.up, tempQueue);
            SetCurOcean(peekVec + Vector2Int.down, tempQueue);
            SetCurOcean(peekVec + Vector2Int.right + Vector2Int.up, tempQueue);
            SetCurOcean(peekVec + Vector2Int.right + Vector2Int.down, tempQueue);
            SetCurOcean(peekVec + Vector2Int.left + Vector2Int.up, tempQueue);
            SetCurOcean(peekVec + Vector2Int.left + Vector2Int.down, tempQueue);
        }

        AdjustHeight();
    }

    void SetCurOcean(Vector2Int cooridinate, Queue<Vector2Int> tempQue)
    {
        if (originalOceanSet.Contains(cooridinate))
        {
            originalOceanSet.Remove(cooridinate);
            tempQue.Enqueue(cooridinate);
        }
    }

    void AdjustHeight()
    {
        foreach(var vec in originalOceanSet)
        {
            heightData[vec.x, vec.y] = (Utils.seaLevel + 1.2f) / Utils.mapHeight;
            if (IsAdjustPos(vec + Vector2Int.right)) AvgCurPos(vec + Vector2Int.right);
            if (IsAdjustPos(vec + Vector2Int.left)) AvgCurPos(vec + Vector2Int.left);
            if (IsAdjustPos(vec + Vector2Int.up)) AvgCurPos(vec + Vector2Int.up);
            if (IsAdjustPos(vec + Vector2Int.down)) AvgCurPos(vec + Vector2Int.down);
            if (IsAdjustPos(vec + Vector2Int.right + Vector2Int.up)) AvgCurPos(vec + Vector2Int.right + Vector2Int.up);
            if (IsAdjustPos(vec + Vector2Int.right + Vector2Int.down)) AvgCurPos(vec + Vector2Int.right + Vector2Int.down);
            if (IsAdjustPos(vec + Vector2Int.left + Vector2Int.up)) AvgCurPos(vec + Vector2Int.left + Vector2Int.up);
            if (IsAdjustPos(vec + Vector2Int.left + Vector2Int.down)) AvgCurPos(vec + Vector2Int.left + Vector2Int.down);
        }
    }

    bool IsAdjustPos(Vector2Int cooridinate) => !originalOceanSet.Contains(cooridinate) && !myData.ContainOceanGrid(cooridinate);

    void AvgCurPos(Vector2Int cooridinate)
    {
        int x = cooridinate.x, z = cooridinate.y;
        if (x <= 0 || x >= heightData.GetLength(0) - 1 || z <= 0 || z >= heightData.GetLength(1) - 1)
            return;
        heightData[x, z] = (heightData[x - 1, z - 1] + heightData[x - 1, z] + heightData[x - 1, z + 1] +
                            heightData[x, z - 1]     + heightData[x, z]     + heightData[x, z + 1] +
                            heightData[x + 1, z - 1] + heightData[x + 1, z] + heightData[x + 1, z + 1]) / 9;
    }

    void SetOceanDistance()
    {
        Queue<Vector2Int> nodeQueue = new Queue<Vector2Int>();
        Utils.maxOceanDis = 0;

        for (int i = 0; i < oceanDis.GetLength(0); i++)
        {
            for(int j = 0; j < oceanDis.GetLength(1); j++)
            {
                if (myData.ContainOceanGrid(i, j))
                {
                    oceanDis[i, j] = -1;
                    nodeQueue.Enqueue(new Vector2Int(i,j));
                    myData.type[i, j] = TerrainType.ocean;
                }
                else
                {
                    oceanDis[i, j] = int.MinValue;
                }
            }
        }
        while (nodeQueue.Count > 0)
        {
            var node = nodeQueue.Dequeue();
            int x = node.x, z = node.y, value = oceanDis[x, z];

            if (SetCurOceanDistance(x - 1,  z - 1,  value))     nodeQueue.Enqueue(new Vector2Int(x - 1, z - 1));
            if (SetCurOceanDistance(x - 1,  z,      value))     nodeQueue.Enqueue(new Vector2Int(x - 1, z));
            if (SetCurOceanDistance(x - 1,  z + 1,  value))     nodeQueue.Enqueue(new Vector2Int(x - 1, z + 1));
            if (SetCurOceanDistance(x,      z - 1,  value))     nodeQueue.Enqueue(new Vector2Int(x,     z - 1));
            if (SetCurOceanDistance(x,      z + 1,  value))     nodeQueue.Enqueue(new Vector2Int(x,     z + 1));
            if (SetCurOceanDistance(x + 1,  z - 1,  value))     nodeQueue.Enqueue(new Vector2Int(x + 1, z - 1));
            if (SetCurOceanDistance(x + 1,  z,      value))     nodeQueue.Enqueue(new Vector2Int(x + 1, z));
            if (SetCurOceanDistance(x + 1,  z + 1,  value))     nodeQueue.Enqueue(new Vector2Int(x + 1, z + 1));
        }
    }

    bool SetCurOceanDistance(int x, int z, int value)
    {
        if (x < 0 || x >= Utils.mapSize || z < 0 || z >= Utils.mapSize)
        {
            return false;
        }
        if (oceanDis[x, z] == int.MinValue)
        {
            oceanDis[x, z] = value + 1;
            myData.type[x, z] = TerrainType.land;
            if (oceanDis[x, z] > Utils.maxOceanDis)
                Utils.maxOceanDis = oceanDis[x, z];
            return true;
        }
        else
            return false;
    }

    void SetMainLand()
    {
        Queue<Vector2Int> nodeQueue = new Queue<Vector2Int>();
        myData.AddMainLandGrid(new Vector2Int(Utils.mapSize / 2, Utils.mapSize / 2));
        nodeQueue.Enqueue(new Vector2Int(Utils.mapSize / 2, Utils.mapSize / 2));

        while (nodeQueue.Count > 0)
        {
            var node = nodeQueue.Dequeue();
            int x = node.x, z = node.y;

            if (myData.waterDistance[x, z] == 0) myData.AddLandContourGrid(node);

            if (SetCurMainLand(x - 1, z - 1)) nodeQueue.Enqueue(new Vector2Int(x - 1, z - 1));
            if (SetCurMainLand(x - 1, z)) nodeQueue.Enqueue(new Vector2Int(x - 1, z));
            if (SetCurMainLand(x - 1, z + 1)) nodeQueue.Enqueue(new Vector2Int(x - 1, z + 1));
            if (SetCurMainLand(x, z - 1)) nodeQueue.Enqueue(new Vector2Int(x, z - 1));
            if (SetCurMainLand(x, z + 1)) nodeQueue.Enqueue(new Vector2Int(x, z + 1));
            if (SetCurMainLand(x + 1, z - 1)) nodeQueue.Enqueue(new Vector2Int(x + 1, z - 1));
            if (SetCurMainLand(x + 1, z)) nodeQueue.Enqueue(new Vector2Int(x + 1, z));
            if (SetCurMainLand(x + 1, z + 1)) nodeQueue.Enqueue(new Vector2Int(x + 1, z + 1));
        }

    }

    bool SetCurMainLand(int x, int z)
    {
        if (x < 0 || x >= Utils.mapSize || z < 0 || z >= Utils.mapSize)
        {
            return false;
        }
        if (myData.type[x, z] == TerrainType.land && !myData.ContainMainLandGrid(x, z))
        {
            myData.AddMainLandGrid(x, z);
            return true;
        }
        return false;
    }

    public void Clear()
    {

        originalOceanSet.Clear();
        if (myData != null)
        {
            myData.Clear();
            data.SetHeights(0, 0, myData.finalElevation);
        }
    }

}
