using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class Utils
{
    public static int mapSize = 256;
    public static int mapHeight = 10;
    public static int seaLevel;
    public static int maxOceanDis;
    public static int Seed { get; set; }


    /// <summary>
    /// 判断以AB，CD为两端的两条线段是否相交
    /// </summary>
    /// <param name="A"></param>
    /// <param name="B"></param>
    /// <param name="C"></param>
    /// <param name="D"></param>
    /// <returns>
    /// true:相交; false:不相交
    /// </returns>
    public static bool IsSeggmentCross(Vector2Int A, Vector2Int B, Vector2Int C, Vector2Int D)
    {
        Vector2Int l1 = B - A, l2 = D - C;
        //平行
        if (Cross(l1, l2) == 0)
        {
            //共线
            if (Cross(C - B, l2) == 0)
            {
                SortNode(ref A, ref B);
                SortNode(ref C, ref D);
                if (Dot(C - A, C - B) < 0 || Dot(A - C, A - D) < 0)
                    return true;
                return false;
            }
            else
            {
                return false;
            }
        }
        else
        {
            if (Cross(l1, C - A) * Cross(l1, D - A) <= 0 && Cross(l2, A - C) * Cross(l2, B - C)<=0)
            {
                return true;
            }
        }
        return false;
    }

    static void SortNode(ref Vector2Int a, ref Vector2Int b)
    {
        if (a.x > b.x || (a.x == b.x && a.y > b.y)) 
        {
            Swap(ref a, ref b);
        }
    }

    static void Swap(ref Vector2Int a, ref Vector2Int b)
    {
        var temp = a;
        a = b;
        b = temp;
    }

    public static int Cross(Vector2Int l, Vector2Int r)
    {
        return l.x * r.y - l.y * r.x;
    }

    public static int Dot(Vector2Int l, Vector2Int r)
    {
        return l.x * r.x + l.y * r.y;
    }


    //计算路径的长度
    public static float PathLength(Vector3[] path, int smooth, out List<Vector3> pathNode)
    {
        pathNode = new List<Vector3>();
        float pathLength = 0;

        Vector3[] vector3s = PathControlPointGenerator(path);

        Vector3 prePt = Interp(vector3s, 0);
        if (prePt.x < 0) prePt.x = 0; if (prePt.x >= mapSize) prePt.x = mapSize - 1;
        if (prePt.z < 0) prePt.z = 0; if (prePt.z >= mapSize) prePt.z = mapSize - 1;
        pathNode.Add(prePt);

        int SmoothAmount = path.Length * smooth;
        for (int i = 1; i <= SmoothAmount; i++)
        {
            float pm = (float)i / SmoothAmount;
            Vector3 curPt = Interp(vector3s, pm);
            pathLength += Vector3.Distance(prePt, curPt);
            prePt = curPt;
            if (curPt.x < 0) curPt.x = 0; if (curPt.x >= mapSize) curPt.x = mapSize - 1;
            if (curPt.z < 0) curPt.z = 0; if (curPt.z >= mapSize) curPt.z = mapSize - 1;
            pathNode.Add(curPt);
        }

        return pathLength;
    }

    //生成曲线控制点,path.length>=2（为路径添加首尾点，便于绘制Cutmull-Rom曲线）
    static Vector3[] PathControlPointGenerator(Vector3[] path)
    {
        Vector3[] suppliedPath;
        Vector3[] vector3s;

        suppliedPath = path;

        int offset = 2;
        vector3s = new Vector3[suppliedPath.Length + offset];
        Array.Copy(suppliedPath, 0, vector3s, 1, suppliedPath.Length);

        //计算第一个控制点和最后一个控制点位置
        vector3s[0] = vector3s[1] + (vector3s[1] - vector3s[2]);
        vector3s[vector3s.Length - 1] = vector3s[vector3s.Length - 2] + (vector3s[vector3s.Length - 2] - vector3s[vector3s.Length - 3]);

        //首位点重合时，形成闭合的Catmull-Rom曲线
        if (vector3s[1] == vector3s[vector3s.Length - 2])
        {
            Vector3[] tmpLoopSpline = new Vector3[vector3s.Length];
            Array.Copy(vector3s, tmpLoopSpline, vector3s.Length);
            tmpLoopSpline[0] = tmpLoopSpline[tmpLoopSpline.Length - 3];
            tmpLoopSpline[tmpLoopSpline.Length - 1] = tmpLoopSpline[2];
            vector3s = new Vector3[tmpLoopSpline.Length];
            Array.Copy(tmpLoopSpline, vector3s, tmpLoopSpline.Length);
        }

        return (vector3s);
    }

    //Catmull-Rom曲线 参考：https://blog.csdn.net/u012154588/article/details/98977717
    static Vector3 Interp(Vector3[] pts, float t)
    {
        int numSections = pts.Length - 3;
        int currPt = Mathf.Min(Mathf.FloorToInt(t * (float)numSections), numSections - 1);
        float u = t * (float)numSections - (float)currPt;

        Vector3 a = pts[currPt];
        Vector3 b = pts[currPt + 1];
        Vector3 c = pts[currPt + 2];
        Vector3 d = pts[currPt + 3];

        return 0.5f * (
                        (-a + 3f * b - 3f * c + d) * (u * u * u) +
                        (2f * a - 5f * b + 4f * c - d) * (u * u) +
                        (-a + c) * u +
                        2f * b
                      );
    }

    public static int Floor(float t) => (int)(t + 0.5f);

}
