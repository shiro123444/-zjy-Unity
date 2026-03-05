using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Build Settings 辅助工具 - 自动添加场景到构建配置
/// </summary>
public class BuildSettingsHelper : EditorWindow
{
    [MenuItem("Tools/自动添加所有场景到 Build Settings")]
    public static void AddAllScenesToBuildSettings()
    {
        // 查找所有场景文件
        string[] sceneGuids = AssetDatabase.FindAssets("t:Scene", new[] { "Assets/Scenes" });
        
        if (sceneGuids.Length == 0)
        {
            Debug.LogWarning("未找到任何场景文件");
            return;
        }

        List<EditorBuildSettingsScene> scenes = new List<EditorBuildSettingsScene>();
        
        foreach (string guid in sceneGuids)
        {
            string scenePath = AssetDatabase.GUIDToAssetPath(guid);
            EditorBuildSettingsScene scene = new EditorBuildSettingsScene(scenePath, true);
            scenes.Add(scene);
            Debug.Log($"添加场景到 Build Settings: {scenePath}");
        }

        // 更新 Build Settings
        EditorBuildSettings.scenes = scenes.ToArray();
        
        Debug.Log($"成功添加 {scenes.Count} 个场景到 Build Settings");
        
        // 显示结果
        ShowBuildSettingsWindow();
    }

    [MenuItem("Tools/查看 Build Settings 中的场景")]
    public static void ShowBuildSettingsWindow()
    {
        // 打开 Build Settings 窗口
        EditorWindow.GetWindow(System.Type.GetType("UnityEditor.BuildPlayerWindow,UnityEditor"));
    }

    [MenuItem("Tools/清除 Build Settings 中的所有场景")]
    public static void ClearBuildSettings()
    {
        if (EditorUtility.DisplayDialog("确认", "确定要清除 Build Settings 中的所有场景吗？", "确定", "取消"))
        {
            EditorBuildSettings.scenes = new EditorBuildSettingsScene[0];
            Debug.Log("已清除 Build Settings 中的所有场景");
        }
    }
}
