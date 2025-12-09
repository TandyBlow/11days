// KiraController.cs (Updated for Sprite Swapping)
using UnityEngine;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class KiraController : MonoBehaviour
{
    // public static KiraController Instance { get; private set; }
    private static KiraController _instance;
    public static KiraController Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<KiraController>();
            }
            return _instance;
        }
    }
    private SuggestionDatabase suggestionDatabase;

    [Header("视觉与UI组件")]
    [Tooltip("包含普通立绘的GameObject")]
    [SerializeField] private GameObject normalKiraVisual; 
    [Tooltip("包含举牌立绘和建议文本的GameObject")]
    [SerializeField] private GameObject suggestionKiraVisual; 
    [Tooltip("显示建议文本的TextMeshPro组件")]
    [SerializeField] private TextMeshProUGUI suggestionText; 

    [Header("系统参数")]
    [Tooltip("基础检查间隔（秒），每隔这么久尝试触发一次通用建议")]
    [SerializeField] private float generalCheckInterval = 30f; 
    [Tooltip("建议显示时长（秒）")]
    [SerializeField] private float suggestionDisplayTime = 8f;

    // 内部状态
    private Dictionary<SuggestionData, float> suggestionCooldowns = new Dictionary<SuggestionData, float>();
    private bool isShowingSuggestion = false;
    private float generalCheckTimer;

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;
        // --- 紧急修复：在这里加载资源 ---
        suggestionDatabase = Resources.Load<SuggestionDatabase>("SuggestionDatabase"); // <-- 把 "SuggestionDatabase" 换成你的文件名
        if (suggestionDatabase == null)
        {
            Debug.LogError("!!!!!!!!!! 紧急错误：无法从 Resources 文件夹加载 SuggestionDatabase！请检查文件名和位置！ !!!!!!!!!!");
        }
    }

    private void Start()
    {
        // 确保游戏开始时显示的是普通立绘
        normalKiraVisual.SetActive(true);
        suggestionKiraVisual.SetActive(false);
        generalCheckTimer = generalCheckInterval;
    }

    private void Update()
    {
        // 更新所有建议的冷却时间
        var cooledDownSuggestions = suggestionCooldowns.Keys.ToList();
        foreach (var suggestion in cooledDownSuggestions)
        {
            suggestionCooldowns[suggestion] -= Time.deltaTime;
        }

        // 如果正在显示建议或游戏不处于Normal状态，则不进行新的建议检查
        if (isShowingSuggestion || GameManager.Instance.CurrentState != GameState.Normal)
        {
            return;
        }

        // 通用建议的周期性检查
        generalCheckTimer -= Time.deltaTime;
        if (generalCheckTimer <= 0)
        {
            TryShowSuggestion(SuggestionTrigger.General);
            generalCheckTimer = generalCheckInterval;
        }
    }

    public void TryShowSuggestion(SuggestionTrigger triggerType)
    {
        if (isShowingSuggestion) return;

        int currentDay = GameManager.Instance.day;
        float misleadingChance = Mathf.Clamp01((float)(currentDay - 1) / 8.0f);

        List<SuggestionData> eligibleSuggestions = suggestionDatabase.allSuggestions
            .Where(s => s.trigger == triggerType)
            .Where(s => currentDay >= s.minDayToShow)
            .Where(s => !suggestionCooldowns.ContainsKey(s) || suggestionCooldowns[s] <= 0)
            .ToList();

        if (eligibleSuggestions.Count == 0) return;

        bool showMisleading = Random.value < misleadingChance;
        List<SuggestionData> finalCandidates = eligibleSuggestions.Where(s => s.isMisleading == showMisleading).ToList();

        if (finalCandidates.Count == 0)
        {
            finalCandidates = eligibleSuggestions.Where(s => s.isMisleading != showMisleading).ToList();
        }
        
        if (finalCandidates.Count == 0) return;

        SuggestionData selectedSuggestion = finalCandidates[Random.Range(0, finalCandidates.Count)];
        StartCoroutine(ShowSuggestionCoroutine(selectedSuggestion));
    }

    // 显示建议的协程
    private IEnumerator ShowSuggestionCoroutine(SuggestionData suggestion)
    {
        isShowingSuggestion = true;

        // 1. 设置文本内容
        suggestionText.text = suggestion.suggestionText;

        // 2. 切换立绘
        normalKiraVisual.SetActive(false);
        suggestionKiraVisual.SetActive(true);

        // 3. 等待显示时间
        yield return new WaitForSeconds(suggestionDisplayTime);

        // 4. 切换回普通立绘
        suggestionKiraVisual.SetActive(false);
        normalKiraVisual.SetActive(true);

        // 5. 设置冷却时间并重置状态
        suggestionCooldowns[suggestion] = suggestion.cooldown;
        isShowingSuggestion = false;
    }
}
