using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 简化版镜头控制器 - 直接控制摄像机位置和旋转
/// </summary>
public class SimpleCameraController : MonoBehaviour
{
    [Header("移动设置")]
    public float panSpeed = 0.5f;
    
    [Header("缩放设置")]
    public float zoomSpeed = 5f;
    public float minZoom = 5f;
    public float maxZoom = 50f;

    private Vector3 lastMousePosition;
    private Camera cam;

    void Start()
    {
        cam = GetComponent<Camera>();
    }

    void Update()
    {
        Mouse mouse = Mouse.current;
        if (mouse == null) return;

        // 鼠标中键或右键拖动平移
        if (mouse.rightButton.wasPressedThisFrame || mouse.middleButton.wasPressedThisFrame)
        {
            lastMousePosition = mouse.position.ReadValue();
        }

        if (mouse.rightButton.isPressed || mouse.middleButton.isPressed)
        {
            Vector2 currentMousePosition = mouse.position.ReadValue();
            Vector3 delta = currentMousePosition - (Vector2)lastMousePosition;
            
            // 根据鼠标移动调整摄像机位置
            Vector3 move = new Vector3(-delta.x * panSpeed * Time.deltaTime, 
                                       -delta.y * panSpeed * Time.deltaTime, 
                                       0);
            transform.Translate(move, Space.Self);
            
            lastMousePosition = currentMousePosition;
        }

        // 滚轮缩放
        Vector2 scrollDelta = mouse.scroll.ReadValue();
        float scroll = scrollDelta.y / 120f; // 标准化滚轮值
        
        if (scroll != 0f)
        {
            // 正交相机缩放
            if (cam.orthographic)
            {
                cam.orthographicSize -= scroll * zoomSpeed;
                cam.orthographicSize = Mathf.Clamp(cam.orthographicSize, minZoom, maxZoom);
            }
            // 透视相机缩放
            else
            {
                transform.Translate(Vector3.forward * scroll * zoomSpeed, Space.Self);
            }
        }
    }
}
