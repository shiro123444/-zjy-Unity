using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// 设备列表面板 - 显示所有消防器和烟雾报警器
/// </summary>
public class EquipmentListPanel : MonoBehaviour
{
    [Header("UI 引用")]
    [Tooltip("消防器列表容器")]
    public Transform extinguisherContainer;
    
    [Tooltip("烟雾报警器列表容器")]
    public Transform detectorContainer;
    
    [Tooltip("列表项预制体")]
    public GameObject listItemPrefab;

    [Header("设置")]
    [Tooltip("自动查找设备")]
    public bool autoFindEquipment = true;

    [Header("颜色设置")]
    [Tooltip("正常颜色")]
    public Color normalColor = new Color(0.2f, 0.2f, 0.2f, 0.8f);
    
    [Tooltip("选中颜色")]
    public Color selectedColor = new Color(0.3f, 0.6f, 0.9f, 1f);
    
    [Tooltip("悬停颜色")]
    public Color hoverColor = new Color(0.3f, 0.3f, 0.3f, 0.9f);

    private List<ClickableObject> allEquipment = new List<ClickableObject>();
    private Dictionary<ClickableObject, Image> equipmentButtons = new Dictionary<ClickableObject, Image>();
    private ClickableObject currentSelected = null;

    void Start()
    {
        if (autoFindEquipment)
        {
            FindAllEquipment();
            PopulateList();
        }

        // 监听信息面板关闭事件
        InfoPanel infoPanel = FindObjectOfType<InfoPanel>();
        if (infoPanel != null)
        {
            infoPanel.onPanelClosed += OnInfoPanelClosed;
            Debug.Log("EquipmentListPanel: 已绑定信息面板关闭事件");
        }
        else
        {
            Debug.LogWarning("EquipmentListPanel: 未找到 InfoPanel");
        }
    }

    /// <summary>
    /// 信息面板关闭时取消选中
    /// </summary>
    void OnInfoPanelClosed()
    {
        Debug.Log("EquipmentListPanel: 信息面板关闭，恢复列表项颜色");
        
        if (currentSelected != null && equipmentButtons.ContainsKey(currentSelected))
        {
            equipmentButtons[currentSelected].color = normalColor;
            Debug.Log($"已恢复 {currentSelected.objectName} 的颜色");
            currentSelected = null;
        }
    }

    /// <summary>
    /// 查找场景中所有设备
    /// </summary>
    void FindAllEquipment()
    {
        allEquipment.Clear();
        ClickableObject[] equipment = FindObjectsOfType<ClickableObject>();
        allEquipment.AddRange(equipment);
        
        Debug.Log($"找到 {allEquipment.Count} 个设备");
    }

    /// <summary>
    /// 填充列表
    /// </summary>
    void PopulateList()
    {
        // 清空现有列表
        ClearContainer(extinguisherContainer);
        ClearContainer(detectorContainer);
        equipmentButtons.Clear();

        foreach (ClickableObject equipment in allEquipment)
        {
            // 根据名称判断类型
            bool isExtinguisher = equipment.objectName.ToLower().Contains("extinguisher") || 
                                 equipment.objectName.ToLower().Contains("灭火器") ||
                                 equipment.objectName.ToLower().Contains("消防器");
            
            Transform targetContainer = isExtinguisher ? extinguisherContainer : detectorContainer;
            
            CreateListItem(equipment, targetContainer);
        }
    }

    /// <summary>
    /// 创建列表项
    /// </summary>
    void CreateListItem(ClickableObject equipment, Transform container)
    {
        GameObject item;
        
        if (listItemPrefab != null)
        {
            item = Instantiate(listItemPrefab, container);
        }
        else
        {
            // 如果没有预制体，创建简单的按钮
            item = new GameObject(equipment.objectName);
            item.transform.SetParent(container, false);
            
            RectTransform rect = item.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(0, 35);
            
            Button button = item.AddComponent<Button>();
            Image image = item.AddComponent<Image>();
            image.color = normalColor;
            
            // 保存按钮图像引用
            equipmentButtons[equipment] = image;
            
            // 设置按钮颜色（使用 Transition.None，手动控制颜色）
            button.transition = Selectable.Transition.None;
            
            // 添加文本
            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(item.transform, false);
            
            RectTransform textRect = textObj.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.sizeDelta = Vector2.zero;
            textRect.offsetMin = new Vector2(10, 0);
            textRect.offsetMax = new Vector2(-10, 0);
            
            TextMeshProUGUI text = textObj.AddComponent<TextMeshProUGUI>();
            text.text = equipment.objectName;
            text.fontSize = 14;
            text.color = Color.white;
            text.alignment = TextAlignmentOptions.Left;
            text.alignment = TextAlignmentOptions.Midline;
            
            // 添加悬停效果
            UnityEngine.EventSystems.EventTrigger trigger = item.AddComponent<UnityEngine.EventSystems.EventTrigger>();
            
            // 鼠标进入
            UnityEngine.EventSystems.EventTrigger.Entry entryEnter = new UnityEngine.EventSystems.EventTrigger.Entry();
            entryEnter.eventID = UnityEngine.EventSystems.EventTriggerType.PointerEnter;
            entryEnter.callback.AddListener((data) => { OnPointerEnter(equipment); });
            trigger.triggers.Add(entryEnter);
            
            // 鼠标离开
            UnityEngine.EventSystems.EventTrigger.Entry entryExit = new UnityEngine.EventSystems.EventTrigger.Entry();
            entryExit.eventID = UnityEngine.EventSystems.EventTriggerType.PointerExit;
            entryExit.callback.AddListener((data) => { OnPointerExit(equipment); });
            trigger.triggers.Add(entryExit);
            
            // 绑定点击事件
            button.onClick.AddListener(() => OnEquipmentClicked(equipment));
        }
    }

    /// <summary>
    /// 鼠标进入
    /// </summary>
    void OnPointerEnter(ClickableObject equipment)
    {
        if (currentSelected != equipment && equipmentButtons.ContainsKey(equipment))
        {
            equipmentButtons[equipment].color = hoverColor;
        }
    }

    /// <summary>
    /// 鼠标离开
    /// </summary>
    void OnPointerExit(ClickableObject equipment)
    {
        if (currentSelected != equipment && equipmentButtons.ContainsKey(equipment))
        {
            equipmentButtons[equipment].color = normalColor;
        }
    }

    /// <summary>
    /// 设备被点击
    /// </summary>
    void OnEquipmentClicked(ClickableObject equipment)
    {
        Debug.Log($"列表中点击了: {equipment.objectName}");
        
        // 更新选中状态
        UpdateSelection(equipment);
        
        // 重新订阅信息面板关闭事件（确保事件绑定）
        InfoPanel infoPanel = FindObjectOfType<InfoPanel>();
        if (infoPanel != null)
        {
            // 先取消订阅，避免重复
            infoPanel.onPanelClosed -= OnInfoPanelClosed;
            // 重新订阅
            infoPanel.onPanelClosed += OnInfoPanelClosed;
            Debug.Log("重新订阅了信息面板关闭事件");
        }
        
        // 触发设备点击
        equipment.OnClick();
    }

    /// <summary>
    /// 场景中的设备被选中（从 ClickableObject 调用）
    /// </summary>
    public void OnEquipmentSelectedInScene(ClickableObject equipment)
    {
        Debug.Log($"场景中选中了: {equipment.objectName}");
        
        // 更新选中状态
        UpdateSelection(equipment);
        
        // 重新订阅信息面板关闭事件（确保事件绑定）
        InfoPanel infoPanel = FindObjectOfType<InfoPanel>();
        if (infoPanel != null)
        {
            // 先取消订阅，避免重复
            infoPanel.onPanelClosed -= OnInfoPanelClosed;
            // 重新订阅
            infoPanel.onPanelClosed += OnInfoPanelClosed;
            Debug.Log("重新订阅了信息面板关闭事件");
        }
    }

    /// <summary>
    /// 更新选中状态
    /// </summary>
    void UpdateSelection(ClickableObject equipment)
    {
        // 取消之前选中的高亮
        if (currentSelected != null && equipmentButtons.ContainsKey(currentSelected))
        {
            equipmentButtons[currentSelected].color = normalColor;
        }
        
        // 设置新的选中项
        currentSelected = equipment;
        if (equipmentButtons.ContainsKey(equipment))
        {
            equipmentButtons[equipment].color = selectedColor;
        }
        else
        {
            Debug.LogWarning($"设备 {equipment.objectName} 不在列表中");
        }
    }

    /// <summary>
    /// 清空容器
    /// </summary>
    void ClearContainer(Transform container)
    {
        if (container == null) return;
        
        foreach (Transform child in container)
        {
            Destroy(child.gameObject);
        }
    }

    /// <summary>
    /// 刷新列表
    /// </summary>
    public void RefreshList()
    {
        FindAllEquipment();
        PopulateList();
    }
}
