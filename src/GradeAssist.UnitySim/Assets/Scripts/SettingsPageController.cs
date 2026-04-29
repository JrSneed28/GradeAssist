using UnityEngine;
using UnityEngine.UI;

public enum SettingsField { Depth, Slope, CrossSlope, Tolerance }

public sealed class SettingsPageController : MonoBehaviour
{
    [Header("Value Displays")]
    public Text txtStagedDepth = null!;
    public Text txtStagedSlope = null!;
    public Text txtStagedCrossSlope = null!;
    public Text txtStagedTolerance = null!;

    [Header("Buttons")]
    public Button btnDepthPlus = null!;
    public Button btnDepthMinus = null!;
    public Button btnSlopePlus = null!;
    public Button btnSlopeMinus = null!;
    public Button btnCrossSlopePlus = null!;
    public Button btnCrossSlopeMinus = null!;
    public Button btnTolerancePlus = null!;
    public Button btnToleranceMinus = null!;
    public Button btnApply = null!;
    public Button btnCancel = null!;

    [Header("Target Script")]
    public GradeMonitorSimulator monitor = null!;

    [Header("Page Manager")]
    public MonitorPageManager pageManager = null!;

    private float stagedDepth;
    private float stagedSlope;
    private float stagedCrossSlope;
    private float stagedTolerance;
    private SettingsField selectedField = SettingsField.Depth;

    // Colors
    private static readonly Color ColorStaged = new Color32(0xF4, 0xA2, 0x61, 0xFF); // amber
    private static readonly Color ColorApplied = Color.white;
    private static readonly Color ColorSelected = new Color32(0x2D, 0x5F, 0x8A, 0xFF); // blue highlight

    private void OnEnable()
    {
        if (monitor == null) return;

        stagedDepth = monitor.targetCutDepthMeters;
        stagedSlope = monitor.slopePercent;
        stagedCrossSlope = monitor.crossSlopePercent;
        stagedTolerance = monitor.toleranceMeters;
        selectedField = SettingsField.Depth;

        UpdateDisplays();
    }

    private void Update()
    {
        // Keyboard shortcuts for settings page
        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
            OnApply();
        // Esc is handled globally by MonitorPageManager — do NOT duplicate here

        // Field selection cycling
        if (Input.GetKeyDown(KeyCode.UpArrow))
            CycleField(-1);
        if (Input.GetKeyDown(KeyCode.DownArrow))
            CycleField(+1);

        // Nudge selected field
        if (Input.GetKeyDown(KeyCode.Equals) || Input.GetKeyDown(KeyCode.KeypadPlus))
            NudgeSelected(increment: true);
        if (Input.GetKeyDown(KeyCode.Minus) || Input.GetKeyDown(KeyCode.KeypadMinus))
            NudgeSelected(increment: false);
    }

    private void CycleField(int direction)
    {
        int count = System.Enum.GetValues(typeof(SettingsField)).Length;
        int next = ((int)selectedField + direction + count) % count;
        selectedField = (SettingsField)next;
        UpdateDisplays();
    }

    private void NudgeSelected(bool increment)
    {
        switch (selectedField)
        {
            case SettingsField.Depth: OnDepthNudge(increment); break;
            case SettingsField.Slope: OnSlopeNudge(increment); break;
            case SettingsField.CrossSlope: OnCrossSlopeNudge(increment); break;
            case SettingsField.Tolerance: OnToleranceNudge(increment); break;
        }
    }

    public void OnDepthNudge(bool increment)
    {
        selectedField = SettingsField.Depth;
        stagedDepth += increment ? 0.01f : -0.01f;
        stagedDepth = Mathf.Max(0f, stagedDepth);
        UpdateDisplays();
    }

    public void OnSlopeNudge(bool increment)
    {
        selectedField = SettingsField.Slope;
        stagedSlope += increment ? 0.1f : -0.1f;
        stagedSlope = Mathf.Clamp(stagedSlope, -500f, 500f);
        UpdateDisplays();
    }

    public void OnCrossSlopeNudge(bool increment)
    {
        selectedField = SettingsField.CrossSlope;
        stagedCrossSlope += increment ? 0.1f : -0.1f;
        stagedCrossSlope = Mathf.Clamp(stagedCrossSlope, -500f, 500f);
        UpdateDisplays();
    }

    public void OnToleranceNudge(bool increment)
    {
        selectedField = SettingsField.Tolerance;
        stagedTolerance += increment ? 0.005f : -0.005f;
        stagedTolerance = Mathf.Max(0.001f, stagedTolerance);
        UpdateDisplays();
    }

    public void OnApply()
    {
        if (monitor == null) return;

        monitor.targetCutDepthMeters = stagedDepth;
        monitor.slopePercent = stagedSlope;
        monitor.crossSlopePercent = stagedCrossSlope;
        monitor.toleranceMeters = stagedTolerance;

        // Auto-return to Work page
        if (pageManager != null)
            pageManager.ShowPage(pageManager.pageWork);
    }

    public void OnCancel()
    {
        // Discard staged values and return to Work page
        if (pageManager != null)
            pageManager.ShowPage(pageManager.pageWork);
    }

    private void UpdateDisplays()
    {
        if (monitor == null) return;

        UpdateField(txtStagedDepth, stagedDepth, monitor.targetCutDepthMeters, "0.000", " m", SettingsField.Depth);
        UpdateField(txtStagedSlope, stagedSlope, monitor.slopePercent, "0.00", " %", SettingsField.Slope);
        UpdateField(txtStagedCrossSlope, stagedCrossSlope, monitor.crossSlopePercent, "0.00", " %", SettingsField.CrossSlope);
        UpdateField(txtStagedTolerance, stagedTolerance, monitor.toleranceMeters, "0.000", " m", SettingsField.Tolerance);
    }

    private void UpdateField(Text? text, float staged, float applied, string format, string unit, SettingsField field)
    {
        if (text == null) return;
        text.text = $"{staged.ToString(format)}{unit}";

        if (field == selectedField)
            text.color = ColorSelected;
        else if (!Mathf.Approximately(staged, applied))
            text.color = ColorStaged;
        else
            text.color = ColorApplied;
    }
}
