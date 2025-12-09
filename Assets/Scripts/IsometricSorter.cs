// IsometricSorter.cs

using UnityEngine;

[RequireComponent(typeof(Renderer))]
public class IsometricSorter : MonoBehaviour
{
    private Renderer spriteRenderer;

    // 精度乘数。因为Y坐标是浮点数，而sortingOrder是整数。
    // 这个值越大，Y坐标上很小的差异也能被区分开。通常100会是个不错的选择。
    [Tooltip("精度乘数，用于将Y坐标的小数差异放大为整数。建议值为100。")]
    [SerializeField]
    private float precisionMultiplier = 100f;

    // 是否为静态物体。如果为true，则只在开始时排序一次，以优化性能。
    // 建筑是静态的，应该勾选此项。角色和怪物则不能。
    [Tooltip("如果物体是静态的（如建筑），勾选此项以优化性能。脚本只会在启动时运行一次。")]
    [SerializeField]
    private bool isStatic = false;

    [SerializeField] private int externalOffset = 0;

    [SerializeField] private bool sortChildRenderers = false;

    private Renderer[] childRenderers;

    private void Awake()
    {
        // 获取SpriteRenderer组件
        spriteRenderer = GetComponent<Renderer>();
        if (sortChildRenderers) childRenderers = GetComponentsInChildren<Renderer>(true);
    }

    private void Start()
    {
        // 如果是静态物体，在游戏开始时计算一次排序顺序即可
        if (isStatic)
        {
            UpdateSortOrder();
            // 运行一次后禁用脚本，不再消耗Update资源，性能开销为零
            this.enabled = false; 
        }
    }

    // 使用LateUpdate确保在物体完成所有移动（在Update中完成）之后，再根据最终位置计算渲染顺序
    private void LateUpdate()
    {
        // if (isStatic) return; // 双重保险
        UpdateSortOrder();
    }

    public void UpdateSortOrder()
    {
        if (spriteRenderer == null) return;
        // 核心逻辑：
        // 1. 使用 spriteRenderer.bounds.min.y 来获取物体在世界空间中的最低点Y坐标。
        //    这与物体的Pivot（轴心）位置无关，总是准确的。
        // 2. 乘以精度乘数，放大差异。
        // 3. 转换为整数。
        // 4. 取负值。因为Y越大（越靠上）的物体，sortingOrder需要越小（越靠后）。
        int parentOrder = -(int)(spriteRenderer.bounds.min.y * precisionMultiplier);
        spriteRenderer.sortingOrder = parentOrder + externalOffset;
        // 2. 如果启用了子对象排序，遍历并设置所有子渲染器
        if (sortChildRenderers && childRenderers != null)
        {
            foreach (var renderer in childRenderers)
            {
                // 跳过自己
                if (renderer == spriteRenderer) continue;

                // 强制设置子渲染器的 sortingOrder
                renderer.sortingOrder = parentOrder + externalOffset;
            }
        }

    }

    public void Initialize(bool isStaticObject, float precision = 100f, int offset = 0)
    {
        this.isStatic = isStaticObject;
        this.precisionMultiplier = precision;
        this.externalOffset = offset;

        // 确保 Renderer 被获取
        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponent<Renderer>();
        }

        UpdateSortOrder(); // 立即更新一次
        if (isStatic)
        {
            this.enabled = false; // 如果是静态的，禁用脚本
        }
    }


    public void SetExternalOffset(int offset)
    {
        externalOffset = offset;
        UpdateSortOrder();
    }
}
