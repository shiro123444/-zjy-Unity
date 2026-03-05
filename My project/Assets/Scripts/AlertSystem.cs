using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 设备警报系统 - 管理设备警报的触发、显示和摄像头控制
/// Equipment Alert System - Manages equipment alert triggering, display, and camera control
/// </summary>
public class AlertSystem : MonoBehaviour
{
    #region Core Data Structures and Enums

    /// <summary>
    /// 警报状态枚举 - 定义警报序列的各个阶段
    /// Alert state enum - Defines the stages of the alert sequence
    /// </summary>
    public enum AlertState
    {
        Idle,                    // 空闲状态，等待触发 / Idle state, waiting for trigger
        MovingToFloorView,       // 移动到楼层视角 / Moving to floor view
        WaitingAtFloorView,      // 在楼层视角等待 / Waiting at floor view
        MovingToEquipmentView,   // 移动到设备视角 / Moving to equipment view
        FocusedOnEquipment       // 聚焦在设备上 / Focused on equipment
    }

    /// <summary>
    /// 摄像头视角结构 - 存储摄像头的位置和旋转
    /// Camera view structure - Stores camera position and rotation
    /// </summary>
    [Serializable]
    public struct CameraView
    {
        public Vector3 position;    // 摄像头位置 / Camera position
        public Quaternion rotation; // 摄像头旋转 / Camera rotation

        public CameraView(Vector3 pos, Quaternion rot)
        {
            position = pos;
            rotation = rot;
        }
    }

    /// <summary>
    /// 设备信息类 - 存储设备的状态和所属楼层信息
    /// Equipment info class - Stores equipment state and floor information
    /// </summary>
    private class EquipmentInfo
    {
        public GameObject gameObject;  // 设备GameObject / Equipment GameObject
        public string floorName;       // 所属楼层名称 / Parent floor name
        public bool isAlerting;        // 是否正在报警 / Whether alerting

        public EquipmentInfo(GameObject obj, string floor)
        {
            gameObject = obj;
            floorName = floor;
            isAlerting = false;
        }
    }

    /// <summary>
    /// 楼层预设视角类 - 可序列化的楼层视角配置
    /// Floor preset view class - Serializable floor view configuration
    /// </summary>
    [Serializable]
    public class FloorPresetView
    {
        public string floorName;    // 楼层名称 / Floor name
        public Vector3 position;    // 摄像头位置 / Camera position
        public Vector3 rotation;    // 摄像头旋转（欧拉角）/ Camera rotation (Euler angles)

        public FloorPresetView()
        {
            floorName = "";
            position = Vector3.zero;
            rotation = Vector3.zero;
        }

        public FloorPresetView(string name, Vector3 pos, Vector3 rot)
        {
            floorName = name;
            position = pos;
            rotation = rot;
        }
    }

    #endregion

    #region Public Configuration Fields

    [Header("Alert Timing")]
    [Tooltip("最小警报间隔（秒）/ Minimum alert interval (seconds)")]
    [Range(5f, 300f)]
    public float minAlertInterval = 10f;

    [Tooltip("最大警报间隔（秒）/ Maximum alert interval (seconds)")]
    [Range(5f, 300f)]
    public float maxAlertInterval = 60f;

    [Tooltip("楼层视角停留时间（秒）/ Focus delay duration at floor view (seconds)")]
    [Range(1f, 10f)]
    public float focusDelayDuration = 2f;

    [Header("Camera Transition")]
    [Tooltip("摄像头过渡时间（秒）/ Camera transition time (seconds)")]
    public float cameraTransitionTime = 1f;

    [Tooltip("设备聚焦距离 / Equipment focus distance")]
    public float equipmentFocusDistance = 5f;

    [Header("Ripple Effect")]
    [Tooltip("警报波纹颜色 / Alert ripple color")]
    public Color alertRippleColor = Color.red;

    [Tooltip("波纹大小 / Ripple size")]
    public float rippleSize = 2f;

    [Tooltip("波纹透明度 / Ripple alpha")]
    [Range(0f, 1f)]
    public float rippleAlpha = 0.6f;

    [Header("Equipment Identification")]
    [Tooltip("设备标签 / Equipment tag")]
    public string equipmentTag = "Equipment";

    [Header("Floor Preset Views (Optional)")]
    [Tooltip("楼层预设视角列表（可选）/ Floor preset view list (optional)")]
    public List<FloorPresetView> floorPresetViews = new List<FloorPresetView>();

    #endregion

    #region Private State Fields

    // State Machine
    private AlertState currentState = AlertState.Idle;

    // Current Alert
    private GameObject currentAlertEquipment = null;
    private GameObject currentRippleObject = null;
    private string currentAlertFloor = null;

    // Equipment Registry
    private Dictionary<string, List<EquipmentInfo>> equipmentByFloor;
    private List<EquipmentInfo> allEquipment;

    // Component References
    private FloorController floorController;
    private MonoBehaviour[] cameraControllers; // 支持多种摄像头控制器
    private Camera mainCamera;

    // Timer State
    private float alertTimer = 0f;
    private float nextAlertTime = 0f;
    private float focusDelayTimer = 0f;
    private bool isSystemActive = false;

    // Camera State
    private CameraView savedCameraView;
    private Coroutine currentCameraTransition = null;
    private bool isCameraTransitioning = false;

    // Error Handling
    private float stateTimeoutDuration = 30f; // 状态超时时间（秒）
    private float currentStateTimer = 0f;     // 当前状态计时器

    #endregion

    #region Public Query Methods

    /// <summary>
    /// 获取当前警报状态
    /// Get the current alert state
    /// Validates: Requirements 10.6
    /// </summary>
    /// <returns>当前状态 / Current state</returns>
    public AlertState GetCurrentState()
    {
        return currentState;
    }

    /// <summary>
    /// 获取当前报警设备
    /// Get the current alerting equipment
    /// Validates: Requirements 10.6
    /// </summary>
    /// <returns>当前报警设备GameObject，如果没有活动警报则返回null / Current alerting equipment GameObject, or null if no active alert</returns>
    public GameObject GetCurrentAlertEquipment()
    {
        return currentAlertEquipment;
    }

    #endregion

    #region Public Control Methods

    /// <summary>
    /// 启动警报系统
    /// Start the alert system
    /// Validates: Requirements 7.6
    /// </summary>
    public void StartAlertSystem()
    {
        if (isSystemActive)
        {
            Debug.LogWarning("AlertSystem: 系统已经在运行中");
            return;
        }

        Debug.Log("AlertSystem: 启动警报系统");

        // 设置isSystemActive为true
        isSystemActive = true;

        // 初始化计时器
        alertTimer = 0f;
        
        // 使用Random.Range在minAlertInterval和maxAlertInterval之间生成第一个间隔
        nextAlertTime = UnityEngine.Random.Range(minAlertInterval, maxAlertInterval);
        
        Debug.Log($"AlertSystem: 第一次警报将在 {nextAlertTime:F2}秒后触发");
    }

    /// <summary>
    /// 停止警报系统
    /// Stop the alert system
    /// Validates: Requirements 7.6
    /// </summary>
    public void StopAlertSystem()
    {
        if (!isSystemActive)
        {
            Debug.LogWarning("AlertSystem: 系统已经停止");
            return;
        }

        Debug.Log("AlertSystem: 停止警报系统");

        // 设置isSystemActive为false
        isSystemActive = false;

        // 重置计时器
        alertTimer = 0f;
        nextAlertTime = 0f;
    }

    /// <summary>
    /// 手动触发警报（用于测试）
    /// Manually trigger an alert on a specific equipment (for testing)
    /// Validates: Requirements 7.6
    /// </summary>
    /// <param name="equipment">要触发警报的设备 / Equipment to trigger alert on</param>
    public void TriggerAlertManually(GameObject equipment)
    {
        if (equipment == null)
        {
            Debug.LogError("AlertSystem: 无法手动触发警报，设备参数为null");
            return;
        }

        // 验证系统在Idle状态
        if (currentState != AlertState.Idle)
        {
            Debug.LogWarning($"AlertSystem: 无法手动触发警报，系统不在Idle状态（当前状态: {currentState}）");
            return;
        }

        // 验证设备在注册表中
        EquipmentInfo equipmentInfo = allEquipment.Find(e => e.gameObject == equipment);
        if (equipmentInfo == null)
        {
            Debug.LogError($"AlertSystem: 无法手动触发警报，设备 '{equipment.name}' 不在注册表中");
            return;
        }

        Debug.Log($"AlertSystem: 手动触发警报，设备: {equipment.name}");

        // 设置设备的isAlerting标志
        equipmentInfo.isAlerting = true;

        // 调用StartAlertSequence
        StartAlertSequence(equipment);
    }

    /// <summary>
    /// 运行时注册设备
    /// Register equipment at runtime
    /// Task 14.1: 实现运行时设备注册方法
    /// Validates: Requirements 9.5
    /// </summary>
    /// <param name="equipment">要注册的设备 / Equipment to register</param>
    /// <returns>注册是否成功 / Whether registration was successful</returns>
    public bool RegisterEquipment(GameObject equipment)
    {
        if (equipment == null)
        {
            Debug.LogError("AlertSystem: 无法注册设备，设备参数为null");
            return false;
        }

        // 检查设备是否已经注册
        if (allEquipment != null && allEquipment.Exists(e => e.gameObject == equipment))
        {
            Debug.LogWarning($"AlertSystem: 设备 '{equipment.name}' 已经注册，无需重复注册");
            return false;
        }

        // 确保注册表已初始化
        if (equipmentByFloor == null)
        {
            equipmentByFloor = new Dictionary<string, List<EquipmentInfo>>();
        }
        if (allEquipment == null)
        {
            allEquipment = new List<EquipmentInfo>();
        }

        // 查找设备的父楼层
        string floorName = FindParentFloor(equipment);

        if (string.IsNullOrEmpty(floorName))
        {
            Debug.LogWarning($"AlertSystem: 设备 '{equipment.name}' 不在任何楼层下，无法注册");
            return false;
        }

        // 创建设备信息
        EquipmentInfo equipmentInfo = new EquipmentInfo(equipment, floorName);
        allEquipment.Add(equipmentInfo);

        // 将设备分组到equipmentByFloor字典
        if (!equipmentByFloor.ContainsKey(floorName))
        {
            equipmentByFloor[floorName] = new List<EquipmentInfo>();
        }
        equipmentByFloor[floorName].Add(equipmentInfo);

        Debug.Log($"AlertSystem: 设备 '{equipment.name}' 已成功注册到楼层 '{floorName}'");
        return true;
    }

    /// <summary>
    /// 运行时注销设备
    /// Unregister equipment at runtime
    /// Task 14.1: 实现运行时设备注销方法
    /// Validates: Requirements 9.5
    /// </summary>
    /// <param name="equipment">要注销的设备 / Equipment to unregister</param>
    /// <returns>注销是否成功 / Whether unregistration was successful</returns>
    public bool UnregisterEquipment(GameObject equipment)
    {
        if (equipment == null)
        {
            Debug.LogError("AlertSystem: 无法注销设备，设备参数为null");
            return false;
        }

        // 检查设备是否已注册
        if (allEquipment == null || !allEquipment.Exists(e => e.gameObject == equipment))
        {
            Debug.LogWarning($"AlertSystem: 设备 '{equipment.name}' 未注册，无法注销");
            return false;
        }

        // 查找设备信息
        EquipmentInfo equipmentInfo = allEquipment.Find(e => e.gameObject == equipment);
        if (equipmentInfo == null)
        {
            Debug.LogError($"AlertSystem: 无法找到设备 '{equipment.name}' 的信息");
            return false;
        }

        // 如果设备正在报警，先解除警报
        if (equipmentInfo.isAlerting && currentAlertEquipment == equipment)
        {
            Debug.LogWarning($"AlertSystem: 设备 '{equipment.name}' 正在报警，将先解除警报");
            DismissCurrentAlert();
        }

        // 从allEquipment列表中移除
        allEquipment.Remove(equipmentInfo);

        // 从equipmentByFloor字典中移除
        if (equipmentByFloor.ContainsKey(equipmentInfo.floorName))
        {
            equipmentByFloor[equipmentInfo.floorName].Remove(equipmentInfo);

            // 如果楼层没有设备了，移除楼层条目
            if (equipmentByFloor[equipmentInfo.floorName].Count == 0)
            {
                equipmentByFloor.Remove(equipmentInfo.floorName);
                Debug.LogWarning($"AlertSystem: 楼层 '{equipmentInfo.floorName}' 已没有任何设备");
            }
        }

        Debug.Log($"AlertSystem: 设备 '{equipment.name}' 已成功从楼层 '{equipmentInfo.floorName}' 注销");
        return true;
    }

    /// <summary>
    /// 解除当前警报
    /// Dismiss the current alert
    /// Validates: Requirements 6.1, 6.2, 6.3, 6.4, 6.5, 6.6
    /// </summary>
    public void DismissCurrentAlert()
    {
        // 验证当前有活动警报
        if (currentState == AlertState.Idle || currentAlertEquipment == null)
        {
            Debug.LogWarning("AlertSystem: 没有活动的警报可以解除");
            return;
        }

        Debug.Log($"AlertSystem: 解除警报，设备: {currentAlertEquipment.name}");

        // 设置设备的isAlerting为false
        EquipmentInfo equipmentInfo = allEquipment.Find(e => e.gameObject == currentAlertEquipment);
        if (equipmentInfo != null)
        {
            equipmentInfo.isAlerting = false;
        }

        // 销毁波纹效果
        DestroyRippleEffect();

        // 退出楼层隔离模式 (Task 9.2)
        if (floorController != null && floorController.IsInIsolationMode())
        {
            Debug.Log("AlertSystem: 退出楼层隔离模式");
            floorController.ExitIsolationMode();
        }

        // 停止当前的摄像头过渡
        if (currentCameraTransition != null)
        {
            StopCoroutine(currentCameraTransition);
            currentCameraTransition = null;
        }

        // 重新启用CameraController
        RestoreCameraControl();

        // 清空当前警报信息
        currentAlertEquipment = null;
        currentAlertFloor = null;

        // 重置警报计时器
        alertTimer = 0f;
        nextAlertTime = UnityEngine.Random.Range(minAlertInterval, maxAlertInterval);
        Debug.Log($"AlertSystem: 下一次警报将在 {nextAlertTime:F2}秒后触发");

        // 转换状态到Idle
        TransitionToState(AlertState.Idle);
    }

    #endregion

    #region Unity Lifecycle Methods

    /// <summary>
    /// Unity Start方法 - 初始化系统
    /// Unity Start method - Initialize the system
    /// </summary>
    private void Start()
    {
        Initialize();
        StartAlertSystem();
    }

    /// <summary>
    /// Unity Update方法 - 处理警报计时逻辑和状态更新
    /// Unity Update method - Handle alert timer logic and state updates
    /// Validates: Requirements 1.5, 4.1, 4.2, 6.6
    /// </summary>
    private void Update()
    {
        if (!isSystemActive)
        {
            return;
        }

        // 检查运行时错误
        CheckRuntimeErrors();

        // 处理不同状态的逻辑
        switch (currentState)
        {
            case AlertState.Idle:
                // 在Idle状态下运行警报触发计时器
                UpdateIdleState();
                break;

            case AlertState.WaitingAtFloorView:
                // Task 10.2: 在楼层视角等待，递增focusDelayTimer
                UpdateWaitingAtFloorViewState();
                break;

            case AlertState.MovingToFloorView:
            case AlertState.MovingToEquipmentView:
            case AlertState.FocusedOnEquipment:
                // 这些状态不需要在Update中处理
                break;
        }

        // 更新状态超时计时器
        UpdateStateTimeout();
    }

    /// <summary>
    /// 更新Idle状态 - 处理警报触发计时
    /// Update Idle state - Handle alert trigger timing
    /// </summary>
    private void UpdateIdleState()
    {
        // 递增警报计时器
        alertTimer += Time.deltaTime;

        // 检查是否达到下一次警报时间
        if (alertTimer >= nextAlertTime)
        {
            Debug.Log($"AlertSystem: 警报计时器到达 {alertTimer:F2}秒，触发随机警报");
            
            // 触发随机警报
            TriggerRandomAlert();
            
            // 重置计时器
            alertTimer = 0f;
            
            // 使用Random.Range在minAlertInterval和maxAlertInterval之间生成下一个间隔
            nextAlertTime = UnityEngine.Random.Range(minAlertInterval, maxAlertInterval);
            
            Debug.Log($"AlertSystem: 下一次警报将在 {nextAlertTime:F2}秒后触发");
        }
    }

    /// <summary>
    /// 更新WaitingAtFloorView状态 - 处理楼层视角等待逻辑
    /// Update WaitingAtFloorView state - Handle floor view waiting logic
    /// Task 10.2: 实现楼层视角等待逻辑
    /// Validates: Requirements 4.1, 4.2
    /// </summary>
    private void UpdateWaitingAtFloorViewState()
    {
        // 递增focusDelayTimer
        focusDelayTimer += Time.deltaTime;

        // 当达到focusDelayDuration时转换到MovingToEquipmentView
        if (focusDelayTimer >= focusDelayDuration)
        {
            Debug.Log($"AlertSystem: 楼层视角等待完成 ({focusDelayTimer:F2}秒)，开始移动到设备视角");
            
            // 重置focusDelayTimer
            focusDelayTimer = 0f;

            // Task 10.3: 实现设备聚焦过渡
            StartEquipmentFocusTransition();
        }
    }

    /// <summary>
    /// 开始设备聚焦过渡
    /// Start equipment focus transition
    /// Task 10.3: 实现设备聚焦过渡
    /// Validates: Requirements 4.3, 4.4, 4.5
    /// </summary>
    private void StartEquipmentFocusTransition()
    {
        if (currentAlertEquipment == null)
        {
            Debug.LogError("AlertSystem: 无法开始设备聚焦过渡，当前报警设备为null");
            TransitionToState(AlertState.Idle);
            return;
        }

        // 转换状态到MovingToEquipmentView
        if (!TransitionToState(AlertState.MovingToEquipmentView))
        {
            Debug.LogError("AlertSystem: 无法转换到MovingToEquipmentView状态");
            return;
        }

        // 计算设备聚焦视角
        CameraView equipmentView = CalculateEquipmentFocusView(currentAlertEquipment);
        Debug.Log($"AlertSystem: 设备聚焦视角 - 位置: {equipmentView.position}, 旋转: {equipmentView.rotation.eulerAngles}");

        // 启动摄像头过渡到设备视角
        if (currentCameraTransition != null)
        {
            StopCoroutine(currentCameraTransition);
        }
        currentCameraTransition = StartCoroutine(MoveCameraToViewWithCallback(equipmentView, OnEquipmentViewReached));
    }

    /// <summary>
    /// 摄像头到达设备视角的回调
    /// Callback when camera reaches equipment view
    /// </summary>
    private void OnEquipmentViewReached()
    {
        Debug.Log("AlertSystem: 摄像头已到达设备视角");
        
        // 转换到FocusedOnEquipment状态
        TransitionToState(AlertState.FocusedOnEquipment);
    }

    /// <summary>
    /// 摄像头到达楼层视角的回调
    /// Callback when camera reaches floor view
    /// </summary>
    private void OnFloorViewReached()
    {
        Debug.Log("AlertSystem: 摄像头已到达楼层视角，开始等待");
        
        // 重置focusDelayTimer
        focusDelayTimer = 0f;
        
        // 转换到WaitingAtFloorView状态
        TransitionToState(AlertState.WaitingAtFloorView);
    }

    #endregion

    #region Runtime Error Handling

    /// <summary>
    /// 检查运行时错误
    /// Check for runtime errors
    /// Task 13.2: 实现运行时错误处理
    /// </summary>
    private void CheckRuntimeErrors()
    {
        // 检查设备在警报期间被销毁
        if (currentAlertEquipment != null && currentState != AlertState.Idle)
        {
            if (currentAlertEquipment == null || !currentAlertEquipment.activeInHierarchy)
            {
                Debug.LogError("AlertSystem: 检测到当前报警设备已被销毁或禁用，强制解除警报");
                ForceRecoveryToIdle("设备被销毁");
                return;
            }
        }

        // 检查FloorController状态不匹配
        if (currentState != AlertState.Idle && floorController != null)
        {
            bool shouldBeInIsolation = (currentState == AlertState.MovingToFloorView ||
                                       currentState == AlertState.WaitingAtFloorView ||
                                       currentState == AlertState.MovingToEquipmentView ||
                                       currentState == AlertState.FocusedOnEquipment);

            if (shouldBeInIsolation && !floorController.IsInIsolationMode())
            {
                Debug.LogWarning("AlertSystem: 检测到FloorController状态不匹配，重新进入隔离模式");
                if (!string.IsNullOrEmpty(currentAlertFloor))
                {
                    floorController.EnterIsolationMode(currentAlertFloor);
                }
            }
        }
    }

    /// <summary>
    /// 更新状态超时计时器
    /// Update state timeout timer
    /// Task 13.2: 实现状态超时和强制恢复
    /// </summary>
    private void UpdateStateTimeout()
    {
        // 只在非Idle状态下计时
        if (currentState != AlertState.Idle)
        {
            currentStateTimer += Time.deltaTime;

            // 检查是否超时
            if (currentStateTimer >= stateTimeoutDuration)
            {
                Debug.LogError($"AlertSystem: 状态 {currentState} 超时（{currentStateTimer:F2}秒），强制恢复到Idle状态");
                ForceRecoveryToIdle("状态超时");
            }
        }
        else
        {
            // Idle状态下重置计时器
            currentStateTimer = 0f;
        }
    }

    /// <summary>
    /// 强制恢复到Idle状态
    /// Force recovery to Idle state
    /// Task 13.2: 实现强制恢复机制
    /// </summary>
    /// <param name="reason">恢复原因 / Recovery reason</param>
    private void ForceRecoveryToIdle(string reason)
    {
        Debug.LogWarning($"AlertSystem: 强制恢复到Idle状态，原因: {reason}");

        // 停止当前的摄像头过渡
        if (currentCameraTransition != null)
        {
            StopCoroutine(currentCameraTransition);
            currentCameraTransition = null;
            isCameraTransitioning = false;
        }

        // 清理波纹效果
        if (currentRippleObject != null)
        {
            DestroyRippleEffect();
        }

        // 退出楼层隔离模式
        if (floorController != null && floorController.IsInIsolationMode())
        {
            floorController.ExitIsolationMode();
        }

        // 恢复摄像头控制
        RestoreCameraControl();

        // 清理当前警报设备的isAlerting标志
        if (currentAlertEquipment != null)
        {
            EquipmentInfo equipmentInfo = allEquipment.Find(e => e.gameObject == currentAlertEquipment);
            if (equipmentInfo != null)
            {
                equipmentInfo.isAlerting = false;
            }
        }

        // 清空当前警报信息
        currentAlertEquipment = null;
        currentAlertFloor = null;

        // 重置计时器
        alertTimer = 0f;
        nextAlertTime = UnityEngine.Random.Range(minAlertInterval, maxAlertInterval);
        focusDelayTimer = 0f;
        currentStateTimer = 0f;

        // 强制转换到Idle状态（绕过状态转换验证）
        currentState = AlertState.Idle;
        Debug.Log("AlertSystem: 已强制恢复到Idle状态");
    }

    #endregion

    #region Private Methods

    /// <summary>
    /// 发现并注册所有设备
    /// Discover and register all equipment in the scene
    /// Validates: Requirements 9.1, 9.2, 9.3, 9.4, 1.1
    /// </summary>
    private void DiscoverEquipment()
    {
        // 初始化设备注册表
        equipmentByFloor = new Dictionary<string, List<EquipmentInfo>>();
        allEquipment = new List<EquipmentInfo>();

        // 使用GameObject.FindGameObjectsWithTag查找所有设备
        GameObject[] equipmentObjects = GameObject.FindGameObjectsWithTag(equipmentTag);

        Debug.Log($"AlertSystem: 发现 {equipmentObjects.Length} 个标记为 '{equipmentTag}' 的设备对象");

        // 遍历每个设备，确定其父楼层
        foreach (GameObject equipment in equipmentObjects)
        {
            // 遍历GameObject层级确定父楼层
            string floorName = FindParentFloor(equipment);

            if (string.IsNullOrEmpty(floorName))
            {
                // 记录无效设备（不在楼层下）并输出警告
                Debug.LogWarning($"AlertSystem: 设备 '{equipment.name}' 不在任何楼层下，将被排除在警报系统之外");
                continue;
            }

            // 创建设备信息
            EquipmentInfo equipmentInfo = new EquipmentInfo(equipment, floorName);
            allEquipment.Add(equipmentInfo);

            // 将设备分组到equipmentByFloor字典
            if (!equipmentByFloor.ContainsKey(floorName))
            {
                equipmentByFloor[floorName] = new List<EquipmentInfo>();
            }
            equipmentByFloor[floorName].Add(equipmentInfo);

            Debug.Log($"AlertSystem: 设备 '{equipment.name}' 已注册到楼层 '{floorName}'");
        }

        // 输出统计信息
        Debug.Log($"AlertSystem: 设备注册完成。总计 {allEquipment.Count} 个有效设备，分布在 {equipmentByFloor.Count} 个楼层");
        foreach (var kvp in equipmentByFloor)
        {
            Debug.Log($"  - 楼层 '{kvp.Key}': {kvp.Value.Count} 个设备");
        }
    }

    /// <summary>
    /// 查找设备的父楼层
    /// Find the parent floor of an equipment by traversing the GameObject hierarchy
    /// </summary>
    /// <param name="equipment">设备GameObject / Equipment GameObject</param>
    /// <returns>楼层名称，如果未找到则返回null / Floor name, or null if not found</returns>
    private string FindParentFloor(GameObject equipment)
    {
        Transform current = equipment.transform.parent;

        // 向上遍历层级，直到找到楼层对象或到达根节点
        while (current != null)
        {
            string name = current.name.ToLower();

            // 检查是否是楼层对象（支持中文和数字格式）
            if (IsFloorObject(name))
            {
                return current.name; // 返回原始名称（保持大小写）
            }

            current = current.parent;
        }

        return null; // 未找到父楼层
    }

    /// <summary>
    /// 检查GameObject名称是否是楼层对象
    /// Check if a GameObject name represents a floor object
    /// </summary>
    /// <param name="name">GameObject名称（小写）/ GameObject name (lowercase)</param>
    /// <returns>是否是楼层对象 / Whether it's a floor object</returns>
    private bool IsFloorObject(string name)
    {
        // 支持中文楼层名称：一楼、二楼、三楼、四楼、五楼、六楼
        if (name.Contains("一楼") || name.Contains("二楼") || name.Contains("三楼") ||
            name.Contains("四楼") || name.Contains("五楼") || name.Contains("六楼"))
        {
            return true;
        }

        // 支持数字楼层名称：1楼、2楼、3楼、4楼、5楼、6楼
        if (name.Contains("1楼") || name.Contains("2楼") || name.Contains("3楼") ||
            name.Contains("4楼") || name.Contains("5楼") || name.Contains("6楼"))
        {
            return true;
        }

        // 支持英文格式：floor1, floor2, 1f, 2f等
        if (name.Contains("floor") || name.EndsWith("f"))
        {
            return true;
        }

        return false;
    }

    /// <summary>
    /// 随机触发设备警报
    /// Trigger a random equipment alert
    /// Validates: Requirements 1.2, 1.3, 1.4
    /// </summary>
    private void TriggerRandomAlert()
    {
        // 验证系统状态
        if (currentState != AlertState.Idle)
        {
            Debug.LogWarning("AlertSystem: 系统不在Idle状态，无法触发新警报");
            return;
        }

        // 验证设备列表不为空
        if (allEquipment == null || allEquipment.Count == 0)
        {
            Debug.LogError("AlertSystem: 没有可用的设备来触发警报");
            return;
        }

        // 验证只有一个设备处于警报状态（应该是0个，因为我们在Idle状态）
        int alertingCount = 0;
        foreach (var equipment in allEquipment)
        {
            if (equipment.isAlerting)
            {
                alertingCount++;
            }
        }

        if (alertingCount > 0)
        {
            Debug.LogError($"AlertSystem: 检测到 {alertingCount} 个设备仍处于警报状态！这不应该发生。");
            // 清理所有警报状态
            foreach (var equipment in allEquipment)
            {
                equipment.isAlerting = false;
            }
        }

        // 从allEquipment列表随机选择一个设备
        int randomIndex = UnityEngine.Random.Range(0, allEquipment.Count);
        EquipmentInfo selectedEquipment = allEquipment[randomIndex];

        Debug.Log($"AlertSystem: 随机选择设备 '{selectedEquipment.gameObject.name}' (楼层: {selectedEquipment.floorName}) 触发警报");

        // 设置设备的isAlerting标志
        selectedEquipment.isAlerting = true;

        // 调用StartAlertSequence
        StartAlertSequence(selectedEquipment.gameObject);
    }

    /// <summary>
    /// 开始警报序列
    /// Start the alert sequence for a specific equipment
    /// Validates: Requirements 1.3, 2.1, 3.1, 3.2, 3.3, 5.1
    /// </summary>
    /// <param name="equipment">报警设备 / Alerting equipment</param>
    private void StartAlertSequence(GameObject equipment)
    {
        Debug.Log($"AlertSystem: 开始警报序列，设备: {equipment.name}");

        // 1. 转换状态到MovingToFloorView
        if (!TransitionToState(AlertState.MovingToFloorView))
        {
            Debug.LogError("AlertSystem: 无法转换到MovingToFloorView状态");
            return;
        }

        // 2. 识别设备所属楼层
        EquipmentInfo equipmentInfo = allEquipment.Find(e => e.gameObject == equipment);
        if (equipmentInfo == null)
        {
            Debug.LogError($"AlertSystem: 设备 {equipment.name} 不在注册表中");
            TransitionToState(AlertState.Idle);
            return;
        }

        currentAlertEquipment = equipment;
        currentAlertFloor = equipmentInfo.floorName;
        Debug.Log($"AlertSystem: 设备所属楼层: {currentAlertFloor}");

        // 3. 创建波纹效果
        CreateRippleEffect(equipment);

        // 4. 激活楼层隔离模式 (Task 9.1)
        if (floorController != null)
        {
            Debug.Log($"AlertSystem: 激活楼层隔离模式，楼层: {currentAlertFloor}");
            floorController.EnterIsolationMode(currentAlertFloor);
        }
        else
        {
            Debug.LogWarning("AlertSystem: FloorController未找到，无法激活楼层隔离模式");
        }

        // 5. 计算楼层预设视角
        CameraView floorView = CalculateFloorPresetView(currentAlertFloor);
        Debug.Log($"AlertSystem: 楼层预设视角 - 位置: {floorView.position}, 旋转: {floorView.rotation.eulerAngles}");

        // 6. 启动摄像头过渡到楼层视角
        // Task 10.1: 在过渡完成后转换到WaitingAtFloorView状态
        if (currentCameraTransition != null)
        {
            StopCoroutine(currentCameraTransition);
        }
        currentCameraTransition = StartCoroutine(MoveCameraToViewWithCallback(floorView, OnFloorViewReached));
    }

    /// <summary>
    /// 计算楼层预设视角
    /// Calculate the floor preset view for a given floor
    /// Validates: Requirements 3.2, 3.5
    /// </summary>
    /// <param name="floorName">楼层名称 / Floor name</param>
    /// <returns>摄像头视角 / Camera view</returns>
    private CameraView CalculateFloorPresetView(string floorName)
    {
        Debug.Log($"AlertSystem: 计算楼层预设视角，楼层: {floorName}");

        // 首先检查floorPresetViews列表中是否有配置的视角
        if (floorPresetViews != null && floorPresetViews.Count > 0)
        {
            foreach (var presetView in floorPresetViews)
            {
                if (presetView.floorName == floorName)
                {
                    // 找到配置的视角，返回配置的CameraView
                    CameraView configuredView = new CameraView(
                        presetView.position,
                        Quaternion.Euler(presetView.rotation)
                    );
                    Debug.Log($"AlertSystem: 使用配置的楼层视角 - 位置: {configuredView.position}, 旋转: {presetView.rotation}");
                    return configuredView;
                }
            }
        }

        // 如果没有配置，计算楼层的边界（Bounds）
        Debug.Log($"AlertSystem: 未找到配置的视角，计算默认视角");

        // 查找楼层GameObject
        GameObject floorObject = FindFloorObject(floorName);
        if (floorObject == null)
        {
            Debug.LogWarning($"AlertSystem: 未找到楼层对象 '{floorName}'，使用默认视角");
            // 返回一个默认视角
            return new CameraView(new Vector3(0, 20, -20), Quaternion.Euler(45, 0, 0));
        }

        // 计算楼层的边界
        Bounds floorBounds = CalculateFloorBounds(floorObject);

        // 基于楼层边界计算默认视角（位置和旋转）
        // 摄像头位置：在楼层中心上方和后方
        Vector3 cameraPosition = floorBounds.center + new Vector3(0, floorBounds.size.y * 1.5f, -floorBounds.size.z * 1.2f);
        
        // 摄像头旋转：朝向楼层中心
        Vector3 directionToCenter = floorBounds.center - cameraPosition;
        Quaternion cameraRotation = Quaternion.LookRotation(directionToCenter);

        CameraView calculatedView = new CameraView(cameraPosition, cameraRotation);
        Debug.Log($"AlertSystem: 计算的默认视角 - 位置: {cameraPosition}, 旋转: {cameraRotation.eulerAngles}");

        return calculatedView;
    }

    /// <summary>
    /// 计算设备聚焦视角
    /// Calculate the equipment focus view for a given equipment
    /// Validates: Requirements 4.3, 4.6
    /// </summary>
    /// <param name="equipment">设备GameObject / Equipment GameObject</param>
    /// <returns>摄像头视角 / Camera view</returns>
    private CameraView CalculateEquipmentFocusView(GameObject equipment)
    {
        if (equipment == null)
        {
            Debug.LogError("AlertSystem: 无法计算设备聚焦视角，设备为null");
            return new CameraView(Vector3.zero, Quaternion.identity);
        }

        Debug.Log($"AlertSystem: 计算设备聚焦视角，设备: {equipment.name}");

        // 获取设备的位置和边界
        Vector3 equipmentPosition;
        Bounds equipmentBounds;

        Renderer equipmentRenderer = equipment.GetComponent<Renderer>();
        if (equipmentRenderer != null)
        {
            // 使用Renderer的bounds
            equipmentBounds = equipmentRenderer.bounds;
            equipmentPosition = equipmentBounds.center;
        }
        else
        {
            // 如果没有Renderer，使用transform位置并创建一个默认边界
            equipmentPosition = equipment.transform.position;
            equipmentBounds = new Bounds(equipmentPosition, Vector3.one * 2f);
        }

        Debug.Log($"AlertSystem: 设备位置: {equipmentPosition}, 边界大小: {equipmentBounds.size}");

        // 计算摄像头位置（距离设备equipmentFocusDistance）
        // 摄像头在设备前方和上方，形成一个良好的观察角度
        Vector3 offset = new Vector3(0, equipmentFocusDistance * 0.5f, -equipmentFocusDistance);
        Vector3 cameraPosition = equipmentPosition + offset;

        // 计算摄像头旋转（朝向设备）
        Vector3 directionToEquipment = equipmentPosition - cameraPosition;
        Quaternion cameraRotation = Quaternion.LookRotation(directionToEquipment);

        // 确保设备在摄像头视锥内可见
        // 如果设备太大，调整摄像头距离
        if (mainCamera != null)
        {
            float equipmentSize = Mathf.Max(equipmentBounds.size.x, equipmentBounds.size.y, equipmentBounds.size.z);
            float requiredDistance = CalculateRequiredDistance(equipmentSize, mainCamera.fieldOfView);
            
            if (requiredDistance > equipmentFocusDistance)
            {
                // 需要更远的距离才能完整看到设备
                float adjustedDistance = requiredDistance * 1.2f; // 增加20%的边距
                offset = new Vector3(0, adjustedDistance * 0.5f, -adjustedDistance);
                cameraPosition = equipmentPosition + offset;
                directionToEquipment = equipmentPosition - cameraPosition;
                cameraRotation = Quaternion.LookRotation(directionToEquipment);
                
                Debug.Log($"AlertSystem: 调整摄像头距离以适应设备大小，新距离: {adjustedDistance:F2}");
            }
        }

        CameraView focusView = new CameraView(cameraPosition, cameraRotation);
        Debug.Log($"AlertSystem: 设备聚焦视角 - 位置: {cameraPosition}, 旋转: {cameraRotation.eulerAngles}");

        return focusView;
    }

    /// <summary>
    /// 查找楼层GameObject
    /// Find the floor GameObject by name
    /// </summary>
    /// <param name="floorName">楼层名称 / Floor name</param>
    /// <returns>楼层GameObject，如果未找到则返回null / Floor GameObject, or null if not found</returns>
    private GameObject FindFloorObject(string floorName)
    {
        GameObject[] allObjects = FindObjectsOfType<GameObject>();
        
        foreach (GameObject obj in allObjects)
        {
            if (obj.name == floorName)
            {
                return obj;
            }
        }

        return null;
    }

    /// <summary>
    /// 计算楼层的边界
    /// Calculate the bounds of a floor by combining all renderers
    /// </summary>
    /// <param name="floorObject">楼层GameObject / Floor GameObject</param>
    /// <returns>楼层边界 / Floor bounds</returns>
    private Bounds CalculateFloorBounds(GameObject floorObject)
    {
        Renderer[] renderers = floorObject.GetComponentsInChildren<Renderer>();
        
        if (renderers.Length == 0)
        {
            // 如果没有Renderer，返回一个默认边界
            Debug.LogWarning($"AlertSystem: 楼层 '{floorObject.name}' 没有Renderer，使用默认边界");
            return new Bounds(floorObject.transform.position, Vector3.one * 10f);
        }

        // 初始化边界为第一个Renderer的边界
        Bounds bounds = renderers[0].bounds;

        // 扩展边界以包含所有Renderer
        for (int i = 1; i < renderers.Length; i++)
        {
            bounds.Encapsulate(renderers[i].bounds);
        }

        Debug.Log($"AlertSystem: 楼层 '{floorObject.name}' 边界 - 中心: {bounds.center}, 大小: {bounds.size}");

        return bounds;
    }

    /// <summary>
    /// 计算所需的摄像头距离以完整显示对象
    /// Calculate the required camera distance to fully show an object
    /// </summary>
    /// <param name="objectSize">对象大小 / Object size</param>
    /// <param name="fieldOfView">摄像头视野角度 / Camera field of view</param>
    /// <returns>所需距离 / Required distance</returns>
    private float CalculateRequiredDistance(float objectSize, float fieldOfView)
    {
        // 使用三角函数计算所需距离
        // distance = (objectSize / 2) / tan(fieldOfView / 2)
        float halfFOV = fieldOfView * 0.5f * Mathf.Deg2Rad;
        float distance = (objectSize * 0.5f) / Mathf.Tan(halfFOV);
        
        return distance;
    }

    /// <summary>
    /// 创建波纹效果
    /// Create ripple effect on the equipment
    /// Validates: Requirements 2.1, 2.2, 2.4, 2.5
    /// </summary>
    /// <param name="equipment">报警设备 / Alerting equipment</param>
    private void CreateRippleEffect(GameObject equipment)
    {
        if (equipment == null)
        {
            Debug.LogError("AlertSystem: 无法创建波纹效果，设备为null");
            return;
        }

        Debug.Log($"AlertSystem: 为设备 '{equipment.name}' 创建波纹效果");

        // 在设备位置创建新的GameObject
        currentRippleObject = new GameObject($"AlertRipple_{equipment.name}");
        
        // 设置波纹位置为设备的中心点
        // 获取设备的Renderer来确定中心点
        Renderer equipmentRenderer = equipment.GetComponent<Renderer>();
        if (equipmentRenderer != null)
        {
            // 使用Renderer的bounds中心作为波纹位置
            currentRippleObject.transform.position = equipmentRenderer.bounds.center;
        }
        else
        {
            // 如果没有Renderer，使用设备的transform位置
            currentRippleObject.transform.position = equipment.transform.position;
        }

        // 添加SelectionRipple组件
        SelectionRipple ripple = currentRippleObject.AddComponent<SelectionRipple>();

        // 使用配置的颜色、大小和透明度初始化波纹
        Color rippleColorWithAlpha = alertRippleColor;
        rippleColorWithAlpha.a = rippleAlpha;
        
        ripple.Initialize(rippleColorWithAlpha, rippleSize, null);

        Debug.Log($"AlertSystem: 波纹效果创建成功，位置: {currentRippleObject.transform.position}, 颜色: {rippleColorWithAlpha}, 大小: {rippleSize}");
    }

    /// <summary>
    /// 销毁波纹效果
    /// Destroy the ripple effect
    /// Validates: Requirements 2.6
    /// </summary>
    private void DestroyRippleEffect()
    {
        if (currentRippleObject != null)
        {
            Debug.Log($"AlertSystem: 销毁波纹效果 '{currentRippleObject.name}'");
            
            // 销毁currentRippleObject
            Destroy(currentRippleObject);
            
            // 清空currentRippleObject引用
            currentRippleObject = null;
        }
        else
        {
            Debug.Log("AlertSystem: 没有活动的波纹效果需要销毁");
        }
    }

    /// <summary>
    /// 状态转换方法 - 验证并执行状态转换
    /// Transition to a new state - Validates and executes state transitions
    /// Validates: Requirements 10.1, 10.2
    /// </summary>
    /// <param name="newState">目标状态 / Target state</param>
    /// <returns>转换是否成功 / Whether the transition was successful</returns>
    private bool TransitionToState(AlertState newState)
    {
        // 如果目标状态与当前状态相同，直接返回成功
        if (currentState == newState)
        {
            Debug.Log($"AlertSystem: 状态已经是 {newState}，无需转换");
            return true;
        }

        // 验证状态转换的合法性
        bool isValidTransition = IsValidStateTransition(currentState, newState);

        if (!isValidTransition)
        {
            // 拒绝非法转换并记录错误
            Debug.LogError($"AlertSystem: 非法状态转换！从 {currentState} 到 {newState} 不被允许。");
            return false;
        }

        // 记录状态转换日志
        Debug.Log($"AlertSystem: 状态转换 {currentState} -> {newState}");

        // 更新currentState
        AlertState previousState = currentState;
        currentState = newState;

        // 重置状态计时器
        currentStateTimer = 0f;

        // 状态转换成功
        return true;
    }

    /// <summary>
    /// 验证状态转换是否合法
    /// Validate if a state transition is legal according to the defined sequence
    /// </summary>
    /// <param name="from">当前状态 / Current state</param>
    /// <param name="to">目标状态 / Target state</param>
    /// <returns>转换是否合法 / Whether the transition is valid</returns>
    private bool IsValidStateTransition(AlertState from, AlertState to)
    {
        // 定义合法的状态转换序列
        // Idle → MovingToFloorView → WaitingAtFloorView → MovingToEquipmentView → FocusedOnEquipment → Idle
        
        switch (from)
        {
            case AlertState.Idle:
                // 从Idle只能转换到MovingToFloorView（开始新警报）
                return to == AlertState.MovingToFloorView;

            case AlertState.MovingToFloorView:
                // 从MovingToFloorView只能转换到WaitingAtFloorView（到达楼层视角）
                // 或者返回Idle（警报被取消或出错）
                return to == AlertState.WaitingAtFloorView || to == AlertState.Idle;

            case AlertState.WaitingAtFloorView:
                // 从WaitingAtFloorView只能转换到MovingToEquipmentView（延迟结束）
                // 或者返回Idle（警报被取消或出错）
                return to == AlertState.MovingToEquipmentView || to == AlertState.Idle;

            case AlertState.MovingToEquipmentView:
                // 从MovingToEquipmentView只能转换到FocusedOnEquipment（到达设备视角）
                // 或者返回Idle（警报被取消或出错）
                return to == AlertState.FocusedOnEquipment || to == AlertState.Idle;

            case AlertState.FocusedOnEquipment:
                // 从FocusedOnEquipment只能返回Idle（警报被解除）
                return to == AlertState.Idle;

            default:
                // 未知状态，拒绝转换
                return false;
        }
    }

    /// <summary>
    /// 初始化警报系统
    /// Initialize the alert system
    /// Validates: Requirements 7.2, 7.3, 7.4, 7.5
    /// </summary>
    private void Initialize()
    {
        Debug.Log("AlertSystem: 开始初始化...");

        // 获取FloorController引用
        floorController = FindObjectOfType<FloorController>();
        if (floorController == null)
        {
            Debug.LogError("AlertSystem: 未找到FloorController组件！警报系统将被禁用。");
            enabled = false;
            return;
        }
        Debug.Log("AlertSystem: 成功获取FloorController引用");

        // 获取主摄像头引用
        mainCamera = Camera.main;
        if (mainCamera == null)
        {
            Debug.LogError("AlertSystem: 未找到主摄像头！警报系统将被禁用。");
            enabled = false;
            return;
        }
        Debug.Log("AlertSystem: 成功获取主摄像头引用");

        // 获取所有摄像头控制器组件（支持多种类型）
        // 查找常见的摄像头控制器类型
        var controllers = new System.Collections.Generic.List<MonoBehaviour>();
        
        // 尝试查找 CameraController
        var cameraController = mainCamera.GetComponent<CameraController>();
        if (cameraController != null)
        {
            controllers.Add(cameraController);
            Debug.Log("AlertSystem: 找到 CameraController 组件");
        }
        
        // 尝试查找 SimpleCameraController
        var simpleCameraController = mainCamera.GetComponent<SimpleCameraController>();
        if (simpleCameraController != null)
        {
            controllers.Add(simpleCameraController);
            Debug.Log("AlertSystem: 找到 SimpleCameraController 组件");
        }
        
        // 尝试查找 SceneViewCameraController
        var sceneViewCameraController = mainCamera.GetComponent<SceneViewCameraController>();
        if (sceneViewCameraController != null)
        {
            controllers.Add(sceneViewCameraController);
            Debug.Log("AlertSystem: 找到 SceneViewCameraController 组件");
        }
        
        // 如果没有找到任何控制器，发出警告但不禁用系统
        if (controllers.Count == 0)
        {
            Debug.LogWarning("AlertSystem: 未找到任何摄像头控制器组件。警报系统将继续运行，但无法禁用用户输入。");
            cameraControllers = new MonoBehaviour[0];
        }
        else
        {
            cameraControllers = controllers.ToArray();
            Debug.Log($"AlertSystem: 成功获取 {cameraControllers.Length} 个摄像头控制器组件");
        }

        // 调用DiscoverEquipment发现并注册所有设备
        DiscoverEquipment();

        // 验证是否发现了设备
        if (allEquipment == null || allEquipment.Count == 0)
        {
            Debug.LogError("AlertSystem: 场景中未发现任何设备！警报系统将被禁用。");
            enabled = false;
            return;
        }

        // 验证每个楼层至少有一个设备
        bool hasWarning = false;
        foreach (var kvp in equipmentByFloor)
        {
            if (kvp.Value.Count == 0)
            {
                Debug.LogWarning($"AlertSystem: 楼层 '{kvp.Key}' 没有任何设备！");
                hasWarning = true;
            }
        }

        // 验证配置参数
        ValidateConfiguration();

        Debug.Log($"AlertSystem: 初始化完成。系统已准备就绪，共注册 {allEquipment.Count} 个设备。");
    }

    /// <summary>
    /// 验证配置参数
    /// Validate configuration parameters
    /// Validates: Requirements 1.5, 4.2
    /// </summary>
    private void ValidateConfiguration()
    {
        // 验证并限制alertInterval在[5, 300]范围
        if (minAlertInterval < 5f)
        {
            Debug.LogWarning($"AlertSystem: minAlertInterval ({minAlertInterval}) 小于最小值5秒，已调整为5秒");
            minAlertInterval = 5f;
        }
        if (minAlertInterval > 300f)
        {
            Debug.LogWarning($"AlertSystem: minAlertInterval ({minAlertInterval}) 大于最大值300秒，已调整为300秒");
            minAlertInterval = 300f;
        }

        if (maxAlertInterval < 5f)
        {
            Debug.LogWarning($"AlertSystem: maxAlertInterval ({maxAlertInterval}) 小于最小值5秒，已调整为5秒");
            maxAlertInterval = 5f;
        }
        if (maxAlertInterval > 300f)
        {
            Debug.LogWarning($"AlertSystem: maxAlertInterval ({maxAlertInterval}) 大于最大值300秒，已调整为300秒");
            maxAlertInterval = 300f;
        }

        // 确保maxAlertInterval >= minAlertInterval
        if (maxAlertInterval < minAlertInterval)
        {
            Debug.LogWarning($"AlertSystem: maxAlertInterval ({maxAlertInterval}) 小于 minAlertInterval ({minAlertInterval})，已调整为相等");
            maxAlertInterval = minAlertInterval;
        }

        // 验证并限制focusDelay在[1, 10]范围
        if (focusDelayDuration < 1f)
        {
            Debug.LogWarning($"AlertSystem: focusDelayDuration ({focusDelayDuration}) 小于最小值1秒，已调整为1秒");
            focusDelayDuration = 1f;
        }
        if (focusDelayDuration > 10f)
        {
            Debug.LogWarning($"AlertSystem: focusDelayDuration ({focusDelayDuration}) 大于最大值10秒，已调整为10秒");
            focusDelayDuration = 10f;
        }

        // 验证cameraTransitionTime > 0
        if (cameraTransitionTime <= 0f)
        {
            Debug.LogWarning($"AlertSystem: cameraTransitionTime ({cameraTransitionTime}) 必须大于0，已调整为1秒");
            cameraTransitionTime = 1f;
        }

        // 验证equipmentFocusDistance > 0
        if (equipmentFocusDistance <= 0f)
        {
            Debug.LogWarning($"AlertSystem: equipmentFocusDistance ({equipmentFocusDistance}) 必须大于0，已调整为5");
            equipmentFocusDistance = 5f;
        }

        // 验证rippleSize > 0
        if (rippleSize <= 0f)
        {
            Debug.LogWarning($"AlertSystem: rippleSize ({rippleSize}) 必须大于0，已调整为2");
            rippleSize = 2f;
        }

        // 验证rippleAlpha在[0, 1]范围
        rippleAlpha = Mathf.Clamp01(rippleAlpha);
    }

    /// <summary>
    /// 摄像头过渡协程 - 平滑移动摄像头到目标视角
    /// Camera transition coroutine - Smoothly move camera to target view
    /// Validates: Requirements 3.4, 4.5, 8.1, 8.2
    /// </summary>
    /// <param name="targetView">目标摄像头视角 / Target camera view</param>
    /// <returns>协程迭代器 / Coroutine iterator</returns>
    private System.Collections.IEnumerator MoveCameraToView(CameraView targetView)
    {
        if (mainCamera == null)
        {
            Debug.LogError("AlertSystem: 无法移动摄像头，主摄像头引用为null");
            yield break;
        }

        Debug.Log($"AlertSystem: 开始摄像头过渡到目标视角 - 位置: {targetView.position}, 旋转: {targetView.rotation.eulerAngles}");

        // 保存当前摄像头位置和旋转到savedCameraView
        savedCameraView = new CameraView(
            mainCamera.transform.position,
            mainCamera.transform.rotation
        );
        Debug.Log($"AlertSystem: 已保存当前摄像头状态 - 位置: {savedCameraView.position}, 旋转: {savedCameraView.rotation.eulerAngles}");

        // 禁用所有摄像头控制器组件以阻止用户输入
        if (cameraControllers != null && cameraControllers.Length > 0)
        {
            foreach (var controller in cameraControllers)
            {
                if (controller != null && controller.enabled)
                {
                    controller.enabled = false;
                }
            }
            Debug.Log($"AlertSystem: 已禁用 {cameraControllers.Length} 个摄像头控制器，用户输入已暂停");
        }

        // 设置isCameraTransitioning标志
        isCameraTransitioning = true;

        // 记录起始位置和旋转
        Vector3 startPosition = mainCamera.transform.position;
        Quaternion startRotation = mainCamera.transform.rotation;

        // 使用Lerp和Slerp平滑过渡到目标视角
        float elapsedTime = 0f;

        while (elapsedTime < cameraTransitionTime)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / cameraTransitionTime;

            // 使用平滑插值曲线（SmoothStep）使过渡更自然
            t = Mathf.SmoothStep(0f, 1f, t);

            // Lerp位置
            mainCamera.transform.position = Vector3.Lerp(startPosition, targetView.position, t);

            // Slerp旋转
            mainCamera.transform.rotation = Quaternion.Slerp(startRotation, targetView.rotation, t);

            yield return null;
        }

        // 确保最终位置和旋转精确到达目标
        mainCamera.transform.position = targetView.position;
        mainCamera.transform.rotation = targetView.rotation;

        // 清除isCameraTransitioning标志
        isCameraTransitioning = false;

        Debug.Log($"AlertSystem: 摄像头过渡完成 - 最终位置: {mainCamera.transform.position}, 最终旋转: {mainCamera.transform.rotation.eulerAngles}");
    }

    /// <summary>
    /// 摄像头过渡协程（带回调）- 平滑移动摄像头到目标视角并在完成后调用回调
    /// Camera transition coroutine with callback - Smoothly move camera to target view and invoke callback on completion
    /// Validates: Requirements 3.4, 4.5, 8.1, 8.2
    /// </summary>
    /// <param name="targetView">目标摄像头视角 / Target camera view</param>
    /// <param name="onComplete">完成回调 / Completion callback</param>
    /// <returns>协程迭代器 / Coroutine iterator</returns>
    private System.Collections.IEnumerator MoveCameraToViewWithCallback(CameraView targetView, System.Action onComplete)
    {
        if (mainCamera == null)
        {
            Debug.LogError("AlertSystem: 无法移动摄像头，主摄像头引用为null");
            yield break;
        }

        Debug.Log($"AlertSystem: 开始摄像头过渡到目标视角 - 位置: {targetView.position}, 旋转: {targetView.rotation.eulerAngles}");

        // 保存当前摄像头位置和旋转到savedCameraView（仅在第一次过渡时保存）
        if (currentState == AlertState.MovingToFloorView)
        {
            savedCameraView = new CameraView(
                mainCamera.transform.position,
                mainCamera.transform.rotation
            );
            Debug.Log($"AlertSystem: 已保存当前摄像头状态 - 位置: {savedCameraView.position}, 旋转: {savedCameraView.rotation.eulerAngles}");
        }

        // 禁用所有摄像头控制器组件以阻止用户输入（仅在第一次过渡时）
        if (currentState == AlertState.MovingToFloorView)
        {
            if (cameraControllers != null && cameraControllers.Length > 0)
            {
                foreach (var controller in cameraControllers)
                {
                    if (controller != null && controller.enabled)
                    {
                        controller.enabled = false;
                    }
                }
                Debug.Log($"AlertSystem: 已禁用 {cameraControllers.Length} 个摄像头控制器，用户输入已暂停");
            }
        }

        // 设置isCameraTransitioning标志
        isCameraTransitioning = true;

        // 记录起始位置和旋转
        Vector3 startPosition = mainCamera.transform.position;
        Quaternion startRotation = mainCamera.transform.rotation;

        // 使用Lerp和Slerp平滑过渡到目标视角
        float elapsedTime = 0f;

        while (elapsedTime < cameraTransitionTime)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / cameraTransitionTime;

            // 使用平滑插值曲线（SmoothStep）使过渡更自然
            t = Mathf.SmoothStep(0f, 1f, t);

            // Lerp位置
            mainCamera.transform.position = Vector3.Lerp(startPosition, targetView.position, t);

            // Slerp旋转
            mainCamera.transform.rotation = Quaternion.Slerp(startRotation, targetView.rotation, t);

            yield return null;
        }

        // 确保最终位置和旋转精确到达目标
        mainCamera.transform.position = targetView.position;
        mainCamera.transform.rotation = targetView.rotation;

        // 清除isCameraTransitioning标志
        isCameraTransitioning = false;

        Debug.Log($"AlertSystem: 摄像头过渡完成 - 最终位置: {mainCamera.transform.position}, 最终旋转: {mainCamera.transform.rotation.eulerAngles}");

        // 调用完成回调
        onComplete?.Invoke();
    }

    /// <summary>
    /// 恢复摄像头控制 - 重新启用所有摄像头控制器
    /// Restore camera control - Re-enable all camera controllers
    /// Validates: Requirements 8.3, 8.4
    /// </summary>
    private void RestoreCameraControl()
    {
        if (cameraControllers != null && cameraControllers.Length > 0)
        {
            foreach (var controller in cameraControllers)
            {
                if (controller != null)
                {
                    controller.enabled = true;
                }
            }
            Debug.Log($"AlertSystem: 已重新启用 {cameraControllers.Length} 个摄像头控制器，用户输入已恢复");
        }
        else
        {
            Debug.LogWarning("AlertSystem: 无法恢复摄像头控制，未找到摄像头控制器");
        }
    }

    #endregion
}
