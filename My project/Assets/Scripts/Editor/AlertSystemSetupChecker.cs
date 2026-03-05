using UnityEngine;
using UnityEditor;

/// <summary>
/// AlertSystem 设置检查工具
/// 帮助诊断 AlertSystem 无法运行的问题
/// </summary>
public class AlertSystemSetupChecker : EditorWindow
{
    [MenuItem("Tools/检查 AlertSystem 设置")]
    public static void ShowWindow()
    {
        GetWindow<AlertSystemSetupChecker>("AlertSystem 设置检查");
    }

    private Vector2 scrollPosition;

    void OnGUI()
    {
        GUILayout.Label("AlertSystem 设置检查", EditorStyles.boldLabel);
        GUILayout.Space(10);

        scrollPosition = GUILayout.BeginScrollView(scrollPosition);

        // 检查 AlertSystem 组件
        CheckAlertSystem();
        GUILayout.Space(10);

        // 检查 FloorController
        CheckFloorController();
        GUILayout.Space(10);

        // 检查主摄像头
        CheckMainCamera();
        GUILayout.Space(10);

        // 检查摄像头控制器
        CheckCameraControllers();
        GUILayout.Space(10);

        // 检查设备对象
        CheckEquipment();
        GUILayout.Space(10);

        // 检查楼层结构
        CheckFloorStructure();

        GUILayout.EndScrollView();

        GUILayout.Space(20);
        if (GUILayout.Button("刷新检查", GUILayout.Height(30)))
        {
            Repaint();
        }
    }

    void CheckAlertSystem()
    {
        GUILayout.Label("1. AlertSystem 组件", EditorStyles.boldLabel);
        
        AlertSystem alertSystem = FindObjectOfType<AlertSystem>();
        if (alertSystem != null)
        {
            ShowSuccess($"✓ 找到 AlertSystem 组件在: {alertSystem.gameObject.name}");
            
            if (alertSystem.enabled)
            {
                ShowSuccess("✓ AlertSystem 组件已启用");
            }
            else
            {
                ShowError("✗ AlertSystem 组件已禁用！");
                ShowInfo("  可能原因：初始化失败，请检查 Console 日志");
            }
        }
        else
        {
            ShowError("✗ 场景中未找到 AlertSystem 组件！");
            ShowInfo("  解决方法：创建一个 GameObject 并添加 AlertSystem 脚本");
        }
    }

    void CheckFloorController()
    {
        GUILayout.Label("2. FloorController 组件", EditorStyles.boldLabel);
        
        FloorController floorController = FindObjectOfType<FloorController>();
        if (floorController != null)
        {
            ShowSuccess($"✓ 找到 FloorController 组件在: {floorController.gameObject.name}");
        }
        else
        {
            ShowError("✗ 场景中未找到 FloorController 组件！");
            ShowInfo("  AlertSystem 需要 FloorController 才能运行");
        }
    }

    void CheckMainCamera()
    {
        GUILayout.Label("3. 主摄像头", EditorStyles.boldLabel);
        
        Camera mainCamera = Camera.main;
        if (mainCamera != null)
        {
            ShowSuccess($"✓ 找到主摄像头: {mainCamera.gameObject.name}");
            
            if (mainCamera.gameObject.tag == "MainCamera")
            {
                ShowSuccess("✓ 摄像头标签正确设置为 'MainCamera'");
            }
            else
            {
                ShowWarning($"⚠ 摄像头标签是 '{mainCamera.gameObject.tag}'，应该是 'MainCamera'");
            }
        }
        else
        {
            ShowError("✗ 场景中未找到主摄像头！");
            ShowInfo("  解决方法：确保场景中有一个标记为 'MainCamera' 的摄像头");
        }
    }

    void CheckCameraControllers()
    {
        GUILayout.Label("4. 摄像头控制器", EditorStyles.boldLabel);
        
        Camera mainCamera = Camera.main;
        if (mainCamera == null)
        {
            ShowWarning("⚠ 无法检查摄像头控制器（主摄像头未找到）");
            return;
        }

        int controllerCount = 0;
        
        if (mainCamera.GetComponent<CameraController>() != null)
        {
            ShowSuccess("✓ 找到 CameraController");
            controllerCount++;
        }
        
        if (mainCamera.GetComponent<SimpleCameraController>() != null)
        {
            ShowSuccess("✓ 找到 SimpleCameraController");
            controllerCount++;
        }
        
        if (mainCamera.GetComponent<SceneViewCameraController>() != null)
        {
            ShowSuccess("✓ 找到 SceneViewCameraController");
            controllerCount++;
        }

        if (controllerCount == 0)
        {
            ShowWarning("⚠ 未找到任何摄像头控制器");
            ShowInfo("  警报期间无法禁用用户输入，但系统可以运行");
        }
        else
        {
            ShowSuccess($"✓ 共找到 {controllerCount} 个摄像头控制器");
        }
    }

    void CheckEquipment()
    {
        GUILayout.Label("5. 设备对象", EditorStyles.boldLabel);
        
        GameObject[] equipment = GameObject.FindGameObjectsWithTag("Equipment");
        
        if (equipment.Length > 0)
        {
            ShowSuccess($"✓ 找到 {equipment.Length} 个设备对象");
            
            foreach (GameObject eq in equipment)
            {
                ShowInfo($"  - {eq.name}");
            }
        }
        else
        {
            ShowError("✗ 场景中未找到任何标记为 'Equipment' 的对象！");
            ShowInfo("  解决方法：");
            ShowInfo("  1. 选择设备 GameObject");
            ShowInfo("  2. 在 Inspector 中设置 Tag 为 'Equipment'");
            ShowInfo("  3. 确保设备在楼层层级结构下");
        }
    }

    void CheckFloorStructure()
    {
        GUILayout.Label("6. 楼层结构", EditorStyles.boldLabel);
        
        string[] floorPatterns = { "一楼", "二楼", "三楼", "四楼", "五楼", "六楼",
                                   "1楼", "2楼", "3楼", "4楼", "5楼", "6楼",
                                   "floor", "Floor" };
        
        GameObject[] allObjects = FindObjectsOfType<GameObject>();
        int floorCount = 0;
        
        foreach (GameObject obj in allObjects)
        {
            string name = obj.name.ToLower();
            foreach (string pattern in floorPatterns)
            {
                if (name.Contains(pattern.ToLower()))
                {
                    floorCount++;
                    ShowInfo($"  - 找到楼层: {obj.name}");
                    break;
                }
            }
        }
        
        if (floorCount > 0)
        {
            ShowSuccess($"✓ 找到 {floorCount} 个楼层对象");
        }
        else
        {
            ShowWarning("⚠ 未找到任何楼层对象");
            ShowInfo("  楼层名称应包含：一楼/二楼/1楼/2楼/floor 等");
        }
    }

    void ShowSuccess(string message)
    {
        GUIStyle style = new GUIStyle(EditorStyles.label);
        style.normal.textColor = Color.green;
        GUILayout.Label(message, style);
    }

    void ShowError(string message)
    {
        GUIStyle style = new GUIStyle(EditorStyles.label);
        style.normal.textColor = Color.red;
        GUILayout.Label(message, style);
    }

    void ShowWarning(string message)
    {
        GUIStyle style = new GUIStyle(EditorStyles.label);
        style.normal.textColor = new Color(1f, 0.5f, 0f); // Orange
        GUILayout.Label(message, style);
    }

    void ShowInfo(string message)
    {
        GUIStyle style = new GUIStyle(EditorStyles.label);
        style.normal.textColor = Color.gray;
        GUILayout.Label(message, style);
    }
}
