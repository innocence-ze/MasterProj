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
    readonly HashSet<Vector2Int> oceanCoorSet;
    
    public MyTerrainData(int size)
    {
        Size = size;
        type = new TerrainType[size, size];
        waterDistance = new int[size, size];
        elevation = new float[size, size];
        finalElevation = new float[size, size];
        oceanDic = new Dictionary<int, List<Vector2Int>>();
        oceanCoorSet = new HashSet<Vector2Int>();
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
        oceanDic.Clear();
        oceanCoorSet.Clear();
    }

    /// <summary>
    /// 根据ocean序号获取ocean的所有坐标
    /// </summary>
    /// <param name="oceanIndex"></param>
    /// <param name="oceanCooridinate"></param>
    /// <returns></returns>
    public bool TryGetOcean(int oceanIndex, out List<Vector2Int>oceanCooridinate)
    {
        return oceanDic.TryGetValue(oceanIndex, out oceanCooridinate);
    }

    /// <summary>
    /// 获取ocean的数量
    /// </summary>
    /// <returns></returns>
    public int GetOceanCount()
    {
        return oceanDic.Count;
    }

    /// <summary>
    /// 添加一个新的ocean，它包含当前这个坐标，并返回ocean序号
    /// </summary>
    /// <param name="curCoor"></param>
    public int AddNewOcean(Vector2Int curCoor)
    {
        oceanDic.Add(oceanDic.Count, new List<Vector2Int>());
        oceanDic[oceanDic.Count - 1].Add(curCoor);
        oceanCoorSet.Add(curCoor);
        return oceanDic.Count - 1;
    }

    /// <summary>
    /// 把序号为index1 和 index2 的两个ocean合并
    /// </summary>
    /// <param name="index1"></param>
    /// <param name="index2"></param>
    /// <returns></returns>
    public int CombineOcean(int index1, int index2)
    {
        if (index1 > index2)
        {
            var temp = index2;
            index2 = index1;
            index1 = temp;
        }
        if (index1 < 0 || index2 >= oceanDic.Count)
        {
            return -1;
        }
        
        oceanDic[index1].AddRange(oceanDic[index2]);
        for(int i = index2; i < oceanDic.Count - 1; i++)
        {
            oceanDic[i] = oceanDic[i + 1];
        }
        oceanDic.Remove(oceanDic.Count - 1);
        return index1;
    }

    /// <summary>
    /// 把当前坐标添加到当前序号的ocean中
    /// </summary>
    /// <param name="oceanIndex"></param>
    /// <param name="oceanCooridinate"></param>
    /// <returns></returns>
    public bool AddToCurOcean(int oceanIndex, Vector2Int oceanCooridinate)
    {
        if (oceanDic.ContainsKey(oceanIndex))
        {
            if (!oceanDic[oceanIndex].Contains(oceanCooridinate))
            {
                oceanDic[oceanIndex].Add(oceanCooridinate);
                oceanCoorSet.Add(oceanCooridinate);
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// 获取当前序号的ocean中的坐标数量
    /// </summary>
    /// <param name="oceanIndex"></param>
    /// <returns></returns>
    public int GetCurOceanCount(int oceanIndex)
    {
        if (!oceanDic.ContainsKey(oceanIndex))
            return 0;
        return oceanDic[oceanIndex].Count;
    }

    /// <summary>
    /// 获得当前坐标的ocean的序号，若无则返回-1
    /// </summary>
    /// <param name="coordinate"></param>
    /// <returns></returns>
    public int GetCurOceanIndex(Vector2Int coordinate)
    {
        if (coordinate.x < 0 || coordinate.x >= Utils.mapSize || coordinate.y < 0 || coordinate.y >= Utils.mapSize)
            return -1;
        if (waterDistance[coordinate.x, coordinate.y] != -1)
            return -1;

        if (!oceanCoorSet.Contains(coordinate))
            return -1;

        foreach(var d in oceanDic)
        {
            if (d.Value.Contains(coordinate))
                return d.Key;
        }
        return -1;
    }

}
