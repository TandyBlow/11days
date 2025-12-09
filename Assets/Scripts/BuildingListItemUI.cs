using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class BuildingListItemUI : MonoBehaviour
{
    [Header("UI组件")]
    [SerializeField] private Image iconImage; // 这个Image就是用来显示建筑图片的UI元素
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI costText;
    [SerializeField] private Button button;

    private BuildingData buildingData;

    private void Awake()
    {
        button = GetComponent<Button>();
        button.onClick.AddListener(OnItemClicked);
    }

    // 由BuildMenuUI调用，用于设置此列表项显示的数据
    public void Setup(BuildingData data)
    {
        this.buildingData = data;

        // 1. 从建筑Prefab上获取SpriteRenderer组件
        if (data.prefab != null && data.prefab.TryGetComponent<SpriteRenderer>(out var spriteRenderer))
        {
            // 2. 将获取到的Sprite直接赋值给UI的Image组件
            iconImage.sprite = spriteRenderer.sprite;
        }
        else
        {
            Debug.LogWarning($"建筑 '{data.buildingName}' 的Prefab上没有找到SpriteRenderer，无法显示图标。");
        }
        // --- 核心改动结束 ---

        nameText.text = data.buildingName;
        costText.text = data.cost.ToString();

        // 根据玩家金钱判断按钮是否可交互
        bool canAfford = GameManager.Instance.money >= data.cost;
        button.interactable = canAfford;
    }

    private void OnItemClicked()
    {
        if (buildingData != null)
        {
            GameManager.Instance.RequestEnterBuildMode(buildingData);
        }
    }
}
