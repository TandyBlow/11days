// FlashbackManager.cs (已修改为使用SpriteRenderer)
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class FlashbackManager : MonoBehaviour
{
    public static FlashbackManager Instance { get; private set; }

    [System.Serializable]
    public class FlashbackEntry
    {
        public int day;
        public Sprite flashbackImage;
        public Color backgroundColor = Color.black; 
    }

    [Header("闪回设置")]
    [SerializeField] private List<FlashbackEntry> flashbacks;
    // 【改动】将UI Image替换为SpriteRenderer
    [SerializeField] private SpriteRenderer flashbackSpriteRenderer;

    private Color originalCameraColor;

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        // 确保一开始闪回对象是隐藏的
        if (flashbackSpriteRenderer != null)
        {
            flashbackSpriteRenderer.gameObject.SetActive(false);
        }
        else
        {
            Debug.LogError("FlashbackManager上的Flashback Sprite Renderer字段未设置！");
        }
    }

    public FlashbackEntry GetFlashbackForDay(int day)
    {
        return flashbacks.FirstOrDefault(e => e.day == day);
    }

    public void ShowFlashback(FlashbackEntry entry)
    {
        if (entry == null || entry.flashbackImage == null || flashbackSpriteRenderer == null)
        {
            Debug.LogWarning($"请求的闪回数据或SpriteRenderer为空。");
            return;
        }

        Debug.Log($"--- 准备显示第 {entry.day} 天的闪回Sprite ---");
        
        originalCameraColor = Camera.main.backgroundColor;
        Camera.main.backgroundColor = entry.backgroundColor;

        // 【改动】操作SpriteRenderer来显示图片
        flashbackSpriteRenderer.sprite = entry.flashbackImage;
        flashbackSpriteRenderer.gameObject.SetActive(true);
    }

    public void HideFlashback()
    {
        Debug.Log("--- 隐藏闪回Sprite ---");
        
        Camera.main.backgroundColor = originalCameraColor;

        // 【改动】禁用SpriteRenderer的游戏对象
        if (flashbackSpriteRenderer != null)
        {
            flashbackSpriteRenderer.gameObject.SetActive(false);
        }
    }
}
