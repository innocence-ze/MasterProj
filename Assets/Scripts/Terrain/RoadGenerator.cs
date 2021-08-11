using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RoadGenerator : MonoBehaviour, IGenerator
{
    int seedOffset = 0;
    public float maxSteepness;
    MyTerrainData myData;
    TerrainData data;
    public int roadIntersectionNum;
    public int minIntersectionDis;

    List<RoadNode> roadNodeList = new List<RoadNode>();
    HashSet<RoadNode> roadIntersections = new HashSet<RoadNode>();
    HashSet<RoadNode> roadNodes = new HashSet<RoadNode>();

    enum Direction
    {
        Up = 0, 
        Down, 
        Left, 
        Right,
    }

    private void OnDrawGizmos()
    {
        foreach (var p in roadNodeList)
        {
            Gizmos.DrawCube(new Vector3(p.Z - Utils.mapSize / 2, data.GetHeight(p.Z, p.X) - Utils.seaLevel, p.X - Utils.mapSize / 2), Vector3.one * 2);
        }

    }

    public void Clear()
    {
        seedOffset = 0;
        roadNodeList.Clear();
        roadIntersections.Clear();
        roadNodes.Clear();
    }

    public void Generate()
    {
        myData = TerrainManager.Singleton.MyData;
        data = TerrainManager.Singleton.Data;

        bool hasOriginalNode = false;

        while (!hasOriginalNode)
        {
            Random.InitState(Utils.Seed + (seedOffset++));
            int x = Random.Range(Utils.mapSize / 4, Utils.mapSize / 4 * 3);
            Random.InitState(Utils.Seed + (seedOffset++));
            int z = Random.Range(Utils.mapSize / 4, Utils.mapSize / 4 * 3);
            if (RoadNode.IsSuitableNode(x, z))
            {
                var originalNode = new RoadNode(x, z);
                if (IsSuitableNodeInMap(originalNode, out bool _))
                {
                    List<RoadNode> tempList = new List<RoadNode>();
                    tempList.Add(originalNode);
                    OriginalDirectionalGrowth(Direction.Up, originalNode, tempList);
                    OriginalDirectionalGrowth(Direction.Down, originalNode, tempList);
                    OriginalDirectionalGrowth(Direction.Left, originalNode, tempList);
                    OriginalDirectionalGrowth(Direction.Right, originalNode, tempList);
                    if (tempList.Count >= minIntersectionDis * 2)
                    {
                        hasOriginalNode = true;
                        roadIntersections.Add(originalNode);
                        foreach(var r in tempList)
                        {
                            roadNodeList.Add(r);
                            roadNodes.Add(r);
                            foreach(var i in r.GetGrids())
                            {
                                myData.AddRoadGrid(i);
                                myData.type[i.x, i.y] = TerrainType.road;
                            }
                        }
                    }
                }
            }
        }

        for(int i = 1, j = 0; i < roadIntersectionNum && j < roadIntersectionNum * 3; j++)
        {
            Random.InitState(Utils.Seed + (seedOffset++));
            int roadIndex = Random.Range(0, roadNodeList.Count);
            if (IsSuitableIntersection(roadNodeList[roadIndex]))
            {
                roadIntersections.Add(roadNodeList[roadIndex]);
                NodeGrowth(roadNodeList[roadIndex]);
                i++;
            }
        }
    }

    void OriginalDirectionalGrowth(Direction dir, RoadNode node, List<RoadNode> tempList)
    {
        RoadNode curNode = node[(int)dir];

        while (RoadNode.IsSuitableNode(curNode.X, curNode.Z) && IsSuitableNodeInMap(curNode, out bool _))
        {
            tempList.Add(curNode);
            curNode = curNode[(int)dir];
        }
    }

    bool IsSuitableIntersection(RoadNode inter)
    {
        if (myData.type[inter.X, inter.Z] == TerrainType.river)
            return false;
        bool result = true;
        foreach(var i in roadIntersections)
        {
            result = (inter.Distance(i) >= minIntersectionDis);
            if (!result)
                break;
        }
        return result;
    }

    void NodeGrowth(RoadNode node)
    {
        bool VerticalGrowth = true;
        if (roadNodes.Contains(node.Up) || roadNodes.Contains(node.Down))
        {
            VerticalGrowth = false;
        }

        if (VerticalGrowth)
        {
            DirectionalGrowth(Direction.Up, node);
            DirectionalGrowth(Direction.Down, node);
        }
        else
        {
            DirectionalGrowth(Direction.Left, node);
            DirectionalGrowth(Direction.Right, node);
        }
    }

    void DirectionalGrowth(Direction dir, RoadNode node)
    {

        RoadNode curNode = node[(int)dir];

        bool isRoad = false;
        while (RoadNode.IsSuitableNode(curNode.X, curNode.Z) && IsSuitableNodeInMap(curNode, out isRoad))
        {
            foreach (var p in curNode.GetGrids())
            {
                myData.type[p.x, p.y] = TerrainType.road;
                myData.AddRoadGrid(p);
            }
            roadNodeList.Add(curNode);
            roadNodes.Add(curNode);
            curNode = curNode[(int)dir];
        }
        if (isRoad)
        {
            foreach (var p in curNode.GetGrids())
            {
                myData.type[p.x, p.y] = TerrainType.road;
                myData.AddRoadGrid(p);
            }
            roadNodeList.Add(curNode);
            roadNodes.Add(curNode);
            roadIntersections.Add(curNode);
        }
    }

    bool IsSuitableNodeInMap(RoadNode node, out bool IsRoad)
    {
        IsRoad = false;
        int z = node.Z, x = node.X;
        float scaledX = 1.0f * x / data.heightmapResolution;
        float scaledZ = 1.0f * z / data.heightmapResolution;
        float angle = data.GetSteepness(scaledZ, scaledX);
        bool result =   (myData.ContainMainLandGrid(x, z) || myData.ContainRiverGrid(x, z)) &&
                        (myData.type[x, z] == TerrainType.land || myData.type[x, z] == TerrainType.river) &&
                        (angle < maxSteepness || myData.waterDistance[x, z] <= 2);

        return result;
    }

    //2*2µÄÍø¸ñ
    public struct RoadNode
    {
        public RoadNode this[int index]
        {
            get
            {
                if (index == (int)Direction.Left)
                    return Left; 
                if (index == (int)Direction.Right)
                    return Right; 
                if (index == (int)Direction.Up)
                    return Up; 
                if (index == (int)Direction.Down)
                    return Down;
                return 
                    this;
            }
            private set
            {

            }
        }

        public int X { get; private set; }
        public int Z { get; private set; }
        public static bool IsSuitableNode(int x, int z) => x >= 0 && z >= 0 && x + 1 <= Utils.mapSize - 1 && z + 1 <= Utils.mapSize - 1;
        public static bool IsSuitableNode(Vector2Int pos) => IsSuitableNode(pos.x, pos.y);

        public RoadNode(int x, int z)
        {
            this.X = x;
            this.Z = z;
        }
        public RoadNode(Vector2Int pos)
        {
            X = pos.x;
            Z = pos.y;
        }

        public List<Vector2Int> GetGrids()
        {
            return new List<Vector2Int>
            {
                new Vector2Int(X,Z),
                new Vector2Int(X,Z+1),
                new Vector2Int(X+1,Z),
                new Vector2Int(X+1,Z+1),
            };
        }

        public int Distance(RoadNode dis) => Mathf.Max(Mathf.Abs(X - dis.X) / 2, Mathf.Abs(Z - dis.Z) / 2);

        public RoadNode Up => new RoadNode(X, Z + 2);
        public RoadNode Down => new RoadNode(X, Z - 2);
        public RoadNode Left => new RoadNode(X - 2, Z);
        public RoadNode Right => new RoadNode(X + 2, Z);
    }
}
