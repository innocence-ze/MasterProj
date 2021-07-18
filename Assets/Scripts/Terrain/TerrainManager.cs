using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using LibNoise;
using LibNoise.Generator;
using LibNoise.Operator;

[RequireComponent(typeof(Terrain))]
public class TerrainManager : MonoBehaviour
{
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
    //[HideInInspector]
    public bool heightMapGenerated = false;

    private void Start()
    {

    }

    private void OnValidate()
    {
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
