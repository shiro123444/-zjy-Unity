using UnityEngine;

/// <summary>
/// 选中波纹效果 - 创建一个动态扩散的圆环
/// </summary>
public class SelectionRipple : MonoBehaviour
{
    private Material rippleMaterial;
    private Color rippleColor;
    private float maxSize;
    private float currentSize;
    private float pulseSpeed = 2f;
    private LineRenderer lineRenderer;

    /// <summary>
    /// 初始化波纹
    /// </summary>
    public void Initialize(Color color, float size, Material material = null)
    {
        rippleColor = color;
        maxSize = size;
        currentSize = 0f;

        CreateRipple(material);
    }

    /// <summary>
    /// 创建波纹效果
    /// </summary>
    void CreateRipple(Material material)
    {
        // 使用 LineRenderer 创建圆环
        lineRenderer = gameObject.AddComponent<LineRenderer>();
        
        // 设置材质
        if (material != null)
        {
            lineRenderer.material = material;
        }
        else
        {
            // 使用 Unlit/Color shader 以便在 URP 中正常显示
            Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null)
                shader = Shader.Find("Unlit/Color");
            lineRenderer.material = new Material(shader);
        }

        // 设置颜色
        lineRenderer.startColor = rippleColor;
        lineRenderer.endColor = rippleColor;

        // 设置宽度
        lineRenderer.startWidth = 0.15f;
        lineRenderer.endWidth = 0.15f;

        // 设置为圆环
        lineRenderer.loop = true;
        lineRenderer.useWorldSpace = false;

        // 创建圆形
        int segments = 64;
        lineRenderer.positionCount = segments;

        UpdateRippleSize(maxSize * 0.8f);
    }

    void Update()
    {
        if (lineRenderer == null) return;

        // 脉动效果
        float pulse = Mathf.Sin(Time.time * pulseSpeed) * 0.2f + 0.8f;
        float targetSize = maxSize * pulse;
        
        UpdateRippleSize(targetSize);

        // 透明度脉动
        Color color = rippleColor;
        color.a = rippleColor.a * pulse;
        lineRenderer.startColor = color;
        lineRenderer.endColor = color;
    }

    /// <summary>
    /// 更新波纹大小 - 在物体周围的水平面上
    /// </summary>
    void UpdateRippleSize(float size)
    {
        if (lineRenderer == null) return;

        int segments = lineRenderer.positionCount;
        float angleStep = 360f / segments;

        for (int i = 0; i < segments; i++)
        {
            float angle = i * angleStep * Mathf.Deg2Rad;
            float x = Mathf.Cos(angle) * size;
            float z = Mathf.Sin(angle) * size;
            
            // 在物体底部创建圆环
            lineRenderer.SetPosition(i, new Vector3(x, 0f, z));
        }
    }
}
