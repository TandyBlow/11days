// GridCoordinatePicker.cs
// ！！！请务必将此脚本放置在项目中的 "Editor" 文件夹下 ！！！

using UnityEngine;
using UnityEditor;

public class GridCoordinatePicker : Editor
{
    private static bool isPickerActive = false; // 控制调试功能是否开启
    private static BuildManager buildManager; // 缓存BuildManager的引用

    // 在顶部菜单栏创建一个开关 "Tools/Game/Show Grid Coordinates"
    [MenuItem("Tools/Game/Show Grid Coordinates")]
    private static void TogglePicker()
    {
        isPickerActive = !isPickerActive;
        // 更新菜单项的勾选状态
        Menu.SetChecked("Tools/Game/Show Grid Coordinates", isPickerActive);

        if (isPickerActive)
        {
            // 当功能开启时，订阅Scene视图的更新事件
            SceneView.duringSceneGui += OnSceneGUI;
            Debug.Log("Grid Coordinate Picker is now ON. Click in the Scene view to see grid coordinates.");
        }
        else
        {
            // 当功能关闭时，取消订阅
            SceneView.duringSceneGui -= OnSceneGUI;
            Debug.Log("Grid Coordinate Picker is now OFF.");
        }
    }

    // 这个方法确保菜单项的勾选状态是正确的
    [MenuItem("Tools/Game/Show Grid Coordinates", true)]
    private static bool ValidateTogglePicker()
    {
        Menu.SetChecked("Tools/Game/Show Grid Coordinates", isPickerActive);
        return true; // 总是启用菜单项
    }

    private static void OnSceneGUI(SceneView sceneView)
    {
        // 如果功能未激活，或游戏未在运行，则不执行任何操作
        if (!isPickerActive || !Application.isPlaying)
        {
            return;
        }

        // 尝试获取BuildManager和GridSystem的实例
        if (buildManager == null)
        {
            buildManager = BuildManager.Instance;
            if (buildManager == null)
            {
                // 如果游戏正在运行但找不到BuildManager，则不执行
                return;
            }
        }
        GridSystem grid = buildManager.GetGrid();
        if (grid == null)
        {
            return;
        }

        // 获取当前事件
        Event e = Event.current;

        // 将鼠标位置从屏幕空间转换为世界射线
        // 注意：这里我们使用的是sceneView的camera，因此这在Scene视图中更准确
        Ray ray = HandleUtility.GUIPointToWorldRay(e.mousePosition);
        
        // 使用BuildManager中的groundLayer进行射线检测
        if (Physics.Raycast(ray, out RaycastHit hit, 2000f, buildManager.groundLayer))
        {
            // 获取鼠标指向的网格坐标
            grid.GetXY(hit.point, out int x, out int y);

            // 在鼠标位置绘制一个标签显示坐标
            Handles.BeginGUI();
            // GUI坐标系和Scene视图坐标系不同，需要转换
            Vector2 guiPoint = HandleUtility.WorldToGUIPoint(hit.point);
            GUIStyle style = new GUIStyle();
            style.normal.textColor = Color.cyan;
            style.fontSize = 16;
            style.fontStyle = FontStyle.Bold;
            // 在鼠标指针旁边显示坐标
            GUI.Label(new Rect(guiPoint.x + 15, guiPoint.y - 10, 150, 40), $"Grid: ({x}, {y})", style);
            Handles.EndGUI();
            
            // 如果鼠标左键点击
            if (e.type == EventType.MouseDown && e.button == 0)
            {
                // 在Console中打印坐标，方便复制
                Debug.Log($"<color=cyan><b>Grid Coordinate Clicked: ({x}, {y})</b></color>");
                // 消费事件，防止触发游戏内的其他点击逻辑
                e.Use();
            }
        }
        
        // 强制重绘Scene视图，让标签实时更新
        sceneView.Repaint();
    }
}
