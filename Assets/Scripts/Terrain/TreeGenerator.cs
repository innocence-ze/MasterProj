using LibNoise;
using LibNoise.Generator;
using LibNoise.Operator;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TreeGenerator : MonoBehaviour,IGenerator
{
    int seedOffset = 0;
    //perlin noise
    [Header("noise")]
    public float frequency = 2;
    public float lacunarity = 2;
    public float persistence = 0.5f;
    public int octaves = 4;
    //noise 2d
    [Header("noise map")]
    public float size = 40;
    public float offset = 100;

    [Header("poisson disc distribution")]
    public int treeDistance = 1;

    [Header("confine")]
    [Range(0, 1)]
    public float noiseThreshold = 0.5f;
    [Range(0, 90)]
    public float maxSteepness = 70;
    [Range(0, 50)]
    public float minHeight = 14;
    [Range(0, 50)]
    public float maxHeight = 35;
    

    public List<GameObject> treePrefabs = new List<GameObject>();

    TerrainData data;
    MyTerrainData myData;

    GameObject treeRoot;
    readonly HashSet<Vector2Int> treePosSet = new HashSet<Vector2Int>();

    public void Clear()
    {
        seedOffset = 0;
        treePosSet.Clear();
        if (treeRoot != null)
            DestroyImmediate(treeRoot);
    }

    public void Generate()
    {
        var t = Time.realtimeSinceStartup;
        Clear();
        treeRoot = new GameObject("TreeRoot");
        treeRoot.transform.position = new Vector3(-Utils.mapSize / 2, -Utils.seaLevel, -Utils.mapSize / 2);

        data = TerrainManager.Singleton.Data;
        myData = TerrainManager.Singleton.MyData;

        var poissonTreeList = PoissonDisc(data.alphamapWidth, treeDistance);

        var noiseData = CreateNoiseData();

        for(int i = 0; i < poissonTreeList.Count; i++)
        {
            var pos = poissonTreeList[i];


            if (noiseData[pos.x,pos.y] > noiseThreshold && IsSuitablePosInMap(pos.x, pos.y, out float height))
            {
                Random.InitState(Utils.Seed + (seedOffset++));
                var tree = Instantiate(treePrefabs[Random.Range(0, treePrefabs.Count)], treeRoot.transform).transform;
                Random.InitState(Utils.Seed + (seedOffset++));
                tree.localScale = Vector3.one * Random.Range(0.3f, 0.4f);
                tree.localPosition = new Vector3(pos.x, height, pos.y);
            }
        }

        Debug.Log(this.GetType().ToString() + (Time.realtimeSinceStartup - t));
    }

    List<Vector2Int> PoissonDisc(int size, int r)
    {
        //init
        Random.InitState(Utils.Seed + (seedOffset++));
        int x = Random.Range(0, size);
        Random.InitState(Utils.Seed + (seedOffset++));
        int z = Random.Range(0, size);
        var firPos = new Vector2Int(x, z);
        List<Vector2Int> openList = new List<Vector2Int>
        {
            firPos
        };
        treePosSet.Add(firPos);
        List<Vector2Int> closeList = new List<Vector2Int>();

        int iterTime = 12 * r * r + 4 * r;
        int range = (r * 4 + 1) * (r * 4 + 1);

        while (openList.Count > 0)
        {
            Random.InitState(Utils.Seed + (seedOffset++));
            int index = Random.Range(0, openList.Count);
            var pos = openList[index];
            bool findPos = false;
            for (int i = 0; i < iterTime; i++)
            {
                Random.InitState(Utils.Seed + (seedOffset++));
                int rand = Random.Range(0, range);
                var newPos = ConvertIntToVector(pos, rand, 4 * r + 1);
                if(IsSuitablePos(newPos, size) && !treePosSet.Contains(newPos) && IsIsolationPos(newPos.x, newPos.y, r))
                {
                    findPos = true;
                    openList.Add(newPos);
                    treePosSet.Add(newPos);
                    break;
                }
            }
            if (!findPos)
            {
                openList.RemoveAt(index);
                closeList.Add(pos);
            }
        }
        return closeList;
    }

    Vector2Int ConvertIntToVector(Vector2Int center, int i, int l)
    {
        int x = i % l, z = i / l;
        int offset = (l - 1) / 2;
        x -= offset;
        z -= offset;
        return new Vector2Int(x, z) + center;
    }

    bool IsSuitablePos(Vector2Int pos, int size) => pos.x >= 0 && pos.y >= 0 && pos.x < size && pos.y < size;
    
    bool IsSuitablePosInMap(int x, int z, out float height)
    {
        height = data.GetHeight(x, z);
        if (myData.ContainRiverGrid(z, x) || myData.type[z, x] != TerrainType.land || myData.waterDistance[z, x] <= 1)
            return false;
        float scaledX = 1.0f * x / data.heightmapResolution;
        float scaledZ = 1.0f * z / data.heightmapResolution;
        float angle = data.GetSteepness(scaledX, scaledZ);
        return angle < maxSteepness && height > minHeight && height < maxHeight && !IsNextRoad(z, x);
    }

    bool IsNextRoad(int x, int z)
    {
        return myData.ContainRoadGrid(x, z) || myData.ContainRoadGrid(x, z + 1) || myData.ContainRoadGrid(x, z - 1) ||
            myData.ContainRoadGrid(x + 1, z) || myData.ContainRoadGrid(x - 1, z);
    }

    bool IsIsolationPos(int x, int z, int range = 1)
    {
        if (range <= 0) return false;
        bool result = false;
        for(int i = 1; i <= range; i++)
        {
            for(int j = -i; j < i; j++)
            {
                result = !(treePosSet.Contains(new Vector2Int((x + j), z + i)) || treePosSet.Contains(new Vector2Int((x + i), z - j))
                        || treePosSet.Contains(new Vector2Int((x - j), z - i)) || treePosSet.Contains(new Vector2Int((x - i), z + j)));
                if (!result) return result;
            }
        }
        return result;
    }

    float[,] CreateNoiseData()
    {
        var billow = new Billow
        {
            Seed = Utils.Seed,
            Frequency = frequency,
            Lacunarity = lacunarity,
            Persistence = persistence,
            OctaveCount = octaves,
        };
        var noiseBuilder = new Noise2D
        (
            data.detailWidth,
            data.detailWidth,
            new Exponent(2 ,new Turbulence(1, billow))
        );
        noiseBuilder.GeneratePlanar(offset, offset + size, offset, offset + size);
        return noiseBuilder.GetNormalizedData();
    }
}
