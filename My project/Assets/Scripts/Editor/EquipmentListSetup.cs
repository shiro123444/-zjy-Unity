using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 设备列表面板快速设置工具
/// </summary>
public class EquipmentListSetup : EditorWindow
{
    [MenuItem("Tools/创建设备列表面板")]
    static void CreateEquipmentListPanel()
    {
        // 查找或创建 Canvas
        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            GameObject canvasObj = new GameObject("Canvas");
            canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            
            CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            
            canvasObj.AddComponent<GraphicRaycaster>();
        }

        // 创建设备列表面板根对象
        GameObject panelRoot = new GameObject("EquipmentListPanel");
        panelRoot.transform.SetParent(canvas.transform, false);
        
        RectTransform rootRect = panelRoot.AddComponent<RectTransform>();
        rootRect.anchorMin = new Vector2(0, 0);
        rootRect.anchorMax = new Vector2(0, 0);
        rootRect.pivot = new Vector2(0, 0);
        rootRect.anchoredPosition = new Vector2(20, 20);
        rootRect.sizeDelta = new Vector2(280, 500);

        // 添加背景
        Image bgImage = panelRoot.AddComponent<Image>();
        bgImage.color = new Color(0.05f, 0.05f, 0.05f, 0.9f);

        // 添加 EquipmentListPanel 脚本
        EquipmentListPanel listPanel = panelRoot.AddComponent<EquipmentListPanel>();

        // 创建标题
        GameObject title = new GameObject("Title");
        title.transform.SetParent(panelRoot.transform, false);
        
        RectTransform titleRect = title.AddComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0, 1);
        titleRect.anchorMax = new Vector2(1, 1);
        titleRect.pivot = new Vector2(0.5f, 1);
        titleRect.anchoredPosition = new Vector2(0, -10);
        titleRect.sizeDelta = new Vector2(-20, 35);

        TextMeshProUGUI titleText = title.AddComponent<TextMeshProUGUI>();
        titleText.text = "设备列表";
        titleText.fontSize = 20;
        titleText.fontStyle = FontStyles.Bold;
        titleText.alignment = TextAlignmentOptions.Center;
        titleText.color = new Color(1f, 0.9f, 0.3f);

        // 创建滚动视图
        GameObject scrollView = new GameObject("ScrollView");
        scrollView.transform.SetParent(panelRoot.transform, false);
        
        RectTransform scrollRect = scrollView.AddComponent<RectTransform>();
        scrollRect.anchorMin = new Vector2(0, 0);
        scrollRect.anchorMax = new Vector2(1, 1);
        scrollRect.pivot = new Vector2(0.5f, 1);
        scrollRect.anchoredPosition = new Vector2(0, -55);
        scrollRect.sizeDelta = new Vector2(-20, -65);

        ScrollRect scroll = scrollView.AddComponent<ScrollRect>();
        scroll.horizontal = false;
        scroll.vertical = true;
        scroll.scrollSensitivity = 20f;  // 设置滚轮灵敏度(默认值通常较小)
        scroll.movementType = ScrollRect.MovementType.Clamped;  // 限制滚动范围
        scroll.inertia = true;  // 启用惯性滚动
        scroll.decelerationRate = 0.135f;  // 惯性减速率

        // 创建 Viewport
        GameObject viewport = new GameObject("Viewport");
        viewport.transform.SetParent(scrollView.transform, false);
        
        RectTransform viewportRect = viewport.AddComponent<RectTransform>();
        viewportRect.anchorMin = Vector2.zero;
        viewportRect.anchorMax = Vector2.one;
        viewportRect.sizeDelta = Vector2.zero;
        
        viewport.AddComponent<Image>().color = new Color(0, 0, 0, 0.1f);
        Image viewportImage = viewport.GetComponent<Image>();
        viewportImage.raycastTarget = false;  // Viewport不需要接收点击,禁用以允许滚轮穿透
        viewport.AddComponent<Mask>().showMaskGraphic = false;

        scroll.viewport = viewportRect;

        // 创建 Content
        GameObject content = new GameObject("Content");
        content.transform.SetParent(viewport.transform, false);
        
        RectTransform contentRect = content.AddComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0, 1);
        contentRect.anchorMax = new Vector2(1, 1);
        contentRect.pivot = new Vector2(0.5f, 1);
        contentRect.anchoredPosition = Vector2.zero;
        contentRect.sizeDelta = new Vector2(0, 0);

        VerticalLayoutGroup layout = content.AddComponent<VerticalLayoutGroup>();
        layout.spacing = 5;
        layout.padding = new RectOffset(5, 5, 5, 5);
        layout.childControlHeight = false;
        layout.childControlWidth = true;
        layout.childForceExpandHeight = false;
        layout.childForceExpandWidth = true;

        ContentSizeFitter fitter = content.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        scroll.content = contentRect;

        // 创建消防器分类
        GameObject extinguisherSection = CreateSection(content.transform, "Fire Extinguishers");
        GameObject extinguisherContainer = CreateContainer(extinguisherSection.transform);

        // 创建烟雾报警器分类
        GameObject detectorSection = CreateSection(content.transform, "Smoke Detectors");
        GameObject detectorContainer = CreateContainer(detectorSection.transform);

        // 设置引用
        listPanel.extinguisherContainer = extinguisherContainer.transform;
        listPanel.detectorContainer = detectorContainer.transform;

        Debug.Log("✓ 设备列表面板创建成功！位于左下角。");
        Selection.activeGameObject = panelRoot;
    }

    static GameObject CreateSection(Transform parent, string sectionName)
    {
        // 转换为中文
        string chineseName = sectionName;
        if (sectionName == "Fire Extinguishers")
            chineseName = "消防器";
        else if (sectionName == "Smoke Detectors")
            chineseName = "烟雾报警器";
        
        GameObject section = new GameObject(sectionName + "Section");
        section.transform.SetParent(parent, false);
        
        RectTransform rect = section.AddComponent<RectTransform>();
        rect.sizeDelta = new Vector2(0, 0);
        
        VerticalLayoutGroup layout = section.AddComponent<VerticalLayoutGroup>();
        layout.spacing = 3;
        layout.padding = new RectOffset(0, 0, 5, 5);
        layout.childControlHeight = false;
        layout.childControlWidth = true;
        layout.childForceExpandHeight = false;
        layout.childForceExpandWidth = true;
        
        ContentSizeFitter fitter = section.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        // 创建分类标题
        GameObject header = new GameObject("Header");
        header.transform.SetParent(section.transform, false);
        
        RectTransform headerRect = header.AddComponent<RectTransform>();
        headerRect.sizeDelta = new Vector2(0, 30);
        
        Image headerBg = header.AddComponent<Image>();
        headerBg.color = new Color(0.15f, 0.15f, 0.15f, 1f);
        headerBg.raycastTarget = false;  // 分类标题背景不需要接收点击,禁用以允许滚轮穿透
        
        GameObject headerText = new GameObject("Text");
        headerText.transform.SetParent(header.transform, false);
        
        RectTransform headerTextRect = headerText.AddComponent<RectTransform>();
        headerTextRect.anchorMin = Vector2.zero;
        headerTextRect.anchorMax = Vector2.one;
        headerTextRect.sizeDelta = Vector2.zero;
        headerTextRect.offsetMin = new Vector2(10, 0);
        
        TextMeshProUGUI text = headerText.AddComponent<TextMeshProUGUI>();
        text.text = chineseName;  // 使用中文名称
        text.fontSize = 16;
        text.fontStyle = FontStyles.Bold;
        text.color = new Color(0.8f, 0.8f, 0.8f);
        text.alignment = TextAlignmentOptions.Left;
        text.alignment = TextAlignmentOptions.Midline;

        return section;
    }

    static GameObject CreateContainer(Transform parent)
    {
        GameObject container = new GameObject("Container");
        container.transform.SetParent(parent, false);
        
        RectTransform rect = container.AddComponent<RectTransform>();
        rect.sizeDelta = new Vector2(0, 0);
        
        VerticalLayoutGroup layout = container.AddComponent<VerticalLayoutGroup>();
        layout.spacing = 2;
        layout.childControlHeight = false;
        layout.childControlWidth = true;
        layout.childForceExpandHeight = false;
        layout.childForceExpandWidth = true;
        
        ContentSizeFitter fitter = container.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        return container;
    }
}
