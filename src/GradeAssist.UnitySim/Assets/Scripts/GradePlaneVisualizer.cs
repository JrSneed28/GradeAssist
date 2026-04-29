using UnityEngine;

/// <summary>
/// Draws debug gizmos showing the current grade plane in the Scene view.
/// Attach to SimulationDirector or any persistent GameObject.
/// </summary>
public sealed class GradePlaneVisualizer : MonoBehaviour
{
    [Header("Data Source")]
    public UnityGradePlane gradePlane = null!;

    [Header("Grid Settings")]
    public int gridLines = 10;
    public float gridSpacing = 1.0f;
    public float gridExtent = 10.0f;

    [Header("Colors")]
    public Color benchmarkColor = Color.yellow;
    public Color gradeDirectionColor = Color.green;
    public Color crossDirectionColor = Color.cyan;
    public Color planeColor = new Color(0.2f, 0.8f, 0.2f, 0.3f);

    private void OnDrawGizmos()
    {
        if (gradePlane == null) return;

        Vector3 benchmark = gradePlane.benchmarkPoint;

        // Benchmark point
        Gizmos.color = benchmarkColor;
        Gizmos.DrawWireSphere(benchmark, 0.15f);

        // Grade direction arrow
        var flatDir = new Vector3(gradePlane.gradeDirection.x, 0, gradePlane.gradeDirection.z);
        Vector3 normDir = flatDir.sqrMagnitude > 0.0001f ? flatDir.normalized : Vector3.forward;
        Gizmos.color = gradeDirectionColor;
        Gizmos.DrawLine(benchmark, benchmark + normDir * 3f);
        Gizmos.DrawLine(benchmark + normDir * 3f, benchmark + normDir * 2.5f + new Vector3(-normDir.z, 0, normDir.x) * 0.3f);
        Gizmos.DrawLine(benchmark + normDir * 3f, benchmark + normDir * 2.5f + new Vector3(normDir.z, 0, -normDir.x) * 0.3f);

        // Cross direction arrow
        Vector3 crossDir = new Vector3(-normDir.z, 0, normDir.x);
        Gizmos.color = crossDirectionColor;
        Gizmos.DrawLine(benchmark, benchmark + crossDir * 3f);

        // Grade plane grid
        Gizmos.color = planeColor;
        for (int i = -gridLines; i <= gridLines; i++)
        {
            float offset = i * gridSpacing;

            // Line along grade direction
            Vector3 start1 = benchmark + crossDir * offset - normDir * gridExtent;
            Vector3 end1 = benchmark + crossDir * offset + normDir * gridExtent;
            float y1a = gradePlane.HeightAt(start1);
            float y1b = gradePlane.HeightAt(end1);
            Gizmos.DrawLine(new Vector3(start1.x, y1a, start1.z), new Vector3(end1.x, y1b, end1.z));

            // Line along cross direction
            Vector3 start2 = benchmark + normDir * offset - crossDir * gridExtent;
            Vector3 end2 = benchmark + normDir * offset + crossDir * gridExtent;
            float y2a = gradePlane.HeightAt(start2);
            float y2b = gradePlane.HeightAt(end2);
            Gizmos.DrawLine(new Vector3(start2.x, y2a, start2.z), new Vector3(end2.x, y2b, end2.z));
        }
    }
}
