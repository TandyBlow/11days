// SaveData.cs
using System.Collections.Generic;

[System.Serializable]
public class SaveData
{
    // 存档格式版本。以后改动字段语义时用它做迁移判断，现在只是写入。
    public const int CurrentVersion = 1;

    public int version = CurrentVersion;
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
