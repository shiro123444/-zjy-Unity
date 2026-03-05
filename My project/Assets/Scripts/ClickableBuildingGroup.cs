using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;

/// <summary>
/// 点击行为枚举
/// </summary>
public enum ClickBehavior
{
    FocusBuilding,      // 聚焦建筑物
    SceneTransition     // 场景切换
}

/// <summary>
/// 可点击建筑组 - 支持多个子物体的整体高亮和点击
/// </summary>
[RequireComponent(typeof(Collider))]
public class ClickableBuildingGroup : MonoBehaviour
{
    [Header("交互模式")]
    [Tooltip("点击后的行为")]
    public ClickBehavior clickBehavior = ClickBehavior.FocusBuilding;
    
    [Header("聚焦设置（仅用于FocusBuilding模式）")]
    [Tooltip("自定义镜头目标位置（留空则自动计算）")]
    public Transform customCameraTarget;
    
    [Header("场景设置（仅用于SceneTransition模式）")]
    [Tooltip("目标场景名称")]
    public string targetSceneName;
    
    [Tooltip("或使用场景索引（-1表示使用场景名称）")]
    public int targetSceneIndex = -1;

    [Header("视觉反馈")]
    [Tooltip("鼠标悬停时的高亮颜色")]
    public Color hoverColor = new Color(1f, 1f, 0.5f, 1f);
    
    [Tooltip("是否启用高亮效果")]
    public bool enableHighlight = true;

    [Header("高亮范围")]
    [Tooltip("是否高亮所有子物体")]
    public bool highlightChildren = true;

    private List<Renderer> allRenderers = new List<Renderer>();
    private List<Material[]> originalMaterials = new List<Material[]>();
    private List<Material[]> highlightMaterials = new List<Material[]>();
    private bool isHovering = false;

    void Start()
    {
        // 收集所有Renderer组件
        CollectRenderers();

        // 确保有Collider
        Collider col = GetComponent<Collider>();
        if (col == null)
        {
            Debug.LogWarning($"{gameObject.name} 没有Collider组件，无法检测点击！");
        }
    }

    /// <summary>
    /// 收集所有需要高亮的Renderer
    /// </summary>
    void CollectRenderers()
    {
        allRenderers.Clear();
        originalMaterials.Clear();
        highlightMaterials.Clear();

        if (highlightChildren)
        {
            // 获取自身和所有子物体的Renderer
            Renderer[] renderers = GetComponentsInChildren<Renderer>();
            allRenderers.AddRange(renderers);
        }
        else
        {
            // 只获取自身的Renderer
            Renderer renderer = GetComponent<Renderer>();
            if (renderer != null)
            {
                allRenderers.Add(renderer);
            }
        }

        // 为每个Renderer创建材质副本
        foreach (Renderer renderer in allRenderers)
        {
            if (renderer == null) continue;

            // 保存原始材质
            Material[] originals = renderer.materials;
            originalMaterials.Add(originals);

            // 创建高亮材质副本
            Material[] highlights = new Material[originals.Length];
            for (int i = 0; i < originals.Length; i++)
            {
                highlights[i] = new Material(originals[i]);
            }
            highlightMaterials.Add(highlights);
        }

        if (allRenderers.Count == 0)
        {
            Debug.LogWarning($"{gameObject.name} 没有找到任何Renderer组件！");
        }
        else
        {
            Debug.Log($"{gameObject.name} 找到 {allRenderers.Count} 个Renderer组件");
        }
    }

    void Update()
    {
        CheckMouseInteraction();
    }

    /// <summary>
    /// 检测鼠标交互
    /// </summary>
    void CheckMouseInteraction()
    {
        Mouse mouse = Mouse.current;
        if (mouse == null) return;

        // 检测鼠标左键点击
        if (mouse.leftButton.wasPressedThisFrame)
        {
            Ray ray = Camera.main.ScreenPointToRay(mouse.position.ReadValue());
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit))
            {
                if (hit.collider.gameObject == gameObject)
                {
                    OnClicked();
                }
            }
        }

        // 检测鼠标悬停（用于高亮效果）
        if (enableHighlight)
        {
            Ray ray = Camera.main.ScreenPointToRay(mouse.position.ReadValue());
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit, 1000f))
            {
                if (hit.collider.gameObject == gameObject)
                {
                    if (!isHovering)
                    {
                        OnHoverEnter();
                    }
                }
                else
                {
                    if (isHovering)
                    {
                        OnHoverExit();
                    }
                }
            }
            else
            {
                if (isHovering)
                {
                    OnHoverExit();
                }
            }
        }
    }

    /// <summary>
    /// 点击时触发
    /// </summary>
    void OnClicked()
    {
        Debug.Log($"点击了建筑 {gameObject.name}");

        // 根据点击行为执行不同操作
        switch (clickBehavior)
        {
            case ClickBehavior.FocusBuilding:
                FocusOnThisBuilding();
                break;
                
            case ClickBehavior.SceneTransition:
                TransitionToScene();
                break;
        }
    }

    /// <summary>
    /// 聚焦到当前建筑物
    /// </summary>
    void FocusOnThisBuilding()
    {
        // 检查BuildingFocusManager是否存在
        if (BuildingFocusManager.Instance == null)
        {
            Debug.LogError("场景中没有BuildingFocusManager！请添加该组件。");
            return;
        }

        // 如果有自定义镜头目标，使用自定义位置
        if (customCameraTarget != null)
        {
            BuildingFocusManager.Instance.FocusOnBuildingWithCustomTarget(
                gameObject, 
                customCameraTarget.position, 
                customCameraTarget.rotation
            );
        }
        else
        {
            // 否则使用自动计算的位置
            BuildingFocusManager.Instance.FocusOnBuilding(gameObject);
        }
    }

    /// <summary>
    /// 切换场景
    /// </summary>
    void TransitionToScene()
    {
        // 检查SceneTransitionManager是否存在
        if (SceneTransitionManager.Instance == null)
        {
            Debug.LogError("场景中没有SceneTransitionManager！请添加该组件。");
            return;
        }

        // 获取建筑的中心位置作为目标点
        Vector3 targetPosition = transform.position;
        
        // 如果有Renderer，使用bounds的中心
        Renderer renderer = GetComponent<Renderer>();
        if (renderer != null)
        {
            targetPosition = renderer.bounds.center;
        }

        // 使用带镜头移动的场景切换
        if (targetSceneIndex >= 0)
        {
            SceneTransitionManager.Instance.TransitionToSceneWithCamera(targetSceneIndex, targetPosition);
        }
        else if (!string.IsNullOrEmpty(targetSceneName))
        {
            SceneTransitionManager.Instance.TransitionToSceneWithCamera(targetSceneName, targetPosition);
        }
        else
        {
            Debug.LogWarning($"{gameObject.name} 没有设置目标场景！");
        }
    }

    /// <summary>
    /// 鼠标进入时
    /// </summary>
    void OnHoverEnter()
    {
        isHovering = true;
        ApplyHighlight(true);
    }

    /// <summary>
    /// 鼠标离开时
    /// </summary>
    void OnHoverExit()
    {
        isHovering = false;
        ApplyHighlight(false);
    }

    /// <summary>
    /// 应用或移除高亮效果
    /// </summary>
    void ApplyHighlight(bool highlight)
    {
        for (int i = 0; i < allRenderers.Count; i++)
        {
            if (allRenderers[i] == null) continue;

            if (highlight)
            {
                // 应用高亮颜色
                Material[] materials = highlightMaterials[i];
                for (int j = 0; j < materials.Length; j++)
                {
                    materials[j].color = hoverColor;
                }
                allRenderers[i].materials = materials;
            }
            else
            {
                // 恢复原始材质
                allRenderers[i].materials = originalMaterials[i];
            }
        }
    }

    void OnDestroy()
    {
        // 清理高亮材质副本
        foreach (Material[] materials in highlightMaterials)
        {
            foreach (Material mat in materials)
            {
                if (mat != null)
                {
                    Destroy(mat);
                }
            }
        }
    }

    // 在编辑器中可视化Collider范围
    void OnDrawGizmosSelected()
    {
        Collider col = GetComponent<Collider>();
        if (col != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.matrix = transform.localToWorldMatrix;
            
            if (col is BoxCollider)
            {
                BoxCollider box = col as BoxCollider;
                Gizmos.DrawWireCube(box.center, box.size);
            }
            else if (col is SphereCollider)
            {
                SphereCollider sphere = col as SphereCollider;
                Gizmos.DrawWireSphere(sphere.center, sphere.radius);
            }
        }
    }
}
