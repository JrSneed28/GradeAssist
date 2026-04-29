# Section 5: Cab Monitor UI and RenderTexture System

## 5.1 RenderTexture Setup

The monitor display is driven by a `RenderTexture` bound to a dedicated UI camera. Configuration:

| Property | Value | Notes |
|----------|-------|-------|
| Width | 1024 | Matches `RenderTextureMonitorBinder.width` |
| Height | 600 | Matches `RenderTextureMonitorBinder.height` |
| Color Format | ARGB32 | Standard 32-bit color with alpha for UI transparency |
| Depth Buffer | 24 | Depth 24 for proper z-sorting of overlapping UI elements |
| Filter Mode | Bilinear | Smooth scaling when the screen mesh is viewed at angles |
| Anti-aliasing | None | Disabled to reduce GPU cost; UI is pixel-precise |
| Auto Generate Mip Maps | False | UI art does not benefit from mipmapping |

The `RenderTextureMonitorBinder` MonoBehaviour creates the `RenderTexture` at runtime in `Start()` and assigns it to `uiCamera.targetTexture`. It then clones the screen mesh's material and binds the texture to `_MainTex`, `_BaseMap`, and `_EmissionMap` (with emission boost for visibility in dark cab environments).

> **Recommendation:** Ensure the monitor screen mesh uses an Unlit or simple Lit material with emission so the display is visible under low-light scene conditions.

---

## 5.2 UI Camera Configuration

A dedicated `Camera` GameObject (`Monitor_UI_Camera`) renders the monitor UI to the RenderTexture. It must be configured as follows:

| Property | Value | Rationale |
|----------|-------|-----------|
| Projection | Orthographic | UI is 2D; perspective distortion is undesirable |
| Size | 300 | Half of render height (600) for 1:1 world-to-pixel mapping at native resolution |
| Culling Mask | UI only | Isolates monitor UI from world geometry |
| Clear Flags | Solid Color | Prevents scene bleed-through; use opaque background color (`#1A1A1A`) |
| Background | `#1A1A1A` (near-black) | Matches industrial cab monitor aesthetic |
| Target Texture | `GradeAssist_MonitorRT` | Assigned by `RenderTextureMonitorBinder` at runtime |
| Depth | 0 | Only camera in UI layer; ordering is irrelevant |
| HDR | Disabled | Unnecessary for UI; reduces bandwidth |
| MSAA | Disabled | UI does not benefit from multisampling |

> **Important:** The UI camera must not be the same as the main scene camera. It is a child of the `CabMonitor` prefab root and inactive in the scene hierarchy until the monitor is powered on.

---

## 5.3 Monitor Page System

### 5.3.1 `MonitorPageManager`

A `MonoBehaviour` placed on the root `Canvas` GameObject (`Monitor_Canvas`) controls which page is visible.

```csharp
public sealed class MonitorPageManager : MonoBehaviour
{
    public GameObject grade2DPage = null!;
    public GameObject settingsPage = null!;
    public GameObject diagnosticsPage = null!;

    private GameObject? currentPage;

    private void Start()
    {
        ShowPage(grade2DPage);
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.M))
        {
            CyclePage();
        }
    }

    public void ShowPage(GameObject? page)
    {
        if (currentPage != null) currentPage.SetActive(false);
        currentPage = page;
        if (currentPage != null) currentPage.SetActive(true);
    }

    private void CyclePage()
    {
        if (currentPage == grade2DPage) ShowPage(settingsPage);
        else if (currentPage == settingsPage) ShowPage(diagnosticsPage);
        else ShowPage(grade2DPage);
    }
}
```

### 5.3.2 Page GameObjects

Each page is a child of the `Canvas` with a full-screen `RectTransform` (anchor min/max set to stretch) and a `VerticalLayoutGroup` or manually placed UI elements.

| Page GameObject | Purpose | Default Visibility |
|-----------------|---------|-------------------|
| `Page_Grade2D` | Primary operating display with live grade data | Active |
| `Page_Settings` | Target depth, slope, cross-slope, tolerance input | Inactive |
| `Page_Diagnostics` | FPS, telemetry frame count, benchmark status | Inactive |

### 5.3.3 Tab Navigation

Tab buttons are placed in a header row at the top of the canvas (y = 560, height = 40). Each tab is a `Button` with an `Image` background and a `Text` label. Clicking a tab invokes `MonitorPageManager.ShowPage(targetPage)`.

| Tab | Label | Key Binding |
|-----|-------|-------------|
| Tab 1 | Work | M (cycles to Target) |
| Tab 2 | Target | M (cycles to System) |
| Tab 3 | System | M (cycles to Work) |

Tab visuals:
- Active tab: background `#2D5F8A` (blue), text white, 2px bottom border
- Inactive tab: background `#333333`, text `#AAAAAA`

---

## 5.4 Grade 2D Page Layout

### 5.4.1 Mock Layout Description

```text
+----------------------------------------------------------+
| [Grade]  [Settings]  [Diagnostics]          GradeAssist  |
+----------------------------------------------------------+
|                                                          |
|   TARGET CUT DEPTH                    SLOPE              |
|   1.500 m                             0.00 %             |
|                                                          |
|   CROSS-SLOPE                         GRADE DIRECTION    |
|   0.00 %                                 N               |
|                                                         |
|   +--------------------------------------------------+   |
|   |                                                  |   |
|   |           LIVE ERROR                             |   |
|   |           +0.023 m                               |   |
|   |                                                  |   |
|   |           [  ABOVE GRADE  ]                     |   |
|   |                                                  |   |
|   +--------------------------------------------------+   |
|                                                          |
|   TOLERANCE: 0.030 m          BENCHMARK: SET           |
|                                                          |
+----------------------------------------------------------+
```

### 5.4.2 Field Specifications

| Field | Data Type | Display Format | Color Coding | GameObject Name |
|-------|-----------|----------------|--------------|-----------------|
| Target cut depth | float | `0.000 m` (3 decimals) | White | `Txt_TargetCutDepth` |
| Slope % | float | `0.00 %` (2 decimals) | White | `Txt_Slope` |
| Cross-slope % | float | `0.00 %` (2 decimals) | White | `Txt_CrossSlope` |
| Grade direction | Vector3 | Compass arrow or cardinal text (`N`, `NE`, `E`, etc.) | White | `Img_DirectionArrow` + `Txt_Direction` |
| Live error | float | `▲ +0.000` / `▼ -0.000` / `= 0.000` m (3 decimals) | Green / Red / Blue text + prefix symbol | `Txt_LiveError` |
| Status indicator | string | `ON GRADE`, `ABOVE GRADE`, `BELOW GRADE` | Green (#1DB954) / Red (#E63946) / Blue (#457B9D) banner, white bold text, pulsing when off-grade | `Pnl_StatusBanner` + `Txt_Status` |
| Tolerance | float | `0.000 m` (3 decimals) | `#BBBBBB` (dim) | `Txt_Tolerance` |
| Benchmark set | bool | `SET` (green) / `NOT SET` (amber) | `#1DB954` / `#F4A261` with gentle flash | `Txt_Benchmark` |

### 5.4.3 Grade Direction Visual

The grade direction is displayed as a compass arrow (`Img_DirectionArrow`, a rotated `Image` using a simple triangle sprite) and a cardinal text label (`Txt_Direction`).

Rotation logic (applied in `GradeMonitorSimulator.Update`):
```csharp
float yaw = Mathf.Atan2(gradeDir.x, gradeDir.z) * Mathf.Rad2Deg;
imgDirectionArrow.rectTransform.rotation = Quaternion.Euler(0, 0, -yaw);
```

Cardinal mapping:
| Yaw Range | Label |
|-----------|-------|
| -22.5 to 22.5 | N |
| 22.5 to 67.5 | NE |
| 67.5 to 112.5 | E |
| 112.5 to 157.5 | SE |
| 157.5 or -157.5 | S |
| -157.5 to -112.5 | SW |
| -112.5 to -67.5 | W |
| -67.5 to -22.5 | NW |

---

## 5.5 UI Framework Recommendation

Use **uGUI** (Unity's built-in `Canvas`, `Text`, `Image`, `Button`, `Toggle`, `InputField` components) for all monitor UI.

### Rationale

| Factor | uGUI | TextMeshPro (TMP) |
|--------|------|-------------------|
| Version compatibility | Works in Unity 2022.3 LTS without extra packages | Requires TMP package import |
| External dependency | Zero | Adds package dependency to prototype |
| Text clarity at 1024x600 | Acceptable for prototype-grade display | Superior, but not required |
| Setup complexity | Minimal | Requires font asset generation |
| Future upgrade path | Can migrate to TMP later if needed | Baseline is heavier |

For this prototype, uGUI keeps the Unity project self-contained and reduces setup friction. If text crispness is insufficient at the monitor's physical size, TMP can be adopted later via the Unity Package Manager without code changes (both use `Text` base classes for layout).

### Canvas Configuration

| Property | Value |
|----------|-------|
| Render Mode | Screen Space - Camera |
| Render Camera | `Monitor_UI_Camera` |
| Plane Distance | 100 |
| Pixel Perfect | True |
| Sorting Layer | UI |
| Additional Shader Channels | None |

---

## 5.6 Component Assignments

### 5.6.1 CabMonitor Prefab Hierarchy

```text
CabMonitor
|-- Monitor_Body           (MeshRenderer, generic cab monitor housing mesh)
|-- Monitor_Bezel            (MeshRenderer, dark trim)
|-- Monitor_Screen           (MeshRenderer, screen surface; material gets RenderTexture)
|   |-- RenderTextureMonitorBinder (MonoBehaviour)
|-- Monitor_UI_Camera        (Camera, orthographic, UI culling mask)
|-- Monitor_Canvas           (Canvas, Screen Space - Camera)
|   |-- MonitorPageManager   (MonoBehaviour)
|   |-- HeaderRow            (Empty GO, layout group)
|   |   |-- Tab_Work         (Button)
|   |   |-- Tab_Target       (Button)
|   |   |-- Tab_System       (Button)
|   |-- Page_Work            (GameObject, default active)
|   |   |-- Txt_TargetCutDepth
|   |   |-- Txt_Slope
|   |   |-- Txt_CrossSlope
|   |   |-- Img_DirectionArrow
|   |   |-- Txt_Direction
|   |   |-- Txt_LiveError
|   |   |-- Pnl_StatusBanner
|   |   |   |-- Txt_Status
|   |   |-- Txt_Tolerance
|   |   |-- Txt_Benchmark
|   |-- Page_Target          (GameObject, inactive)
|   |-- Page_System          (GameObject, inactive)
|   |-- Page_Boot            (GameObject, shown 2s on startup)
|-- Button_F1                (optional physical button mesh)
|-- Button_F2
|-- Button_F3
|-- Button_F4
|-- Button_Home
|-- Knob_Rotary
```

### 5.6.2 MonoBehaviour to GameObject Mapping

| MonoBehaviour | Host GameObject | Responsibility |
|---------------|----------------|--------------|
| `RenderTextureMonitorBinder` | `Monitor_Screen` | Creates RenderTexture, binds to camera and screen material |
| `MonitorPageManager` | `Monitor_Canvas` | Tab switching, page activation, input handling |
| `GradeMonitorSimulator` | `Monitor_Canvas` or `CabMonitor` root | Computes grade math, updates `Page_Work` text fields every frame |
| `SettingsPageController` | `Page_Target` | Reads/writes target depth, slope, cross-slope, tolerance; validates input; two-step APPLY/CANCEL commit |
| `DiagnosticsPageController` | `Page_System` | Shows FPS, frame time, telemetry replay progress, benchmark validity |
| `GradeStatusDisplay` | `Pnl_StatusBanner` | Updates banner color, icon sprite, and arrow animation based on `GradeStatus` |

---

## 5.7 Input Bindings

| Key / Action | Effect |
|--------------|--------|
| `M` | Cycle monitor page forward (Work -> Target -> System) |
| `Shift+M` | Cycle monitor page backward |
| `F1` | Jump to Work page |
| `F2` | Jump to Target page |
| `F3` | Jump to System page |
| `B` | Set benchmark at current bucket reference point (global shortcut; works from any page) |
| `Esc` | Return to Work page from any non-Work page |
| `Enter` | Apply settings on Target page |
| `+` / `-` | Increment / decrement selected setting on Target page |
| Tab click (mouse) | Jump directly to selected page |
| Arrow keys / PageUp / PageDown | Move mock excavator bucket (handled by `MockExcavatorRig`) |

---

## 5.8 Material Specifications

### MonitorScreen.mat

| Property | Value | Rationale |
|----------|-------|-----------|
| Shader | Standard (Built-in) | Supports emission, gloss, and reflection for glass-like appearance |
| Albedo | `#1A1A1A` | Matches UI background; dark base color |
| Metallic | 0.1 | Glass is non-metallic but catches highlights |
| Smoothness | 0.6 | Moderate gloss to catch cabin lights; not a mirror |
| Emission | Enabled | Driven by `RenderTextureMonitorBinder` with emission boost |
| `_MainTex` / `_BaseMap` | `GradeAssist_MonitorRT` | RenderTexture bound at runtime |

### MonitorBezel.mat

| Property | Value | Rationale |
|----------|-------|-----------|
| Shader | Standard (Built-in) | Simple plastic appearance |
| Albedo | `#1E1E1E` | Near-black to minimize distraction |
| Metallic | 0.0 | Plastic is non-metallic |
| Smoothness | 0.15 | Very matte to prevent cab-light glare |
| Specular Highlights | Off | Eliminates distracting reflections |

### MonitorBody.mat

| Property | Value | Rationale |
|----------|-------|-----------|
| Shader | Standard (Built-in) | Simple plastic appearance |
| Albedo | `#2A2A2A` | Dark grey for industrial housing |
| Metallic | 0.0 | Plastic is non-metallic |
| Smoothness | 0.2 | Very matte to prevent cab-light glare |

> **Note:** Do not increase Smoothness above 0.25 for bezel or body materials. High reflectivity causes distracting glare from sunlight entering cab windows or interior dome lights.

## 5.9 Cross-Slope Support Note

The current `GradeMonitorSimulator` computes grade error using `targetY = benchmarkY - targetCutDepthMeters + slopeDecimal * along`. The design reserves `crossSlopeDecimal * crossDistance` for future expansion.

To display cross-slope on the Grade 2D page:
1. Add `crossSlopePercent` field to `GradeMonitorSimulator`.
2. Compute `cross = Vector3.Dot(delta, crossDir)` where `crossDir = Vector3.Cross(Vector3.up, gradeDir).normalized`.
3. Update formula: `targetY += (crossSlopePercent / 100f) * cross`.
4. Wire `Txt_CrossSlope` to the inspector field.

This is documented here but not required for the initial prototype milestone.
