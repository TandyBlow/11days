// SuggestionData.cs
using UnityEngine;

// 定义建议的触发条件，让建议更智能
public enum SuggestionTrigger
{
    // 通用类型
    General,            // 通用建议，可随时出现
    Idle,               // 玩家长时间无操作时
    DayStart,           // 每日开始时
    
    // 资源相关
    LowMoney,           // 钱太少时
    LowGoods,           // 货物太少时
    
    // 建筑相关
    NoFarmland,         // 没有农田时
    NoResidential,      // 没有住宅时
    NoCommercial,       // 没有商业建筑时
    
    // 二周目特殊
    TrueBuildingPlaced  // 玩家放置了“真建筑”时
}

[CreateAssetMenu(fileName = "NewSuggestion", menuName = "Game/Suggestion Data")]
public class SuggestionData : ScriptableObject
{
    [Header("核心属性")]
    [Tooltip("如果勾选，这是一个误导玩家的伪建议")]
    public bool isMisleading;

    [TextArea(3, 5)]
    [Tooltip("显示在牌子上的文本内容")]
    public string suggestionText;

    [Header("触发逻辑")]
    [Tooltip("此建议最早在哪一天可以出现")]
    public int minDayToShow = 1;
    
    [Tooltip("此建议的触发条件")]
    public SuggestionTrigger trigger;
    
    [Tooltip("与触发条件相关的值（例如：LowMoney的阈值）")]
    public float triggerValue;

    [Header("冷却设置")]
    [Tooltip("该建议出现后，需要等待多少秒才能再次出现")]
    public float cooldown = 120f; // 默认2分钟冷却
}
