// UnlockManager.cs
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class UnlockManager : MonoBehaviour
{
    // 二周目“真建筑”的克隆名字后缀（与存档中的名字一致，改名会导致老存档读不回真建筑）
    public const string TrueVersionSuffix = "_True";

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

        // 场景里没指定时兜底，避免解锁判定与“真建筑”解析静默失效
        if (buildingDatabase == null)
        {
            buildingDatabase = Resources.Load<BuildingDatabase>("BuildingDatabase");
        }
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
        if (original == null) return;

        CreateTrueVersion(original);
        Debug.Log($"<color=magenta>为 {original.buildingType} 类建筑 '{original.name}' 生成并解锁了真建筑版本。</color>");
    }

    // 生成（或复用）某个建筑的“真建筑”克隆。幂等，可以在读档重建时反复调用。
    private BuildingData CreateTrueVersion(BuildingData original)
    {
        string trueName = original.name + TrueVersionSuffix;

        BuildingData existing = runtimeTrueBuildings.FirstOrDefault(b => b != null && b.name == trueName);
        if (existing != null) return existing;

        BuildingData trueVersion = Instantiate(original);
        trueVersion.name = trueName;
        trueVersion.isTrueVersion = true;
        runtimeTrueBuildings.Add(trueVersion);
        UnlockBuilding(trueVersion.name);
        return trueVersion;
    }

    /// <summary>
    /// 按存档里的名字取真建筑。读档后 runtimeTrueBuildings 是空的（克隆不是资产，不会被序列化），
    /// 这里按需重建，保证上次解锁过的真建筑在“继续游戏”后依然能加载出来。
    /// </summary>
    public BuildingData GetTrueVersionByName(string buildingName)
    {
        if (string.IsNullOrEmpty(buildingName)) return null;

        BuildingData existing = runtimeTrueBuildings.FirstOrDefault(b => b != null && b.name == buildingName);
        if (existing != null) return existing;

        if (!buildingName.EndsWith(TrueVersionSuffix) || buildingDatabase == null) return null;

        string baseName = buildingName.Substring(0, buildingName.Length - TrueVersionSuffix.Length);
        BuildingData original = buildingDatabase.allBuildings.FirstOrDefault(b => b != null && b.name == baseName);
        if (original == null) return null;

        Debug.Log($"<color=magenta>读档时重建真建筑 '{buildingName}'。</color>");
        return CreateTrueVersion(original);
    }

    // 读档后重建运行时真建筑列表：解锁集合里存在 "<原名>_True" 的，就把对应克隆补回来
    private void RebuildRuntimeTrueVersions()
    {
        runtimeTrueBuildings.Clear();
        if (buildingDatabase == null) return;

        foreach (var building in buildingDatabase.allBuildings)
        {
            if (building == null || building.isTrueVersion || building.isSpecialReplacement) continue;
            if (building.buildingType != BuildingType.Residential && building.buildingType != BuildingType.Commercial) continue;

            if (unlockedBuildings.Contains(building.name + TrueVersionSuffix))
            {
                CreateTrueVersion(building);
            }
        }
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
        unlockedBuildings = unlockedNames != null ? new HashSet<string>(unlockedNames) : new HashSet<string>();
        // 读档后运行时克隆列表是空的，必须按已解锁的名字重建
        RebuildRuntimeTrueVersions();
        Debug.Log($"已从存档加载 {unlockedBuildings.Count} 个已解锁建筑，重建 {runtimeTrueBuildings.Count} 个真建筑。");
    }

    // 获取要保存的解锁状态
    public List<string> GetSaveData()
    {
        return unlockedBuildings.ToList();
    }
}
