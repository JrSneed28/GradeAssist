using UnityEngine;
using UnityEngine.UI;

public sealed class DiagnosticsPageController : MonoBehaviour
{
    [Header("Display Fields")]
    public Text txtFps = null!;
    public Text txtFrameTime = null!;
    public Text txtBenchmarkStatus = null!;
    public Text txtTelemetryProgress = null!;
    public Text txtGradeAssistVersion = null!;

    [Header("Data Sources")]
    public GradeMonitorSimulator monitor = null!;

    private float fpsAccumulator;
    private int fpsFrameCount;
    private const float FpsUpdateInterval = 0.5f;
    private float fpsTimer;

    private static readonly Color ColorDim = new Color32(0xBB, 0xBB, 0xBB, 0xFF);

    private void Update()
    {
        // FPS calculation
        fpsAccumulator += Time.unscaledDeltaTime;
        fpsFrameCount++;
        fpsTimer += Time.unscaledDeltaTime;

        if (fpsTimer >= FpsUpdateInterval)
        {
            float avgFps = fpsFrameCount / fpsAccumulator;
            float avgMs = (fpsAccumulator / fpsFrameCount) * 1000f;

            if (txtFps != null) txtFps.text = $"{avgFps:0} FPS";
            if (txtFrameTime != null) txtFrameTime.text = $"{avgMs:0.0} ms";

            fpsAccumulator = 0f;
            fpsFrameCount = 0;
            fpsTimer = 0f;
        }

        // Benchmark status
        if (txtBenchmarkStatus != null && monitor != null)
        {
            txtBenchmarkStatus.text = monitor.IsBenchmarkSet
                ? "Benchmark: SET"
                : "Benchmark: NOT SET";
            txtBenchmarkStatus.color = monitor.IsBenchmarkSet
                ? new Color32(0x2A, 0x9D, 0x8F, 0xFF)
                : new Color32(0xF4, 0xA2, 0x61, 0xFF);
        }

        // Telemetry progress
        if (txtTelemetryProgress != null)
        {
            txtTelemetryProgress.text = "Telemetry: Not Available";
            txtTelemetryProgress.color = ColorDim;
        }
        if (txtGradeAssistVersion != null)
            txtGradeAssistVersion.text = "GradeAssist v0.1.0";
    }
}
