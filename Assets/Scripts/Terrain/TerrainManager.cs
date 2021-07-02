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
    public int seaLevel;


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

    public TerrainData data;
    public MyTerrainData myData;


    private void Start()
    {
        UpdateTerrain();
    }

    private void OnValidate()
    {
        data = Terrain.activeTerrain.terrainData;

        Utils.mapSize = Mathf.ClosestPowerOfTwo(mapSize);
        mapSize = Utils.mapSize;

        Utils.mapHeight = mapHeight;
        if (seaLevel > mapHeight)
            seaLevel = mapHeight;
        Utils.seaLevel = seaLevel;

        if (myData == null || myData.Size != mapSize)
        {
            myData = new MyTerrainData(mapSize);
        }

    }

    public void UpdateTerrain()
    {
        transform.position = new Vector3(-mapSize / 2, -seaLevel, -mapSize / 2);

        TerrainData data = Terrain.activeTerrain.terrainData;

        data.heightmapResolution = mapSize + 1;
        data.alphamapResolution = mapSize;
        data.SetDetailResolution(mapSize, 8);

        data.size = new Vector3(mapSize, mapHeight, mapSize);

        data.SetHeights(0, 0, myData.finalElevation);

    }
}
