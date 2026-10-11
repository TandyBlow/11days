// GameManager.cs (已修改以配合SpriteRenderer闪回)
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum GameState { MainMenu, Normal, Building, Transitioning, Finale }

public class GameManager : MonoBehaviour
{
    private static GameManager _instance;
    public static GameManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<GameManager>();
                if (_instance == null)
                {
                    Debug.LogError("场景中找不到 GameManager 实例！");
                }
            }
            return _instance;
        }
    }
    public GameState CurrentState { get; private set; }
    public int day = 9;
    public float dayDuration = 300f;
    public float transDuration = 15f;
    public float flashDuration = 3f;
    private float dayTimer;
    public int population = 0;
    public int money = 100;
    public int goods = 50;
    public UIManager uiManager;
    public Transform worldRoot;
    public static bool IsNewGamePlusPending { get; set; } = false;
    public bool isNewGamePlus { get; private set; } = false;

    public static bool ShouldLoadGame = false;
    private LayoutData initialLayout;

    public BuildingData RequestedBuilding { get; private set; }
    
    public static event Action<GameState> OnGameStateChanged;
    public static event Action<int> OnDayStart;
    public static event Action<int> OnDayEnd;
    public static event Action<float, float> OnTimeUpdated;

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;
        
        initialLayout = Resources.Load<LayoutData>("InitialLayout");
        if (initialLayout == null)
        {
            Debug.LogError("!!!!!!!!!! 紧急错误：无法从 Resources 文件夹加载 InitialLayout！请检查文件名和位置！ !!!!!!!!!!");
        }
        if (IsNewGamePlusPending)
        {
            this.isNewGamePlus = true;
        }
    }

    private void Start()
    {
        if (ShouldLoadGame)
        {
            LoadGameState();
        }
        else
        {
            StartNewGame();
        }
    }

    // 新游戏的唯一入口：Start() 与“读档失败”的回退都走这里，
    // 避免出现 day/money/population 没重置、解锁状态没清空的半初始化对局。
    private void StartNewGame()
    {
        UnlockManager.Instance.ResetUnlocks();
        day = 1;
        money = 100;
        goods = 50;
        population = 0;
        if (initialLayout != null)
        {
            BuildManager.Instance.LoadLayout(initialLayout);
        }
        StartNewDay();
    }

    private void Update()
    {
        if (CurrentState != GameState.Normal) return;
        dayTimer -= Time.deltaTime;
        OnTimeUpdated?.Invoke(dayTimer, dayDuration);

        if (dayTimer <= 0)
        {
            UpdateGameState(GameState.Transitioning);
            OnDayEnd?.Invoke(day);
            StartCoroutine(DayTransitionCoroutine());
        }
    }

    private IEnumerator DayTransitionCoroutine()
    {
        UpdateGameState(GameState.Transitioning);
        CameraController camController = Camera.main.GetComponent<CameraController>();

        // 1. 镜头平滑移动
        if (camController != null)
        {
            yield return StartCoroutine(camController.MoveToView(camController.flashbackCameraPosition, camController.flashbackCameraSize, 1.5f));
        }

        // 2. 屏幕缓缓变黑
        yield return StartCoroutine(uiManager.FadeScreen(true, 1.0f));

        var flashbackEntry = FlashbackManager.Instance.GetFlashbackForDay(day);

        if (flashbackEntry != null)
        {
            // 在黑屏时，隐藏主游戏世界，并准备好闪回画面
            worldRoot.gameObject.SetActive(false);
            uiManager.HideInGameUI();
            FlashbackManager.Instance.ShowFlashback(flashbackEntry);
            
            // 突然撤掉黑屏，显示闪回
            uiManager.fadeScreen.color = Color.clear;
            
            // 等待
            yield return new WaitForSeconds(flashDuration);
            
            // 突然恢复黑屏
            uiManager.fadeScreen.color = Color.black;
            
            // 在黑屏时，清理闪回画面
            FlashbackManager.Instance.HideFlashback();
        }
        else
        {
            // --- 普通日流程 ---
            yield return new WaitForSeconds(1.0f);
        }

        // --- 后续通用流程 ---
        // 8. 在黑屏期间处理后台逻辑
        day++;
        OnDayStart?.Invoke(day); 

        if (day == 10)
        {
            // 【改动】在进入结局前，确保主世界是可见的
            worldRoot.gameObject.SetActive(true);
            uiManager.ShowInGameUI();
            UpdateGameState(GameState.Finale);
            if (FinaleManager.Instance != null) FinaleManager.Instance.StartFinale();
            else Debug.LogError("场景中找不到 FinaleManager!");
            yield break;
        }

        SaveGameState();

        // 【改动】在恢复相机前，确保主世界重新可见
        worldRoot.gameObject.SetActive(true);
        uiManager.ShowInGameUI();

        if (camController != null)
        {
            yield return StartCoroutine(camController.RestoreOriginalView(0.01f));
        }

        // 9. 屏幕缓缓变亮
        yield return StartCoroutine(uiManager.FadeScreen(false, 1.0f));

        // 10. 开始新的一天
        StartNewDay();
    }

    private void StartNewDay()
    {
        if (uiManager != null)
        {
            uiManager.UpdateAllDisplays(money, goods);
        }
        dayTimer = dayDuration;
        UpdateGameState(GameState.Normal);
        OnDayStart?.Invoke(day);
    }

    public void SaveGameState()
    {
        SaveData data = new SaveData();
        data.day = this.day;
        data.money = this.money;
        data.goods = this.goods;
        data.population = this.population;
        data.placedBuildings = BuildManager.Instance.GetPlacedBuildingsSaveData();
        data.unlockedBuildingNames = UnlockManager.Instance.GetSaveData();
        SaveManager.SaveGame(data);
    }
    public void LoadGameState()
    {
        SaveData data = SaveManager.LoadGame();
        if (data == null)
        {
            Debug.LogError("加载失败: 找不到或无法解析存档文件，改为开新游戏。");
            StartNewGame();
            return;
        }
        this.day = data.day;
        this.money = data.money;
        this.goods = data.goods;
        this.population = data.population;
        UnlockManager.Instance.LoadFromSave(data.unlockedBuildingNames);
        BuildManager.Instance.LoadBuildingsFromSave(data.placedBuildings);
        StartNewDay();
    }

    public void RequestEnterBuildMode(BuildingData buildingToBuild)
    {
        if (buildingToBuild == null) return;

        if (buildingToBuild.isSpecialReplacement)
        {
            // 特殊替换不是“进入建造模式”，而是直接替换场上已有的建筑
            if (CurrentState == GameState.Building)
            {
                RequestEnterNormalMode();
            }
            BuildManager.Instance.TrySpecialReplacement(buildingToBuild);
            return;
        }

        RequestedBuilding = buildingToBuild;

        if (CurrentState == GameState.Building)
        {
            // 已经处于建造模式时状态不会变化，OnGameStateChanged 不会触发，
            // 必须直接通知 BuildManager 换选建筑，否则再点另一个建筑没反应。
            BuildManager.Instance.SetSelectedBuilding(buildingToBuild);
        }
        else
        {
            UpdateGameState(GameState.Building);
        }
    }

    public void RequestEnterNormalMode()
    {
        UpdateGameState(GameState.Normal);
    }

    public void AddMoney(int amount)
    {
        money += amount;
        if (money < 0) money = 0;
        if (uiManager != null) uiManager.UpdateAllDisplays(money, goods);
    }

    public void AddGoods(int amount)
    {
        goods += amount;
        if (goods < 0) goods = 0;
        if (uiManager != null) uiManager.UpdateAllDisplays(money, goods);
    }
    
    public void AddPopulation(int amount)
    {
        population += amount;
        if (population < 0) population = 0;
    }

    // 人口由场上的住宅建筑推导，而不是在加载时累加。
    // 否则每次读档都会把住宅人口重新加一遍（存档里已经有一份），人口会不断翻倍。
    public void RecalculatePopulation()
    {
        int total = 0;
        GridSystem grid = BuildManager.Instance != null ? BuildManager.Instance.GetGrid() : null;
        if (grid != null)
        {
            foreach (PlacedObject placedObject in grid.GetAllPlacedObjects())
            {
                BuildingData data = placedObject.GetBuildingData();
                if (data != null && data.buildingType == BuildingType.Residential)
                {
                    total += data.population;
                }
            }
        }
        population = total;
    }

    private void UpdateGameState(GameState newState)
    {
        if (CurrentState == newState) return;
        CurrentState = newState;
        if (newState != GameState.Building)
        {
            RequestedBuilding = null;
        }
        OnGameStateChanged?.Invoke(newState);
    }
}
