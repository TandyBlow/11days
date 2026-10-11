// MainMenuUI.cs
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class MainMenuUI : MonoBehaviour
{
    [SerializeField] private Button continueButton;
    // [SerializeField] private TextMeshProUGUI continueButtonText;
    [SerializeField] private Button newGameButton;
    [SerializeField] private string gameSceneName = "SampleScene"; // 真正的游戏场景（旧值 GameScene 已不在 Build Settings 里）

    private void Start()
    {
        // 检查是否存在存档
        if (SaveManager.HasSaveData())
        {
            continueButton.interactable = true;
            // continueButtonText.text = $"从第 {data.day} 日继续";
        }
        else
        {
            continueButton.interactable = false;
            // 按钮文本保持默认的 "从第？日继续"
        }

        // 绑定按钮点击事件
        continueButton.onClick.AddListener(OnContinueClick);
        newGameButton.onClick.AddListener(OnNewGameClick);
    }

    private void OnContinueClick()
    {
        // 设置加载标记，然后加载游戏场景
        GameManager.ShouldLoadGame = true;
        GameManager.IsNewGamePlusPending = false; 
        SceneManager.LoadScene(gameSceneName);
    }

    private void OnNewGameClick()
    {
        // Check for the completion flag to decide if this new game is an NG+
        if (SaveManager.HasCompletionFlag())
        {
            GameManager.IsNewGamePlusPending = true;
            Debug.Log("Completion flag found. Starting New Game+.");
        }
        else
        {
            GameManager.IsNewGamePlusPending = false;
            Debug.Log("No completion flag. Starting a fresh game.");
        }
        // --- End of modification ---
        SaveManager.DeleteSaveData();
        GameManager.ShouldLoadGame = false;
        SceneManager.LoadScene(gameSceneName);
    }
}
