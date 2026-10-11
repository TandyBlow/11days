// BuildManager.cs
using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections.Generic;
using System.Linq;
using System.Collections;

public class BuildManager : MonoBehaviour
{
    public static BuildManager Instance { get; private set; }

    [Header("网格设置")]
    [SerializeField] public int gridWidth = 45;
    [SerializeField] public int gridHeight = 25;
    [SerializeField] public float cellSize = 1f;
    [SerializeField] public Vector3 gridOrigin;

    [Header("可建造区域")]
    [Tooltip("挂载了BuildableArea脚本的对象，用于定义可建造区域")]
    [SerializeField] private BuildableArea buildableArea;

    [Header("视觉表现")]
    [SerializeField] private Transform gridVisualParent;
    [SerializeField] private GameObject gridCellVisualPrefab;

    [Header("数据")]
    public BuildingDatabase buildingDatabase;
    [SerializeField] public Transform buildingParent;

    [SerializeField] private BuildingData fountainBuildingData;
    public LayerMask groundLayer;
    private GridSystem grid; 
    private bool isInBuildMode = false;
    private BuildingData selectedBuilding;

    private GameObject previewObject; // 用于存放半透明的预览建筑
    private Vector2Int previewPosition; // 预览建筑所在的网格坐标
    private SpriteRenderer previewRenderer; // 预览建筑的SpriteRenderer，方便修改颜色

    private GameObject[,] gridVisualArray;
    private List<Vector2Int> highlightedCells;
    private Color defaultGridColor = new Color(1, 1, 1, 0.2f);
    private BuildingInstance fountainInstance;

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        buildingDatabase = Resources.Load<BuildingDatabase>("BuildingDatabase"); // <-- USE YOUR FILENAME
        if (buildingDatabase == null)
        {
            Debug.LogError("!!!!!!!!!! BUILD MANAGER FIX FAILED: Cannot load BuildingDatabase from Resources! Check name/location! !!!!!!!!!!");
        }
        InitializeGrid();
        gridVisualArray = new GameObject[gridWidth, gridHeight];
        highlightedCells = new List<Vector2Int>();
        CreateGridVisuals();
    }
    
    public void TrySpecialReplacement(BuildingData replacementData)
    {
        if (fountainInstance == null)
        {
            Debug.LogError($"无法执行替换：目标对象(喷泉)的引用丢失或未在加载时找到。请确保喷泉在初始布局中，并且其BuildingData已在BuildManager中指定。");
            return;
        }
        if (GameManager.Instance.money < replacementData.cost)
        {
            Debug.Log("金钱不足，无法建造 " + replacementData.buildingName);
            // 这里可以添加UI提示
            return;
        }
        Debug.Log($"正在将喷泉替换为 {replacementData.buildingName}.");
        GameManager.Instance.AddMoney(-replacementData.cost);
        // 使用新的数据重新初始化现有的喷泉实例
        fountainInstance.Initialize(replacementData);
        // 更新网格上的对象信息
        grid.GetXY(fountainInstance.transform.position, out int x, out int y);
        var gridObject = grid.GetGridObject(x, y);
        if (gridObject != null && gridObject.GetPlacedObject() != null)
        {
            // 创建一个新的PlacedObject并替换掉网格中的旧引用
            var newPlacedObject = new PlacedObject(replacementData, x, y, fountainInstance);
            grid.PlaceObjectOnArea(x, y, replacementData.width, replacementData.height, newPlacedObject);
        }
    }


    private void OnEnable()
    {
        GameManager.OnGameStateChanged += HandleGameStateChange;
    }

    private void OnDisable()
    {
        GameManager.OnGameStateChanged -= HandleGameStateChange;
    }

    private void Update()
    {
        if (!isInBuildMode) return;
        if (previewObject == null)
        {
            UpdateGhostPreview();
        }
        // UpdateHighlightPreview();
    }
    
    // 从BuildableArea初始化网格系统
    private void InitializeGrid()
    {
        bool[,] buildableMap = new bool[gridWidth, gridHeight];
        var tempGrid = new GridSystem(gridWidth, gridHeight, cellSize, gridOrigin, transform, null);
        Vector3 halfCell = transform.rotation * new Vector3(cellSize, cellSize, 0) * 0.5f;
        for (int x = 0; x < gridWidth; x++)
        {
            for (int y = 0; y < gridHeight; y++)
            {
                Vector3 cellCenter = tempGrid.GetWorldPosition(x, y) + halfCell;
                // 判断该点是否在边界内
                buildableMap[x, y] = buildableArea.IsWorldPointInside(cellCenter);
            }
        }
        grid = new GridSystem(gridWidth, gridHeight, cellSize, gridOrigin, transform, buildableMap);
    }

    // 处理来自CameraController的点击事件，在建造模式下尝试放置建筑
    public void HandleTapOnGrid(Vector3 worldPosition)
    {
        if (!isInBuildMode) return;
        grid.GetXY(worldPosition, out int x, out int y);
        // 如果还没有预览对象 (第一次点击)
        if (previewObject == null)
        {
            if (grid.CanBuildArea(x, y, selectedBuilding.width, selectedBuilding.height))
            {
                // 创建预览对象
                Vector3 placementPosition = grid.GetWorldPosition(x, y);
                previewObject = Instantiate(selectedBuilding.prefab, placementPosition, Quaternion.identity, buildingParent);
                previewPosition = new Vector2Int(x, y);
                // 禁用其逻辑脚本，它只是个预览
                if (previewObject.TryGetComponent<BuildingInstance>(out var instance))
                {
                    instance.enabled = false;
                }
                
                // 设置为半透明
                previewRenderer = previewObject.GetComponent<SpriteRenderer>();
                if (previewRenderer != null)
                {
                    Color color = previewRenderer.color;
                    color.a = 0.6f; // 60% 透明度
                    previewRenderer.color = color;
                }
                
                // 放置预览后，隐藏网格
                HideGridVisual();
            }
        }
        // 如果已经有预览对象了 (第二次或之后的点击)
        else
        {
            // 如果点击的是预览对象所在的位置 (确认放置)
            if (x >= previewPosition.x && x < previewPosition.x + selectedBuilding.width &&
                y >= previewPosition.y && y < previewPosition.y + selectedBuilding.height)
            {
                // 销毁预览对象
                Destroy(previewObject);
                previewObject = null;
                // 在相同位置放置真实建筑
                PlaceBuilding(previewPosition.x, previewPosition.y);
            }
            // 如果点击的是其他位置 (移动预览)
            else
            {
                if (grid.CanBuildArea(x, y, selectedBuilding.width, selectedBuilding.height))
                {
                    // 移动预览对象
                    previewObject.transform.position = grid.GetWorldPosition(x, y);
                    previewPosition = new Vector2Int(x, y);
                }
                else
                {
                    // 可以在此添加一个无效提示，比如让预览闪烁红色
                    Debug.Log("无法移动到此处！");
                }
            }
        }
    }

    // 在指定网格坐标放置建筑
    private void PlaceBuilding(int startX, int startY)
    {
        if (selectedBuilding == null) return;

        // 建造前校验余额。以前是直接 AddMoney(-cost) 再由 GameManager 把负数夹到 0，
        // 相当于钱不够也能白拿建筑。
        if (GameManager.Instance.money < selectedBuilding.cost)
        {
            Debug.LogWarning($"金钱不足，无法建造 {selectedBuilding.name}：需要 {selectedBuilding.cost}，当前 {GameManager.Instance.money}。");
            ExitBuildMode();
            GameManager.Instance.RequestEnterNormalMode();
            return;
        }

        // 这个点就是 (startX, startY) 格子的左下角顶点
        Vector3 placementPosition = grid.GetWorldPosition(startX, startY);
        // 实例化建筑，它的Pivot(底部中心)会被精确地放置在 placementPosition 上
        GameObject buildingGO = Instantiate(selectedBuilding.prefab, placementPosition, Quaternion.identity, buildingParent);

        BuildingInstance buildingInstance = buildingGO.GetComponent<BuildingInstance>();
        if (buildingInstance != null)
        {
            buildingInstance.Initialize(selectedBuilding);
        }
        else
        {
            Debug.LogError($"放置的建筑 {selectedBuilding.name} 的Prefab上没有找到 BuildingInstance 脚本！");
        }
        GameManager.Instance.AddMoney(-selectedBuilding.cost);
        grid.PlaceObjectOnArea(startX, startY, selectedBuilding.width, selectedBuilding.height,
                new PlacedObject(selectedBuilding, startX, startY, buildingInstance));
        if (buildingInstance != null)
        {
            buildingInstance.UpdateRoadConnectionStatus();
        }
        // 新建筑可能改变人口（住宅），统一按网格重算
        GameManager.Instance.RecalculatePopulation();
        // 2. 如果新放置的是道路，则通知其所有邻居更新状态
        if (selectedBuilding.buildingType == BuildingType.Road)
        {
            NotifyNeighborsOfRoadPlacement(startX, startY, selectedBuilding.width, selectedBuilding.height);
        }
        Debug.Log($"在 ({startX},{startY}) 放置了建筑 {selectedBuilding.name}。");
        GameManager.Instance.RequestEnterNormalMode();
    }
    
    private void NotifyNeighborsOfRoadPlacement(int roadX, int roadY, int roadWidth, int roadHeight)
    {
        var neighbors = grid.GetNeighborsOfArea(roadX, roadY, roadWidth, roadHeight);
        foreach (var neighborObject in neighbors)
        {
            // 获取邻居建筑的实例
            BuildingInstance neighborInstance = neighborObject.GetInstance(); // 你需要在PlacedObject中添加一个GetInstance()方法
            if (neighborInstance != null)
            {
                // 通知邻居更新其道路连接状态
                neighborInstance.UpdateRoadConnectionStatus();
            }
        }
    }

    // 在建造模式中换选建筑（GameManager 在已经处于建造模式时会调用）
    public void SetSelectedBuilding(BuildingData buildingData)
    {
        if (buildingData == null) return;

        this.selectedBuilding = buildingData;
        Debug.Log($"换选建筑: {selectedBuilding.name}");

        if (isInBuildMode)
        {
            // 丢掉上一栋的预览，并把网格重新显示出来
            if (previewObject != null)
            {
                Destroy(previewObject);
                previewObject = null;
            }
            ShowGridVisual();
        }
    }

    private void HandleGameStateChange(GameState state)
    {
        if (state == GameState.Building)
        {
            selectedBuilding = GameManager.Instance.RequestedBuilding;
            if (selectedBuilding != null)
            {
                EnterBuildMode();
            }
            else
            {
                Debug.LogError("进入建造模式失败：没有提供建筑数据。");
                GameManager.Instance.RequestEnterNormalMode();
            }
        }
        else
        {
            ExitBuildMode();
        }
    }

    private void EnterBuildMode()
    {
        isInBuildMode = true;
        ShowGridVisual();
        Debug.Log("进入建造模式");
    }

    private void ExitBuildMode()
    {
        if (!isInBuildMode) return;
        if (previewObject != null)
        {
            Destroy(previewObject);
            previewObject = null;
        }
        isInBuildMode = false;
        selectedBuilding = null;
        HideGridVisual();
        Debug.Log("退出建造模式");
    }

    #region 视觉表现逻辑

    /// <summary>
    /// 只为可建造的格子创建视觉元素
    /// </summary>
    private void CreateGridVisuals()
    {
        Quaternion gridRotation = transform.rotation;
        for (int x = 0; x < gridWidth; x++)
        {
            for (int y = 0; y < gridHeight; y++)
            {
                if (grid.GetGridObject(x, y).isBuildable)
                {
                    Vector3 pos = grid.GetWorldPosition(x, y) + (gridRotation * new Vector3(cellSize, cellSize) * 0.5f);
                    GameObject cellVisual = Instantiate(gridCellVisualPrefab, pos, gridRotation, gridVisualParent);
                    cellVisual.transform.localScale = Vector3.one * cellSize;
                    if (cellVisual.TryGetComponent<SpriteRenderer>(out var renderer))
                    {
                        renderer.color = defaultGridColor;
                    }
                    gridVisualArray[x, y] = cellVisual;
                }
            }
        }
        gridVisualParent.gameObject.SetActive(false);
    }

    private void ShowGridVisual()
    {
        gridVisualParent.gameObject.SetActive(true);
    }

    private void HideGridVisual()
    {
        ClearHighlights();
        gridVisualParent.gameObject.SetActive(false);
    }

    private void UpdateAreaHighlight(int startX, int startY, int width, int height, Color color)
    {
        ClearHighlights();

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                int currentX = startX + x;
                int currentY = startY + y;
                if (currentX >= 0 && currentY >= 0 && currentX < gridWidth && currentY < gridHeight)
                {
                    if (gridVisualArray[currentX, currentY] != null)
                    {
                        gridVisualArray[currentX, currentY].GetComponent<SpriteRenderer>().color = color;
                        highlightedCells.Add(new Vector2Int(currentX, currentY));
                    }
                }
            }
        }
    }
    
    private void ClearHighlights()
    {
        foreach (var cellPos in highlightedCells)
        {
            if (gridVisualArray[cellPos.x, cellPos.y] != null)
            {
                gridVisualArray[cellPos.x, cellPos.y].GetComponent<SpriteRenderer>().color = defaultGridColor;
            }
        }
        highlightedCells.Clear();
    }

    private void UpdateGhostPreview()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        // 确保地面有一个大的碰撞体，并且属于能被射线检测到的Layer
        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            Vector3 worldPosition = hit.point;
            grid.GetXY(worldPosition, out int x, out int y);
            
            // 清除上一帧的高亮
            ClearHighlights();
            
            if (x >= 0 && y >= 0 && x < gridWidth && y < gridHeight)
            {
                bool canBuild = grid.CanBuildArea(x, y, selectedBuilding.width, selectedBuilding.height);
                Color highlightColor = canBuild ? new Color(0, 1, 0, 0.3f) : new Color(1, 0, 0, 0.3f);
                UpdateAreaHighlight(x, y, selectedBuilding.width, selectedBuilding.height, highlightColor);
            }
        }
        else
        {
            // 如果鼠标没在网格上，清除高亮
            ClearHighlights();
        }
    }

    #endregion

    #region 存档与读档

    public List<BuildingSaveData> GetPlacedBuildingsSaveData()
    {
        List<BuildingSaveData> buildingsData = new List<BuildingSaveData>();
        for (int x = 0; x < gridWidth; x++)
        {
            for (int y = 0; y < gridHeight; y++)
            {
                PlacedObject placedObject = grid.GetGridObject(x, y)?.GetPlacedObject();
                if (placedObject != null && placedObject.IsOrigin(x, y))
                {
                    BuildingSaveData data = placedObject.GetSaveData();
                    if (data != null)
                    {
                        buildingsData.Add(data);
                    }
                }
            }
        }
        return buildingsData;
    }

    public void LoadLayout(LayoutData layoutData)
    {
        if (layoutData == null)
        {
            Debug.LogWarning("提供的 LayoutData 为空，无法加载布局。");
            return;
        }
        // 调用现有的加载逻辑，传入布局中的建筑列表
        LoadBuildingsFromSave(layoutData.GetBuildingSaveData());
        Debug.Log($"成功从 LayoutData '{layoutData.name}' 获取到 {layoutData.GetBuildingSaveData().Count} 个建筑数据并开始加载。");
    }


    public void LoadBuildingsFromSave(List<BuildingSaveData> buildingsData)
    {
        if (buildingsData == null)
        {
            Debug.LogWarning("读档数据为空，跳过建筑加载。");
            ClearAllBuildings();
            return;
        }

        ClearAllBuildings();
        List<BuildingInstance> allLoadedInstances = new List<BuildingInstance>();
        foreach (var data in buildingsData)
        {
            BuildingData buildingData = FindBuildingData(data.buildingName);
            if (buildingData == null)
            {
                Debug.LogError($"无法从存档加载建筑，找不到名为 '{data.buildingName}' 的BuildingData！");
                continue;
            }

            // 越界坐标以前会被静默忽略：对象实例化了却没登记到网格上，于是看不见、不能修路，
            // 下次存档还会被丢掉。这里直接跳过并报警，避免出现“幽灵建筑”。
            if (data.gridX < 0 || data.gridY < 0 ||
                data.gridX + buildingData.width > gridWidth || data.gridY + buildingData.height > gridHeight)
            {
                Debug.LogWarning($"存档中的建筑 '{data.buildingName}' 坐标 ({data.gridX},{data.gridY}) " +
                                 $"超出 {gridWidth}x{gridHeight} 网格范围，已跳过。");
                continue;
            }

            // Vector3 localCenterOffset = new Vector3(buildingData.width * cellSize * 0.5f, buildingData.height * cellSize * 0.5f, 0f);
            // Vector3 worldCenterOffset = transform.rotation * localCenterOffset;
            // Vector3 buildingPosition = grid.GetWorldPosition(data.gridX, data.gridY) + worldCenterOffset;
            Vector3 buildingPosition = grid.GetWorldPosition(data.gridX, data.gridY);

            GameObject buildingGO = Instantiate(buildingData.prefab, buildingPosition, Quaternion.identity, buildingParent);
            BuildingInstance buildingInstance = buildingGO.GetComponent<BuildingInstance>();
            if (buildingInstance != null)
            {
                buildingInstance.Initialize(buildingData);
                buildingInstance.RestoreState(data);
                allLoadedInstances.Add(buildingInstance);
                if (fountainBuildingData != null && buildingData == fountainBuildingData)
                {
                    this.fountainInstance = buildingInstance;
                    Debug.Log("<color=green>喷泉实例已在加载时成功重新链接！</color>");
                }
            }

            grid.PlaceObjectOnArea(data.gridX, data.gridY, buildingData.width, buildingData.height,
                new PlacedObject(buildingData, data.gridX, data.gridY, buildingInstance));
        }
        // 先按网格上的住宅重算人口，再更新道路连接
        GameManager.Instance.RecalculatePopulation();
        StartCoroutine(UpdateAllConnectionsAfterLoad(allLoadedInstances));
    }

    /// <summary>
    /// 按存档里的名字找 BuildingData。
    /// 二周目的“真建筑”是运行时 Instantiate 出来的克隆（名字形如 "&lt;原名&gt;_True"），
    /// 并不在 buildingDatabase 里，需要向 UnlockManager 索取。
    /// </summary>
    public BuildingData FindBuildingData(string buildingName)
    {
        if (string.IsNullOrEmpty(buildingName)) return null;

        if (buildingDatabase != null)
        {
            BuildingData direct = buildingDatabase.allBuildings.FirstOrDefault(b => b != null && b.name == buildingName);
            if (direct != null) return direct;
        }

        return UnlockManager.Instance != null ? UnlockManager.Instance.GetTrueVersionByName(buildingName) : null;
    }

    private void ClearAllBuildings()
    {
        foreach (Transform child in buildingParent)
        {
            Destroy(child.gameObject);
        }
        // 重新初始化网格，以确保网格数据被清空
        InitializeGrid();
    }

    #endregion

    private void OnDrawGizmos()
    {
        if (grid != null)
        {
            for (int x = 0; x < gridWidth; x++)
            {
                for (int y = 0; y < gridHeight; y++)
                {
                    // 只为可建造的格子绘制Gizmos，让场景更清晰
                    if (grid.GetGridObject(x, y).isBuildable)
                    {
                        Gizmos.color = new Color(0, 1, 0, 0.2f); // 绿色半透明
                        Vector3 center = grid.GetWorldPosition(x, y) + transform.rotation * new Vector3(cellSize, cellSize) * 0.5f;
                        Gizmos.DrawCube(center, transform.rotation * new Vector3(cellSize, cellSize, 0.1f));
                    }
                }
            }
        }
    }

    public GridSystem GetGrid()
    {
        return grid;
    }

    private IEnumerator UpdateAllConnectionsAfterLoad(List<BuildingInstance> instances)
    {
        yield return null; // 等待下一帧
        Debug.Log("正在为所有已加载的建筑更新道路连接...");
        foreach (var instance in instances)
        {
            if(instance != null) // 确保对象没有在加载过程中被销毁
            {
                instance.UpdateRoadConnectionStatus();
            }
        }
    }
}
