using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// AlertSystem 测试工具
/// 按 T 键手动触发警报
/// 按 D 键解除当前警报
/// 按 S 键启动/停止系统
/// </summary>
public class AlertSystemTester : MonoBehaviour
{
    private AlertSystem alertSystem;
    private bool systemStarted = false;

    void Start()
    {
        // 查找 AlertSystem
        alertSystem = FindObjectOfType<AlertSystem>();
        
        if (alertSystem == null)
        {
            Debug.LogError("AlertSystemTester: 未找到 AlertSystem 组件！");
            enabled = false;
            return;
        }

        Debug.Log("=== AlertSystem 测试工具已启动 ===");
        Debug.Log("按 T 键 - 手动触发随机警报");
        Debug.Log("按 D 键 - 解除当前警报");
        Debug.Log("按 S 键 - 启动/停止警报系统");
        Debug.Log("按 I 键 - 显示系统信息");
        Debug.Log("================================");
        
        systemStarted = true;
    }

    void Update()
    {
        if (alertSystem == null) return;

        Keyboard keyboard = Keyboard.current;
        if (keyboard == null) return;

        // 按 T 键触发随机警报
        if (keyboard.tKey.wasPressedThisFrame)
        {
            TriggerRandomAlert();
        }

        // 按 D 键解除警报
        if (keyboard.dKey.wasPressedThisFrame)
        {
            DismissAlert();
        }

        // 按 S 键启动/停止系统
        if (keyboard.sKey.wasPressedThisFrame)
        {
            ToggleSystem();
        }

        // 按 I 键显示信息
        if (keyboard.iKey.wasPressedThisFrame)
        {
            ShowSystemInfo();
        }
    }

    void TriggerRandomAlert()
    {
        // 获取所有标记为 Equipment 的对象
        GameObject[] equipment = GameObject.FindGameObjectsWithTag("Equipment");
        
        if (equipment.Length == 0)
        {
            Debug.LogError("测试失败：场景中没有标记为 'Equipment' 的对象！");
            return;
        }

        // 随机选择一个设备
        GameObject randomEquipment = equipment[Random.Range(0, equipment.Length)];
        
        Debug.Log($"<color=cyan>手动触发警报：{randomEquipment.name}</color>");
        
        // 触发警报
        alertSystem.TriggerAlertManually(randomEquipment);
    }

    void DismissAlert()
    {
        Debug.Log("<color=yellow>手动解除警报</color>");
        alertSystem.DismissCurrentAlert();
    }

    void ToggleSystem()
    {
        if (systemStarted)
        {
            Debug.Log("<color=red>停止警报系统</color>");
            alertSystem.StopAlertSystem();
            systemStarted = false;
        }
        else
        {
            Debug.Log("<color=green>启动警报系统</color>");
            alertSystem.StartAlertSystem();
            systemStarted = true;
        }
    }

    void ShowSystemInfo()
    {
        Debug.Log("=== AlertSystem 状态信息 ===");
        Debug.Log($"当前状态: {alertSystem.GetCurrentState()}");
        
        GameObject currentEquipment = alertSystem.GetCurrentAlertEquipment();
        if (currentEquipment != null)
        {
            Debug.Log($"当前报警设备: {currentEquipment.name}");
        }
        else
        {
            Debug.Log("当前报警设备: 无");
        }
        
        GameObject[] equipment = GameObject.FindGameObjectsWithTag("Equipment");
        Debug.Log($"场景中设备数量: {equipment.Length}");
        Debug.Log("============================");
    }

    // 屏幕提示已隐藏 - 如需显示，取消注释下面的代码
    /*
    void OnGUI()
    {
        // 在屏幕上显示控制提示
        GUIStyle style = new GUIStyle();
        style.fontSize = 16;
        style.normal.textColor = Color.white;
        style.padding = new RectOffset(10, 10, 10, 10);

        string info = 
            "=== AlertSystem 测试工具 ===\n" +
            "T - 手动触发警报\n" +
            "D - 解除当前警报\n" +
            "S - 启动/停止系统\n" +
            "I - 显示系统信息\n" +
            $"当前状态: {alertSystem.GetCurrentState()}";

        GUI.Label(new Rect(10, Screen.height - 150, 300, 150), info, style);
    }
    */
}
