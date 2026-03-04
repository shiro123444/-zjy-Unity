using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 信息面板管理器 - 显示物体详细信息
/// 需要添加到 Canvas 上
/// </summary>
public class InfoPanel : MonoBehaviour
{
    [Header("UI 引用")]
    [Tooltip("面板根对象")]
    public GameObject panelRoot;
    
    [Tooltip("标题文本")]
    public TextMeshProUGUI titleText;
    
    [Tooltip("详细信息文本")]
    public TextMeshProUGUI detailText;
    
    [Tooltip("关闭按钮")]
    public Button closeButton;

    [Header("面板设置")]
    [Tooltip("显示/隐藏动画时间")]
    public float animationDuration = 0.3f;

    private CanvasGroup canvasGroup;
    private bool isAnimating = false;

    // 面板关闭事件
    public System.Action onPanelClosed;

    void Awake()
    {
        Debug.Log("InfoPanel Awake 被调用");

        // 如果没有手动设置，尝试自动查找
        if (panelRoot == null && transform.childCount > 0)
            panelRoot = transform.GetChild(0).gameObject;

        if (panelRoot != null)
        {
            if (titleText == null)
                titleText = panelRoot.transform.Find("Title")?.GetComponent<TextMeshProUGUI>();

            if (detailText == null)
                detailText = panelRoot.transform.Find("DetailText")?.GetComponent<TextMeshProUGUI>();

            if (closeButton == null)
                closeButton = panelRoot.transform.Find("CloseButton")?.GetComponent<Button>();

            // 添加 CanvasGroup 用于淡入淡出
            canvasGroup = panelRoot.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
                canvasGroup = panelRoot.AddComponent<CanvasGroup>();

            // 初始隐藏
            panelRoot.SetActive(false);
        }
        else
        {
            Debug.LogError("InfoPanel: panelRoot 未设置！");
        }
    }

    void Start()
    {
        // 在 Start 中绑定按钮，确保所有组件都已初始化
        if (closeButton != null)
        {
            closeButton.onClick.RemoveAllListeners();
            closeButton.onClick.AddListener(() => {
                Debug.Log("关闭按钮被点击！");
                HideInfo();
            });
            Debug.Log($"关闭按钮已绑定，按钮名称: {closeButton.name}");
        }
        else
        {
            Debug.LogError("关闭按钮为 null！请检查 Inspector 中的设置");
        }
    }

    /// <summary>
    /// 显示信息
    /// </summary>
    public void ShowInfo(string title, string detail)
    {
        Debug.Log($"ShowInfo 被调用: {title}");

        if (titleText != null)
            titleText.text = title;
        else
            Debug.LogWarning("titleText 为 null");

        if (detailText != null)
            detailText.text = detail;
        else
            Debug.LogWarning("detailText 为 null");

        if (panelRoot != null && !panelRoot.activeSelf)
        {
            panelRoot.SetActive(true);
            StartCoroutine(FadeIn());
        }
    }

    /// <summary>
    /// 隐藏信息
    /// </summary>
    public void HideInfo()
    {
        Debug.Log("HideInfo 被调用");

        if (panelRoot != null && panelRoot.activeSelf && !isAnimating)
        {
            StartCoroutine(FadeOut());
        }
    }

    /// <summary>
    /// 立即隐藏（无动画）
    /// </summary>
    public void HideInfoImmediate()
    {
        Debug.Log("立即隐藏面板");
        if (panelRoot != null)
        {
            panelRoot.SetActive(false);
            
            // 触发关闭事件
            onPanelClosed?.Invoke();
        }
    }

    /// <summary>
    /// 淡入动画
    /// </summary>
    System.Collections.IEnumerator FadeIn()
    {
        isAnimating = true;
        float elapsed = 0f;
        canvasGroup.alpha = 0f;

        while (elapsed < animationDuration)
        {
            elapsed += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(0f, 1f, elapsed / animationDuration);
            yield return null;
        }

        canvasGroup.alpha = 1f;
        isAnimating = false;
    }

    /// <summary>
    /// 淡出动画
    /// </summary>
    System.Collections.IEnumerator FadeOut()
    {
        isAnimating = true;
        float elapsed = 0f;
        canvasGroup.alpha = 1f;

        while (elapsed < animationDuration)
        {
            elapsed += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(1f, 0f, elapsed / animationDuration);
            yield return null;
        }

        canvasGroup.alpha = 0f;
        panelRoot.SetActive(false);
        isAnimating = false;
        
        // 触发关闭事件
        onPanelClosed?.Invoke();
    }

    /// <summary>
    /// 更新信息（不重新播放动画）
    /// </summary>
    public void UpdateInfo(string title, string detail)
    {
        if (titleText != null)
            titleText.text = title;

        if (detailText != null)
            detailText.text = detail;
    }
}
