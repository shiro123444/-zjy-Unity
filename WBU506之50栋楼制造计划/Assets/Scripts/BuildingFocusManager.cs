using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// 建筑聚焦管理器 - 管理建筑物的显示/隐藏和镜头聚焦
/// </summary>
public class BuildingFocusManager : MonoBehaviour
{
    [Header("聚焦设置")]
    [Tooltip("镜头移动到建筑物的持续时间")]
    public float focusDuration = 1.5f;
    
    [Tooltip("镜头距离建筑物的距离")]
    public float focusDistance = 10f;
    
    [Tooltip("镜头高度偏移")]
    public float heightOffset = 2f;

    [Header("淡入淡出设置")]
    [Tooltip("建筑物淡入淡出持续时间")]
    public float fadeDuration = 1f;

    [Header("地面设置")]
    [Tooltip("地面物体（Plane）")]
    public GameObject groundPlane;
    
    [Tooltip("聚焦时的地面颜色")]
    public Color focusGroundColor = new Color(0.3f, 0.3f, 0.3f, 1f);
    
    [Tooltip("地面颜色变化持续时间")]
    public float groundColorChangeDuration = 1f;

    [Header("自动检测")]
    [Tooltip("自动查找场景中所有建筑物（带ClickableBuildingGroup的物体）")]
    public bool autoFindBuildings = true;
    
    [Tooltip("手动指定建筑物列表")]
    public List<GameObject> buildings = new List<GameObject>();

    private static BuildingFocusManager instance;
    private GameObject currentFocusedBuilding;
    private bool isFocused = false;
    private bool buildingsHidden = false; // 建筑物是否已隐藏
    private Camera mainCamera;
    private SceneViewCameraController cameraController;
    
    // 保存原始状态
    private Vector3 originalCameraPosition;
    private Quaternion originalCameraRotation;
    private Dictionary<GameObject, List<Renderer>> buildingRenderers = new Dictionary<GameObject, List<Renderer>>();
    
    // 地面相关
    private Renderer groundRenderer;
    private Color originalGroundColor;
    private Material groundMaterial;

    public static BuildingFocusManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindObjectOfType<BuildingFocusManager>();
            }
            return instance;
        }
    }

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else if (instance != this)
        {
            Destroy(gameObject);
            return;
        }
    }

    void Start()
    {
        mainCamera = Camera.main;
        if (mainCamera != null)
        {
            cameraController = mainCamera.GetComponent<SceneViewCameraController>();
        }

        // 自动查找建筑物
        if (autoFindBuildings)
        {
            FindAllBuildings();
        }

        // 收集所有建筑物的Renderer
        CollectBuildingRenderers();
        
        // 初始化地面
        InitializeGround();
    }

    void Update()
    {
        // 检测A键恢复（在建筑物隐藏或镜头聚焦时都可以按A恢复）
        if (buildingsHidden || isFocused)
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard != null && keyboard.aKey.wasPressedThisFrame)
            {
                UnfocusBuilding();
            }
        }
    }

    /// <summary>
    /// 自动查找所有建筑物
    /// </summary>
    void FindAllBuildings()
    {
        buildings.Clear();
        ClickableBuildingGroup[] buildingGroups = FindObjectsOfType<ClickableBuildingGroup>();
        
        foreach (var group in buildingGroups)
        {
            buildings.Add(group.gameObject);
        }

        Debug.Log($"BuildingFocusManager: 找到 {buildings.Count} 个建筑物");
    }

    /// <summary>
    /// 收集所有建筑物的Renderer组件
    /// </summary>
    void CollectBuildingRenderers()
    {
        buildingRenderers.Clear();

        foreach (GameObject building in buildings)
        {
            if (building == null) continue;

            List<Renderer> renderers = new List<Renderer>();
            renderers.AddRange(building.GetComponentsInChildren<Renderer>());
            buildingRenderers[building] = renderers;
        }

        Debug.Log($"BuildingFocusManager: 收集了 {buildingRenderers.Count} 个建筑物的Renderer");
    }

    /// <summary>
    /// 初始化地面
    /// </summary>
    void InitializeGround()
    {
        if (groundPlane != null)
        {
            groundRenderer = groundPlane.GetComponent<Renderer>();
            if (groundRenderer != null)
            {
                // 创建材质副本以避免修改原始材质
                groundMaterial = new Material(groundRenderer.material);
                groundRenderer.material = groundMaterial;
                originalGroundColor = groundMaterial.color;
                Debug.Log($"地面初始化成功，原始颜色: {originalGroundColor}");
            }
            else
            {
                Debug.LogWarning("地面物体没有Renderer组件！");
            }
        }
        else
        {
            Debug.LogWarning("未设置地面物体！请在Inspector中指定Ground Plane。");
        }
    }

    /// <summary>
    /// 聚焦到指定建筑物（两步流程）
    /// </summary>
    public void FocusOnBuilding(GameObject building)
    {
        if (!buildings.Contains(building))
        {
            Debug.LogWarning($"建筑物 {building.name} 不在管理列表中");
            return;
        }

        // 第一次点击：隐藏建筑物 + 地面变色
        if (!buildingsHidden)
        {
            currentFocusedBuilding = building;
            StartCoroutine(HideBuildingsAndChangeGroundCoroutine(building));
        }
        // 第二次点击：移动镜头
        else if (!isFocused && currentFocusedBuilding == building)
        {
            StartCoroutine(MoveCameraCoroutine(building, null, null));
        }
        else if (currentFocusedBuilding != building)
        {
            Debug.LogWarning("请点击同一个建筑物来移动镜头，或按A键恢复");
        }
    }

    /// <summary>
    /// 聚焦到指定建筑物（使用自定义镜头位置，两步流程）
    /// </summary>
    public void FocusOnBuildingWithCustomTarget(GameObject building, Vector3 targetPosition, Quaternion targetRotation)
    {
        if (!buildings.Contains(building))
        {
            Debug.LogWarning($"建筑物 {building.name} 不在管理列表中");
            return;
        }

        // 第一次点击：隐藏建筑物 + 地面变色
        if (!buildingsHidden)
        {
            currentFocusedBuilding = building;
            StartCoroutine(HideBuildingsAndChangeGroundCoroutine(building));
        }
        // 第二次点击：移动镜头
        else if (!isFocused && currentFocusedBuilding == building)
        {
            StartCoroutine(MoveCameraCoroutine(building, targetPosition, targetRotation));
        }
        else if (currentFocusedBuilding != building)
        {
            Debug.LogWarning("请点击同一个建筑物来移动镜头，或按A键恢复");
        }
    }

    /// <summary>
    /// 取消聚焦，恢复所有建筑物
    /// </summary>
    public void UnfocusBuilding()
    {
        // 如果建筑物已隐藏但镜头还没移动，只恢复建筑物和地面
        if (buildingsHidden && !isFocused)
        {
            StartCoroutine(RestoreBuildingsAndGroundCoroutine());
            return;
        }

        // 如果镜头已经移动，执行完整的恢复流程
        if (isFocused)
        {
            StartCoroutine(UnfocusCoroutine());
        }
    }

    /// <summary>
    /// 第一步：隐藏建筑物并改变地面颜色
    /// </summary>
    IEnumerator HideBuildingsAndChangeGroundCoroutine(GameObject targetBuilding)
    {
        Debug.Log($"第一步：隐藏其他建筑物并改变地面颜色");
        
        float elapsed = 0f;
        Color startGroundColor = groundMaterial != null ? groundMaterial.color : Color.white;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / fadeDuration;
            float smoothT = Mathf.SmoothStep(0f, 1f, t);

            // 淡出其他建筑物
            FadeOtherBuildings(targetBuilding, 1f - smoothT);
            
            // 改变地面颜色
            if (groundMaterial != null)
            {
                groundMaterial.color = Color.Lerp(startGroundColor, focusGroundColor, smoothT);
            }

            yield return null;
        }

        // 确保最终状态
        FadeOtherBuildings(targetBuilding, 0f);
        if (groundMaterial != null)
        {
            groundMaterial.color = focusGroundColor;
        }

        buildingsHidden = true;
        Debug.Log("其他建筑物已隐藏，地面颜色已改变。再次点击建筑物以移动镜头。");
    }

    /// <summary>
    /// 恢复建筑物和地面（仅在第一步后按A键时使用）
    /// </summary>
    IEnumerator RestoreBuildingsAndGroundCoroutine()
    {
        Debug.Log("恢复建筑物和地面颜色");
        
        float elapsed = 0f;
        Color startGroundColor = groundMaterial != null ? groundMaterial.color : focusGroundColor;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / fadeDuration;
            float smoothT = Mathf.SmoothStep(0f, 1f, t);

            // 淡入其他建筑物
            FadeOtherBuildings(currentFocusedBuilding, smoothT);
            
            // 恢复地面颜色
            if (groundMaterial != null)
            {
                groundMaterial.color = Color.Lerp(startGroundColor, originalGroundColor, smoothT);
            }

            yield return null;
        }

        // 确保最终状态
        FadeOtherBuildings(currentFocusedBuilding, 1f);
        if (groundMaterial != null)
        {
            groundMaterial.color = originalGroundColor;
        }

        buildingsHidden = false;
        currentFocusedBuilding = null;
        
        Debug.Log("建筑物和地面已恢复");
    }

    /// <summary>
    /// 第二步：移动镜头到建筑物
    /// </summary>
    IEnumerator MoveCameraCoroutine(GameObject targetBuilding, Vector3? customPosition, Quaternion? customRotation)
    {
        isFocused = true;

        // 保存镜头原始状态
        if (mainCamera != null)
        {
            originalCameraPosition = mainCamera.transform.position;
            originalCameraRotation = mainCamera.transform.rotation;
        }

        // 禁用镜头控制器
        if (cameraController != null)
        {
            cameraController.enabled = false;
        }

        // 计算目标镜头位置和旋转
        Vector3 targetCameraPosition;
        Quaternion targetCameraRotation;

        if (customPosition.HasValue && customRotation.HasValue)
        {
            // 使用自定义位置和旋转
            targetCameraPosition = customPosition.Value;
            targetCameraRotation = customRotation.Value;
            Debug.Log($"使用自定义镜头位置: {targetCameraPosition}");
        }
        else
        {
            // 自动计算位置
            Vector3 buildingPosition = targetBuilding.transform.position;
            
            // 使用Renderer的bounds获取更准确的中心
            Renderer renderer = targetBuilding.GetComponent<Renderer>();
            if (renderer != null)
            {
                buildingPosition = renderer.bounds.center;
            }

            // 计算镜头位置（建筑物正面）
            targetCameraPosition = buildingPosition + new Vector3(0, heightOffset, -focusDistance);
            targetCameraRotation = Quaternion.LookRotation(buildingPosition - targetCameraPosition);
            Debug.Log($"自动计算镜头位置: {targetCameraPosition}");
        }

        // 移动镜头
        float elapsed = 0f;
        Vector3 startCameraPosition = mainCamera.transform.position;
        Quaternion startCameraRotation = mainCamera.transform.rotation;

        while (elapsed < focusDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / focusDuration;
            float smoothT = Mathf.SmoothStep(0f, 1f, t);

            // 移动镜头
            if (mainCamera != null)
            {
                mainCamera.transform.position = Vector3.Lerp(startCameraPosition, targetCameraPosition, smoothT);
                mainCamera.transform.rotation = Quaternion.Slerp(startCameraRotation, targetCameraRotation, smoothT);
            }

            yield return null;
        }

        // 确保最终状态
        if (mainCamera != null)
        {
            mainCamera.transform.position = targetCameraPosition;
            mainCamera.transform.rotation = targetCameraRotation;
        }

        Debug.Log($"镜头已移动到建筑物: {targetBuilding.name}");
    }

    /// <summary>
    /// 取消聚焦协程
    /// </summary>
    IEnumerator UnfocusCoroutine()
    {
        Debug.Log("开始恢复场景");

        // 同时进行：显示其他建筑 + 镜头回正 + 地面恢复颜色
        float elapsed = 0f;
        Vector3 startCameraPosition = mainCamera.transform.position;
        Quaternion startCameraRotation = mainCamera.transform.rotation;
        Color startGroundColor = groundMaterial != null ? groundMaterial.color : focusGroundColor;

        while (elapsed < focusDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / focusDuration;
            float smoothT = Mathf.SmoothStep(0f, 1f, t);

            // 移动镜头回到原位
            if (mainCamera != null)
            {
                mainCamera.transform.position = Vector3.Lerp(startCameraPosition, originalCameraPosition, smoothT);
                mainCamera.transform.rotation = Quaternion.Slerp(startCameraRotation, originalCameraRotation, smoothT);
            }

            // 淡入其他建筑物
            FadeOtherBuildings(currentFocusedBuilding, smoothT);
            
            // 恢复地面颜色
            if (groundMaterial != null)
            {
                groundMaterial.color = Color.Lerp(startGroundColor, originalGroundColor, smoothT);
            }

            yield return null;
        }

        // 确保最终状态
        if (mainCamera != null)
        {
            mainCamera.transform.position = originalCameraPosition;
            mainCamera.transform.rotation = originalCameraRotation;
        }
        FadeOtherBuildings(currentFocusedBuilding, 1f);
        if (groundMaterial != null)
        {
            groundMaterial.color = originalGroundColor;
        }

        // 重新启用镜头控制器
        if (cameraController != null)
        {
            cameraController.enabled = true;
        }

        isFocused = false;
        buildingsHidden = false;
        currentFocusedBuilding = null;

        Debug.Log("场景已恢复");
    }

    /// <summary>
    /// 淡入淡出其他建筑物
    /// </summary>
    void FadeOtherBuildings(GameObject exceptBuilding, float alpha)
    {
        foreach (var kvp in buildingRenderers)
        {
            GameObject building = kvp.Key;
            List<Renderer> renderers = kvp.Value;

            // 跳过当前聚焦的建筑物
            if (building == exceptBuilding)
            {
                continue;
            }

            // 设置所有Renderer的透明度
            foreach (Renderer renderer in renderers)
            {
                if (renderer == null) continue;

                foreach (Material mat in renderer.materials)
                {
                    if (mat == null) continue;

                    // 如果alpha为0，直接禁用Renderer（性能优化）
                    if (alpha <= 0.01f)
                    {
                        renderer.enabled = false;
                    }
                    else
                    {
                        renderer.enabled = true;
                        
                        // 设置透明度
                        if (mat.HasProperty("_Color"))
                        {
                            Color color = mat.color;
                            color.a = alpha;
                            mat.color = color;
                        }

                        // 如果需要，切换到透明渲染模式
                        if (alpha < 1f && mat.HasProperty("_Mode"))
                        {
                            mat.SetFloat("_Mode", 3); // Transparent
                            mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                            mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                            mat.SetInt("_ZWrite", 0);
                            mat.DisableKeyword("_ALPHATEST_ON");
                            mat.EnableKeyword("_ALPHABLEND_ON");
                            mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                            mat.renderQueue = 3000;
                        }
                    }
                }
            }
        }
    }

    /// <summary>
    /// 检查是否处于聚焦状态
    /// </summary>
    public bool IsFocused()
    {
        return isFocused;
    }

    /// <summary>
    /// 获取当前聚焦的建筑物
    /// </summary>
    public GameObject GetFocusedBuilding()
    {
        return currentFocusedBuilding;
    }
}
