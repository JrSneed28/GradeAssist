using UnityEngine;

public sealed class RenderTextureMonitorBinder : MonoBehaviour
{
    public Camera uiCamera = null!;
    public Renderer screenRenderer = null!;
    public int width = 1024;
    public int height = 600;

    [Range(0.5f, 3.0f)]
    public float emissionIntensity = 1.3f;

    private RenderTexture? renderTexture;

    private void Start()
    {
        if (uiCamera == null || screenRenderer == null)
        {
            Debug.LogWarning("[RenderTextureMonitorBinder] uiCamera or screenRenderer is not assigned.");
            return;
        }

        renderTexture = new RenderTexture(width, height, 24)
        {
            name = "GradeAssist_MonitorRT"
        };

        uiCamera.targetTexture = renderTexture;

        var material = new Material(screenRenderer.sharedMaterial);
        if (material.HasProperty("_MainTex")) material.SetTexture("_MainTex", renderTexture);
        if (material.HasProperty("_BaseMap")) material.SetTexture("_BaseMap", renderTexture);
        if (material.HasProperty("_EmissionMap")) material.SetTexture("_EmissionMap", renderTexture);
        if (material.HasProperty("_EmissionColor")) material.SetColor("_EmissionColor", Color.white * emissionIntensity);
        screenRenderer.material = material;

        ValidateOrthographicSize();
    }

    private void OnValidate()
    {
        ValidateOrthographicSize();
    }

    private void ValidateOrthographicSize()
    {
        if (uiCamera == null) return;
        if (!uiCamera.orthographic) return;

        float expectedSize = height / 2f;
        if (!Mathf.Approximately(uiCamera.orthographicSize, expectedSize))
        {
            Debug.LogWarning($"[RenderTextureMonitorBinder] Orthographic size ({uiCamera.orthographicSize}) does not match expected ({expectedSize}) for RT height ({height}). 1:1 pixel mapping may be broken.");
        }
    }

    private void OnDestroy()
    {
        if (renderTexture != null)
        {
            renderTexture.Release();
            Destroy(renderTexture);
        }
    }
}
