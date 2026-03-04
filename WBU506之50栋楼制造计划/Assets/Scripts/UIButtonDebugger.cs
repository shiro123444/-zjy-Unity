using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/// <summary>
/// UI按钮调试器 - 帮助诊断按钮无法点击的问题
/// </summary>
public class UIButtonDebugger : MonoBehaviour
{
    void Start()
    {
        Debug.Log("=== UI按钮诊断开始 ===");
        
        // 检查EventSystem
        CheckEventSystem();
        
        // 检查Canvas
        CheckCanvas();
        
        // 检查Button
        CheckButton();
        
        // 检查Raycaster
        CheckRaycaster();
        
        Debug.Log("=== UI按钮诊断结束 ===");
    }

    void CheckEventSystem()
    {
        EventSystem eventSystem = FindObjectOfType<EventSystem>();
        if (eventSystem == null)
        {
            Debug.LogError("❌ 场景中没有EventSystem！请添加：GameObject → UI → Event System");
        }
        else
        {
            Debug.Log($"✓ EventSystem存在: {eventSystem.gameObject.name}");
            if (!eventSystem.enabled)
            {
                Debug.LogWarning("⚠ EventSystem被禁用了！");
            }
        }
    }

    void CheckCanvas()
    {
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas == null)
        {
            Debug.LogError("❌ Button不在Canvas下！");
        }
        else
        {
            Debug.Log($"✓ Canvas存在: {canvas.gameObject.name}");
            Debug.Log($"  - Render Mode: {canvas.renderMode}");
            Debug.Log($"  - Sort Order: {canvas.sortingOrder}");
            
            if (!canvas.enabled)
            {
                Debug.LogWarning("⚠ Canvas被禁用了！");
            }
        }
    }

    void CheckButton()
    {
        Button button = GetComponent<Button>();
        if (button == null)
        {
            Debug.LogError("❌ 没有Button组件！");
        }
        else
        {
            Debug.Log($"✓ Button组件存在");
            Debug.Log($"  - Interactable: {button.interactable}");
            Debug.Log($"  - OnClick事件数量: {button.onClick.GetPersistentEventCount()}");
            
            if (!button.interactable)
            {
                Debug.LogWarning("⚠ Button的Interactable被设置为false！");
            }
            
            if (!button.enabled)
            {
                Debug.LogWarning("⚠ Button组件被禁用了！");
            }
        }
        
        // 检查Image组件
        Image image = GetComponent<Image>();
        if (image != null)
        {
            Debug.Log($"✓ Image组件存在");
            Debug.Log($"  - Raycast Target: {image.raycastTarget}");
            
            if (!image.raycastTarget)
            {
                Debug.LogWarning("⚠ Image的Raycast Target被禁用！按钮无法接收点击。");
            }
        }
    }

    void CheckRaycaster()
    {
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas != null)
        {
            GraphicRaycaster raycaster = canvas.GetComponent<GraphicRaycaster>();
            if (raycaster == null)
            {
                Debug.LogError("❌ Canvas上没有GraphicRaycaster！");
            }
            else
            {
                Debug.Log($"✓ GraphicRaycaster存在");
                if (!raycaster.enabled)
                {
                    Debug.LogWarning("⚠ GraphicRaycaster被禁用了！");
                }
            }
        }
    }

    void Update()
    {
        // 检测鼠标点击
        if (Input.GetMouseButtonDown(0))
        {
            Debug.Log($"鼠标点击位置: {Input.mousePosition}");
            
            // 检查是否点击到UI
            if (EventSystem.current != null)
            {
                PointerEventData pointerData = new PointerEventData(EventSystem.current);
                pointerData.position = Input.mousePosition;
                
                var results = new System.Collections.Generic.List<RaycastResult>();
                EventSystem.current.RaycastAll(pointerData, results);
                
                if (results.Count > 0)
                {
                    Debug.Log($"点击到了 {results.Count} 个UI元素:");
                    foreach (var result in results)
                    {
                        Debug.Log($"  - {result.gameObject.name}");
                    }
                }
                else
                {
                    Debug.Log("没有点击到任何UI元素");
                }
            }
        }
    }
}
