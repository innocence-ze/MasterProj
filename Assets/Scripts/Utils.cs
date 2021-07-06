using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class Utils
{
    public static int mapSize = 256;
    public static int mapHeight = 10;
    public static int seaLevel;
    public static int maxOceanDis;


    public static void DiagonalTransform(ref float[,] data)
    {
        for(int i = 0; i < data.GetLength(0); i++)
        {
            for(int j = i; j< data.GetLength(1); j++)
            {
                if (i == j)
                    continue;
                Swap(ref data[i, j], ref data[j, i]);
            }
        }
    }

    static void Swap(ref float f1, ref float f2)
    {
        var t = f1;
        f1 = f2;
        f2 = t;
    }

}
