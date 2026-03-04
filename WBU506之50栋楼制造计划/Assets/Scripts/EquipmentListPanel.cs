using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using UnityEngine.EventSystems;

/// <summary>
/// 设备列表面板 - 显示所有消防器和烟雾报警器
/// </summary>
public class EquipmentListPanel : MonoBehaviour, IScrollHandler
{
    [Header("UI 引用")]
    [Tooltip("消防器列表容器")]
    public Transform extinguisherContainer;

    [Tooltip("烟雾报警器列表容器")]
    public Transform detectorContainer;

    [Tooltip("列表项预制体")]
    public GameObject listItemPrefab;

    [Tooltip("中文字体资源")]
    public TMP_FontAsset chineseFont;

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

    /// <summary>
    /// 楼层标题布局配置常量
    /// </summary>
    private const float ARROW_HORIZONTAL_POSITION = 5f;    // 箭头距离左边缘的距离
    private const float ARROW_SIZE = 14f;                   // 箭头图标的宽度和高度
    private const float TEXT_LEFT_OFFSET = 22f;             // 文本距离左边缘的距离
    private const float FLOOR_HEADER_HEIGHT = 30f;          // 楼层标题的总高度
    private const float COLLAPSED_HEADER_ALPHA = 0.3f;      // 折叠状态下其他标题的透明度

    private List<ClickableObject> allEquipment = new List<ClickableObject>();
    private Dictionary<ClickableObject, Image> equipmentButtons = new Dictionary<ClickableObject, Image>();
    private ClickableObject currentSelected = null;
    private Dictionary<string, GameObject> floorContainers = new Dictionary<string, GameObject>();
    private Dictionary<string, bool> floorExpandedState = new Dictionary<string, bool>();
    private Dictionary<string, TextMeshProUGUI> floorIcons = new Dictionary<string, TextMeshProUGUI>();
    private Dictionary<string, Image> floorHeaderBackgrounds = new Dictionary<string, Image>();  // 存储楼层标题背景
    private ScrollRect scrollRect;  // ScrollRect引用
    private FloorController floorController;  // 楼层控制器引用

    void Start()
    {
        // 确保EquipmentListPanel有Image组件来接收滚轮事件
        Image panelImage = GetComponent<Image>();
        if (panelImage == null)
        {
            panelImage = gameObject.AddComponent<Image>();
            panelImage.color = new Color(0, 0, 0, 0);  // 完全透明
            panelImage.raycastTarget = true;  // 必须启用以接收滚轮事件
            Debug.Log("EquipmentListPanel: 添加了透明Image组件以接收滚轮事件");
        }

        // 获取ScrollRect引用 - 尝试多种方式
        scrollRect = GetComponentInParent<ScrollRect>();
        if (scrollRect == null)
        {
            scrollRect = GetComponentInChildren<ScrollRect>();
        }
        if (scrollRect == null)
        {
            // 尝试通过名称查找ScrollView
            Transform scrollViewTransform = transform.Find("ScrollView");
            if (scrollViewTransform != null)
            {
                scrollRect = scrollViewTransform.GetComponent<ScrollRect>();
            }
        }
        if (scrollRect == null)
        {
            // 最后尝试在场景中查找
            scrollRect = FindObjectOfType<ScrollRect>();
        }

        if (scrollRect != null)
        {
            Debug.Log("EquipmentListPanel: 成功找到ScrollRect");
            Debug.Log($"ScrollRect配置: Sensitivity={scrollRect.scrollSensitivity}, Vertical={scrollRect.vertical}, Content={(scrollRect.content != null ? "已分配" : "未分配")}");
        }
        else
        {
            Debug.LogWarning("EquipmentListPanel: 未找到ScrollRect,滚动位置可能无法保持");
        }

        // 查找FloorController
        floorController = FindObjectOfType<FloorController>();
        if (floorController == null)
        {
            Debug.LogWarning("EquipmentListPanel: 未找到 FloorController");
        }

        if (autoFindEquipment)
        {
            FindAllEquipment();
            PopulateList();
        }

        // 监听信息面板关闭事件（只订阅一次）
        SubscribeToInfoPanelEvents();
    }

    /// <summary>
    /// 订阅信息面板事件（确保只订阅一次）
    /// </summary>
    void SubscribeToInfoPanelEvents()
    {
        InfoPanel infoPanel = FindObjectOfType<InfoPanel>();
        if (infoPanel != null)
        {
            // 先取消订阅，避免重复
            infoPanel.onPanelClosed -= OnInfoPanelClosed;
            // 重新订阅
            infoPanel.onPanelClosed += OnInfoPanelClosed;
            Debug.Log("EquipmentListPanel: 已绑定信息面板关闭事件");
        }
        else
        {
            Debug.LogWarning("EquipmentListPanel: 未找到 InfoPanel");
        }
    }

    /// <summary>
    /// 实现IScrollHandler接口 - 手动处理滚轮事件
    /// </summary>
    public void OnScroll(PointerEventData eventData)
    {
        if (scrollRect != null && scrollRect.content != null)
        {
            // 将滚轮事件传递给ScrollRect
            scrollRect.OnScroll(eventData);
            Debug.Log($"手动处理滚轮: {eventData.scrollDelta}");
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
        floorContainers.Clear();
        floorExpandedState.Clear();
        floorIcons.Clear();
        floorHeaderBackgrounds.Clear();

        // 按类型和楼层分组
        var extinguishersByFloor = new Dictionary<string, List<ClickableObject>>();
        var detectorsByFloor = new Dictionary<string, List<ClickableObject>>();

        foreach (ClickableObject equipment in allEquipment)
        {
            // 根据名称判断类型
            bool isExtinguisher = equipment.objectName.ToLower().Contains("extinguisher") ||
                                 equipment.objectName.ToLower().Contains("灭火器") ||
                                 equipment.objectName.ToLower().Contains("消防器");

            // 提取楼层信息
            string floor = ExtractFloor(equipment);

            if (isExtinguisher)
            {
                if (!extinguishersByFloor.ContainsKey(floor))
                    extinguishersByFloor[floor] = new List<ClickableObject>();
                extinguishersByFloor[floor].Add(equipment);
            }
            else
            {
                if (!detectorsByFloor.ContainsKey(floor))
                    detectorsByFloor[floor] = new List<ClickableObject>();
                detectorsByFloor[floor].Add(equipment);
            }
        }

        // 创建消防器分组（使用唯一的键前缀）
        CreateFloorGroups(extinguisherContainer, extinguishersByFloor, "Ext_");

        // 创建烟雾报警器分组（使用唯一的键前缀）
        CreateFloorGroups(detectorContainer, detectorsByFloor, "Det_");
    }

    /// <summary>
    /// 提取楼层信息
    /// </summary>
    string ExtractFloor(ClickableObject equipment)
    {
        string name = equipment.objectName.ToLower();
        string info = equipment.detailInfo.ToLower();

        // 检查名称和详细信息中的楼层标识
        string[] floorKeywords = { "1f", "2f", "3f", "4f", "5f", "一楼", "二楼", "三楼", "四楼", "五楼",
                                   "1楼", "2楼", "3楼", "4楼", "5楼" };

        for (int i = 0; i < floorKeywords.Length; i++)
        {
            if (name.Contains(floorKeywords[i]) || info.Contains(floorKeywords[i]))
            {
                // 返回统一的楼层标识
                if (i < 5)
                    return $"{i + 1}F";
                else if (i < 10)
                    return $"{i - 4}F";
                else
                    return $"{i - 9}F";
            }
        }

        return "未分类";
    }

    /// <summary>
    /// 创建楼层分组
    /// </summary>
    void CreateFloorGroups(Transform container, Dictionary<string, List<ClickableObject>> groupedEquipment, string keyPrefix)
    {
        // 按楼层排序
        var sortedFloors = new List<string>(groupedEquipment.Keys);
        sortedFloors.Sort((a, b) => {
            if (a == "未分类") return 1;
            if (b == "未分类") return -1;

            int numA = int.TryParse(a.Replace("F", ""), out int na) ? na : 999;
            int numB = int.TryParse(b.Replace("F", ""), out int nb) ? nb : 999;
            return numA.CompareTo(numB);
        });

        foreach (string floor in sortedFloors)
        {
            // 使用唯一的键（前缀 + 楼层）
            string uniqueKey = keyPrefix + floor;

            // 创建楼层组容器（包含标题和内容）
            GameObject floorGroup = new GameObject($"FloorGroup_{uniqueKey}");
            floorGroup.transform.SetParent(container, false);

            RectTransform groupRect = floorGroup.AddComponent<RectTransform>();
            groupRect.sizeDelta = new Vector2(0, 0);

            VerticalLayoutGroup groupLayout = floorGroup.AddComponent<VerticalLayoutGroup>();
            groupLayout.spacing = 0;
            groupLayout.childControlHeight = false;
            groupLayout.childControlWidth = true;
            groupLayout.childForceExpandHeight = false;
            groupLayout.childForceExpandWidth = true;

            ContentSizeFitter groupFitter = floorGroup.AddComponent<ContentSizeFitter>();
            groupFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            // 创建楼层标题（作为组的第一个子对象）
            GameObject header = CreateFloorHeader(floorGroup.transform, floor, uniqueKey);

            // 创建内容容器（作为组的第二个子对象，紧跟标题）
            GameObject contentContainer = new GameObject($"Content_{uniqueKey}");
            contentContainer.transform.SetParent(floorGroup.transform, false);

            RectTransform contentRect = contentContainer.AddComponent<RectTransform>();
            contentRect.sizeDelta = new Vector2(0, 0);

            VerticalLayoutGroup contentLayout = contentContainer.AddComponent<VerticalLayoutGroup>();
            contentLayout.spacing = 2;
            contentLayout.padding = new RectOffset(15, 0, 2, 5);
            contentLayout.childControlHeight = false;
            contentLayout.childControlWidth = true;
            contentLayout.childForceExpandHeight = false;
            contentLayout.childForceExpandWidth = true;

            ContentSizeFitter contentFitter = contentContainer.AddComponent<ContentSizeFitter>();
            contentFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            // 保存容器引用（使用唯一键）
            floorContainers[uniqueKey] = contentContainer;

            // 初始状态为折叠
            floorExpandedState[uniqueKey] = false;
            contentContainer.SetActive(false);

            // 创建该楼层的设备列表
            foreach (ClickableObject equipment in groupedEquipment[floor])
            {
                CreateListItem(equipment, contentContainer.transform);
            }
        }
    }

    /// <summary>
    /// 创建楼层标题
    /// </summary>
    GameObject CreateFloorHeader(Transform parent, string floorName, string uniqueKey)
    {
        GameObject header = new GameObject($"FloorHeader_{uniqueKey}");
        header.transform.SetParent(parent, false);

        RectTransform rect = header.AddComponent<RectTransform>();
        rect.sizeDelta = new Vector2(0, FLOOR_HEADER_HEIGHT);

        // 添加按钮组件
        Button button = header.AddComponent<Button>();
        button.transition = Selectable.Transition.ColorTint;

        ColorBlock colors = button.colors;
        colors.normalColor = new Color(0.25f, 0.25f, 0.25f, 0.8f);
        colors.highlightedColor = new Color(0.35f, 0.35f, 0.35f, 0.9f);
        colors.pressedColor = new Color(0.2f, 0.2f, 0.2f, 1f);
        button.colors = colors;

        Image headerBg = header.AddComponent<Image>();
        headerBg.color = new Color(0.25f, 0.25f, 0.25f, 0.8f);
        headerBg.raycastTarget = true;  // 楼层标题需要接收点击,但Button会处理,Image保持启用让Button工作

        // 保存背景引用
        floorHeaderBackgrounds[uniqueKey] = headerBg;

        // 添加展开/折叠图标
        GameObject iconObj = new GameObject("Icon");
        iconObj.transform.SetParent(header.transform, false);

        RectTransform iconRect = iconObj.AddComponent<RectTransform>();
        iconRect.anchorMin = new Vector2(0, 0.5f);
        iconRect.anchorMax = new Vector2(0, 0.5f);
        iconRect.pivot = new Vector2(0, 0.5f);
        iconRect.anchoredPosition = new Vector2(ARROW_HORIZONTAL_POSITION, 0);
        iconRect.sizeDelta = new Vector2(ARROW_SIZE, ARROW_SIZE);

        TextMeshProUGUI icon = iconObj.AddComponent<TextMeshProUGUI>();
        icon.text = "▶";
        icon.fontSize = 14;
        icon.color = new Color(0.9f, 0.9f, 0.9f);
        icon.alignment = TextAlignmentOptions.Center;

        if (chineseFont != null)
        {
            icon.font = chineseFont;
        }

        // 保存图标引用
        floorIcons[uniqueKey] = icon;

        // 添加文本
        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(header.transform, false);

        RectTransform textRect = textObj.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.sizeDelta = Vector2.zero;
        textRect.offsetMin = new Vector2(TEXT_LEFT_OFFSET, 0);

        TextMeshProUGUI text = textObj.AddComponent<TextMeshProUGUI>();

        // 转换楼层名称为中文
        string displayName = floorName;
        if (floorName.EndsWith("F") && floorName != "未分类")
        {
            string num = floorName.Replace("F", "");
            displayName = $"{num}楼";
        }

        text.text = displayName;
        text.fontSize = 15;
        text.fontStyle = FontStyles.Bold;
        text.color = new Color(0.95f, 0.95f, 0.95f);
        text.alignment = TextAlignmentOptions.Left;
        text.alignment = TextAlignmentOptions.Midline;

        if (chineseFont != null)
        {
            text.font = chineseFont;
        }

        // 绑定点击事件（使用唯一键）
        button.onClick.AddListener(() => ToggleFloor(uniqueKey));

        return header;
    }

    /// <summary>
    /// 切换楼层展开/折叠状态
    /// </summary>
    void ToggleFloor(string uniqueKey)
    {
        if (!floorContainers.ContainsKey(uniqueKey))
        {
            Debug.LogWarning($"未找到楼层容器: {uniqueKey}");
            return;
        }

        // 提取楼层名称（移除前缀）
        string floorName = uniqueKey.Replace("Ext_", "").Replace("Det_", "");

        // **关键**: 在修改任何UI之前先保存滚动位置
        Vector2 savedScrollPosition = Vector2.zero;
        float savedVerticalPosition = 0f;
        if (scrollRect != null)
        {
            savedScrollPosition = scrollRect.normalizedPosition;
            savedVerticalPosition = scrollRect.verticalNormalizedPosition;
            Debug.Log($"[ToggleFloor] 保存滚动位置: {savedScrollPosition}, 垂直: {savedVerticalPosition}");
        }

        // 如果FloorController存在，调用楼层隔离功能
        if (floorController != null)
        {
            bool isExpanded = floorExpandedState[uniqueKey];
            
            // 如果当前楼层已展开，则收起并退出隔离模式
            if (isExpanded)
            {
                // 收起列表
                floorExpandedState[uniqueKey] = false;
                floorContainers[uniqueKey].SetActive(false);
                
                // 更新箭头方向
                if (floorIcons.ContainsKey(uniqueKey))
                {
                    floorIcons[uniqueKey].text = "▶";
                }
                
                // 退出隔离模式
                floorController.ExitIsolationMode();
            }
            else
            {
                // 展开列表并进入隔离模式
                floorExpandedState[uniqueKey] = true;
                floorContainers[uniqueKey].SetActive(true);
                
                // 更新箭头方向
                if (floorIcons.ContainsKey(uniqueKey))
                {
                    floorIcons[uniqueKey].text = "▼";
                }
                
                // 进入隔离模式
                floorController.EnterIsolationMode(floorName);
            }
            
            // 更新所有楼层标题的透明度
            UpdateFloorHeadersAlpha();
            
            // 强制刷新布局并恢复滚动位置
            StartCoroutine(RefreshLayoutAndRestoreScroll(savedScrollPosition, savedVerticalPosition));
            
            return;
        }

        // 如果没有FloorController，执行原来的展开/折叠逻辑
        // 切换状态
        bool isExpandedState = floorExpandedState[uniqueKey];
        floorExpandedState[uniqueKey] = !isExpandedState;

        // 显示/隐藏内容
        floorContainers[uniqueKey].SetActive(!isExpandedState);

        // 更新箭头方向
        if (floorIcons.ContainsKey(uniqueKey))
        {
            floorIcons[uniqueKey].text = !isExpandedState ? "▼" : "▶";
        }

        // 更新所有楼层标题的透明度
        UpdateFloorHeadersAlpha();

        // 强制刷新布局并恢复滚动位置
        StartCoroutine(RefreshLayoutAndRestoreScroll(savedScrollPosition, savedVerticalPosition));

        Debug.Log($"楼层 {uniqueKey} {(!isExpandedState ? "展开" : "折叠")}");
    }

    /// <summary>
    /// 更新所有楼层标题的透明度
    /// </summary>
    void UpdateFloorHeadersAlpha()
    {
        // 检查是否有任何楼层处于展开状态
        bool hasExpandedFloor = false;
        foreach (var state in floorExpandedState.Values)
        {
            if (state)
            {
                hasExpandedFloor = true;
                break;
            }
        }

        // 如果有展开的楼层,让折叠的楼层标题变透明
        if (hasExpandedFloor)
        {
            foreach (var kvp in floorExpandedState)
            {
                string key = kvp.Key;
                bool isExpanded = kvp.Value;

                if (floorHeaderBackgrounds.ContainsKey(key))
                {
                    Image headerBg = floorHeaderBackgrounds[key];
                    Color bgColor = headerBg.color;

                    // 如果是折叠状态,设置为透明
                    if (!isExpanded)
                    {
                        bgColor.a = COLLAPSED_HEADER_ALPHA;
                    }
                    else
                    {
                        bgColor.a = 0.8f;  // 展开的保持原来的不透明度
                    }

                    headerBg.color = bgColor;
                }

                // 同时调整箭头和文字的透明度
                if (floorIcons.ContainsKey(key))
                {
                    TextMeshProUGUI icon = floorIcons[key];
                    Color iconColor = icon.color;
                    iconColor.a = isExpanded ? 0.9f : COLLAPSED_HEADER_ALPHA;
                    icon.color = iconColor;
                }
            }
        }
        else
        {
            // 如果所有楼层都折叠,恢复所有标题的不透明度
            foreach (var key in floorHeaderBackgrounds.Keys)
            {
                Image headerBg = floorHeaderBackgrounds[key];
                Color bgColor = headerBg.color;
                bgColor.a = 0.8f;
                headerBg.color = bgColor;

                if (floorIcons.ContainsKey(key))
                {
                    TextMeshProUGUI icon = floorIcons[key];
                    Color iconColor = icon.color;
                    iconColor.a = 0.9f;
                    icon.color = iconColor;
                }
            }
        }
    }

    /// <summary>
    /// 刷新布局并恢复滚动位置
    /// </summary>
    System.Collections.IEnumerator RefreshLayoutAndRestoreScroll(Vector2 savedScrollPosition, float savedVerticalPosition)
    {
        Debug.Log($"[RefreshLayout] 开始刷新,目标位置: {savedScrollPosition}, 垂直: {savedVerticalPosition}");

        // 等待一帧让SetActive生效
        yield return null;

        // 第一次布局刷新 - 刷新所有楼层组
        foreach (var container in floorContainers.Values)
        {
            if (container != null && container.activeInHierarchy)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(container.GetComponent<RectTransform>());
            }
        }

        yield return null;

        // 第二次布局刷新 - 刷新分类容器
        if (extinguisherContainer != null)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(extinguisherContainer.GetComponent<RectTransform>());
        }

        if (detectorContainer != null)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(detectorContainer.GetComponent<RectTransform>());
        }

        yield return null;

        // 第三次布局刷新 - 刷新ScrollRect的content
        if (scrollRect != null && scrollRect.content != null)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(scrollRect.content);
        }

        // 再等待两帧确保布局完全稳定
        yield return null;
        yield return null;

        // 恢复滚动位置
        if (scrollRect != null && scrollRect.content != null)
        {
            scrollRect.normalizedPosition = savedScrollPosition;
            scrollRect.verticalNormalizedPosition = savedVerticalPosition;
            scrollRect.velocity = Vector2.zero;

            Debug.Log($"[RefreshLayout] 第一次恢复位置: {scrollRect.normalizedPosition}");

            // 再等一帧并再次设置,确保生效
            yield return null;
            scrollRect.normalizedPosition = savedScrollPosition;
            scrollRect.verticalNormalizedPosition = savedVerticalPosition;

            Debug.Log($"[RefreshLayout] 最终位置: {scrollRect.normalizedPosition}");
        }
        else
        {
            Debug.LogWarning("[RefreshLayout] ScrollRect或Content为null,无法恢复滚动位置");
        }
    }

    /// <summary>
    /// 下一帧刷新布局(保持滚动位置)
    /// </summary>
    System.Collections.IEnumerator RefreshLayoutNextFrame()
    {
        // 保存当前滚动位置
        Vector2 savedScrollPosition = Vector2.zero;
        float savedVerticalPosition = 0f;

        if (scrollRect != null)
        {
            savedScrollPosition = scrollRect.normalizedPosition;
            savedVerticalPosition = scrollRect.verticalNormalizedPosition;
            Debug.Log($"保存滚动位置: {savedScrollPosition}, 垂直位置: {savedVerticalPosition}");
        }
        else
        {
            Debug.LogWarning("RefreshLayoutNextFrame: ScrollRect为null,无法保存滚动位置");
        }

        yield return null;

        // 刷新所有容器的布局
        if (extinguisherContainer != null)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(extinguisherContainer.GetComponent<RectTransform>());
        }

        if (detectorContainer != null)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(detectorContainer.GetComponent<RectTransform>());
        }

        // 强制刷新ScrollRect的content布局
        if (scrollRect != null && scrollRect.content != null)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(scrollRect.content);
        }

        // 等待两帧让布局完全更新
        yield return null;
        yield return null;

        // 恢复滚动位置 - 使用多种方式确保恢复
        if (scrollRect != null && scrollRect.content != null)
        {
            scrollRect.normalizedPosition = savedScrollPosition;
            scrollRect.verticalNormalizedPosition = savedVerticalPosition;

            // 强制停止滚动惯性
            scrollRect.velocity = Vector2.zero;

            Debug.Log($"恢复滚动位置: {savedScrollPosition}, 垂直位置: {savedVerticalPosition}");

            // 再等一帧确保位置设置生效
            yield return null;
            scrollRect.normalizedPosition = savedScrollPosition;
        }
        else
        {
            Debug.LogWarning("RefreshLayoutNextFrame: ScrollRect或Content为null,无法恢复滚动位置");
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
            image.raycastTarget = true;  // 按钮需要接收点击,保持启用

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

            // 使用中文字体
            if (chineseFont != null)
            {
                text.font = chineseFont;
            }
            else
            {
                Debug.LogWarning("EquipmentListPanel: 未设置中文字体，列表项可能无法正确显示中文");
            }

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
