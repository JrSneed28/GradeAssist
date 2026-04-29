using UnityEngine;
using UnityEngine.UI;

public sealed class GradeMonitorSimulator : MonoBehaviour
{
    [Header("Math Source")]
    public UnityGradePlane gradePlane = null!;

    [Header("Rig")]
    public MockExcavatorRig rig = null!;

    [Header("Audio Feedback")]
    public GradeAudioFeedback audioFeedback = null!;

    [Header("Settings Fields")]
    public Text txtTargetCutDepth = null!;
    public Text txtSlope = null!;
    public Text txtCrossSlope = null!;
    public Text txtDirection = null!;
    public Image imgDirectionArrow = null!;

    [Header("Live Error")]
    public Text txtLiveError = null!;

    [Header("Status Banner")]
    public Image pnlStatusBanner = null!;
    public Text txtStatus = null!;

    [Header("Footer")]
    public Text txtTolerance = null!;
    public Text txtBenchmark = null!;

    [Header("Grade Parameters (synced to gradePlane)")]
    public float targetCutDepthMeters = 1.5f;
    public float slopePercent = 0f;
    public float crossSlopePercent = 0f;
    public float toleranceMeters = 0.03f;

    // Status colors (color-blind safe palette)
    private static readonly Color ColorOnGrade = new Color32(0x2A, 0x9D, 0x8F, 0xFF);
    private static readonly Color ColorAboveGrade = new Color32(0xE6, 0x39, 0x46, 0xFF);
    private static readonly Color ColorBelowGrade = new Color32(0x45, 0x7B, 0x9D, 0xFF);
    private static readonly Color ColorAmber = new Color32(0xF4, 0xA2, 0x61, 0xFF);
    private static readonly Color ColorDim = new Color32(0xBB, 0xBB, 0xBB, 0xFF);
    private static readonly Color ColorBannerText = Color.white;

    private GradeStatus lastStatus = GradeStatus.OnGrade;
    private float pulseTime;
    private bool benchmarkSet;
    private bool eventSubscribed;

    public bool IsBenchmarkSet => benchmarkSet;
    public GradeStatus CurrentStatus => lastStatus;

    private void Start()
    {
        EnsureGradePlane();
        SyncToGradePlane();
    }

    private void OnDestroy()
    {
        if (gradePlane != null && eventSubscribed)
        {
            gradePlane.OnGradeErrorChanged -= OnGradeErrorChanged;
            eventSubscribed = false;
        }
    }

    private void EnsureGradePlane()
    {
        if (gradePlane == null)
        {
            gradePlane = GetComponent<UnityGradePlane>();
            if (gradePlane == null)
            {
                Debug.LogWarning("[GradeMonitorSimulator] No UnityGradePlane assigned. Creating one dynamically.");
                gradePlane = gameObject.AddComponent<UnityGradePlane>();
            }
        }

        if (gradePlane != null && !eventSubscribed)
        {
            SyncToGradePlane();
            gradePlane.OnGradeErrorChanged += OnGradeErrorChanged;
            eventSubscribed = true;
        }
    }

    private void Update()
    {
        if (rig == null || rig.cuttingEdgeReference == null) return;

        EnsureGradePlane();

        // Global benchmark shortcut (works from any page)
        if (Input.GetKeyDown(KeyCode.B))
        {
            gradePlane.benchmarkPoint = rig.cuttingEdgeReference.position;
            benchmarkSet = true;
        }

        // Sync inspector parameters to gradePlane
        SyncToGradePlane();

        // Evaluate grade plane (fires OnGradeErrorChanged if error changed)
        gradePlane.Evaluate(rig.cuttingEdgeReference.position);

        // Always update frame-driven animations (banner pulse, benchmark flash)
        var error = gradePlane.ComputeError(rig.cuttingEdgeReference.position);
        UpdateFrameAnimations(error);
    }

    private void SyncToGradePlane()
    {
        if (gradePlane == null) return;
        gradePlane.targetCutDepthMeters = targetCutDepthMeters;
        gradePlane.slopePercent = slopePercent;
        gradePlane.crossSlopePercent = crossSlopePercent;
        gradePlane.toleranceMeters = toleranceMeters;
    }

    private void OnGradeErrorChanged(GradeError error)
    {
        var status = error.Status;

        UpdateSettingsDisplay();
        UpdateDirectionDisplay();
        UpdateLiveError(error.ErrorMeters, status);
        UpdateToleranceDisplay();

        if (audioFeedback != null && status != lastStatus)
            audioFeedback.OnGradeStatusChanged(status);

        lastStatus = status;
    }

    private void UpdateFrameAnimations(GradeError error)
    {
        // Always-update items that need smooth per-frame animation
        UpdateStatusBanner(error.ErrorMeters, error.Status);
        UpdateBenchmarkDisplay();
    }

    private void UpdateSettingsDisplay()
    {
        if (txtTargetCutDepth != null)
            txtTargetCutDepth.text = $"{targetCutDepthMeters:0.000} m";
        if (txtSlope != null)
            txtSlope.text = $"{slopePercent:0.00} %";
        if (txtCrossSlope != null)
            txtCrossSlope.text = $"{crossSlopePercent:0.00} %";
    }

    private void UpdateDirectionDisplay()
    {
        if (gradePlane == null) return;
        var gradeDir = gradePlane.gradeDirection;
        var flat = new Vector3(gradeDir.x, 0, gradeDir.z);
        if (flat.sqrMagnitude < 0.0001f) return;

        float yaw = Mathf.Atan2(flat.x, flat.z) * Mathf.Rad2Deg;
        if (imgDirectionArrow != null)
            imgDirectionArrow.rectTransform.rotation = Quaternion.Euler(0, 0, -yaw);

        if (txtDirection != null)
            txtDirection.text = CardinalFromYaw(yaw);
    }

    private static string CardinalFromYaw(float yaw)
    {
        yaw = NormalizeYaw(yaw);
        if (yaw >= -22.5f && yaw < 22.5f) return "N";
        if (yaw >= 22.5f && yaw < 67.5f) return "NE";
        if (yaw >= 67.5f && yaw < 112.5f) return "E";
        if (yaw >= 112.5f && yaw < 157.5f) return "SE";
        if (yaw >= 157.5f || yaw < -157.5f) return "S";
        if (yaw >= -157.5f && yaw < -112.5f) return "SW";
        if (yaw >= -112.5f && yaw < -67.5f) return "W";
        return "NW";
    }

    private static float NormalizeYaw(float yaw)
    {
        while (yaw > 180f) yaw -= 360f;
        while (yaw <= -180f) yaw += 360f;
        return yaw;
    }

    private void UpdateLiveError(float error, GradeStatus status)
    {
        if (txtLiveError == null) return;

        string prefix = status switch
        {
            GradeStatus.OnGrade => "= ",
            GradeStatus.AboveGrade => "▲ ",
            GradeStatus.BelowGrade => "▼ ",
            _ => ""
        };

        txtLiveError.text = $"{prefix}{error:+0.000;-0.000;0.000} m";
        txtLiveError.color = StatusToColor(status);
    }

    private void UpdateStatusBanner(float error, GradeStatus status)
    {
        if (pnlStatusBanner == null || txtStatus == null) return;

        string statusText = status switch
        {
            GradeStatus.OnGrade => "ON GRADE",
            GradeStatus.AboveGrade => "ABOVE GRADE",
            GradeStatus.BelowGrade => "BELOW GRADE",
            _ => "UNKNOWN"
        };

        txtStatus.text = statusText;
        txtStatus.color = ColorBannerText;

        Color bannerColor = StatusToColor(status);

        // Pulsing alpha when off-grade
        if (status != GradeStatus.OnGrade)
        {
            pulseTime += Time.deltaTime;
            float alpha = Mathf.Lerp(0.85f, 1.0f, Mathf.PingPong(pulseTime * 2f, 1f));
            bannerColor.a = alpha;
        }
        else
        {
            bannerColor.a = 1.0f;
            pulseTime = 0f;
        }

        pnlStatusBanner.color = bannerColor;
    }

    private void UpdateToleranceDisplay()
    {
        if (txtTolerance != null)
        {
            txtTolerance.text = $"Tolerance: {toleranceMeters:0.000} m";
            txtTolerance.color = ColorDim;
        }
    }

    private void UpdateBenchmarkDisplay()
    {
        if (txtBenchmark == null) return;

        if (benchmarkSet)
        {
            txtBenchmark.text = "BENCHMARK: SET";
            txtBenchmark.color = ColorOnGrade;
        }
        else
        {
            txtBenchmark.text = "BENCHMARK: NOT SET";
            float alpha = Mathf.Lerp(0.6f, 1.0f, Mathf.PingPong(Time.time, 1f));
            txtBenchmark.color = new Color(ColorAmber.r, ColorAmber.g, ColorAmber.b, alpha);
        }
    }

    private static Color StatusToColor(GradeStatus status) => status switch
    {
        GradeStatus.OnGrade => ColorOnGrade,
        GradeStatus.AboveGrade => ColorAboveGrade,
        GradeStatus.BelowGrade => ColorBelowGrade,
        _ => ColorDim
    };
}
