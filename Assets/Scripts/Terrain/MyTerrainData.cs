using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum TerrainType
{
    land,
    ocean,
    river,
}

public class MyTerrainData 
{
    public int Size { get; private set; }

    public TerrainType[,]   type;
    public int[,]           waterDistance;
    public float[,]         elevation;
    public float[,]         finalElevation;
    public readonly Dictionary<int, List<Vector2Int>> oceanDic;
    
    public MyTerrainData(int size)
    {
        Size = size;
        type = new TerrainType[size, size];
        waterDistance = new int[size, size];
        elevation = new float[size, size];
        finalElevation = new float[size, size];
        oceanDic = new Dictionary<int, List<Vector2Int>>();
    }

    public void Clear()
    {
        for(int i = 0; i < Size; i++)
        {
            for(int j = 0; j < Size; j++)
            {
                type[i, j] = TerrainType.land;
                waterDistance[i, j] = int.MinValue;
                elevation[i, j] = 0;
                finalElevation[i, j] = 0;
            }
        }
    }
    public bool TryGetOcean(int oceanIndex, out List<Vector2Int>oceanCooridinate)
    {
        return oceanDic.TryGetValue(oceanIndex, out oceanCooridinate);
    }

    public int GetOceanCount()
    {
        return oceanDic.Count;
    }

    public void AddNewOcean(Vector2Int curCoor)
    {
        oceanDic.Add(oceanDic.Count, new List<Vector2Int>());
        oceanDic[oceanDic.Count - 1].Add(curCoor);
    }

    public void CombineOcean(int index1, int index2)
    {
        if (index1 > index2)
        {
            var temp = index2;
            index2 = index1;
            index1 = temp;
        }
        if (index1 < 0 || index2 >= oceanDic.Count)
        {
            return;
        }
        
        oceanDic[index1].AddRange(oceanDic[index2]);
        for(int i = index2; i < oceanDic.Count - 1; i++)
        {
            oceanDic[i] = oceanDic[i + 1];
        }
        oceanDic.Remove(oceanDic.Count - 1);
    }

    public bool AddToCurOcean(int oceanIndex, Vector2Int oceanCooridinate)
    {
        if (oceanDic.ContainsKey(oceanIndex))
        {
            if (!oceanDic[oceanIndex].Contains(oceanCooridinate))
            {
                oceanDic[oceanIndex].Add(oceanCooridinate);
                return true;
            }
        }
        return false;
    }

    public int GetCurOceanCount(int oceanIndex)
    {
        if (!oceanDic.ContainsKey(oceanIndex))
            return 0;
        return oceanDic[oceanIndex].Count;
    }

    public int GetCurOceanIndex(Vector2Int coordinate)
    {
        if (coordinate.x < 0 || coordinate.x >= Utils.mapSize || coordinate.y < 0 || coordinate.y >= Utils.mapSize)
            return -1;
        if (waterDistance[coordinate.x, coordinate.y] != -1)
            return -1;

        foreach(var d in oceanDic)
        {
            if (d.Value.Contains(coordinate))
                return d.Key;
        }
        return -1;
    }

}
