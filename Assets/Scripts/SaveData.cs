// SaveData.cs
using System.Collections.Generic;

[System.Serializable]
public class SaveData
{
    public int day;
    public int money;
    public int goods;
    public int population;
    public List<BuildingSaveData> placedBuildings;
    public List<string> unlockedBuildingNames;

    public SaveData()
    {
        placedBuildings = new List<BuildingSaveData>();
    }
}

[System.Serializable]
public class BuildingSaveData
{
    public string buildingName; // 用于从BuildingDatabase中找回BuildingData
    public int gridX;
    public int gridY;

    // --- 需要保存的 BuildingInstance 状态 ---
    public BuildingInstance.BuildingState currentState;
    public float timer;
    public int stockCounter;
}
