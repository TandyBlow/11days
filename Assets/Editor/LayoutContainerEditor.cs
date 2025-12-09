// LayoutContainerEditor.cs
// ！！！请务必将此脚本放置在项目中的 "Editor" 文件夹下 ！！！

using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

[CustomEditor(typeof(LayoutContainer))]
public class LayoutContainerEditor : Editor
{
    private LayoutContainer container;
    private GridSystem grid;

    private void OnEnable()
    {
        container = (LayoutContainer)target;
        // 订阅Scene视图的更新事件，这样我们才能在场景中绘图和交互
        SceneView.duringSceneGui += OnSceneGUI;

        // 初始化或更新虚拟网格
        // 当脚本被启用或代码被重新编译时，这会确保网格是最新的
        container.InitializeEditorGrid();
        if (container.editorGrid != null)
        {
            UpdateGridWithObjects();
        }
    }

    private void OnDisable()
    {
        // 当对象被取消选择或脚本被禁用时，取消订阅，防止内存泄漏
        SceneView.duringSceneGui -= OnSceneGUI;
    }

    // 绘制 Inspector 面板上的自定义UI
    public override void OnInspectorGUI()
    {
        // 绘制默认的 Inspector 字段（就是 LayoutContainer.cs 中定义的那些 public 字段）
        DrawDefaultInspector();

        // 如果关键引用没有设置，显示错误提示
        if (container.buildableArea == null || container.gridTransform == null)
        {
            EditorGUILayout.HelpBox("请先设置 Grid Transform 和 Buildable Area。", MessageType.Error);
            return;
        }

        EditorGUILayout.Space();

        // “刷新”按钮，用于手动强制更新网格和子物体对齐
        if (GUILayout.Button("Refresh Grid and Snap Children", GUILayout.Height(30)))
        {
            container.InitializeEditorGrid();
            SnapAllChildrenToGrid();
            UpdateGridWithObjects();
            SceneView.RepaintAll(); // 强制刷新场景视图，以便立即看到变化
        }

        EditorGUILayout.Space();

        // “保存”按钮
        if (GUILayout.Button("Save Valid Layout", GUILayout.Height(40)))
        {
            SaveLayout();
        }
    }

    // 这是在 Scene 视图中进行所有绘图和交互的核心方法
    private void OnSceneGUI(SceneView sceneView)
    {
        if (container == null || container.editorGrid == null) return;
        
        grid = container.editorGrid;

        // Unity 在移动物体时会设置一个 hasChanged 标志，我们用它来检测变化
        bool hasChanged = false;
        foreach (Transform child in container.transform)
        {
            if (child.hasChanged)
            {
                hasChanged = true;
                child.hasChanged = false; // 立即重置标志，避免重复处理
            }
        }

        // 如果检测到任何子物体被移动，就执行吸附和更新逻辑
        if (hasChanged)
        {
            SnapAllChildrenToGrid();
            UpdateGridWithObjects();
        }

        // 无论是否移动，每一帧都绘制所有子物体的预览框
        DrawPlacementPreviews();
    }

    // 将所有子物体吸附到最近的网格点
    private void SnapAllChildrenToGrid()
    {
        if (grid == null) return;
        
        foreach (Transform child in container.transform)
        {
            // 根据子物体的位置计算它所在的网格坐标
            grid.GetXY(child.position, out int x, out int y);
            // 再根据网格坐标计算出该格子精确的左下角世界坐标
            Vector3 snappedPosition = grid.GetWorldPosition(x, y);
            
            // 如果位置需要改变，则记录操作并更新位置
            if (child.position != snappedPosition)
            {
                // 使用 Undo.RecordObject 可以在 Unity 中通过 Ctrl+Z 撤销这次吸附操作
                Undo.RecordObject(child, "Snap to Grid");
                child.position = snappedPosition;
            }
        }
    }
    
    // 更新虚拟网格，标记哪些格子被占用了
    private void UpdateGridWithObjects()
    {
        if (grid == null) return;

        // 1. 先清空整个网格的占用信息
        for (int x = 0; x < grid.width; x++)
        {
            for (int y = 0; y < grid.height; y++)
            {
                grid.GetGridObject(x, y)?.ClearPlacedObject();
            }
        }

        // 2. 遍历所有子物体，在虚拟网格上重新“放置”它们
        foreach (Transform child in container.transform)
        {
            var info = child.GetComponent<LayoutBuildingInfo>();
            if (info == null || info.buildingData == null) continue;

            grid.GetXY(child.position, out int x, out int y);
            
            // 创建一个临时的 PlacedObject 用于在虚拟网格上占据格子
            var tempPlacedObject = new PlacedObject(info.buildingData, x, y, null);
            grid.PlaceObjectOnArea(x, y, info.buildingData.width, info.buildingData.height, tempPlacedObject);
        }
    }

    // 在Scene视图中绘制每个建筑的边界框和合法性提示
    private void DrawPlacementPreviews()
    {
        if (grid == null) return;

        foreach (Transform child in container.transform)
        {
            var info = child.GetComponent<LayoutBuildingInfo>();
            if (info == null || info.buildingData == null) continue;

            grid.GetXY(child.position, out int x, out int y);

            // 检查这个建筑的当前位置是否合法（不与其他建筑重叠，且在可建造区域内）
            bool canPlace = CheckPlacementValidity(x, y, info.buildingData, child);
            
            // 根据是否合法，选择绿色或红色
            Handles.color = canPlace ? new Color(0, 1, 0, 0.2f) : new Color(1, 0, 0, 0.2f);
            
            Vector3 worldPos = grid.GetWorldPosition(x, y);
            Vector3 size = new Vector3(info.buildingData.width * grid.cellSize, info.buildingData.height * grid.cellSize, 0.1f);
            
            // 绘制一个半透明的立方体来表示占地范围
            Handles.CubeHandleCap(0, worldPos + container.gridTransform.rotation * (size / 2), container.gridTransform.rotation, 1, EventType.Repaint);
        }
    }
    
    // 检查一个区域是否可以放置
    private bool CheckPlacementValidity(int startX, int startY, BuildingData buildingData, Transform selfTransform)
    {
        for (int x = 0; x < buildingData.width; x++)
        {
            for (int y = 0; y < buildingData.height; y++)
            {
                GridObject gridObject = grid.GetGridObject(startX + x, startY + y);
                // 如果格子超出网格边界，或格子本身不可建造（在BuildableArea之外）
                if (gridObject == null || !gridObject.isBuildable) return false;
                
                var placedObject = gridObject.GetPlacedObject();
                // 如果格子上已有其他物体
                if (placedObject != null)
                {
                    // 我们需要判断这个物体是不是自己。
                    // 因为 UpdateGridWithObjects 会把所有物体都放进去，所以检查时会发现自己占了自己的位置。
                    // 我们通过比较物体的起始坐标来判断。
                    if (placedObject.GetOriginX() != startX || placedObject.GetOriginY() != startY)
                    {
                        return false; // 这个格子被一个不是我们自己的物体占用了，所以重叠了
                    }
                }
            }
        }
        return true;
    }

    private void SaveLayout()
    {
        // 保存前，最后再完整地更新和检查一次，确保万无一失
        container.InitializeEditorGrid();
        UpdateGridWithObjects();
        grid = container.editorGrid;

        List<BuildingSaveData> buildingsData = new List<BuildingSaveData>();
        bool hasInvalidPlacement = false;

        foreach (Transform child in container.transform)
        {
            var info = child.GetComponent<LayoutBuildingInfo>();
            if (info == null || info.buildingData == null) continue;

            grid.GetXY(child.position, out int x, out int y);

            // 保存前最后一次检查合法性
            if (!CheckPlacementValidity(x, y, info.buildingData, child))
            {
                hasInvalidPlacement = true;
                Debug.LogError($"无法保存布局：建筑 '{child.name}' 放置位置非法（重叠或超出可建造区域）！", child.gameObject);
                continue; // 跳过这个非法的建筑
            }

            // 根据建筑类型设置合理的初始状态
            BuildingInstance.BuildingState initialState;
            switch (info.buildingData.buildingType)
            {
                case BuildingType.Residential: initialState = BuildingInstance.BuildingState.waiting; break;
                case BuildingType.Farmland: initialState = BuildingInstance.BuildingState.plantsRequired; break;
                case BuildingType.Commercial: initialState = BuildingInstance.BuildingState.goodsRequired; break;
                default: initialState = BuildingInstance.BuildingState.waiting; break;
            }

            BuildingSaveData saveData = new BuildingSaveData
            {
                buildingName = info.buildingData.name,
                gridX = x,
                gridY = y,
                currentState = initialState,
                timer = 0,
                stockCounter = 0
            };
            buildingsData.Add(saveData);
        }

        if (hasInvalidPlacement)
        {
            EditorUtility.DisplayDialog("Error", "布局中有建筑位置非法（在场景中显示为红色），请修正后再保存。详情请查看Console窗口的错误日志。", "OK");
            return;
        }

        if (buildingsData.Count == 0)
        {
            EditorUtility.DisplayDialog("Warning", "在容器下没有找到任何有效的建筑来保存。", "OK");
            return;
        }

        // --- 后续的保存资产文件逻辑 ---
        LayoutData newLayout = ScriptableObject.CreateInstance<LayoutData>();
        newLayout.layoutBuildings = buildingsData;
        
        string path = EditorUtility.SaveFilePanelInProject(
            "Save Layout Asset",
            container.name + ".asset", // 默认使用容器物体的名字作为文件名
            "asset",
            "请输入文件名以保存布局"
        );

        if (string.IsNullOrEmpty(path)) return; // 用户取消了保存

        AssetDatabase.CreateAsset(newLayout, path);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        EditorUtility.FocusProjectWindow();
        Selection.activeObject = newLayout; // 在Project窗口中高亮新创建的文件

        EditorUtility.DisplayDialog("Success", $"布局成功保存到: \n{path}", "OK");
    }
}
