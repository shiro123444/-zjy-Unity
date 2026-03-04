using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 自由相机控制器 - 支持键盘和鼠标控制
/// WASD/方向键：移动
/// 鼠标右键+移动：旋转视角
/// 滚轮：调整移动速度
/// Shift：加速移动
/// </summary>
public class CameraController : MonoBehaviour
{
    [Header("移动设置")]
    [Tooltip("基础移动速度")]
    public float moveSpeed = 10f;
    
    [Tooltip("加速倍数（按住Shift）")]
    public float speedMultiplier = 3f;
    
    [Tooltip("速度调整灵敏度（滚轮）")]
    public float speedAdjustSensitivity = 1f;

    [Header("旋转设置")]
    [Tooltip("鼠标旋转灵敏度")]
    public float mouseSensitivity = 3f;
    
    [Tooltip("最大俯仰角度")]
    public float maxPitchAngle = 90f;

    [Header("平滑设置")]
    [Tooltip("移动平滑度（0-1，越大越平滑）")]
    [Range(0f, 0.99f)]
    public float moveSmoothness = 0.5f;

    private float rotationX = 0f;
    private Vector3 currentVelocity = Vector3.zero;
    private bool isRotating = false;

    void Start()
    {
        // 初始化旋转角度
        Vector3 rot = transform.localRotation.eulerAngles;
        rotationX = rot.x;
    }

    void Update()
    {
        HandleMovement();
        HandleRotation();
        HandleSpeedAdjustment();
    }

    /// <summary>
    /// 处理相机移动
    /// </summary>
    void HandleMovement()
    {
        // 使用新的 Input System
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null) return;

        // 获取输入
        float horizontal = 0f;
        float vertical = 0f;
        float upDown = 0f;

        // WASD 或 方向键
        if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed)
            vertical = 1f;
        if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed)
            vertical = -1f;
        if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed)
            horizontal = -1f;
        if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed)
            horizontal = 1f;

        // E/Q 控制上下移动
        if (keyboard.eKey.isPressed)
            upDown = 1f;
        if (keyboard.qKey.isPressed)
            upDown = -1f;

        // 计算移动方向（相对于相机朝向）
        Vector3 moveDirection = transform.right * horizontal + 
                               transform.forward * vertical + 
                               Vector3.up * upDown;

        // 应用速度
        float currentSpeed = moveSpeed;
        if (keyboard.leftShiftKey.isPressed || keyboard.rightShiftKey.isPressed)
            currentSpeed *= speedMultiplier;

        Vector3 targetVelocity = moveDirection.normalized * currentSpeed;

        // 平滑移动
        currentVelocity = Vector3.Lerp(currentVelocity, targetVelocity, 1f - moveSmoothness);
        transform.position += currentVelocity * Time.deltaTime;
    }

    /// <summary>
    /// 处理相机旋转
    /// </summary>
    void HandleRotation()
    {
        Mouse mouse = Mouse.current;
        if (mouse == null) return;

        // 鼠标右键控制旋转
        if (mouse.rightButton.wasPressedThisFrame)
        {
            isRotating = true;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        if (mouse.rightButton.wasReleasedThisFrame)
        {
            isRotating = false;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        if (isRotating)
        {
            // 获取鼠标移动
            Vector2 mouseDelta = mouse.delta.ReadValue();
            float mouseX = mouseDelta.x * mouseSensitivity * 0.1f;
            float mouseY = mouseDelta.y * mouseSensitivity * 0.1f;

            // 水平旋转（Y轴）
            transform.Rotate(Vector3.up * mouseX, Space.World);

            // 垂直旋转（X轴）- 限制角度
            rotationX -= mouseY;
            rotationX = Mathf.Clamp(rotationX, -maxPitchAngle, maxPitchAngle);
            
            Vector3 currentRotation = transform.localRotation.eulerAngles;
            currentRotation.x = rotationX;
            transform.localRotation = Quaternion.Euler(currentRotation);
        }
    }

    /// <summary>
    /// 处理速度调整（鼠标滚轮）
    /// </summary>
    void HandleSpeedAdjustment()
    {
        Mouse mouse = Mouse.current;
        if (mouse == null) return;

        Vector2 scrollDelta = mouse.scroll.ReadValue();
        float scroll = scrollDelta.y;
        
        if (scroll != 0f)
        {
            moveSpeed += scroll * speedAdjustSensitivity * 0.1f;
            moveSpeed = Mathf.Max(1f, moveSpeed); // 最小速度为1
            Debug.Log($"移动速度: {moveSpeed:F1}");
        }
    }

    /// <summary>
    /// 在编辑器中显示控制提示
    /// </summary>
    void OnGUI()
    {
        if (!Application.isPlaying) return;

        GUIStyle style = new GUIStyle();
        style.fontSize = 14;
        style.normal.textColor = Color.white;
        style.padding = new RectOffset(10, 10, 10, 10);

        string controls = 
            "相机控制:\n" +
            "WASD/方向键 - 移动\n" +
            "Q/E - 下降/上升\n" +
            "鼠标右键 - 旋转视角\n" +
            "Shift - 加速\n" +
            "滚轮 - 调整速度\n" +
            $"当前速度: {moveSpeed:F1}";

        GUI.Label(new Rect(10, 10, 300, 150), controls, style);
    }
}
