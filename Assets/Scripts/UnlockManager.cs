// UnlockManager.cs
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class UnlockManager : MonoBehaviour
{
    // public static UnlockManager Instance { get; private set; }
    private static UnlockManager _instance;
    public static UnlockManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<UnlockManager>();
            }
            return _instance;
        }
    }
    [SerializeField] private BuildingDatabase buildingDatabase;
    private HashSet<string> unlockedBuildings = new HashSet<string>();
    private List<BuildingData> runtimeTrueBuildings = new List<BuildingData>();

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;
    }

    private void OnEnable()
    {
        GameManager.OnDayStart += CheckForUnlocks;
    }

    private void OnDisable()
    {
        GameManager.OnDayStart -= CheckForUnlocks;
    }

    // 每天开始时调用，检查是否有新的建筑需要解锁
    public void CheckForUnlocks(int currentDay)
    {
        if (buildingDatabase == null) return;
        foreach (var building in buildingDatabase.allBuildings)
        {
            if (currentDay >= building.unlockDay && !IsUnlocked(building.name))
            {
                UnlockBuilding(building.name);
                
                // 【修改】 增加建筑类型判断：只为住宅和商业建筑生成真建筑
                if (GameManager.Instance.isNewGamePlus && !building.isTrueVersion && !building.isSpecialReplacement)
                {
                    if (building.buildingType == BuildingType.Residential || building.buildingType == BuildingType.Commercial)
                    {
                        GenerateAndUnlockTrueVersion(building);
                    }
                }
            }
        }
    }

    private void GenerateAndUnlockTrueVersion(BuildingData original)
    {
        // Instantiate a copy in memory
        BuildingData trueVersion = Instantiate(original);
        // Modify it to be the 'True' version
        trueVersion.name = original.name + "_True"; // Unique name for the HashSet
        trueVersion.isTrueVersion = true;
        // Store it
        runtimeTrueBuildings.Add(trueVersion);
        // Unlock it
        UnlockBuilding(trueVersion.name);
        Debug.Log($"<color=magenta>为 {original.buildingType} 类建筑 '{original.name}' 生成并解锁了真建筑版本。</color>");
    }
    
    // --- NEW: Public method for the UI to get the generated true buildings ---
    public List<BuildingData> GetUnlockedTrueBuildingsForCategory(BuildingType category)
    {
        return runtimeTrueBuildings
            .Where(b => b.buildingType == category && IsUnlocked(b.name))
            .ToList();
    }


    // 供UI查询某个建筑是否已解锁
    public bool IsUnlocked(string buildingName)
    {
        return unlockedBuildings.Contains(buildingName);
    }
    
    // 解锁一个建筑
    private void UnlockBuilding(string buildingName)
    {
        if (unlockedBuildings.Add(buildingName))
        {
            Debug.Log($"<color=cyan>建筑已解锁: {buildingName}</color>");
            // 你可以在这里触发一个事件，让UI显示解锁动画等
        }
    }

    // 清空所有解锁状态（用于开始新游戏）
    public void ResetUnlocks()
    {
        unlockedBuildings.Clear();
        runtimeTrueBuildings.Clear();
        Debug.Log("所有建筑解锁状态已重置。");
    }

    // 从存档数据加载解锁状态
    public void LoadFromSave(List<string> unlockedNames)
    {
        unlockedBuildings = new HashSet<string>(unlockedNames);
        Debug.Log($"已从存档加载 {unlockedBuildings.Count} 个已解锁建筑。");
    }

    // 获取要保存的解锁状态
    public List<string> GetSaveData()
    {
        return unlockedBuildings.ToList();
    }
}
