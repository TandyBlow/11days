// BuildingInstance.cs
using UnityEngine;
using System.Linq;

public class BuildingInstance : MonoBehaviour
{
    // 定义建筑状态
    public enum BuildingState { plantsRequired, goodsRequired, rewardsRequired, waiting }

    [Header("组件引用")]
    [SerializeField]
    private GameObject interactionBubble; // 在Prefab中将小泡泡对象拖到这里
    private SpriteRenderer buildingRenderer;
    private SpriteRenderer bubbleRenderer;
    private IsometricSorter buildingSorter;

    [Header("泡泡图标")]
    [Tooltip("需要补货时显示的图标")]
    [SerializeField] private Sprite goodsBubbleSprite;
    [Tooltip("可以收获时显示的图标")]
    [SerializeField] private Sprite cashBubbleSprite;

    [Header("农田贴图")]
    [Tooltip("可种植状态的贴图")]
    [SerializeField] private Sprite plantableSprite;
    [Tooltip("生长中状态的贴图")]
    [SerializeField] private Sprite growingSprite;
    [Tooltip("可收获状态的贴图")]
    [SerializeField] private Sprite harvestableSprite;

    // 内部状态变量
    private BuildingData buildingData;
    private BuildingState currentState;
    private float timer;
    private int stockCounter; // 仅用于商业建筑

    // 道路连接状态
    private bool isConnectedToRoad = false;

    public void Initialize(BuildingData data)
    {
        this.buildingData = data;
        
        buildingRenderer = GetComponent<SpriteRenderer>();
        buildingSorter = GetComponent<IsometricSorter>();
        if (interactionBubble != null)
        {
            bubbleRenderer = interactionBubble.GetComponent<SpriteRenderer>();
            if (buildingRenderer != null && bubbleRenderer != null)
            {
                // 确保它们在同一个Sorting Layer
                bubbleRenderer.sortingLayerID = buildingRenderer.sortingLayerID;
                // 将泡泡的Order设置得比建筑高一点
                bubbleRenderer.sortingOrder = buildingRenderer.sortingOrder + 1;
            }
        
        }
        else
        {
             if(data.buildingType == BuildingType.Commercial || data.buildingType == BuildingType.Residential)
             {
                 Debug.LogError($"建筑 {data.buildingName} 没有分配交互气泡！");
             }
        }
        
        if (buildingRenderer == null)
        {
             Debug.LogError($"建筑 {data.buildingName} 的Prefab上没有找到SpriteRenderer！");
        }

        if (buildingData.buildingType == BuildingType.Residential || buildingData.buildingType == BuildingType.Commercial)
        {
            SetRoadConnection(false); 
        }
        else
        {
            SetRoadConnection(true);
        }

        switch (buildingData.buildingType)
        {
            case BuildingType.Residential:
                GameManager.Instance.AddPopulation(buildingData.population);
                ChangeState(BuildingState.waiting);
                break;
            case BuildingType.Farmland:
                ChangeState(BuildingState.plantsRequired);
                break;
            case BuildingType.Commercial:
                ChangeState(BuildingState.goodsRequired);
                break;
            case BuildingType.Road:
            case BuildingType.Decoration:
                ChangeState(BuildingState.waiting);
                if (interactionBubble != null)
                {
                    interactionBubble.SetActive(false);
                }
                break;
        }
    }

    private void Update()
    {
        // 农田的生长逻辑独立处理
        if (buildingData.buildingType == BuildingType.Farmland)
        {
            if (currentState == BuildingState.waiting) // "waiting" 状态对农田来说就是 "生长中"
            {
                timer -= Time.deltaTime;
                if (timer <= 0)
                {
                    ChangeState(BuildingState.rewardsRequired); // 生长完成，进入可收获状态
                }
            }
            return; // 农田不执行后续的商业逻辑
        }

        // 其他建筑（住宅、商业）的逻辑
        if (!isConnectedToRoad || currentState != BuildingState.waiting) return;

        timer -= Time.deltaTime;

        if (timer <= 0)
        {
            switch (buildingData.buildingType)
            {
                case BuildingType.Residential:
                    ChangeState(BuildingState.rewardsRequired);
                    break;
                case BuildingType.Commercial:
                    HandleCommercialSale();
                    break;
            }
        }
    }

    private void LateUpdate()
    {
        // 只要泡泡是激活的，就每帧强制更新它的 sortingOrder
        if (interactionBubble != null && interactionBubble.activeSelf && bubbleRenderer != null && buildingSorter != null)
        {
            // 核心逻辑：直接读取建筑的sortingOrder，然后+1
            // 这就绕过了所有复杂的计算，直接拿结果
            bubbleRenderer.sortingOrder = buildingSorter.GetComponent<Renderer>().sortingOrder + 1;
        }
    }


    private void HandleCommercialSale()
    {
        timer = 1f;
        float sellChance = GameManager.Instance.population / 30.0f;

        if (Random.value < sellChance)
        {
            stockCounter--;
            Debug.Log($"{buildingData.buildingName} 卖出了一件商品, 剩余 {stockCounter}");

            if (stockCounter <= 0)
            {
                ChangeState(BuildingState.rewardsRequired);
            }
        }
    }

    private void ChangeState(BuildingState newState)
    {
        currentState = newState;
        Debug.Log($"{buildingData.buildingName} 进入状态: {newState}");

        // --- 农田的特殊状态处理：更换贴图 ---
        if (buildingData.buildingType == BuildingType.Farmland)
        {
            if (interactionBubble != null) interactionBubble.SetActive(false); // 确保农田绝不显示泡泡

            switch (newState)
            {
                case BuildingState.plantsRequired: // 可种植
                    if(buildingRenderer != null) buildingRenderer.sprite = plantableSprite;
                    break;
                case BuildingState.waiting: // 生长中
                    if(buildingRenderer != null) buildingRenderer.sprite = growingSprite;
                    timer = buildingData.rewardsTime; // 开始生长计时
                    break;
                case BuildingState.rewardsRequired: // 可收获
                    if(buildingRenderer != null) buildingRenderer.sprite = harvestableSprite;
                    break;
            }
            return; // 农田的状态视觉逻辑到此为止
        }

        // --- 其他建筑的泡泡逻辑 ---
        bool shouldShowBubble = (newState == BuildingState.goodsRequired || newState == BuildingState.rewardsRequired) 
                                && isConnectedToRoad; 
                                
        if(interactionBubble != null)
        {
            interactionBubble.SetActive(shouldShowBubble);

            if (shouldShowBubble && bubbleRenderer != null)
            {
                switch (newState)
                {
                    case BuildingState.goodsRequired:
                        bubbleRenderer.sprite = goodsBubbleSprite;
                        break;
                    case BuildingState.rewardsRequired:
                        bubbleRenderer.sprite = cashBubbleSprite; // 农田已排除，这里只会是现金
                        break;
                }
            }
        }

        // 进入新状态时的初始化操作 (农田的计时器已在上面处理)
        if (newState == BuildingState.waiting)
        {
            switch (buildingData.buildingType)
            {
                case BuildingType.Residential:
                    timer = buildingData.rewardsTime;
                    break;
                case BuildingType.Commercial:
                    stockCounter = buildingData.goodsNeeded;
                    timer = 1f; // 启动1秒售卖计时器
                    break;
            }
        }
    }

    public void HandleInteraction()
    {
        if (!isConnectedToRoad)
        {
            Debug.Log($"{buildingData.buildingName} 未连接道路，无法操作！");
            return;
        }

        Debug.Log("我要处理了！");
        switch (currentState)
        {
            case BuildingState.waiting:
                return;

            case BuildingState.plantsRequired:
                // 点击农田进行种植
                ChangeState(BuildingState.waiting);
                break;

            case BuildingState.goodsRequired:
                if (GameManager.Instance.goods >= buildingData.goodsNeeded)
                {
                    GameManager.Instance.AddGoods(-buildingData.goodsNeeded);
                    ChangeState(BuildingState.waiting);
                }
                else
                {
                    Debug.Log("没货你补个屁");
                }
                break;

            case BuildingState.rewardsRequired:
                if (buildingData.buildingType == BuildingType.Farmland)
                {
                    GameManager.Instance.AddGoods(buildingData.rewards);
                    ChangeState(BuildingState.plantsRequired); // 收获后变为可种植状态
                }
                else // 住宅和商业
                {
                    GameManager.Instance.AddMoney(buildingData.rewards);
                    if (buildingData.buildingType == BuildingType.Residential)
                    {
                        ChangeState(BuildingState.waiting);
                    }
                    else // 商业
                    {
                        ChangeState(BuildingState.goodsRequired);
                    }
                }
                break;
        }
    }
    
    public void UpdateRoadConnectionStatus()
    {
        if (buildingData.buildingType != BuildingType.Residential && buildingData.buildingType != BuildingType.Commercial)
        {
            if (!isConnectedToRoad) SetRoadConnection(true);
            return;
        }
        
        var grid = BuildManager.Instance.GetGrid();
        if (grid == null) return;
        
        var gridObject = grid.GetGridObject(transform.position);
        if (gridObject == null) return;
        var placedObject = gridObject.GetPlacedObject();
        if (placedObject == null) return;

        var neighbors = grid.GetNeighborsOfArea(placedObject.GetOriginX(), placedObject.GetOriginY(), buildingData.width, buildingData.height);
        
        bool isNowConnected = neighbors.Any(neighbor => neighbor.GetBuildingData().buildingType == BuildingType.Road);
        
        if (isNowConnected != isConnectedToRoad)
        {
            SetRoadConnection(isNowConnected);
        }
    }

    private void SetRoadConnection(bool connected)
    {
        isConnectedToRoad = connected;
        
        if (connected)
        {
            Debug.Log($"{buildingData.buildingName} 已连接到道路，激活功能。");
            ChangeState(currentState); 
        }
        else
        {
            Debug.Log($"{buildingData.buildingName} 未连接道路，功能已禁用。");
            if (interactionBubble != null)
            {
                interactionBubble.SetActive(false);
            }
        }
    }

    public void RestoreState(BuildingSaveData data)
    {
        this.timer = data.timer;
        this.stockCounter = data.stockCounter;
        ChangeState(data.currentState);
    }

    public BuildingSaveData GetSaveData(int gridX, int gridY)
    {
        if (buildingData == null) return null;
        return new BuildingSaveData
        {
            buildingName = buildingData.name,
            gridX = gridX,
            gridY = gridY,
            currentState = this.currentState,
            timer = this.timer,
            stockCounter = this.stockCounter
        };
    }
    
    public BuildingData GetBuildingData()
    {
        return this.buildingData;
    }
}
