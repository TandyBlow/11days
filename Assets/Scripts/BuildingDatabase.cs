// --- Start of .\BuildingDatabase.cs ---

using UnityEngine;
using System.Collections.Generic;
using System.Linq;

[CreateAssetMenu(fileName = "BuildingDatabase", menuName = "Game/Building Database")]
public class BuildingDatabase : ScriptableObject
{
    public List<BuildingData> allBuildings = new List<BuildingData>();

    // 提供一个按类型筛选建筑的辅助方法
    public List<BuildingData> GetBuildingsByType(BuildingType type)
    {
        return allBuildings.Where(b => b.buildingType == type).ToList();
    }
}

// --- End of .\BuildingDatabase.cs ---
