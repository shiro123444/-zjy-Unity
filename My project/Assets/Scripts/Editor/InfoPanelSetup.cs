using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 信息面板快速设置工具
/// </summary>
public class InfoPanelSetup : EditorWindow
{
    [MenuItem("Tools/创建信息面板 UI")]
    static void CreateInfoPanel()
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
            
            Debug.Log("创建了新的 Canvas");
        }

        // 创建信息面板根对象
        GameObject panelRoot = new GameObject("InfoPanelManager");
        panelRoot.transform.SetParent(canvas.transform, false);
        
        RectTransform rootRect = panelRoot.AddComponent<RectTransform>();
        rootRect.anchorMin = Vector2.zero;
        rootRect.anchorMax = Vector2.one;
        rootRect.sizeDelta = Vector2.zero;

        // 添加 InfoPanel 脚本
        InfoPanel infoPanel = panelRoot.AddComponent<InfoPanel>();

        // 创建面板背景
        GameObject panel = new GameObject("Panel");
        panel.transform.SetParent(panelRoot.transform, false);
        
        RectTransform panelRect = panel.AddComponent<RectTransform>();
        // 锚点设置为右上角
        panelRect.anchorMin = new Vector2(1, 1);
        panelRect.anchorMax = new Vector2(1, 1);
        panelRect.pivot = new Vector2(1, 1);
        // 位置：从右上角偏移
        panelRect.anchoredPosition = new Vector2(-30, -30);
        panelRect.sizeDelta = new Vector2(380, 280);

        Image panelImage = panel.AddComponent<Image>();
        panelImage.color = new Color(0.05f, 0.05f, 0.05f, 0.95f);
        
        // 添加 CanvasGroup
        CanvasGroup canvasGroup = panel.AddComponent<CanvasGroup>();

        // 创建标题
        GameObject title = new GameObject("Title");
        title.transform.SetParent(panel.transform, false);
        
        RectTransform titleRect = title.AddComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0, 1);
        titleRect.anchorMax = new Vector2(1, 1);
        titleRect.pivot = new Vector2(0, 1);
        titleRect.anchoredPosition = new Vector2(15, -15);
        titleRect.sizeDelta = new Vector2(-70, 40);

        TextMeshProUGUI titleText = title.AddComponent<TextMeshProUGUI>();
        titleText.text = "Object Info";
        titleText.fontSize = 22;
        titleText.fontStyle = FontStyles.Bold;
        titleText.alignment = TextAlignmentOptions.Left;
        titleText.color = new Color(1f, 0.9f, 0.3f);

        // 创建详细信息文本
        GameObject detail = new GameObject("DetailText");
        detail.transform.SetParent(panel.transform, false);
        
        RectTransform detailRect = detail.AddComponent<RectTransform>();
        detailRect.anchorMin = new Vector2(0, 0);
        detailRect.anchorMax = new Vector2(1, 1);
        detailRect.pivot = new Vector2(0, 1);
        detailRect.anchoredPosition = new Vector2(15, -65);
        detailRect.sizeDelta = new Vector2(-30, -80);

        TextMeshProUGUI detailText = detail.AddComponent<TextMeshProUGUI>();
        detailText.text = "点击物体查看详细信息";
        detailText.fontSize = 16;
        detailText.alignment = TextAlignmentOptions.TopLeft;
        detailText.color = Color.white;
        detailText.enableWordWrapping = true;

        // 创建关闭按钮
        GameObject closeBtn = new GameObject("CloseButton");
        closeBtn.transform.SetParent(panel.transform, false);
        
        RectTransform closeBtnRect = closeBtn.AddComponent<RectTransform>();
        closeBtnRect.anchorMin = new Vector2(1, 1);
        closeBtnRect.anchorMax = new Vector2(1, 1);
        closeBtnRect.pivot = new Vector2(1, 1);
        closeBtnRect.anchoredPosition = new Vector2(-10, -10);
        closeBtnRect.sizeDelta = new Vector2(40, 40);

        Button closeButton = closeBtn.AddComponent<Button>();
        
        // 设置按钮颜色
        ColorBlock colors = closeButton.colors;
        colors.normalColor = new Color(0.8f, 0.2f, 0.2f, 1f);
        colors.highlightedColor = new Color(1f, 0.3f, 0.3f, 1f);
        colors.pressedColor = new Color(0.6f, 0.1f, 0.1f, 1f);
        closeButton.colors = colors;
        
        Image closeBtnImage = closeBtn.AddComponent<Image>();
        closeBtnImage.color = new Color(0.8f, 0.2f, 0.2f, 1f);

        // 关闭按钮文本
        GameObject closeBtnText = new GameObject("Text");
        closeBtnText.transform.SetParent(closeBtn.transform, false);
        
        RectTransform closeBtnTextRect = closeBtnText.AddComponent<RectTransform>();
        closeBtnTextRect.anchorMin = Vector2.zero;
        closeBtnTextRect.anchorMax = Vector2.one;
        closeBtnTextRect.sizeDelta = Vector2.zero;

        TextMeshProUGUI closeBtnTMP = closeBtnText.AddComponent<TextMeshProUGUI>();
        closeBtnTMP.text = "X";
        closeBtnTMP.fontSize = 24;
        closeBtnTMP.fontStyle = FontStyles.Bold;
        closeBtnTMP.alignment = TextAlignmentOptions.Center;
        closeBtnTMP.color = Color.white;

        // 设置引用
        infoPanel.panelRoot = panel;
        infoPanel.titleText = titleText;
        infoPanel.detailText = detailText;
        infoPanel.closeButton = closeButton;

        // 初始隐藏
        panel.SetActive(false);

        Debug.Log("✓ 信息面板创建成功！位于右上角。");
        Debug.Log("✓ 关闭按钮已配置。");
        Debug.Log("提示：请使用英文输入信息，或导入中文字体到 TextMesh Pro。");
        
        Selection.activeGameObject = panelRoot;
    }
}
