using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using LibNoise;
using LibNoise.Generator;
using LibNoise.Operator;

[RequireComponent(typeof(Terrain))]
public class TerrainManager : MonoBehaviour
{
    public int seed;
    [Range(16,1024)]
    public int mapSize = 256;
    [Range(1, 100)]
    public int mapHeight = 30;
    [Range(0, 100)]
    public int seaLevel = 9;


    private static TerrainManager singleton = null;
    public static TerrainManager Singleton
    {
        get
        {
            if(singleton == null)
            {
                singleton = FindObjectOfType<TerrainManager>();
            }
            if (singleton == null)
            {
                Debug.LogError("Cannot find terrain manager");
            }
            return singleton;
        }
    }

    public TerrainData Data { get; private set; }
    public MyTerrainData MyData { get; private set; }

    [SerializeField]
    List<IGenerator> generator = new List<IGenerator>();

    private void Start()
    {
        var allComponents = GetComponents<Component>();
        foreach(var c in allComponents)
        {
            if (c is IGenerator)
            {
                generator.Add(c as IGenerator);
                Debug.Log(c.GetType());
            }
        }
    }

    public void Generate(int seed)
    {
        this.seed = seed;
        Utils.Seed = seed;
        for(int i = generator.Count - 1; i >= 0; i--)
        {
            generator[i].Clear();
        }

        foreach(var g in generator)
        {
            g.Generate();
        }
    }

    private void Update()
    {
    }

    private void OnValidate()
    {
        Utils.Seed = seed;

        Utils.mapSize = Mathf.ClosestPowerOfTwo(mapSize);
        mapSize = Utils.mapSize;

        Utils.mapHeight = mapHeight;
        if (seaLevel > mapHeight)
            seaLevel = mapHeight;
        Utils.seaLevel = seaLevel;

        if (MyData == null || MyData.Size != mapSize)
        {
            MyData = new MyTerrainData(mapSize);
        }

        if(Data == null || Data.size != new Vector3(mapSize, mapHeight, mapSize))
        {
            Data = Terrain.activeTerrain.terrainData;

            Data.heightmapResolution = mapSize + 1;
            Data.alphamapResolution = mapSize;
            Data.SetDetailResolution(mapSize, 8);
            Data.size = new Vector3(mapSize, mapHeight, mapSize);
        }

        transform.position = new Vector3(-mapSize / 2, -seaLevel, -mapSize / 2);

    }
}
