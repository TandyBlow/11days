// LayoutData.cs

using UnityEngine;
using System.Collections.Generic;
using System.Linq;

// 这是我们将在编辑器里直接填写的数据结构
[System.Serializable]
public class EditorLayoutEntry
{
    public BuildingData buildingData; // 直接引用 BuildingData，而不是用名字
    public Vector2Int gridPosition;
}

[CreateAssetMenu(fileName = "New LayoutData", menuName = "Game/Layout Data")]
public class LayoutData : ScriptableObject
{
    // 我们在 Inspector 里编辑这个列表
    public List<EditorLayoutEntry> layoutEntries = new List<EditorLayoutEntry>();

    // 这个旧的列表不再需要被直接编辑，但游戏加载逻辑会用到它
    // [HideInInspector] 标记让它不在 Inspector 中显示
    [HideInInspector]
    public List<BuildingSaveData> layoutBuildings;

    // 关键方法：将编辑器数据转换为运行时数据
    public List<BuildingSaveData> GetBuildingSaveData()
    {
        List<BuildingSaveData> saveDataList = new List<BuildingSaveData>();
        foreach (var entry in layoutEntries)
        {
            if (entry.buildingData == null) continue;

            // 根据建筑类型设置合理的初始状态
            BuildingInstance.BuildingState initialState;
            switch (entry.buildingData.buildingType)
            {
                case BuildingType.Residential: initialState = BuildingInstance.BuildingState.waiting; break;
                case BuildingType.Farmland: initialState = BuildingInstance.BuildingState.plantsRequired; break;
                case BuildingType.Commercial: initialState = BuildingInstance.BuildingState.goodsRequired; break;
                default: initialState = BuildingInstance.BuildingState.waiting; break;
            }

            saveDataList.Add(new BuildingSaveData
            {
                buildingName = entry.buildingData.name, // 运行时依然使用名字来查找
                gridX = entry.gridPosition.x,
                gridY = entry.gridPosition.y,
                currentState = initialState,
                timer = 0,
                stockCounter = 0
            });
        }
        return saveDataList;
    }
}
