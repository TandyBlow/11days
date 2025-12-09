// BuildableArea.cs
using UnityEngine;
using System.Collections.Generic;
// 存储边界点，提供查询方法
public class BuildableArea : MonoBehaviour
{
    public List<Vector3> points = new List<Vector3>();
    public bool IsWorldPointInside(Vector3 worldPoint)
    {
        // 将世界坐标点转换为 MyBuildableArea 对象的本地坐标
        // 目前存储的点为本地坐标
        Vector2 point = transform.InverseTransformPoint(worldPoint);

        int intersections = 0;
        for (int i = 0; i < points.Count; i++)
        {
            // 获取多边形的一条边
            Vector2 p1 = points[i];
            Vector2 p2 = points[(i + 1) % points.Count]; // % 用于处理最后一个点与第一个点的连线

            // 检查边是否与从point发出的水平射线相交
            if (p1.y > point.y != p2.y > point.y) // 边是否跨越了测试点的水平线
            {
                // 计算射线与边的交点的x坐标
                float intersectionX = (p2.x - p1.x) * (point.y - p1.y) / (p2.y - p1.y) + p1.x;
                if (point.x < intersectionX) intersections++; // 交点是否在测试点的右侧
            }
        }
        // 如果交点数为奇数，则点在内部
        return (intersections % 2) == 1;
    }
}
