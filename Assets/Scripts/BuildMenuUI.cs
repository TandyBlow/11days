// BuildMenuUI.cs (修改后)
using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class BuildMenuUI : MonoBehaviour
{
    [Header("UI引用")]
    [SerializeField] private RectTransform buildButtonRect; // 主建设按钮的RectTransform
    [SerializeField] private RectTransform buildMenuPanelRect; // 菜单主面板的RectTransform
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private Transform contentParent; // 建筑列表的Content对象
    [SerializeField] private GameObject buildingListItemPrefab;
    [SerializeField] private List<TabButton> tabs;
    
    [SerializeField] private ScrollRect buildingListScrollRect; // 拖入包含建筑列表的ScrollRect组件

    [Header("数据和设置")]
    private BuildingDatabase buildingDatabase;
    [SerializeField] private float animationDuration = 0.3f;

    private bool isMenuOpen = false;
    private Coroutine animationCoroutine;
    private Canvas mainCanvas; // 缓存主Canvas

    private void Awake()
    {
        buildingDatabase = Resources.Load<BuildingDatabase>("BuildingDatabase");
        if (buildingDatabase == null)
        {
            Debug.LogError("!!!!!!!!!! BUILD MENU UI 修复失败: 无法从 Resources 加载 BuildingDatabase! !!!!!!!!!!");
        }
        mainCanvas = GetComponentInParent<Canvas>();
        if (mainCanvas == null)
        {
            Debug.LogError("BuildMenuUI必须在一个Canvas下才能正常工作！");
            return;
        }

        if (buildingListScrollRect != null)
        {
            buildingListScrollRect.vertical = false;
            buildingListScrollRect.horizontal = true;
            buildingListScrollRect.movementType = ScrollRect.MovementType.Clamped;
        }
        else
        {
            // 尝试自动查找，如果未在Inspector中指定
            buildingListScrollRect = GetComponentInChildren<ScrollRect>();
            if (buildingListScrollRect == null)
            {
                Debug.LogWarning("在BuildMenuUI中找不到ScrollRect组件，请在Inspector中手动指定。滑动行为可能不正确。");
            }
        }

        // 初始时将菜单完全移出屏幕右侧
        buildMenuPanelRect.anchoredPosition = new Vector2(buildMenuPanelRect.rect.width, 0);
        canvasGroup.alpha = 0;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
        
        // 为每个选项卡按钮添加监听器
        foreach (var tab in tabs)
        {
            tab.Toggle.onValueChanged.AddListener((isSelected) =>
            {
                if (isSelected)
                {
                    OnTabSelected(tab);
                }
            });
        }
    }
    
    // 主建设按钮调用的方法
    public void ToggleMenu()
    {
        isMenuOpen = !isMenuOpen;
        if (isMenuOpen)
        {
            OpenMenu();
        }
        else
        {
            CloseMenu();
        }
    }

    private void OpenMenu()
    {
        // 1. 设置菜单的高度和位置，使其与建设按钮对齐
        SetPanelPositionAndHeight();

        // 2. 激活面板，准备动画
        buildMenuPanelRect.gameObject.SetActive(true);

        // 3. 默认选中并显示第一个选项卡的内容
        if (tabs.Count > 0)
        {
            if (tabs[0].Toggle.isOn)
            {
                // Toggle 已经是 on 时不会再触发 onValueChanged，
                // 必须手动刷新一次：否则重新打开菜单时列表还停留在我上一次的可购买状态，
                // 钱变多了按钮仍然是灰的。
                OnTabSelected(tabs[0]);
            }
            else
            {
                tabs[0].Toggle.isOn = true; // 会触发 OnTabSelected
            }
        }

        // 4. 执行滑入动画
        if (animationCoroutine != null) StopCoroutine(animationCoroutine);
        animationCoroutine = StartCoroutine(AnimatePanel(0));
    }

    private void CloseMenu()
    {
        if (GameManager.Instance.CurrentState == GameState.Building)
        {
            GameManager.Instance.RequestEnterNormalMode();
        }
        
        // 执行滑出动画
        if (animationCoroutine != null) StopCoroutine(animationCoroutine);
        float hiddenXPos = buildMenuPanelRect.rect.width;
        animationCoroutine = StartCoroutine(AnimatePanel(hiddenXPos));
    }

    private void SetPanelPositionAndHeight()
    {

        // 1. 确保两个RectTransform都在同一个Canvas下，这样它们的坐标系才有可比性
        if (buildButtonRect.root != buildMenuPanelRect.root)
        {
            Debug.LogError("建设按钮和菜单面板不在同一个Canvas下，无法直接对齐！");
            return;
        }

        // 2. 获取建设按钮的中心点在其父RectTransform中的本地坐标
        Vector2 buttonCenterLocalPos = buildButtonRect.anchoredPosition;

        // 3. 将这个本地坐标转换成菜单面板的父RectTransform中的坐标
        //    这步是关键，它处理了两个UI元素可能不在同一个直接父级下的情况。
        Vector3 buttonWorldPos = buildButtonRect.TransformPoint(buildButtonRect.rect.center);
        Vector2 targetLocalPos = ((RectTransform)buildMenuPanelRect.parent).InverseTransformPoint(buttonWorldPos);

        // 4. 设置菜单面板的位置
        //    我们只需要设置Y轴，X轴由滑入/滑出动画控制
        buildMenuPanelRect.anchoredPosition = new Vector2(buildMenuPanelRect.anchoredPosition.x, targetLocalPos.y);

        // （可选）如果你还想让菜单面板的高度和按钮一样
        // float buttonHeight = buildButtonRect.rect.height;
        // buildMenuPanelRect.sizeDelta = new Vector2(buildMenuPanelRect.sizeDelta.x, buttonHeight);
    }



    private IEnumerator AnimatePanel(float targetX)
    {
        float startX = buildMenuPanelRect.anchoredPosition.x;
        float time = 0f;
        
        bool isOpening = targetX == 0;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
        
        if(isOpening) canvasGroup.alpha = 1;

        while (time < animationDuration)
        {
            time += Time.deltaTime;
            float progress = time / animationDuration;
            float newX = Mathf.Lerp(startX, targetX, progress);
            buildMenuPanelRect.anchoredPosition = new Vector2(newX, buildMenuPanelRect.anchoredPosition.y);
            yield return null;
        }

        buildMenuPanelRect.anchoredPosition = new Vector2(targetX, buildMenuPanelRect.anchoredPosition.y);
        
        if (isOpening)
        {
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;
        }
        else
        {
            canvasGroup.alpha = 0;
            buildMenuPanelRect.gameObject.SetActive(false);
        }
    }

    private void OnTabSelected(TabButton selectedTab)
    {
        foreach (var tab in tabs)
        {
            tab.SetSelected(tab == selectedTab);
        }
        PopulateBuildingList(selectedTab.Category);
    }

    private void PopulateBuildingList(BuildingType category)
    {
        // 先把旧项收集出来再处理：SetParent 会改变正在遍历的子节点集合，
        // 直接在 foreach 里 SetParent 会跳过元素，导致旧项没被销毁、列表越堆越多。
        List<Transform> oldItems = new List<Transform>();
        foreach (Transform child in contentParent) oldItems.Add(child);

        foreach (Transform child in oldItems)
        {
            // 先脱离父级再销毁：Destroy 要到帧末才生效，
            // 否则 LayoutGroup 会在本帧把“已删除的旧项 + 新项”一起排版，出现一帧错位。
            child.SetParent(null, false);
            Destroy(child.gameObject);
        }
        // 1. Get normal unlocked buildings from the database
        List<BuildingData> buildingsToShow = buildingDatabase.GetBuildingsByType(category)
            .Where(b => UnlockManager.Instance.IsUnlocked(b.name) && !b.isTrueVersion) // Explicitly exclude true versions here
            .ToList();
        // 2. If in NG+, get the runtime-generated true buildings
        if (GameManager.Instance.isNewGamePlus)
        {
            List<BuildingData> trueBuildings = UnlockManager.Instance.GetUnlockedTrueBuildingsForCategory(category);
            buildingsToShow.AddRange(trueBuildings);
        }
        
        // 3. Instantiate UI items for the combined list
        foreach (BuildingData data in buildingsToShow)
        {
            GameObject itemGO = Instantiate(buildingListItemPrefab, contentParent);
            BuildingListItemUI itemUI = itemGO.GetComponent<BuildingListItemUI>();
            if (itemUI != null)
            {
                itemUI.Setup(data);
            }
        }
    }

    
    // 你的TabButton辅助类 (保持不变)
    [System.Serializable]
    public class TabButton
    {
        public BuildingType Category;
        public Toggle Toggle;
        public Image BackgroundImage;
        public Color SelectedColor = Color.white;
        public Color DeselectedColor = Color.gray;

        public void SetSelected(bool isSelected)
        {
            if (BackgroundImage != null)
            {
                BackgroundImage.color = isSelected ? SelectedColor : DeselectedColor;
            }
        }
    }
}
