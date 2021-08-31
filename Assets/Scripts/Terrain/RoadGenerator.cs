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
    public Material roadMat;

    List<RoadNode> roadNodeList = new List<RoadNode>();
    HashSet<RoadNode> roadIntersections = new HashSet<RoadNode>();
    HashSet<RoadNode> roadNodes = new HashSet<RoadNode>();

    List<RoadNode> waterNode = new List<RoadNode>();

    List<Vector2> uvs = new List<Vector2>();
    List<Vector3> vertices = new List<Vector3>();
    List<int> index = new List<int>();
    GameObject roadObj;

    float roadOffset = 0.1f;

    enum Direction
    {
        Up = 0, 
        Down,
        Right,
        Left, 
        self,
    }


    public void Clear()
    {
        seedOffset = 0;
        roadNodeList.Clear();
        roadIntersections.Clear();
        roadNodes.Clear();
        uvs.Clear();
        vertices.Clear();
        index.Clear();
        waterNode.Clear();
        if (roadObj != null)
            DestroyImmediate(roadObj);
    }

    public void Generate()
    {
        var t = Time.realtimeSinceStartup;
        myData = TerrainManager.Singleton.MyData;
        data = TerrainManager.Singleton.Data;

        bool hasOriginalNode = false;

        //生成初始节点
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
                    //在初始节点上下左右生产十字
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
                            AdjustRoadElevation(r);
                            foreach (var i in r.GetGrids())
                            {
                                myData.AddRoadGrid(i);
                                myData.type[i.x, i.y] = TerrainType.road;
                                
                            }
                        }
                    }
                    else
                    {
                        vertices.Clear();
                        uvs.Clear();
                        index.Clear();
                    }
                }
            }
        }

        //生成路网
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
        
        roadObj = new GameObject("RoadNetwork");
        roadObj.transform.position = new Vector3(-Utils.mapSize / 2, roadOffset, -Utils.mapSize / 2);
        var render = roadObj.AddComponent<MeshRenderer>();
        var filter = roadObj.AddComponent<MeshFilter>();
        var mesh = new Mesh();
        mesh.name = "roadMesh";
        mesh.vertices = vertices.ToArray();
        mesh.uv = uvs.ToArray();
        mesh.triangles = index.ToArray();
        filter.sharedMesh = mesh;
        render.sharedMaterial = roadMat;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        mesh.RecalculateTangents();

        data.SetHeights(0, 0, myData.finalElevation);

        Debug.Log(this.GetType().ToString() + (Time.realtimeSinceStartup - t));
    }

    void OriginalDirectionalGrowth(Direction dir, RoadNode node, List<RoadNode> tempList)
    {
        var elevation = GetFinalElevation(node);
        CalculateRoadVertices(node, ref elevation, Direction.self);
        RoadNode curNode = node[(int)dir];

        while (RoadNode.IsSuitableNode(curNode.X, curNode.Z) && IsSuitableNodeInMap(curNode, out bool _))
        {
            bool isRiver = false;
            foreach(var n in curNode.GetGrids())
            {
                if (myData.ContainRiverGrid(n))
                {
                    isRiver = true;
                    break;
                }

            }
            if (isRiver)
            {
                curNode = curNode[(int)dir];
                continue;
            }

            CalculateRoadVertices(curNode, ref elevation, dir);
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
        var elevation = GetFinalElevation(curNode);

        bool isRoad = false;
        while (RoadNode.IsSuitableNode(curNode.X, curNode.Z) && IsSuitableNodeInMap(curNode, out isRoad))
        {
            bool isRiver = false;
            foreach (var n in curNode.GetGrids())
            {
                if (myData.ContainRiverGrid(n))
                {
                    isRiver = true;
                    break;
                }

            }
            if (isRiver)
            {
                curNode = curNode[(int)dir];
                continue;
            }

            CalculateRoadVertices(curNode, ref elevation, dir);

            foreach (var p in curNode.GetGrids())
            {
                myData.type[p.x, p.y] = TerrainType.road;
                myData.AddRoadGrid(p);
            }
            AdjustRoadElevation(curNode);
            roadNodeList.Add(curNode);
            roadNodes.Add(curNode);
            curNode = curNode[(int)dir];
        }
        if (isRoad)
        {
            roadIntersections.Add(curNode);
        }

    }

    bool IsSuitableNodeInMap(RoadNode node, out bool IsRoad)
    {
        int z = node.Z, x = node.X;
        IsRoad = myData.type[x, z] == TerrainType.road;
        float scaledX = 1.0f * x / data.heightmapResolution;
        float scaledZ = 1.0f * z / data.heightmapResolution;
        float angle = data.GetSteepness(scaledZ, scaledX);
        bool result =   (myData.ContainMainLandGrid(x, z) || myData.ContainRiverGrid(x, z)) &&
                        (myData.type[x, z] == TerrainType.land || myData.type[x, z] == TerrainType.river) &&
                        (angle < maxSteepness || myData.waterDistance[x, z] <= 2);

        return result;
    }

    float GetFinalElevation(RoadNode node) => myData.finalElevation[node.X, node.Z] * Utils.mapHeight - Utils.seaLevel;

    float CalculateRoadElevation(RoadNode n, ref float neighborElevation)
    {
        if(myData.ContainRiverGrid(new Vector2Int(n.X, n.Z)))
        {
            return neighborElevation;
        }

        neighborElevation = GetFinalElevation(n);
        return neighborElevation;
    }

    void CalculateRoadVertices(RoadNode node, ref float neighborElevation, Direction dir)
    {
        int oriCount = vertices.Count;

        float y1 = CalculateRoadElevation(node, ref neighborElevation);
        float y2 = CalculateRoadElevation(node.Up, ref neighborElevation);
        float y3 = CalculateRoadElevation(node.Right, ref neighborElevation);
        float y4 = CalculateRoadElevation(node.Up.Right, ref neighborElevation);

        Vector3 offset = new Vector3(0, -roadOffset * 1.25f, 0);

        Vector3 v1 = new Vector3(node.Z, y1, node.X), v4 = new Vector3(node.Z + 2, y4, node.X + 2),
                v2 = new Vector3(node.Z + 2, y2, node.X), v3 = new Vector3(node.Z, y3, node.X + 2),
                v5 = v1 + offset, v6 = v2 + offset,
                v7 = v3 + offset, v8 = v4 + offset;

        Vector2 uv1 = new Vector2((float)node.Z / Utils.mapSize, (float)node.X / Utils.mapSize), uv4 = new Vector2((node.Z + 2.0f) / Utils.mapSize, (node.X + 2.0f) / Utils.mapSize),
                uv2 = new Vector2((node.Z + 2.0f) / Utils.mapSize, (float)node.X / Utils.mapSize), uv3 = new Vector2((float)node.Z / Utils.mapSize, (node.X + 2.0f) / Utils.mapSize);

        if ((int)dir > 1)
        {
            uvs.AddRange(new Vector2[18] { uv1, uv3, uv2, uv2, uv3, uv4,
                                           uv1, uv3, uv3, uv1, uv1, uv3,
                                           uv4, uv2, uv2, uv4, uv4, uv2});

            vertices.AddRange(new Vector3[18] { v1, v3, v2, v2, v3, v4,
                                                v1, v7, v3, v1, v5, v7,
                                                v4, v6, v2, v4, v8, v6 });
        }
        else
        {
            uvs.AddRange(new Vector2[18] { uv1, uv3, uv2, uv2, uv3, uv4,
                                           uv3, uv3, uv4, uv4, uv3, uv4,
                                           uv1, uv2, uv2, uv1, uv2, uv1});

            vertices.AddRange(new Vector3[18] { v1, v3, v2, v2, v3, v4,
                                                v3, v7, v4, v4, v7, v8,
                                                v1, v2, v6, v1, v6, v5});
        }
        var tempIndex = new int[18];
        for(int i = 0; i < tempIndex.Length; i++)
        {
            tempIndex[i] = oriCount + i;
        }
        index.AddRange(tempIndex);
    }

    void AdjustRoadElevation(RoadNode r)
    {
        Vector2Int o1 = new Vector2Int(r.X, r.Z);
        Vector2Int o2 = new Vector2Int(r.X + 2, r.Z);
        Vector2Int o3 = new Vector2Int(r.X, r.Z + 2);
        Vector2Int o4 = new Vector2Int(r.X + 2, r.Z + 2);
        myData.finalElevation[o1.x + 1, o1.y] = (myData.finalElevation[o1.x, o1.y] + myData.finalElevation[o2.x, o2.y]) / 2;
        myData.finalElevation[o3.x + 1, o3.y] = (myData.finalElevation[o3.x, o3.y] + myData.finalElevation[o4.x, o4.y]) / 2;
        myData.finalElevation[o1.x, o1.y + 1] = (myData.finalElevation[o1.x, o1.y] + myData.finalElevation[o3.x, o3.y]) / 2;
        myData.finalElevation[o2.x, o2.y + 1] = (myData.finalElevation[o2.x, o2.y] + myData.finalElevation[o4.x, o4.y]) / 2;
        myData.finalElevation[o1.x + 1, o1.y + 1] = (myData.finalElevation[o1.x, o1.y] + myData.finalElevation[o2.x, o2.y] +
                                                     myData.finalElevation[o3.x, o3.y] + myData.finalElevation[o4.x, o4.y]) / 4;
    }

    //2*2的网格
    public struct RoadNode
    {
        public RoadNode this[int index]
        {
            get
            {
                if (index == (int)Direction.Up)
                    return Up; 
                if (index == (int)Direction.Down)
                    return Down;
                if (index == (int)Direction.Right)
                    return Right;
                if (index == (int)Direction.Left)
                    return Left;
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
