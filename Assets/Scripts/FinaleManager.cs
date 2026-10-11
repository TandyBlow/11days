// FinaleManager.cs
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine.SceneManagement; 

public class FinaleManager : MonoBehaviour
{
    public static FinaleManager Instance { get; private set; }

    [Header("结局UI")]
    [SerializeField] private GameObject endScreenPanel;
    [SerializeField] private TextMeshProUGUI endScreenText;
    [SerializeField] private string mainMenuSceneName = "MainMenu";

    [Header("单位 Prefabs")]
    [SerializeField] private GameObject monsterPrefab;
    [SerializeField] private GameObject ruinPrefab; 

    [Header("防御塔设置")]
    [SerializeField] private Sprite towerSprite; 
    [SerializeField] public GameObject projectilePrefab;

    [Header("战斗数值 (基础值与动态加成)")]
    [Tooltip("怪物的初始基础血量")]
    [SerializeField] private float baseMonsterHealth = 50f;
    [Tooltip("怪物的初始基础伤害")]
    [SerializeField] private float baseMonsterDamage = 10f;
    [Tooltip("每存在一个建筑(非道路/农田)，怪物额外增加的血量")]
    [SerializeField] private float healthPerBuilding = 5f;
    [Tooltip("每存在一个建筑(非道路/农田)，怪物额外增加的伤害")]
    [SerializeField] private float damagePerBuilding = 0.5f;

    [Space(10)]
    [Tooltip("普通建筑的血量")]
    public float buildingHealth = 200f;
    [Tooltip("防御塔(装饰建筑)的血量")]
    public float towerHealth = 150f;
    [Tooltip("防御塔的伤害")]
    public float towerDamage = 15f;
    [Tooltip("防御塔的攻击速度(秒/次)")]
    public float towerAttackSpeed = 1.2f; 
    [Tooltip("怪物的攻击速度(秒/次)")]
    public float monsterAttackSpeed = 1.5f;
    
    [Header("其他设置")]
    [Tooltip("怪物在坏结局时走向的大海位置")]
    public Vector3 seaPosition = new Vector3(0, -20, 0); 
    
    public float currentMonsterHealth { get; private set; }
    public float currentMonsterDamage { get; private set; }

    private bool hasStarted = false;
    private bool isFinaleRunning = false;
    
    public List<MonsterAI> activeMonsters = new List<MonsterAI>();
    public List<Health> activeBuildings = new List<Health>();
    public List<ResidentAI> activeResidents = new List<ResidentAI>();

    private bool hasHeartOfOcean = false;

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        gameObject.SetActive(false); 
    }

    public void StartFinale()
    {
        if (hasStarted)
        {
            Debug.LogWarning("StartFinale 被重复调用，已阻止！");
            return;
        }
        hasStarted = true;
        Debug.Log("FinaleManager.StartFinale() 被调用。");
    
        gameObject.SetActive(true);
        StartCoroutine(FinaleActivationCoroutine());
    }

    private IEnumerator FinaleActivationCoroutine()
    {
        yield return StartCoroutine(UIManager.Instance.FadeScreen(false, 0.5f)); 
                                                                               
        isFinaleRunning = true;
        Debug.Log("--- 第十日审判开始 ---");
        if (UIManager.Instance != null)
        {
            UIManager.Instance.ShowOverlay();
            UIManager.Instance.HideGameUIForFinale();
        }
        if (KiraController.Instance != null) KiraController.Instance.gameObject.SetActive(false);
        
        CalculateMonsterStats();

        InitializeBuildings();
        SpawnMonsters();
        
        if (activeMonsters.Count == 0 && isFinaleRunning)
        {
            isFinaleRunning = false;
            if (hasHeartOfOcean && activeResidents.Count > 0)
            {
                StartCoroutine(ShowTrueEnd());
            }
            else
            {
                StartCoroutine(ShowGoodEnd());
            }
            yield break; 
        }
        if (activeBuildings.Count == 0 && isFinaleRunning)
        {
            isFinaleRunning = false;
            StartCoroutine(ShowBadEnd());
            yield break; 
        }
        
        StartCoroutine(CheckEndConditions());
    }

    private void CalculateMonsterStats()
    {
        int buildingCount = BuildManager.Instance.GetGrid().GetAllPlacedObjects()
            .Count(obj => obj.GetBuildingData().buildingType != BuildingType.Road && 
                          obj.GetBuildingData().buildingType != BuildingType.Farmland);

        currentMonsterHealth = baseMonsterHealth + (buildingCount * healthPerBuilding);
        currentMonsterDamage = baseMonsterDamage + (buildingCount * damagePerBuilding);
        
        Debug.Log($"<color=orange>最终战动态难度计算: 场上建筑数量 {buildingCount}。" +
                  $"怪物最终血量: {currentMonsterHealth} (基础{baseMonsterHealth} + 加成{buildingCount * healthPerBuilding}), " +
                  $"怪物最终伤害: {currentMonsterDamage} (基础{baseMonsterDamage} + 加成{buildingCount * damagePerBuilding})</color>");
    }

    private void InitializeBuildings()
    {
        List<PlacedObject> allPlacedObjects = BuildManager.Instance.GetGrid().GetAllPlacedObjects();
        Debug.Log($"找到 {allPlacedObjects.Count} 个已放置的建筑对象进行最终战初始化。");

        foreach (var placedObject in allPlacedObjects)
        {
            BuildingInstance buildingInstance = placedObject.GetInstance();
            if (buildingInstance == null) continue;

            BuildingData data = placedObject.GetBuildingData();
            Health health;

            if (data.isSpecialReplacement)
            {
                health = buildingInstance.gameObject.AddComponent<Health>();
                health.Initialize(towerHealth * 5, ruinPrefab);
                buildingInstance.gameObject.AddComponent<PurificationAura>();
                activeBuildings.Add(health);
                hasHeartOfOcean = true;
                Debug.Log("<color=cyan>海洋之心已激活！</color>");
            }
            else if (data.buildingType == BuildingType.Decoration || (GameManager.Instance.isNewGamePlus && data.isTrueVersion))
            {
                health = buildingInstance.gameObject.AddComponent<Health>();
                health.Initialize(towerHealth, null);
                buildingInstance.gameObject.AddComponent<TowerAI>();
                if (towerSprite != null) buildingInstance.GetComponent<SpriteRenderer>().sprite = towerSprite;
                activeBuildings.Add(health);
            }
            else if (data.buildingType == BuildingType.Residential || data.buildingType == BuildingType.Commercial)
            {
                health = buildingInstance.gameObject.AddComponent<Health>();
                health.Initialize(buildingHealth, ruinPrefab);
                activeBuildings.Add(health);
            }
        }
        Debug.Log($"初始化完成，可攻击的建筑/塔总数: {activeBuildings.Count}");
    }

    // 在 FinaleManager.cs 中

    private void SpawnMonsters()
    {
        int monsterCount = GameManager.Instance.population;
        Debug.Log($"总人口: {GameManager.Instance.population}, 生成怪物数量: {monsterCount}");
        if (monsterCount == 0) return;

        // 步骤1: 收集所有可能的生成点（住宅）
        List<Vector3> spawnPoints = new List<Vector3>();
        foreach (var buildingHealth in activeBuildings)
        {
            if (buildingHealth != null && buildingHealth.TryGetComponent<BuildingInstance>(out var instance) &&
                instance.GetBuildingData().buildingType == BuildingType.Residential)
            {
                spawnPoints.Add(buildingHealth.transform.position);
            }
        }

        // 步骤2: 处理没有住宅的边缘情况
        if (spawnPoints.Count == 0)
        {
            if (activeBuildings.Count > 0)
            {
                // 如果没有住宅但有其他建筑，随便找一个
                spawnPoints.Add(activeBuildings[Random.Range(0, activeBuildings.Count)].transform.position);
                Debug.LogWarning("没有找到住宅作为生成点，已使用随机建筑位置替代。");
            }
            else
            {
                // 连建筑都没有，只能在原点生成
                spawnPoints.Add(Vector3.zero);
                Debug.LogError("没有任何建筑可以作为怪物生成点！");
            }
        }

        // 步骤3: 【核心修改】循环生成怪物，并使用取模运算(%)来轮流使用生成点
        for (int i = 0; i < monsterCount; i++)
        {
            // 使用 i % spawnPoints.Count 来确保索引不会越界，并且能在所有生成点之间循环
            // 例如：i=0, 索引=0; i=1, 索引=1; i=2, 索引=0; i=3, 索引=1 ...
            int spawnPointIndex = i % spawnPoints.Count;
            Vector3 basePos = spawnPoints[spawnPointIndex];

            // 在基础位置上增加一个小的随机偏移，防止怪物完全重叠
            Vector3 finalPos = basePos + new Vector3(Random.Range(-0.5f, 0.5f), Random.Range(-0.5f, 0.5f), 0);

            GameObject monsterGO = Instantiate(monsterPrefab, finalPos, Quaternion.identity);

            // 复用 Prefab 上已有的 Health，只有缺失时才补一个。
            // 不能无条件 AddComponent：Monster.prefab 自身已带 Health，再加一个会得到两个 Health，
            // 而所有伤害来源（TowerAI/ResidentAI/Projectile）用 TryGetComponent<Health>() 取到的是
            // 组件列表里靠前的那个（Prefab 上的、从未 Initialize、血量恒为 0），TakeDamage 会被直接 return，
            // 导致怪物无法被击杀。
            Health monsterHealth;
            if (!monsterGO.TryGetComponent(out monsterHealth))
            {
                monsterHealth = monsterGO.AddComponent<Health>();
            }
            monsterHealth.Initialize(currentMonsterHealth, null);

            MonsterAI monsterAI = monsterGO.GetComponent<MonsterAI>();
            activeMonsters.Add(monsterAI);
        }
    }



    private IEnumerator CheckEndConditions()
    {
        while (isFinaleRunning)
        {
            yield return new WaitForSeconds(1f); 

            activeMonsters.RemoveAll(item => item == null);
            activeBuildings.RemoveAll(item => item == null);
            activeResidents.RemoveAll(item => item == null);

            if (hasHeartOfOcean && activeMonsters.Count == 0 && activeResidents.Count > 0 && isFinaleRunning)
            {
                isFinaleRunning = false;
                StartCoroutine(ShowTrueEnd());
                yield break;
            }
            
            if (activeMonsters.Count == 0 && isFinaleRunning)
            {
                isFinaleRunning = false;
                StartCoroutine(ShowGoodEnd());
                yield break;
            }
            
            if (activeBuildings.Count == 0 && isFinaleRunning)
            {
                isFinaleRunning = false;
                StartCoroutine(ShowBadEnd());
                yield break;
            }
        }
    }

    #region 单位管理
    public void OnMonsterDefeated(MonsterAI monster)
    {
        if (activeMonsters.Contains(monster)) activeMonsters.Remove(monster);
    }

    public void OnBuildingDestroyed(Health building)
    {
        if (activeBuildings.Contains(building)) activeBuildings.Remove(building);
    }

    public void OnResidentCreated(ResidentAI resident)
    {
        if (!activeResidents.Contains(resident)) activeResidents.Add(resident);
    }

    public void OnResidentDestroyed(ResidentAI resident)
    {
        if (activeResidents.Contains(resident)) activeResidents.Remove(resident);
    }
    #endregion

    #region 结局协程
    
    // 【新增】用于实现文字动画的协程
    private IEnumerator AnimateEndingText(string narrativeText, string endTitle)
    {
        endScreenPanel.SetActive(true);
        endScreenText.gameObject.SetActive(true);
        
        // --- 第一部分：淡入叙事文字 ---
        endScreenText.text = narrativeText;
        endScreenText.color = new Color(endScreenText.color.r, endScreenText.color.g, endScreenText.color.b, 0);
        float timer = 0f;
        while (timer < 2f)
        {
            timer += Time.unscaledDeltaTime;
            float alpha = Mathf.Lerp(0, 1, timer / 2f);
            endScreenText.color = new Color(endScreenText.color.r, endScreenText.color.g, endScreenText.color.b, alpha);
            yield return null;
        }
        
        yield return new WaitForSecondsRealtime(3f); // 停留

        // --- 第二部分：淡出叙事文字 ---
        timer = 0f;
        while (timer < 2f)
        {
            timer += Time.unscaledDeltaTime;
            float alpha = Mathf.Lerp(1, 0, timer / 2f);
            endScreenText.color = new Color(endScreenText.color.r, endScreenText.color.g, endScreenText.color.b, alpha);
            yield return null;
        }

        yield return new WaitForSecondsRealtime(0.5f);

        // --- 第三部分：淡入结局标题 ---
        endScreenText.text = endTitle;
        timer = 0f;
        while (timer < 2f)
        {
            timer += Time.unscaledDeltaTime;
            float alpha = Mathf.Lerp(0, 1, timer / 2f);
            endScreenText.color = new Color(endScreenText.color.r, endScreenText.color.g, endScreenText.color.b, alpha);
            yield return null;
        }
        
        yield return new WaitForSecondsRealtime(3f); // 结局标题停留
    }

    // 【修改】重写结局流程
    private IEnumerator ShowGoodEnd()
    {
        Debug.Log("结局：好结局");
        SaveManager.SetCompletionFlag(); 
        yield return new WaitForSeconds(1f);
        yield return StartCoroutine(UIManager.Instance.FadeScreen(true, 1.5f));

        yield return StartCoroutine(AnimateEndingText("审判结束了。", "Good End"));

        Time.timeScale = 1f;
        SceneManager.LoadScene(mainMenuSceneName);
    }

    private IEnumerator ShowBadEnd()
    {
        Debug.Log("结局：坏结局");
        SaveManager.SetCompletionFlag();

        foreach (var monster in activeMonsters)
        {
            if(monster != null) monster.GoToSea();
        }
        
        yield return new WaitForSeconds(5f); 
        yield return StartCoroutine(UIManager.Instance.FadeScreen(true, 1.5f));

        yield return StartCoroutine(AnimateEndingText("一切归于沉寂。\n沉寂。", "Bad End"));
        
        Time.timeScale = 1f;
        SceneManager.LoadScene(mainMenuSceneName);
    }

    private IEnumerator ShowTrueEnd()
    {
        Debug.Log("结局：真结局");
        SaveManager.DeleteCompletionFlag();
        SaveManager.DeleteSaveData();

        yield return new WaitForSeconds(2f);
        yield return StartCoroutine(UIManager.Instance.FadeScreen(true, 2f));

        yield return StartCoroutine(AnimateEndingText("你记起来了。\n火焰散尽，她终于得到解脱。\n岛屿迎来了真正的黎明。", "True End"));

        Time.timeScale = 1f;
        SceneManager.LoadScene(mainMenuSceneName);
    }
    #endregion
}
