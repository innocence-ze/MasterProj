using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum TerrainType
{
    land,
    sea,
    river,
}

public class MyTerrainData 
{
    public int Size { get; private set; }

    public TerrainType[,]   type;
    public int[,]           seaDistance;
    public float[,]         elevation;
    public float[,]         finalElevation;
    
    public MyTerrainData(int size)
    {
        Size = size;
        type = new TerrainType[size, size];
        seaDistance = new int[size, size];
        elevation = new float[size, size];
        finalElevation = new float[size, size];
    }

    public void Clear()
    {
        for(int i = 0; i < Size; i++)
        {
            for(int j = 0; j < Size; j++)
            {
                type[i, j] = TerrainType.land;
                seaDistance[i, j] = int.MinValue;
                elevation[i, j] = 0;
                finalElevation[i, j] = 0;
            }
        }
    }
}
