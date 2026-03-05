using UnityEngine;

/// <summary>
/// 选中波纹效果 - 在物体表面创建动态扩散的圆环
/// </summary>
public class SelectionRipple : MonoBehaviour
{
    private Color rippleColor;
    private float maxSize;
    private float pulseSpeed = 2f;
    private GameObject[] rippleRings;
    private int ringCount = 3;

    /// <summary>
    /// 初始化波纹
    /// </summary>
    public void Initialize(Color color, float size, Material material = null)
    {
        rippleColor = color;
        maxSize = size;

        CreateRippleRings();
    }

    /// <summary>
    /// 创建多个波纹环
    /// </summary>
    void CreateRippleRings()
    {
        rippleRings = new GameObject[ringCount];

        for (int i = 0; i < ringCount; i++)
        {
            GameObject ring = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            ring.name = $"RippleRing_{i}";
            ring.transform.SetParent(transform);
            ring.transform.localPosition = Vector3.zero;
            
            // 移除 Collider
            Collider col = ring.GetComponent<Collider>();
            if (col != null)
                Destroy(col);

            // 设置材质
            Renderer renderer = ring.GetComponent<Renderer>();
            if (renderer != null)
            {
                Material mat = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
                mat.color = rippleColor;
                // 启用透明
                mat.SetFloat("_Surface", 1); // Transparent
                mat.SetFloat("_Blend", 0); // Alpha
                mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                mat.SetInt("_ZWrite", 0);
                mat.renderQueue = 3000;
                renderer.material = mat;
            }

            // 扁平化圆柱体使其像圆环
            ring.transform.localScale = new Vector3(maxSize * 0.5f, 0.01f, maxSize * 0.5f);

            rippleRings[i] = ring;
        }
    }

    void Update()
    {
        if (rippleRings == null || rippleRings.Length == 0) return;

        float time = Time.time * pulseSpeed;

        for (int i = 0; i < rippleRings.Length; i++)
        {
            if (rippleRings[i] == null) continue;

            // 每个环有不同的相位
            float phase = (float)i / ringCount * Mathf.PI * 2;
            float pulse = Mathf.Sin(time + phase) * 0.3f + 0.7f;
            
            // 缩放
            float scale = maxSize * pulse * (1f + i * 0.2f);
            rippleRings[i].transform.localScale = new Vector3(scale, 0.01f, scale);

            // 透明度
            Renderer renderer = rippleRings[i].GetComponent<Renderer>();
            if (renderer != null)
            {
                Color color = rippleColor;
                color.a = rippleColor.a * pulse * (1f - (float)i / ringCount * 0.5f);
                renderer.material.color = color;
            }
        }
    }

    void OnDestroy()
    {
        // 清理材质
        if (rippleRings != null)
        {
            foreach (var ring in rippleRings)
            {
                if (ring != null)
                {
                    Renderer renderer = ring.GetComponent<Renderer>();
                    if (renderer != null && renderer.material != null)
                    {
                        Destroy(renderer.material);
                    }
                }
            }
        }
    }
}
