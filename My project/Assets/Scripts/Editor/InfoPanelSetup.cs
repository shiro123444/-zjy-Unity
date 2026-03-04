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
            canvasObj.AddComponent<CanvasScaler>();
            canvasObj.AddComponent<GraphicRaycaster>();
        }

        // 创建信息面板
        GameObject panelRoot = new GameObject("InfoPanelManager");
        panelRoot.transform.SetParent(canvas.transform, false);

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
        // 位置：距离右边20，距离上边20
        panelRect.anchoredPosition = new Vector2(-20, -20);
        panelRect.sizeDelta = new Vector2(350, 250);

        Image panelImage = panel.AddComponent<Image>();
        panelImage.color = new Color(0.1f, 0.1f, 0.1f, 0.95f);

        // 创建标题
        GameObject title = new GameObject("Title");
        title.transform.SetParent(panel.transform, false);
        
        RectTransform titleRect = title.AddComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0, 1);
        titleRect.anchorMax = new Vector2(1, 1);
        titleRect.pivot = new Vector2(0.5f, 1);
        titleRect.anchoredPosition = new Vector2(0, -10);
        titleRect.sizeDelta = new Vector2(-60, 35);

        TextMeshProUGUI titleText = title.AddComponent<TextMeshProUGUI>();
        titleText.text = "物体信息";
        titleText.fontSize = 20;
        titleText.fontStyle = FontStyles.Bold;
        titleText.alignment = TextAlignmentOptions.Left;
        titleText.color = Color.white;
        // 使用 Arial 字体（支持基本拉丁字符）
        titleText.font = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");

        // 创建详细信息文本
        GameObject detail = new GameObject("DetailText");
        detail.transform.SetParent(panel.transform, false);
        
        RectTransform detailRect = detail.AddComponent<RectTransform>();
        detailRect.anchorMin = new Vector2(0, 0);
        detailRect.anchorMax = new Vector2(1, 1);
        detailRect.pivot = new Vector2(0, 1);
        detailRect.anchoredPosition = new Vector2(10, -55);
        detailRect.sizeDelta = new Vector2(-20, -65);

        TextMeshProUGUI detailText = detail.AddComponent<TextMeshProUGUI>();
        detailText.text = "Click object to view details";
        detailText.fontSize = 14;
        detailText.alignment = TextAlignmentOptions.TopLeft;
        detailText.color = Color.white;
        detailText.font = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");

        // 创建关闭按钮
        GameObject closeBtn = new GameObject("CloseButton");
        closeBtn.transform.SetParent(panel.transform, false);
        
        RectTransform closeBtnRect = closeBtn.AddComponent<RectTransform>();
        closeBtnRect.anchorMin = new Vector2(1, 1);
        closeBtnRect.anchorMax = new Vector2(1, 1);
        closeBtnRect.pivot = new Vector2(1, 1);
        closeBtnRect.anchoredPosition = new Vector2(-5, -5);
        closeBtnRect.sizeDelta = new Vector2(35, 35);

        Button closeButton = closeBtn.AddComponent<Button>();
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
        closeBtnTMP.fontSize = 20;
        closeBtnTMP.fontStyle = FontStyles.Bold;
        closeBtnTMP.alignment = TextAlignmentOptions.Center;
        closeBtnTMP.color = Color.white;

        // 设置引用
        infoPanel.panelRoot = panel;
        infoPanel.titleText = titleText;
        infoPanel.detailText = detailText;
        infoPanel.closeButton = closeButton;

        // 添加 CanvasGroup
        CanvasGroup canvasGroup = panel.AddComponent<CanvasGroup>();

        // 初始隐藏
        panel.SetActive(false);

        Debug.Log("信息面板创建成功！位于右上角。请使用英文输入信息，或导入中文字体。");
        Selection.activeGameObject = panelRoot;
    }
}
