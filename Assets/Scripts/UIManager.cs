// UIManager.cs
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    // 【改动】将这些UI元素的直接引用设为私有或移除，因为我们将通过父对象控制它们
    [SerializeField] private TextMeshProUGUI moneyText;
    [SerializeField] private TextMeshProUGUI goodsText;
    [SerializeField] private TextMeshProUGUI dayText;
    [SerializeField] public Image fadeScreen;
    [SerializeField] private GameObject overlayMask;
    [SerializeField] private BuildMenuUI buildMenuUI;

    // 【新增】用于控制所有游戏内UI的父对象
    [Header("UI Parents")]
    [SerializeField] private GameObject inGameUIParent;
    [SerializeField] private GameObject rootCanvasObject; // 保持这个，用于FadeScreen的检查

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        if (rootCanvasObject == null)
        {
            Debug.LogError("致命错误: UIManager上的 'Root Canvas Object' 字段没有被指定！");
        }
        if (inGameUIParent == null)
        {
            Debug.LogError("致命错误: UIManager上的 'In Game UI Parent' 字段没有被指定！请参考文档设置。");
        }
    }

    private void OnEnable()
    {
        GameManager.OnDayStart += HandleDayStart;
    }

    private void OnDisable()
    {
        GameManager.OnDayStart -= HandleDayStart;
    }

    private void HandleDayStart(int newDay)
    {
        dayText.text = $"第{newDay}日";
    }

    // 【新增】显示游戏UI
    public void ShowInGameUI()
    {
        if (inGameUIParent != null) inGameUIParent.SetActive(true);
    }

    // 【新增】隐藏游戏UI
    public void HideInGameUI()
    {
        if (inGameUIParent != null) inGameUIParent.SetActive(false);
    }

    public void ShowOverlay()
    {
        if (overlayMask != null) overlayMask.SetActive(true);
    }
    public void HideOverlay()
    {
        if (overlayMask != null) overlayMask.SetActive(false);
    }

    public void UpdateAllDisplays(int money, int goods)
    {
        moneyText.text = $"{money}";
        goodsText.text = $"{goods}";
    }

    public IEnumerator FadeScreen(bool toBlack, float duration)
    {
        if (fadeScreen == null) yield break;
        if (rootCanvasObject != null && !rootCanvasObject.activeInHierarchy)
        {
            rootCanvasObject.SetActive(true);
        }

        float timer = 0f;
        Color startColor = fadeScreen.color;
        Color endColor = toBlack ? Color.black : Color.clear;
        fadeScreen.raycastTarget = true; 

        while (timer < duration)
        {
            timer += Time.unscaledDeltaTime; 
            fadeScreen.color = Color.Lerp(startColor, endColor, timer / duration);
            yield return null;
        }

        fadeScreen.color = endColor;
        if (!toBlack)
        {
            fadeScreen.raycastTarget = false;
        }
    }

    public void onBuildButtonClicked()
    {
        if (buildMenuUI != null) buildMenuUI.ToggleMenu();
    }

    public void HideGameUIForFinale()
    {
        // 【改动】现在这个方法只调用通用的隐藏方法
        HideInGameUI();
    }
}
