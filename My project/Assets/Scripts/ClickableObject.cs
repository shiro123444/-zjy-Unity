using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

/// <summary>
/// 可点击物体 - 点击后显示信息和选中效果
/// </summary>
[RequireComponent(typeof(Collider))]
public class ClickableObject : MonoBehaviour
{
    [Header("物体信息")]
    [Tooltip("物体名称")]
    public string objectName = "Fire Extinguisher A1";
    
    [Tooltip("详细信息（支持多行）")]
    [TextArea(5, 10)]
    public string detailInfo = "Fire Extinguisher A1\n\nType: Dry Powder\nLocation: 1F Corridor\nStatus: Normal\nLast Check: 2024-03-01";

    [Header("选中效果")]
    [Tooltip("波纹材质（可选）")]
    public Material rippleMaterial;
    
    [Tooltip("波纹颜色")]
    public Color rippleColor = new Color(1f, 0.5f, 0f, 0.8f);
    
    [Tooltip("波纹大小")]
    public float rippleSize = 2f;

    private SelectionRipple currentRipple;
    private static ClickableObject currentSelected;
    private Camera mainCamera;

    void Start()
    {
        // 确保有 Collider
        Collider col = GetComponent<Collider>();
        if (col == null)
        {
            Debug.LogWarning($"{objectName} 没有 Collider，添加 BoxCollider");
            gameObject.AddComponent<BoxCollider>();
        }

        mainCamera = Camera.main;
        Debug.Log($"{objectName} 已初始化，可以点击");
    }

    void Update()
    {
        // 使用新的 Input System 检测点击
        Mouse mouse = Mouse.current;
        if (mouse == null) return;

        if (mouse.leftButton.wasPressedThisFrame)
        {
            // 检查是否点击到 UI
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
                return;

            // 射线检测
            Ray ray = mainCamera.ScreenPointToRay(mouse.position.ReadValue());
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit))
            {
                if (hit.collider.gameObject == gameObject)
                {
                    Debug.Log($"点击了 {objectName}");
                    OnClick();
                }
            }
        }
    }

    void OnMouseDown()
    {
        // 保留旧的输入系统支持（作为备用）
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            return;

        Debug.Log($"OnMouseDown: {objectName}");
        OnClick();
    }

    /// <summary>
    /// 点击处理
    /// </summary>
    public void OnClick()
    {
        Debug.Log($"OnClick 被调用: {objectName}");

        // 取消之前选中的物体
        if (currentSelected != null && currentSelected != this)
        {
            currentSelected.Deselect();
        }

        // 选中当前物体
        Select();
        currentSelected = this;

        // 显示信息面板
        InfoPanel infoPanelManager = FindObjectOfType<InfoPanel>();
        if (infoPanelManager != null)
        {
            Debug.Log($"找到 InfoPanel，显示信息");
            infoPanelManager.ShowInfo(objectName, detailInfo);
        }
        else
        {
            Debug.LogWarning("场景中没有找到 InfoPanel，请使用 Tools > 创建信息面板 UI");
        }
    }

    /// <summary>
    /// 选中物体
    /// </summary>
    void Select()
    {
        Debug.Log($"Select 被调用: {objectName}");

        // 如果已经有波纹效果，先移除
        if (currentRipple != null)
        {
            Destroy(currentRipple.gameObject);
        }

        // 创建波纹效果
        GameObject rippleObj = new GameObject($"{objectName}_Ripple");
        rippleObj.transform.position = transform.position;
        rippleObj.transform.SetParent(transform);

        currentRipple = rippleObj.AddComponent<SelectionRipple>();
        currentRipple.Initialize(rippleColor, rippleSize, rippleMaterial);
        
        Debug.Log($"波纹效果已创建: {rippleObj.name}");
    }

    /// <summary>
    /// 取消选中
    /// </summary>
    void Deselect()
    {
        if (currentRipple != null)
        {
            Destroy(currentRipple.gameObject);
            currentRipple = null;
        }
    }

    /// <summary>
    /// 鼠标悬停高亮（可选）
    /// </summary>
    void OnMouseEnter()
    {
        // 可以添加悬停效果
    }

    void OnMouseExit()
    {
        // 移除悬停效果
    }
}
