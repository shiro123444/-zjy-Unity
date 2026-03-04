using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;

/// <summary>
/// Unity Scene视图风格的镜头控制器
/// 右键拖动：旋转视角
/// 中键拖动：平移镜头
/// 滚轮：缩放（前后移动）
/// A键：回正镜头
/// </summary>
public class SceneViewCameraController : MonoBehaviour
{
    [Header("旋转设置")]
    [Tooltip("鼠标旋转速度")]
    public float rotationSpeed = 3f;
    
    [Tooltip("垂直旋转角度限制（最小值）")]
    public float minVerticalAngle = -89f;
    
    [Tooltip("垂直旋转角度限制（最大值）")]
    public float maxVerticalAngle = 89f;

    [Header("平移设置")]
    [Tooltip("鼠标平移速度")]
    public float panSpeed = 0.5f;

    [Header("缩放设置")]
    [Tooltip("滚轮移动速度")]
    public float zoomSpeed = 10f;
    
    [Tooltip("最小移动速度（距离越近移动越慢）")]
    public float minMoveSpeed = 1f;
    
    [Tooltip("最大移动速度")]
    public float maxMoveSpeed = 50f;

    [Header("平滑设置")]
    [Tooltip("启用平滑移动")]
    public bool enableSmoothing = true;
    
    [Tooltip("平滑系数（越大越平滑）")]
    public float smoothFactor = 5f;

    [Header("回正设置")]
    [Tooltip("回正动画持续时间")]
    public float resetDuration = 1.5f;
    
    [Tooltip("进入场景时自动播放回正动画")]
    public bool playResetOnStart = true;
    
    [Tooltip("回正动画的起始位置偏移")]
    public Vector3 startPositionOffset = new Vector3(0, 5, -10);
    
    [Tooltip("回正动画的起始角度偏移")]
    public Vector3 startRotationOffset = new Vector3(20, 0, 0);

    private float currentRotationX = 0f;
    private float currentRotationY = 0f;
    private Vector3 targetPosition;
    private Quaternion targetRotation;
    
    // 初始状态（用于回正）
    private Vector3 initialPosition;
    private Quaternion initialRotation;
    private float initialRotationX;
    private float initialRotationY;
    
    // 回正动画状态
    private bool isResetting = false;
    private Coroutine resetCoroutine;

    void Start()
    {
        // 保存初始位置和旋转
        initialPosition = transform.position;
        initialRotation = transform.rotation;
        
        Vector3 angles = transform.eulerAngles;
        initialRotationX = angles.y;
        initialRotationY = angles.x;
        
        currentRotationX = initialRotationX;
        currentRotationY = initialRotationY;

        // 初始化目标位置和旋转
        targetPosition = transform.position;
        targetRotation = transform.rotation;

        // 如果启用了进入场景时的回正动画
        if (playResetOnStart)
        {
            PlayStartAnimation();
        }
    }

    void Update()
    {
        // 如果正在播放回正动画，不响应用户输入
        if (isResetting)
        {
            return;
        }

        HandleResetInput();
        HandleRotation();
        HandlePan();
        HandleZoom();
        ApplyTransform();
    }

    /// <summary>
    /// 处理回正输入（A键）
    /// </summary>
    void HandleResetInput()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null) return;

        if (keyboard.aKey.wasPressedThisFrame)
        {
            ResetCameraAnimated();
        }
    }

    /// <summary>
    /// 处理旋转（右键拖动）
    /// </summary>
    void HandleRotation()
    {
        Mouse mouse = Mouse.current;
        if (mouse == null) return;

        // 右键拖动旋转
        if (mouse.rightButton.isPressed)
        {
            Vector2 mouseDelta = mouse.delta.ReadValue();
            float mouseX = mouseDelta.x * rotationSpeed * Time.deltaTime;
            float mouseY = mouseDelta.y * rotationSpeed * Time.deltaTime;

            currentRotationX += mouseX;
            currentRotationY -= mouseY;

            // 限制垂直旋转角度
            currentRotationY = Mathf.Clamp(currentRotationY, minVerticalAngle, maxVerticalAngle);

            // 更新目标旋转
            targetRotation = Quaternion.Euler(currentRotationY, currentRotationX, 0);
        }
    }

    /// <summary>
    /// 处理平移（中键拖动）
    /// </summary>
    void HandlePan()
    {
        Mouse mouse = Mouse.current;
        if (mouse == null) return;

        // 中键拖动平移
        if (mouse.middleButton.isPressed)
        {
            Vector2 mouseDelta = mouse.delta.ReadValue();
            
            // 根据鼠标移动计算平移方向
            Vector3 moveRight = transform.right * (-mouseDelta.x * panSpeed * Time.deltaTime);
            Vector3 moveUp = transform.up * (-mouseDelta.y * panSpeed * Time.deltaTime);
            
            targetPosition += moveRight + moveUp;
        }
    }

    /// <summary>
    /// 处理缩放（滚轮前后移动）
    /// </summary>
    void HandleZoom()
    {
        Mouse mouse = Mouse.current;
        if (mouse == null) return;

        Vector2 scrollDelta = mouse.scroll.ReadValue();
        float scroll = scrollDelta.y / 120f; // 标准化滚轮值
        
        if (scroll != 0f)
        {
            // 根据当前速度动态调整移动距离
            float currentSpeed = Mathf.Lerp(minMoveSpeed, maxMoveSpeed, Mathf.Abs(scroll));
            Vector3 moveForward = transform.forward * scroll * currentSpeed * Time.deltaTime * 100f;
            
            targetPosition += moveForward;
        }
    }

    /// <summary>
    /// 应用变换（支持平滑）
    /// </summary>
    void ApplyTransform()
    {
        if (enableSmoothing)
        {
            // 平滑插值
            transform.position = Vector3.Lerp(transform.position, targetPosition, Time.deltaTime * smoothFactor);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * smoothFactor);
        }
        else
        {
            // 直接应用
            transform.position = targetPosition;
            transform.rotation = targetRotation;
        }
    }

    /// <summary>
    /// 播放进入场景时的回正动画
    /// </summary>
    void PlayStartAnimation()
    {
        // 设置起始位置（偏移）
        Vector3 startPos = initialPosition + startPositionOffset;
        Vector3 startRot = initialRotation.eulerAngles + startRotationOffset;
        
        transform.position = startPos;
        transform.rotation = Quaternion.Euler(startRot);
        targetPosition = startPos;
        targetRotation = Quaternion.Euler(startRot);
        
        // 播放回正动画
        ResetCameraAnimated();
    }

    /// <summary>
    /// 带动画的镜头回正
    /// </summary>
    public void ResetCameraAnimated()
    {
        // 如果已经在播放动画，先停止
        if (resetCoroutine != null)
        {
            StopCoroutine(resetCoroutine);
        }
        
        resetCoroutine = StartCoroutine(ResetCameraCoroutine());
    }

    /// <summary>
    /// 回正动画协程
    /// </summary>
    IEnumerator ResetCameraCoroutine()
    {
        isResetting = true;
        
        Vector3 startPos = transform.position;
        Quaternion startRot = transform.rotation;
        
        float elapsed = 0f;
        
        while (elapsed < resetDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / resetDuration;
            
            // 使用平滑曲线
            float smoothT = Mathf.SmoothStep(0f, 1f, t);
            
            // 插值位置和旋转
            transform.position = Vector3.Lerp(startPos, initialPosition, smoothT);
            transform.rotation = Quaternion.Slerp(startRot, initialRotation, smoothT);
            
            yield return null;
        }
        
        // 确保最终值准确
        transform.position = initialPosition;
        transform.rotation = initialRotation;
        
        // 更新目标值和当前旋转值
        targetPosition = initialPosition;
        targetRotation = initialRotation;
        currentRotationX = initialRotationX;
        currentRotationY = initialRotationY;
        
        isResetting = false;
        resetCoroutine = null;
        
        Debug.Log("镜头已回正");
    }

    /// <summary>
    /// 立即重置镜头（无动画）
    /// </summary>
    public void ResetCameraImmediate()
    {
        // 停止动画
        if (resetCoroutine != null)
        {
            StopCoroutine(resetCoroutine);
            resetCoroutine = null;
        }
        
        transform.position = initialPosition;
        transform.rotation = initialRotation;
        targetPosition = initialPosition;
        targetRotation = initialRotation;
        currentRotationX = initialRotationX;
        currentRotationY = initialRotationY;
        isResetting = false;
    }

    /// <summary>
    /// 设置新的初始状态
    /// </summary>
    public void SetInitialState(Vector3 position, Vector3 eulerAngles)
    {
        initialPosition = position;
        initialRotation = Quaternion.Euler(eulerAngles);
        initialRotationX = eulerAngles.y;
        initialRotationY = eulerAngles.x;
    }

    /// <summary>
    /// 聚焦到指定位置
    /// </summary>
    public void FocusOn(Vector3 focusPoint, float distance = 10f)
    {
        Vector3 direction = (transform.position - focusPoint).normalized;
        targetPosition = focusPoint + direction * distance;
    }
}
