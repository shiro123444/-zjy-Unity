using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections;

/// <summary>
/// 镜头移动行为枚举
/// </summary>
public enum CameraMoveBehavior
{
    MoveBack,      // 向后退
    StayInPlace,   // 保持原位
    OnlyRotate     // 只旋转不移动
}

/// <summary>
/// 场景过渡管理器 - 处理淡入淡出效果和场景切换
/// </summary>
public class SceneTransitionManager : MonoBehaviour
{
    [Header("过渡设置")]
    [Tooltip("淡入淡出持续时间（秒）")]
    public float fadeDuration = 1f;
    
    [Tooltip("淡出颜色")]
    public Color fadeColor = Color.black;

    [Header("镜头移动设置")]
    [Tooltip("镜头移动速度（越小越慢）")]
    public float cameraMoveSpeed = 2f;
    
    [Tooltip("镜头移动到目标的距离偏移")]
    public float cameraDistanceOffset = 3f;

    private Canvas fadeCanvas;
    private Image fadeImage;
    private static SceneTransitionManager instance;

    public static SceneTransitionManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindObjectOfType<SceneTransitionManager>();
            }
            return instance;
        }
    }

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (instance != this)
        {
            Destroy(gameObject);
            return;
        }

        InitializeFadeCanvas();
    }

    /// <summary>
    /// 初始化淡入淡出画布
    /// </summary>
    void InitializeFadeCanvas()
    {
        // 创建Canvas（作为根物体，不设置父物体）
        GameObject canvasObj = new GameObject("FadeCanvas");
        
        fadeCanvas = canvasObj.AddComponent<Canvas>();
        fadeCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        fadeCanvas.sortingOrder = 9999; // 确保在最上层
        
        // 添加CanvasScaler
        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        
        // 添加GraphicRaycaster
        canvasObj.AddComponent<GraphicRaycaster>();

        // 创建黑色遮罩Image
        GameObject imageObj = new GameObject("FadeImage");
        imageObj.transform.SetParent(canvasObj.transform, false);
        
        fadeImage = imageObj.AddComponent<Image>();
        fadeImage.color = fadeColor;
        
        // 设置为全屏
        RectTransform rectTransform = fadeImage.GetComponent<RectTransform>();
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.sizeDelta = Vector2.zero;
        rectTransform.anchoredPosition = Vector2.zero;

        // 初始设置为透明（不可见）
        SetFadeAlpha(0f);
        
        // Canvas必须是根物体才能使用DontDestroyOnLoad
        DontDestroyOnLoad(canvasObj);
    }

    /// <summary>
    /// 带淡入淡出效果的场景切换
    /// </summary>
    public void TransitionToScene(string sceneName)
    {
        StartCoroutine(TransitionCoroutine(sceneName, null));
    }

    /// <summary>
    /// 带淡入淡出效果的场景切换（使用场景索引）
    /// </summary>
    public void TransitionToScene(int sceneIndex)
    {
        StartCoroutine(TransitionCoroutine(sceneIndex, null));
    }

    /// <summary>
    /// 带镜头移动和淡入淡出效果的场景切换
    /// </summary>
    public void TransitionToSceneWithCamera(string sceneName, Vector3 targetPosition)
    {
        StartCoroutine(TransitionCoroutine(sceneName, targetPosition));
    }

    /// <summary>
    /// 带镜头移动和淡入淡出效果的场景切换（使用场景索引）
    /// </summary>
    public void TransitionToSceneWithCamera(int sceneIndex, Vector3 targetPosition)
    {
        StartCoroutine(TransitionCoroutine(sceneIndex, targetPosition));
    }

    private IEnumerator TransitionCoroutine(string sceneName, Vector3? targetPosition)
    {
        // 如果提供了目标位置，同时开始镜头移动和淡出
        if (targetPosition.HasValue)
        {
            // 启动镜头移动（不等待完成）
            StartCoroutine(MoveCameraToTargetSlow(targetPosition.Value));
        }

        // 淡出到黑色
        yield return StartCoroutine(FadeOut());
        
        // 检查场景是否存在
        if (!IsSceneInBuildSettings(sceneName))
        {
            Debug.LogError($"场景 '{sceneName}' 未添加到 Build Settings 中！请使用菜单 Tools > 自动添加所有场景到 Build Settings");
            yield return StartCoroutine(FadeIn());
            yield break;
        }
        
        // 加载新场景
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneName);
        
        if (asyncLoad == null)
        {
            Debug.LogError($"无法加载场景 '{sceneName}'");
            yield return StartCoroutine(FadeIn());
            yield break;
        }
        
        asyncLoad.allowSceneActivation = false;

        // 等待场景加载完成
        while (asyncLoad.progress < 0.9f)
        {
            yield return null;
        }

        asyncLoad.allowSceneActivation = true;
        
        // 等待场景激活
        yield return new WaitForSeconds(0.1f);

        // 从黑色淡入
        yield return StartCoroutine(FadeIn());
    }

    private IEnumerator TransitionCoroutine(int sceneIndex, Vector3? targetPosition)
    {
        // 如果提供了目标位置，同时开始镜头移动和淡出
        if (targetPosition.HasValue)
        {
            // 启动镜头移动（不等待完成）
            StartCoroutine(MoveCameraToTargetSlow(targetPosition.Value));
        }

        // 淡出到黑色
        yield return StartCoroutine(FadeOut());
        
        // 检查场景索引是否有效
        if (sceneIndex < 0 || sceneIndex >= SceneManager.sceneCountInBuildSettings)
        {
            Debug.LogError($"场景索引 {sceneIndex} 无效！Build Settings 中共有 {SceneManager.sceneCountInBuildSettings} 个场景");
            yield return StartCoroutine(FadeIn());
            yield break;
        }
        
        // 加载新场景
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneIndex);
        
        if (asyncLoad == null)
        {
            Debug.LogError($"无法加载场景索引 {sceneIndex}");
            yield return StartCoroutine(FadeIn());
            yield break;
        }
        
        asyncLoad.allowSceneActivation = false;

        // 等待场景加载完成
        while (asyncLoad.progress < 0.9f)
        {
            yield return null;
        }

        asyncLoad.allowSceneActivation = true;
        
        // 等待场景激活
        yield return new WaitForSeconds(0.1f);

        // 从黑色淡入
        yield return StartCoroutine(FadeIn());
    }

    /// <summary>
    /// 缓慢移动镜头到目标位置（持续移动，不受淡出时间限制）
    /// </summary>
    private IEnumerator MoveCameraToTargetSlow(Vector3 targetPosition)
    {
        Camera mainCamera = Camera.main;
        if (mainCamera == null)
        {
            Debug.LogWarning("找不到主摄像机，跳过镜头移动");
            yield break;
        }

        // 禁用镜头控制器（如果有）
        SceneViewCameraController cameraController = mainCamera.GetComponent<SceneViewCameraController>();
        bool wasControllerEnabled = false;
        if (cameraController != null)
        {
            wasControllerEnabled = cameraController.enabled;
            cameraController.enabled = false;
        }

        Vector3 startPosition = mainCamera.transform.position;
        Quaternion startRotation = mainCamera.transform.rotation;

        // 计算到目标的方向
        Vector3 directionToTarget = (targetPosition - startPosition).normalized;
        
        // 计算最终位置（在目标前方一定距离）
        Vector3 finalPosition = targetPosition - directionToTarget * cameraDistanceOffset;

        // 计算目标旋转（看向目标点）
        Vector3 lookDirection = targetPosition - finalPosition;
        Quaternion finalRotation = lookDirection != Vector3.zero 
            ? Quaternion.LookRotation(lookDirection) 
            : startRotation;

        Debug.Log($"开始缓慢移动镜头，速度: {cameraMoveSpeed}");

        // 持续移动直到到达目标或场景切换
        while (mainCamera != null && Vector3.Distance(mainCamera.transform.position, finalPosition) > 0.1f)
        {
            // 使用固定速度移动
            mainCamera.transform.position = Vector3.MoveTowards(
                mainCamera.transform.position, 
                finalPosition, 
                cameraMoveSpeed * Time.deltaTime
            );
            
            // 平滑旋转
            mainCamera.transform.rotation = Quaternion.RotateTowards(
                mainCamera.transform.rotation,
                finalRotation,
                cameraMoveSpeed * 30f * Time.deltaTime
            );

            yield return null;
        }

        Debug.Log("镜头移动完成或场景已切换");
    }

    /// <summary>
    /// 淡出效果（渐渐变黑）
    /// </summary>
    public IEnumerator FadeOut()
    {
        float elapsed = 0f;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            float alpha = Mathf.Clamp01(elapsed / fadeDuration);
            SetFadeAlpha(alpha);
            yield return null;
        }

        SetFadeAlpha(1f);
    }

    /// <summary>
    /// 淡入效果（从黑色渐渐变亮）
    /// </summary>
    public IEnumerator FadeIn()
    {
        float elapsed = 0f;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            float alpha = 1f - Mathf.Clamp01(elapsed / fadeDuration);
            SetFadeAlpha(alpha);
            yield return null;
        }

        SetFadeAlpha(0f);
    }

    /// <summary>
    /// 设置遮罩透明度
    /// </summary>
    private void SetFadeAlpha(float alpha)
    {
        if (fadeImage != null)
        {
            Color color = fadeImage.color;
            color.a = alpha;
            fadeImage.color = color;
        }
    }

    /// <summary>
    /// 检查场景是否在 Build Settings 中
    /// </summary>
    private bool IsSceneInBuildSettings(string sceneName)
    {
        for (int i = 0; i < SceneManager.sceneCountInBuildSettings; i++)
        {
            string scenePath = SceneUtility.GetScenePathByBuildIndex(i);
            string sceneNameInBuild = System.IO.Path.GetFileNameWithoutExtension(scenePath);
            
            if (sceneNameInBuild == sceneName)
            {
                return true;
            }
        }
        
        return false;
    }
}
