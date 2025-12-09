// SplashScreenController.cs
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;

public class SplashScreenController : MonoBehaviour
{
    [SerializeField]
    private Image logoImage; // 将场景中的LogoImage拖到这里

    [SerializeField]
    private string nextSceneName = "MainMenu"; // 你的主菜单场景名字

    [SerializeField]
    private float fadeInDuration = 1.0f;

    [SerializeField]
    private float fadeOutDuration = 1.0f;

    [SerializeField]
    private float holdDuration = 1.0f; // Logo完全显示后停留的时间

    void Start()
    {
        StartCoroutine(FadeSequence());
    }

    private IEnumerator FadeSequence()
    {
        // 淡入
        yield return StartCoroutine(Fade(logoImage, 1f, fadeInDuration));

        // 停留
        yield return new WaitForSeconds(holdDuration);

        // 淡出
        yield return StartCoroutine(Fade(logoImage, 0f, fadeOutDuration));

        // 加载下一个场景
        SceneManager.LoadScene(nextSceneName);
    }

    private IEnumerator Fade(Image image, float targetAlpha, float duration)
    {
        float timer = 0f;
        Color startColor = image.color;
        Color endColor = new Color(startColor.r, startColor.g, startColor.b, targetAlpha);

        while (timer < duration)
        {
            timer += Time.deltaTime;
            image.color = Color.Lerp(startColor, endColor, timer / duration);
            yield return null;
        }

        image.color = endColor;
    }
}
