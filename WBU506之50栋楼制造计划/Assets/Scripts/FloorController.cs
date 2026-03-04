using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// 楼层控制器 - 处理楼层隔离和摄像头控制
/// </summary>
public class FloorController : MonoBehaviour
{
    [Header("摄像头设置")]
    [Tooltip("隔离模式下的摄像头位置")]
    public Vector3 isolationCameraPosition = new Vector3(-30f, 35f, 0f);
    
    [Tooltip("隔离模式下的摄像头旋转")]
    public Vector3 isolationCameraRotation = new Vector3(90f, -90f, 0f);
    
    [Tooltip("摄像头移动时间（秒）")]
    public float cameraTransitionTime = 0.2f;

    private Camera mainCamera;
    private CameraController cameraController;
    private List<GameObject> floorObjects = new List<GameObject>();
    private GameObject roofObject;
    
    private bool isInIsolationMode = false;
    private string currentSelectedFloor = null;
    
    private Vector3 savedCameraPosition;
    private Quaternion savedCameraRotation;
    private bool isCameraMoving = false;

    void Start()
    {
        // 获取主摄像头
        mainCamera = Camera.main;
        if (mainCamera == null)
        {
            Debug.LogError("FloorController: 未找到主摄像头");
            return;
        }

        // 获取摄像头控制器
        cameraController = mainCamera.GetComponent<CameraController>();
        if (cameraController == null)
        {
            Debug.LogWarning("FloorController: 未找到 CameraController 组件");
        }

        // 查找所有楼层对象
        FindFloorObjects();
    }

    /// <summary>
    /// 查找场景中的所有楼层对象和屋顶
    /// </summary>
    void FindFloorObjects()
    {
        floorObjects.Clear();
        
        // 查找所有GameObject
        GameObject[] allObjects = FindObjectsOfType<GameObject>();
        
        foreach (GameObject obj in allObjects)
        {
            string name = obj.name.ToLower();
            
            // 查找楼层对象（一楼、二楼、三楼、四楼等）
            if (name.Contains("一楼") || name.Contains("二楼") || name.Contains("三楼") || 
                name.Contains("四楼") || name.Contains("五楼") || name.Contains("六楼") ||
                name.Contains("1楼") || name.Contains("2楼") || name.Contains("3楼") || 
                name.Contains("4楼") || name.Contains("5楼") || name.Contains("6楼"))
            {
                floorObjects.Add(obj);
                Debug.Log($"找到楼层对象: {obj.name}");
            }
            
            // 查找屋顶对象
            if (name == "roof" || name.Contains("屋顶"))
            {
                roofObject = obj;
                Debug.Log($"找到屋顶对象: {obj.name}");
            }
        }

        if (floorObjects.Count == 0)
        {
            Debug.LogWarning("FloorController: 未找到任何楼层对象");
        }
        else
        {
            Debug.Log($"FloorController: 总共找到 {floorObjects.Count} 个楼层对象");
        }
    }

    /// <summary>
    /// 进入楼层隔离模式
    /// </summary>
    public void EnterIsolationMode(string floorName)
    {
        if (mainCamera == null)
        {
            Debug.LogError("FloorController: 主摄像头不存在，无法进入隔离模式");
            return;
        }

        // 如果已经在隔离模式且选择的是同一楼层，则退出隔离模式
        if (isInIsolationMode && currentSelectedFloor == floorName)
        {
            ExitIsolationMode();
            return;
        }

        // 如果选择了不同的楼层，先退出当前隔离模式
        if (isInIsolationMode && currentSelectedFloor != floorName)
        {
            ExitIsolationMode();
        }

        Debug.Log($"进入楼层隔离模式: {floorName}");

        // 保存当前摄像头状态
        savedCameraPosition = mainCamera.transform.position;
        savedCameraRotation = mainCamera.transform.rotation;

        // 设置隔离模式状态
        isInIsolationMode = true;
        currentSelectedFloor = floorName;

        // 隐藏其他楼层
        HideOtherFloors(floorName);

        // 移动摄像头
        StartCoroutine(MoveCameraToIsolationView());
    }

    /// <summary>
    /// 退出楼层隔离模式
    /// </summary>
    public void ExitIsolationMode()
    {
        if (!isInIsolationMode)
            return;

        Debug.Log("退出楼层隔离模式");

        isInIsolationMode = false;
        currentSelectedFloor = null;

        // 显示所有楼层
        ShowAllFloors();

        // 恢复摄像头位置
        StartCoroutine(MoveCameraToNormalView());
    }

    /// <summary>
    /// 隐藏除选中楼层外的所有楼层
    /// </summary>
    void HideOtherFloors(string selectedFloorName)
    {
        foreach (GameObject floor in floorObjects)
        {
            // 检查是否是选中的楼层
            if (IsFloorMatch(floor.name, selectedFloorName))
            {
                // 选中的楼层保持显示
                floor.SetActive(true);
                Debug.Log($"保持显示楼层: {floor.name}");
            }
            else
            {
                // 其他楼层隐藏
                floor.SetActive(false);
                Debug.Log($"隐藏楼层: {floor.name}");
            }
        }

        // 隐藏屋顶
        if (roofObject != null)
        {
            roofObject.SetActive(false);
            Debug.Log("隐藏屋顶");
        }
    }

    /// <summary>
    /// 显示所有楼层
    /// </summary>
    void ShowAllFloors()
    {
        foreach (GameObject floor in floorObjects)
        {
            floor.SetActive(true);
        }

        // 显示屋顶
        if (roofObject != null)
        {
            roofObject.SetActive(true);
        }
    }

    /// <summary>
    /// 检查楼层名称是否匹配
    /// </summary>
    bool IsFloorMatch(string objectName, string floorName)
    {
        string objLower = objectName.ToLower();
        string floorLower = floorName.ToLower();

        Debug.Log($"匹配检查: 对象名='{objectName}', 楼层名='{floorName}'");

        // 直接比较（例如：对象名"一楼"，楼层名"1楼"）
        // 建立数字和中文的映射
        string[] chineseNumbers = { "一", "二", "三", "四", "五", "六" };
        string[] arabicNumbers = { "1", "2", "3", "4", "5", "6" };
        
        // 将楼层名中的数字转换为中文
        string floorNameWithChinese = floorLower;
        for (int i = 0; i < arabicNumbers.Length; i++)
        {
            floorNameWithChinese = floorNameWithChinese.Replace(arabicNumbers[i], chineseNumbers[i].ToLower());
        }
        
        // 检查对象名是否包含转换后的楼层名
        if (objLower.Contains(floorNameWithChinese.Replace("楼", "")))
        {
            Debug.Log($"  -> 匹配成功（中文数字）");
            return true;
        }
        
        // 将对象名中的中文数字转换为阿拉伯数字进行比较
        string objNameWithArabic = objLower;
        for (int i = 0; i < chineseNumbers.Length; i++)
        {
            objNameWithArabic = objNameWithArabic.Replace(chineseNumbers[i].ToLower(), arabicNumbers[i]);
        }
        
        string floorNumber = floorLower.Replace("楼", "").Replace("f", "").Trim();
        if (objNameWithArabic.Contains(floorNumber))
        {
            Debug.Log($"  -> 匹配成功（阿拉伯数字）");
            return true;
        }

        Debug.Log($"  -> 匹配失败");
        return false;
    }

    /// <summary>
    /// 移动摄像头到隔离视图
    /// </summary>
    IEnumerator MoveCameraToIsolationView()
    {
        if (mainCamera == null)
            yield break;

        isCameraMoving = true;

        // 在移动期间暂时禁用摄像头控制器，避免干扰
        bool wasControllerEnabled = false;
        if (cameraController != null)
        {
            wasControllerEnabled = cameraController.enabled;
            cameraController.enabled = false;
        }

        Vector3 startPos = mainCamera.transform.position;
        Quaternion startRot = mainCamera.transform.rotation;
        Quaternion targetRot = Quaternion.Euler(isolationCameraRotation);

        float elapsed = 0f;

        while (elapsed < cameraTransitionTime)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / cameraTransitionTime;
            
            // 使用平滑插值
            t = Mathf.SmoothStep(0f, 1f, t);

            mainCamera.transform.position = Vector3.Lerp(startPos, isolationCameraPosition, t);
            mainCamera.transform.rotation = Quaternion.Slerp(startRot, targetRot, t);

            yield return null;
        }

        // 确保最终位置准确
        mainCamera.transform.position = isolationCameraPosition;
        mainCamera.transform.rotation = targetRot;

        // 移动完成后重新启用摄像头控制器
        if (cameraController != null && wasControllerEnabled)
        {
            cameraController.enabled = true;
        }

        isCameraMoving = false;
        
        Debug.Log("摄像头移动完成，可以自由控制摄像头");
    }

    /// <summary>
    /// 移动摄像头回到正常视图
    /// </summary>
    IEnumerator MoveCameraToNormalView()
    {
        if (mainCamera == null)
            yield break;

        isCameraMoving = true;

        // 在移动期间暂时禁用摄像头控制器，避免干扰
        bool wasControllerEnabled = false;
        if (cameraController != null)
        {
            wasControllerEnabled = cameraController.enabled;
            cameraController.enabled = false;
        }

        Vector3 startPos = mainCamera.transform.position;
        Quaternion startRot = mainCamera.transform.rotation;

        float elapsed = 0f;

        while (elapsed < cameraTransitionTime)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / cameraTransitionTime;
            
            // 使用平滑插值
            t = Mathf.SmoothStep(0f, 1f, t);

            mainCamera.transform.position = Vector3.Lerp(startPos, savedCameraPosition, t);
            mainCamera.transform.rotation = Quaternion.Slerp(startRot, savedCameraRotation, t);

            yield return null;
        }

        // 确保最终位置准确
        mainCamera.transform.position = savedCameraPosition;
        mainCamera.transform.rotation = savedCameraRotation;

        // 移动完成后重新启用摄像头控制器
        if (cameraController != null && wasControllerEnabled)
        {
            cameraController.enabled = true;
        }

        isCameraMoving = false;
        
        Debug.Log("摄像头已恢复到正常视图");
    }

    void Update()
    {
        // ESC键退出隔离模式
        if (isInIsolationMode && UnityEngine.InputSystem.Keyboard.current != null)
        {
            if (UnityEngine.InputSystem.Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                ExitIsolationMode();
            }
        }
    }

    /// <summary>
    /// 获取当前是否处于隔离模式
    /// </summary>
    public bool IsInIsolationMode()
    {
        return isInIsolationMode;
    }

    /// <summary>
    /// 获取当前选中的楼层
    /// </summary>
    public string GetCurrentSelectedFloor()
    {
        return currentSelectedFloor;
    }
}
