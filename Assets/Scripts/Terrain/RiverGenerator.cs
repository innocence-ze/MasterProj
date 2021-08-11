
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RiverGenerator : MonoBehaviour, IGenerator
{
    int seedOffset = 0;
    public int riverNum = 2;
    public int nodeDistance = 20;
    public int nodeRange = 15;
    public float bankHeight = 2;
    public float deltaHeight = -0.2f;
    public float riverWidth = 8;
    public int riverBenchWidth = 3;
    public int minRiverNode = 10, maxRiverNode = 15;

    //定义域(0,1)，从入海口到发源地
    public AnimationCurve riverWidthWholeLengthCurve;
    public AnimationCurve riverBankCurve;

    List<Vector2Int> landContours = new List<Vector2Int>();
    //两个zone是河流与海岸线上点的最小间距的区域
    readonly HashSet<Vector2Int> contourZone = new HashSet<Vector2Int>();
    readonly HashSet<Vector2Int> riverNodeZone = new HashSet<Vector2Int>();
    readonly List<RiverNode> riverRoots = new List<RiverNode>();

    MyTerrainData myData, originalData;
    TerrainData data;
    readonly List<Vector2Int> riverBanks = new List<Vector2Int>();
    readonly List<Vector2Int> riverBottomPoints = new List<Vector2Int>();
    readonly List<Vector2Int> riverPoints = new List<Vector2Int>();
    readonly HashSet<Vector2Int> bottomSet = new HashSet<Vector2Int>();

    float highestBank = 0;

    public void Clear()
    {
        if (originalData != null)
        {
            MyTerrainData.Copy(originalData, myData);
            data.SetHeights(0, 0, myData.finalElevation);
            originalData = null;
        }

        landContours.Clear();
        contourZone.Clear();
        riverNodeZone.Clear();
        riverRoots.Clear();
        riverBanks.Clear();
        riverBottomPoints.Clear();
        riverPoints.Clear();
        bottomSet.Clear();
        seedOffset = 0;
    }

    public void Generate()
    {
        Clear();
        originalData = new MyTerrainData(TerrainManager.Singleton.MyData);
        myData = TerrainManager.Singleton.MyData;
        data = TerrainManager.Singleton.Data;
        landContours = new List<Vector2Int>(myData.GetLandContourSet);
        //设置岸边距离
        for(int i = 0; i < landContours.Count; i++)
        {
            AddRangeToZone(landContours[i], nodeRange, contourZone);
        }

        //生成河流的关键点，曲线与河岸
        for(int i = 0, j = 0; i < riverNum && j < riverNum * 5; j++)
        {
            //生成一条河流
            if(GenerateRiverNodes(out RiverNode root))
            {
                riverRoots.Add(root);
                List<Vector3> tempList = new List<Vector3>();
                //设置当前河流node的海拔高度
                float rootElevation = (Utils.seaLevel + deltaHeight) / Utils.mapHeight;
                var curNode = root;
                while (curNode != null)
                {
                    float th = 1 / (1 + Mathf.Pow((float)System.Math.E, -curNode.GetPriority())) - 0.5f;

                    //设置海拔
                   myData.finalElevation[curNode.x, curNode.z] = rootElevation + th * 3 / Utils.mapHeight;

                    tempList.Add(new Vector3(curNode.x, myData.finalElevation[curNode.x, curNode.z] * Utils.mapHeight, curNode.z));
                    curNode = curNode.GetChildren();
                }
                //获取河流长度与河流中的所有节点的坐标
                float length = Utils.PathLength(tempList.ToArray(), nodeDistance * nodeDistance, out List<Vector3> riverWayPoints);
                var tempRiverWayPoints = new List<Vector3>();
                foreach (var p in riverWayPoints)
                {
                    int x = Utils.Floor(p.x), z = Utils.Floor(p.z); 
                    var pos = new Vector2Int(x, z);
                    myData.finalElevation[x, z] = p.y / Utils.mapHeight;

                    tempRiverWayPoints.Add(p);
                    riverBottomPoints.Add(pos);
                    bottomSet.Add(pos);
                }
                //计算河岸
                riverBanks.AddRange(RiverBankCalculate(ref tempRiverWayPoints,2));
                i++;
            }
        }

        for(int i = 0; i < riverBottomPoints.Count; i++)
        {
            CalculateOneSideRiverElevation(riverBanks[2 * i], riverBottomPoints[i]);
            CalculateOneSideRiverElevation(riverBanks[2 * i + 1], riverBottomPoints[i]);
        }

        myData.GetRiverSet.UnionWith(riverPoints);
        myData.GetMainLandSet.RemoveWhere(x => myData.GetRiverSet.Contains(x));
        AdjustMainlandElevation();

        //应用到data上
        data.SetHeights(0, 0, myData.finalElevation);
    }

    void AdjustMainlandElevation()
    {
        foreach(var ml in myData.GetMainLandSet)
        {
            if (myData.finalElevation[ml.x, ml.y] < highestBank && !contourZone.Contains(ml))
                myData.finalElevation[ml.x, ml.y] = highestBank;
        }

        float[,] tempElevation = new float[Utils.mapSize, Utils.mapSize];
        System.Array.Copy(myData.finalElevation, tempElevation, myData.finalElevation.Length);
        foreach(var ml in myData.GetMainLandSet)
        {
            tempElevation[ml.x, ml.y] = GetSmoothElevation(ml.x, ml.y, (int)riverWidth / 2);
        }

        foreach(var cz in contourZone)
        {
            if (myData.ContainMainLandGrid(cz))
                tempElevation[cz.x, cz.y] = GetSmoothElevation(cz.x, cz.y, nodeRange / 2);
        }

        var riverBasin = new HashSet<Vector2Int>();
        foreach(var p in riverBanks)
        {
            AddRangeToZone(p, (int)riverWidth, riverBasin);
        }
        foreach (var b in riverBasin)
        {
            if(myData.ContainMainLandGrid(b))
                tempElevation[b.x, b.y] = GetSmoothElevation(b.x, b.y, (int)riverWidth);
        }
        System.Array.Copy(tempElevation, myData.finalElevation, tempElevation.Length);
    }

    float GetSmoothElevation(int x, int z, int range)
    {
        if (range < 0) return GetElevation(x, z);
        int count = (range * 2 + 1) * (range * 2 + 1);
        float value = GetElevation(x, z);
        for (int i = 1; i <= range; i++)
        {
            for (int j = -i; j < i; j++)
            {
                value += GetElevation(x + j, z + i);
                value += GetElevation(x + i, z - j);
                value += GetElevation(x - j, z - i);
                value += GetElevation(x - i, z + j);
            }
        }
        return value / count;
    }

    float GetElevation(int x, int z)
    {
        while (x < 0 || x >= Utils.mapSize)
        {
            if (x < 0) x = -x - 1;
            if (x >= Utils.mapSize) x = -x + 2 * Utils.mapSize - 1;
        } 
        while (z < 0 || z >= Utils.mapSize)
        {
            if (z < 0) z = -z - 1;
            if (z >= Utils.mapSize) z = -z + 2 * Utils.mapSize - 1;
        }
        if (myData.type[x, z] != TerrainType.ocean)
            return myData.finalElevation[x, z];
        else
            return (1.0f * Utils.seaLevel) / Utils.mapHeight;

    }

    void CalculateOneSideRiverElevation(Vector2Int bank, Vector2Int waterBottom)
    {
        if (bottomSet.Contains(bank))
            return;
        var _bankHeight = myData.finalElevation[waterBottom.x, waterBottom.y] + bankHeight / Utils.mapHeight;
        myData.finalElevation[bank.x, bank.y] = _bankHeight;
        if (_bankHeight > highestBank) highestBank = _bankHeight;

        Vector2Int dvector = bank - waterBottom;
        int nx = Mathf.Abs(dvector.x), ny = Mathf.Abs(dvector.y);       
        int signX = dvector.x > 0 ? 1 : -1,      
            signY = dvector.y > 0 ? 1 : -1;

        List<Vector2Int> waterSectionPoints = new List<Vector2Int>();
        Vector2Int p = waterBottom;
        waterSectionPoints.Add(p);

        for (int ix = 0, iy = 0; ix < nx || iy < ny;)
        {
            if ((0.5f + ix) / nx < (0.5f + iy) / ny)
            {
                // next step is horizontal
                p.x += signX;
                ix++;
            }
            else
            {
                // next step is vertical
                p.y += signY;
                iy++;
            }
            waterSectionPoints.Add(p);
            myData.type[p.x, p.y] = TerrainType.river;
            myData.waterDistance[p.x, p.y] = -1;
            CalculateRiverSideDis(p, riverBenchWidth);
        }

        riverPoints.AddRange(waterSectionPoints);

        float bottomElevation = myData.finalElevation[waterBottom.x, waterBottom.y],
              bankElevation = myData.finalElevation[bank.x, bank.y];
        int count = waterSectionPoints.Count;
        for(int i = 0; i < count; i++)
        {
            var wsp = waterSectionPoints[i];
            float percentage = 1.0f * i / count;
            float t = riverBankCurve.Evaluate(percentage);
            float elevation = (1 - t) * bottomElevation + t * bankElevation;
            myData.finalElevation[wsp.x, wsp.y] = elevation;
        }
    }

    void CalculateRiverSideDis(Vector2Int riverPos, int range)
    {
        int x = riverPos.x, z = riverPos.y;
        for (int i = 1; i <= range; i++)
        {
            for (int j = -i; j < i; j++)
            {
                Vector2Int v1 = new Vector2Int(x + j, z + i), v2 = new Vector2Int(x + i, z - j),
                           v3 = new Vector2Int(x - j, z - i), v4 = new Vector2Int(x - i, z + j);
                if (myData.waterDistance[v1.x, v1.y] > i) myData.waterDistance[v1.x, v1.y] = i;
                if (myData.waterDistance[v2.x, v2.y] > i) myData.waterDistance[v2.x, v2.y] = i;
                if (myData.waterDistance[v3.x, v3.y] > i) myData.waterDistance[v3.x, v3.y] = i;
                if (myData.waterDistance[v4.x, v4.y] > i) myData.waterDistance[v4.x, v4.y] = i;
            }
        }
    }

    //resultwaypoints是从入海口到发源口的
    List<Vector2Int> RiverBankCalculate(ref List<Vector3> riverWayPoints, float riverWidthExpand)
    {
        //是河流随机有大有小
        Vector3 _h = Vector3.zero;
        List<Vector2Int> result = new List<Vector2Int>(riverWayPoints.Count * 2);

        for (int i = 0; i < riverWayPoints.Count; i++)
        {
            Vector3 _vetexOffset = Vector3.zero;
            //河流流向
            if (i < riverWayPoints.Count - 1)
            {
                _vetexOffset = riverWayPoints[i + 1] - riverWayPoints[i];
            }
            //河流水平方向
            _h = Vector3.Cross(_vetexOffset, Vector3.up).normalized;
            //河流宽度
            Vector3 _wayPoint = riverWayPoints[i];
            float _halfRiverWidth = (riverWidth *
                (1 + Mathf.PerlinNoise(_wayPoint.x, _wayPoint.z)) +
                riverWidthExpand) * 0.5f;
            float _lengthPercents = (float)i / riverWayPoints.Count;
            _halfRiverWidth *= riverWidthWholeLengthCurve.Evaluate(_lengthPercents);
            //计算曲线两边的顶点位置
            var v1 = riverWayPoints[i] - _h * _halfRiverWidth;
            var v2 = riverWayPoints[i] + _h * _halfRiverWidth;
            var bank1 = new Vector2Int(Utils.Floor(v1.x), Utils.Floor(v1.z));
            var bank2 = new Vector2Int(Utils.Floor(v2.x), Utils.Floor(v2.z));
            if (bank1.x < 0) bank1.x = 0;   if (bank1.x >= Utils.mapSize) bank1.x = Utils.mapSize - 1;
            if (bank1.y < 0) bank1.y = 0;   if (bank1.y >= Utils.mapSize) bank1.y = Utils.mapSize - 1;
            if (bank2.x < 0) bank2.x = 0;   if (bank2.x >= Utils.mapSize) bank2.x = Utils.mapSize - 1;
            if (bank2.y < 0) bank2.y = 0;   if (bank2.y >= Utils.mapSize) bank2.y = Utils.mapSize - 1;
            result.Add(bank1);
            result.Add(bank2);
        }

        return result;
    }

    //生成一条河的节点，而后返回入海口处的节点
    bool GenerateRiverNodes(out RiverNode rootNode)
    {
        int traversalTime = 100 * riverNum;
        HashSet<Vector2Int> tempRiverZone = new HashSet<Vector2Int>();
        rootNode = null;

        //找到入海口
        Vector2Int root;
        do
        {
            if (traversalTime-- < 0)
                return false;
            Random.InitState(Utils.Seed + (seedOffset++));
            root = landContours[Random.Range(0, landContours.Count)];
        } while (riverNodeZone.Contains(root));
        RiverNode parent = new RiverNode(root), lastNode = parent;
        rootNode = parent;
        AddRangeToZone(root, nodeRange, tempRiverZone);

        Random.InitState(Utils.Seed + (seedOffset++));
        int riverNodeCount = Random.Range(minRiverNode - 1, maxRiverNode);


        for(int i = 0, j = 0; i < riverNodeCount && j < riverNodeCount * nodeDistance / 2; j++)
        {
            Random.InitState(Utils.Seed + (seedOffset++));
            int nodeOffset = Random.Range(-nodeDistance, nodeDistance + 1);
            if(IsSuitableRiverNode(nodeOffset, parent, out RiverNode curNode))
            {
                if (!tempRiverZone.Contains(curNode.GetPos()))
                {
                    AddRangeToZone(curNode.GetPos(), nodeRange, tempRiverZone);
                    curNode.SetParent(parent);
                    parent.SetChildren(curNode);
                    parent = curNode;
                    i++;
                    lastNode = curNode;
                }
            }
        }
        if (lastNode.GetPriority() >= riverNodeCount / 2)
        {
            while (lastNode != null)
            {
                AddRangeToZone(lastNode.GetPos(), nodeRange * 2, riverNodeZone);
                lastNode = lastNode.GetParent();
            }
            return true;
        }
        else
            return false;
    }

    //par为父节点，offset为偏移量的四个点随机选择一个而后判断是否符合各种规则
    //在地图上否，间距合适否，有交叉否
    bool IsSuitableRiverNode(int offset, RiverNode par,out RiverNode suitableNode)
    {
        Vector2Int parPos = par.GetPos();
        suitableNode = null;
        Vector2Int[] candidatePos = { parPos + new Vector2Int(nodeDistance, offset), parPos + new Vector2Int(-nodeDistance, offset),
                                      parPos + new Vector2Int(offset, nodeDistance), parPos + new Vector2Int(offset, -nodeDistance)};
        for(int i = 0; i < 4; i++)
        {
            var pos = candidatePos[(parPos.x + i) % 4];
            if (pos.x < 0 || pos.y < 0 || pos.x >= Utils.mapSize || pos.y >= Utils.mapSize)
                continue;
            var wd = myData.waterDistance;
            //在地图上是否符合，往内陆，在大陆，与岸边和其他河流的间距合适
            if (par.GetParent() != null && Utils.Dot(par.GetParent().GetPos() - par.GetPos(), par.GetPos() - pos) < 0) 
            {
                continue;
            }
            if (wd[parPos.x, parPos.y] * 0.9f <= wd[pos.x, pos.y] * 1.0f && 
                myData.GetMainLandSet.Contains(pos) && !contourZone.Contains(pos) && !riverNodeZone.Contains(pos))
            {
                //判断是否交叉
                bool hasCross = false;
                //判断是否和当前河流有交叉
                var p = par.GetParent();
                while(p != null && p.GetParent() != null)
                {
                    hasCross = Utils.IsSeggmentCross(p.GetPos(), p.GetParent().GetPos(), par.GetPos(), pos);
                    if (hasCross)
                        break;
                    p = p.GetParent();
                }
                if (hasCross)
                    continue;

                //判断是否和其他河流有交叉
                foreach(var r in riverRoots)
                {
                    var c = r;
                    while (c.GetChildren() != null)
                    {
                        hasCross = Utils.IsSeggmentCross(c.GetPos(), c.GetChildren().GetPos(), par.GetPos(), pos);
                        if (hasCross)
                            break;
                        c = c.GetChildren();
                    }
                    if (hasCross)
                        break;
                }
                if (!hasCross)
                {
                    suitableNode = new RiverNode(pos);
                    return true;
                }
            }
        }
        return false;
    }

    //以center为中心，range为边长一半的正方形区域添加到zone中
    void AddRangeToZone(Vector2Int center, int range, HashSet<Vector2Int> zone)
    {
        AddRangeToZone(center.x, center.y, range, zone);
    }

    void AddRangeToZone(int x, int z, int range, HashSet<Vector2Int> zone)
    {
        if (range < 0 || zone == null)
            return;
        AddToZone(new Vector2Int(x, z), zone);
        for (int i = 1; i <= range; i++)
        {
            for (int j = -i; j < i; j++)
            {
                Vector2Int v1 = new Vector2Int(x + j, z + i), v2 = new Vector2Int(x + i, z - j),
                           v3 = new Vector2Int(x - j, z - i), v4 = new Vector2Int(x - i, z + j);
                AddToZone(v1, zone);   AddToZone(v2, zone);
                AddToZone(v3, zone);   AddToZone(v4, zone);
            }
        }
    }
    void AddToZone(Vector2Int pos, HashSet<Vector2Int> zone)
    {
        if (pos.x < 0 || pos.y < 0 || pos.x >= Utils.mapSize || pos.y >= Utils.mapSize)
            return;
        zone.Add(pos);
    }


    public class RiverNode
    {
        public int x, z;
        RiverNode parent;
        RiverNode children;
        int priority;

        public RiverNode(int x, int z)
        {
            this.x = x; 
            this.z = z;
            parent = children = null;
            priority = 0;
        }

        public RiverNode(Vector2Int pos)
        {
            x = pos.x;
            z = pos.y;
            parent = children = null;
            priority = 0;
        }

        public void SetChildren(RiverNode r) => children = r;

        public void SetParent(RiverNode r) 
        {
            parent = r;
            priority = r.priority + 1;
        }

        public RiverNode GetParent() => parent;

        public RiverNode GetChildren() => children;

        public Vector2Int GetPos() => new Vector2Int(x, z);

        public int GetPriority() => priority;
    }

}
