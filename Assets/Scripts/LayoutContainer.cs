// LayoutContainer.cs (大幅改进)
using UnityEngine;
using System.Collections.Generic;

public class LayoutContainer : MonoBehaviour
{
    // 这个字段现在不再需要了，因为我们会直接在编辑器脚本里创建GridSystem
    // public BuildManager buildManagerPrefab; 

    // 我们需要这些参数来创建自己的虚拟网格
    [Header("Grid Configuration")]
    public int gridWidth = 45;
    public int gridHeight = 25;
    public float cellSize = 1f;
    public Vector3 gridOrigin = Vector3.zero;
    public Transform gridTransform; // 拖入BuildManager所在的那个旋转了的GameObject

    [Header("Buildable Area Source")]
    [Tooltip("拖入定义了可建造区域的 BuildableArea 对象")]
    public BuildableArea buildableArea;

    // 编辑器模式下的虚拟网格
    // 注意：这个grid只在编辑器脚本中使用，运行时不存在
    [System.NonSerialized] 
    public GridSystem editorGrid;

    private void OnValidate()
    {
        // 当Inspector中的值改变时，重新创建虚拟网格
        InitializeEditorGrid();
    }

    public void InitializeEditorGrid()
    {
        if (buildableArea == null || gridTransform == null)
        {
            editorGrid = null;
            return;
        }

        // --- 模拟 BuildManager 中的 InitializeGrid 逻辑 ---
        bool[,] buildableMap = new bool[gridWidth, gridHeight];
        // 创建一个临时网格用于坐标转换
        var tempGrid = new GridSystem(gridWidth, gridHeight, cellSize, gridOrigin, gridTransform, null);
        Vector3 halfCell = gridTransform.rotation * new Vector3(cellSize, cellSize, 0) * 0.5f;

        for (int x = 0; x < gridWidth; x++)
        {
            for (int y = 0; y < gridHeight; y++)
            {
                Vector3 cellCenter = tempGrid.GetWorldPosition(x, y) + halfCell;
                buildableMap[x, y] = buildableArea.IsWorldPointInside(cellCenter);
            }
        }
        
        // 创建真正的虚拟网格，包含可建造区域信息
        editorGrid = new GridSystem(gridWidth, gridHeight, cellSize, gridOrigin, gridTransform, buildableMap);
    }
}
