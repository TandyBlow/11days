using UnityEngine;
using System.Collections.Generic;

public class GridSystem
{
    public int width; // width * height = 格子数量
    public int height;
    public float cellSize; // 单个格子大小
    private Vector3 originPosition; // 左下角格子的左下角坐标
    private GridObject[,] gridArray;
    private Transform gridTransform;

    public GridSystem(int width, int height, float cellSize, Vector3 originPosition, Transform gridTransform, bool[,] buildableMap)
    {
        this.width = width;
        this.height = height;
        this.cellSize = cellSize;
        this.originPosition = originPosition;
        this.gridTransform = gridTransform;

        gridArray = new GridObject[width, height];

        for (int x = 0; x < gridArray.GetLength(0); x++)
        {
            for (int y = 0; y < gridArray.GetLength(1); y++)
            {
                bool isBuildable = (buildableMap != null) ? buildableMap[x, y] : true;
                gridArray[x, y] = new GridObject(this, x, y, isBuildable);
            }
        }
    }

    // 将世界坐标转换为网格坐标
    public void GetXY(Vector3 worldPosition, out int x, out int y)
    {
        // Convert the world position to the grid's local position
        Vector3 localPosition = gridTransform.InverseTransformPoint(worldPosition);
        // The rest of the calculation is now in local space
        x = Mathf.FloorToInt((localPosition - originPosition).x / cellSize);
        y = Mathf.FloorToInt((localPosition - originPosition).y / cellSize);
    }

    // 将网格坐标转换为世界坐标 (返回格子左下角)
    public Vector3 GetWorldPosition(int x, int y)
    {
        // Calculate the position in the grid's local space first
        Vector3 localPosition = new Vector3(x, y) * cellSize + originPosition;

        // Convert the local position back to world position
        return gridTransform.TransformPoint(localPosition);

    }

    // 获取指定坐标的格子对象
    public GridObject GetGridObject(int x, int y)
    {
        if (x >= 0 && y >= 0 && x < width && y < height)
        {
            return gridArray[x, y];
        }
        else
        {
            return null;
        }
    }

    public GridObject GetGridObject(Vector3 worldPosition)
    {
        GetXY(worldPosition, out int x, out int y);
        return GetGridObject(x, y);
    }

    // 检查一个区域是否可以建造
    public bool CanBuildArea(int startX, int startY, int areaWidth, int areaHeight)
    {
        for (int x = 0; x < areaWidth; x++)
        {
            for (int y = 0; y < areaHeight; y++)
            {
                GridObject gridObject = GetGridObject(startX + x, startY + y);
                // 如果任何一个格子超出边界或不能建造，则整个区域都不能建造
                if (gridObject == null || !gridObject.CanBuild())
                {
                    return false;
                }
            }
        }
        return true;
    }

    // 放置一个建筑
    public void PlaceObjectOnArea(int startX, int startY, int areaWidth, int areaHeight, PlacedObject placedObject)
    {
        for (int x = 0; x < areaWidth; x++)
        {
            for (int y = 0; y < areaHeight; y++)
            {
                GetGridObject(startX + x, startY + y)?.SetPlacedObject(placedObject);
            }
        }
    }

    public List<PlacedObject> GetNeighborsOfArea(int startX, int startY, int width, int height)
    {
        List<PlacedObject> neighbors = new List<PlacedObject>();
        HashSet<PlacedObject> foundObjects = new HashSet<PlacedObject>(); // 用于去重
                                                                          // 遍历区域的底部和顶部边
        for (int x = startX; x < startX + width; x++)
        {
            // 底部下方
            AddNeighbor(x, startY - 1);
            // 顶部上方
            AddNeighbor(x, startY + height);
        }
        // 遍历区域的左侧和右侧边
        for (int y = startY; y < startY + height; y++)
        {
            // 左侧
            AddNeighbor(startX - 1, y);
            // 右侧
            AddNeighbor(startX + width, y);
        }
        void AddNeighbor(int x, int y)
        {
            GridObject gridObject = GetGridObject(x, y);
            if (gridObject != null)
            {
                PlacedObject placedObject = gridObject.GetPlacedObject();
                // 如果邻居格子上有一个建筑，并且我们还没添加过这个建筑实例
                if (placedObject != null && !foundObjects.Contains(placedObject))
                {
                    neighbors.Add(placedObject);
                    foundObjects.Add(placedObject);
                }
            }
        }
        return neighbors;
    }

    public List<PlacedObject> GetAllPlacedObjects()
    {
        List<PlacedObject> placedObjects = new List<PlacedObject>();
        HashSet<PlacedObject> foundObjects = new HashSet<PlacedObject>();
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                PlacedObject po = GetGridObject(x, y).GetPlacedObject();
                if (po != null && !foundObjects.Contains(po))
                {
                    placedObjects.Add(po);
                    foundObjects.Add(po);
                }
            }
        }
        return placedObjects;
    }

}