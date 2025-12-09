// BuildableAreaEditor.cs
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(BuildableArea))]
public class BuildableAreaEditor : Editor
{
    private BuildableArea area;

    private void OnEnable()
    {
        area = (BuildableArea)target;
    }

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
    }

    // 在Scene视图中绘制和处理句柄
    private void OnSceneGUI()
    {
        if (area.points == null) return;

        // 绘制多边形的连线
        Handles.color = Color.yellow;
        for (int i = 0; i < area.points.Count; i++)
        {
            Vector3 currentPoint = area.transform.TransformPoint(area.points[i]);
            Vector3 nextPoint = area.transform.TransformPoint(area.points[(i + 1) % area.points.Count]);
            Handles.DrawLine(currentPoint, nextPoint);
        }

        // 为每个点绘制可移动的句柄
        for (int i = 0; i < area.points.Count; i++)
        {
            EditorGUI.BeginChangeCheck();
            Vector3 worldPos = area.transform.TransformPoint(area.points[i]);
            Vector3 newWorldPos = Handles.PositionHandle(worldPos, Quaternion.identity);

            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(area, "Move Point");
                Vector3 newLocalPos = area.transform.InverseTransformPoint(newWorldPos);
                newLocalPos.z = 0; // 强制z为0
                area.points[i] = newLocalPos;
                area.points[i] = area.transform.InverseTransformPoint(newWorldPos);
                
            }
        }
        
        // 添加和删除点的逻辑
        Event e = Event.current;
        if (e.type == EventType.MouseDown)
        {
            // 按住Shift并左键点击来添加点
            if (e.button == 0 && e.shift)
            {
                // 将鼠标位置从屏幕空间转换到世界空间
                Ray worldRay = HandleUtility.GUIPointToWorldRay(e.mousePosition);
                // 我们假定在XY平面上操作
                Plane plane = new Plane(area.transform.forward, area.transform.position);
                if (plane.Raycast(worldRay, out float distance))
                {
                    Vector3 point = worldRay.GetPoint(distance);
                    Undo.RecordObject(area, "Add Point");
                    // 找到最近的线段并插入新点
                    int closestSegmentIndex = FindClosestSegment(point);
                    Vector3 localPoint = area.transform.InverseTransformPoint(point);
                    localPoint.z = 0; // 强制z为0
                    area.points.Insert(closestSegmentIndex + 1, localPoint);
                    e.Use(); // 消耗事件，防止其他操作响应
                }
            }
            // 按住Ctrl并左键点击来删除点
            else if (e.button == 0 && e.control)
            {
                float minDistance = 20f; // 屏幕空间像素距离
                int pointToDelete = -1;
                for (int i = 0; i < area.points.Count; i++)
                {
                    float dist = HandleUtility.DistanceToCircle(area.transform.TransformPoint(area.points[i]), 0);
                    if (dist < minDistance)
                    {
                        minDistance = dist;
                        pointToDelete = i;
                    }
                }

                if (pointToDelete != -1)
                {
                    Undo.RecordObject(area, "Delete Point");
                    area.points.RemoveAt(pointToDelete);
                    e.Use();
                }
            }
        }
    }

    private int FindClosestSegment(Vector3 point)
    {
        float minDistance = float.MaxValue;
        int closestIndex = -1;
        for (int i = 0; i < area.points.Count; i++)
        {
            Vector3 p1 = area.transform.TransformPoint(area.points[i]);
            Vector3 p2 = area.transform.TransformPoint(area.points[(i + 1) % area.points.Count]);
            float dist = HandleUtility.DistancePointToLineSegment(point, p1, p2);
            if (dist < minDistance)
            {
                minDistance = dist;
                closestIndex = i;
            }
        }
        return closestIndex;
    }
}
