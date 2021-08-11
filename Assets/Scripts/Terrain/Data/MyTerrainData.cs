using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
public enum TerrainType
{
    unknown,
    land,
    road,
    ocean,
    river,
}

public class MyTerrainData 
{
    public int Size { get; private set; }

    public TerrainType[,]   type;
    public int[,]           waterDistance;
    public float[,]         finalElevation;
    public bool[,]          hasTree;
    HashSet<Vector2Int> oceanSet;
    HashSet<Vector2Int> mainLandSet;
    HashSet<Vector2Int> landContourSet;
    HashSet<Vector2Int> riverSet;
    HashSet<Vector2Int> roadSet;

    public static void Copy(MyTerrainData src, MyTerrainData dis)
    {
        if (src == null)
            throw new ArgumentNullException("src is null");
        if (dis == null)
            throw new ArgumentNullException("dis is null");
        dis.Size = src.Size;
        int arrayLength = src.type.Length;
        Array.Copy(src.type, dis.type, arrayLength);
        Array.Copy(src.waterDistance, dis.waterDistance, arrayLength);
        Array.Copy(src.finalElevation, dis.finalElevation, arrayLength);
        Array.Copy(src.hasTree, dis.hasTree, arrayLength);
        dis.oceanSet = new HashSet<Vector2Int>(src.oceanSet);
        dis.mainLandSet = new HashSet<Vector2Int>(src.mainLandSet);
        dis.landContourSet = new HashSet<Vector2Int>(src.landContourSet);
        dis.riverSet = new HashSet<Vector2Int>(src.riverSet);
        dis.roadSet = new HashSet<Vector2Int>(src.roadSet);
    }

    public MyTerrainData(int size)
    {
        Size = size;
        type = new TerrainType[size, size];
        waterDistance = new int[size, size];
        finalElevation = new float[size, size];
        hasTree = new bool[size, size];
        oceanSet = new HashSet<Vector2Int>();
        mainLandSet = new HashSet<Vector2Int>();
        landContourSet = new HashSet<Vector2Int>();
        riverSet = new HashSet<Vector2Int>();
        roadSet = new HashSet<Vector2Int>();
    }

    public MyTerrainData(MyTerrainData src)
    {
        if(src == null)
            throw new ArgumentNullException("src is null");
        Size = src.Size;
        int arrayLength = src.type.Length;
        type = new TerrainType[Size, Size]; 
        waterDistance = new int[Size, Size];
        finalElevation = new float[Size, Size];
        hasTree = new bool[Size, Size];
        Array.Copy(src.type, type, arrayLength);
        Array.Copy(src.waterDistance, waterDistance, arrayLength);
        Array.Copy(src.finalElevation, finalElevation, arrayLength);
        Array.Copy(src.hasTree, hasTree, arrayLength);
        oceanSet = new HashSet<Vector2Int>(src.oceanSet);
        mainLandSet = new HashSet<Vector2Int>(src.mainLandSet);
        landContourSet = new HashSet<Vector2Int>(src.landContourSet);
        riverSet = new HashSet<Vector2Int>(src.riverSet);
        roadSet = new HashSet<Vector2Int>(src.roadSet);
    }

    public void Clear()
    {
        for(int i = 0; i < Size; i++)
        {
            for(int j = 0; j < Size; j++)
            {
                type[i, j] = TerrainType.unknown;
                waterDistance[i, j] = int.MinValue;
                finalElevation[i, j] = 0;
                hasTree[i, j] = false;
            }
        }
        oceanSet.Clear();
        mainLandSet.Clear();
        landContourSet.Clear();
        riverSet.Clear();
        roadSet.Clear();
    }

    public bool AddOceanGrid(Vector2Int coordinate) => oceanSet.Add(coordinate);
    public bool AddOceanGrid(int x, int z) => AddOceanGrid(new Vector2Int(x, z));

    public bool ContainOceanGrid(Vector2Int coordinate) => oceanSet.Contains(coordinate);
    public bool ContainOceanGrid(int x, int z) => ContainOceanGrid(new Vector2Int(x, z));

    public bool RemoveOceanGrid(Vector2Int coordinate) => oceanSet.Remove(coordinate);
    public bool RemoveOceanGrid(int x, int z) => RemoveOceanGrid(new Vector2Int(x, z));

    public HashSet<Vector2Int> GetOceanSet => oceanSet;


    public bool AddMainLandGrid(Vector2Int coordinate) => mainLandSet.Add(coordinate);
    public bool AddMainLandGrid(int x, int z) => AddMainLandGrid(new Vector2Int(x, z));

    public bool ContainMainLandGrid(Vector2Int coordinate) => mainLandSet.Contains(coordinate);
    public bool ContainMainLandGrid(int x, int z) => ContainMainLandGrid(new Vector2Int(x, z));

    public bool RemoveMainLandGrid(Vector2Int coordinate) => mainLandSet.Remove(coordinate);
    public bool RemoveMainLandGrid(int x, int z) => RemoveMainLandGrid(new Vector2Int(x, z));

    public HashSet<Vector2Int> GetMainLandSet => mainLandSet;


    public bool AddLandContourGrid(Vector2Int coordinate) => landContourSet.Add(coordinate);
    public bool AddLandContourGrid(int x, int z) => AddLandContourGrid(new Vector2Int(x, z));

    public bool ContainLandContourGrid(Vector2Int coordinate) => landContourSet.Contains(coordinate);
    public bool ContainLandContourGrid(int x, int z) => ContainLandContourGrid(new Vector2Int(x, z));

    public bool RemoveLandContourGrid(Vector2Int coordinate) => landContourSet.Remove(coordinate);
    public bool RemoveLandContourGrid(int x, int z) => RemoveLandContourGrid(new Vector2Int(x, z));

    public HashSet<Vector2Int> GetLandContourSet => landContourSet;


    public bool AddRiverGrid(Vector2Int coordinate) => riverSet.Add(coordinate);
    public bool AddRiverGrid(int x, int z) => AddRiverGrid(new Vector2Int(x, z));

    public bool ContainRiverGrid(Vector2Int coordinate) => riverSet.Contains(coordinate);
    public bool ContainRiverGrid(int x, int z) => ContainRiverGrid(new Vector2Int(x, z));

    public bool RemoveRiverGrid(Vector2Int coordinate) => riverSet.Remove(coordinate);
    public bool RemoveRiverGrid(int x, int z) => RemoveRiverGrid(new Vector2Int(x, z));

    public HashSet<Vector2Int> GetRiverSet => riverSet;


    public bool AddRoadGrid(Vector2Int coordinate) => roadSet.Add(coordinate);
    public bool AddRoadGrid(int x, int z) => AddRoadGrid(new Vector2Int(x, z));

    public bool ContainRoadGrid(Vector2Int coordinate) => roadSet.Contains(coordinate);
    public bool ContainRoadGrid(int x, int z) => ContainRoadGrid(new Vector2Int(x, z));

    public bool RemoveRoadGrid(Vector2Int coordinate) => roadSet.Remove(coordinate);
    public bool RemoveRoadGrid(int x, int z) => RemoveRoadGrid(new Vector2Int(x, z));

    public HashSet<Vector2Int> GetRoadSet => roadSet;

}
