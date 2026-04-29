using UnityEngine;
using UnityEditor;

public class SetupWizard : EditorWindow
{
    [MenuItem("GradeAssist/Setup Wizard")]
    public static void ShowWindow()
    {
        GetWindow<SetupWizard>("GradeAssist Setup");
    }

    private void OnGUI()
    {
        GUILayout.Label("GradeAssist Setup Wizard", EditorStyles.boldLabel);
        GUILayout.Space(10);

        if (GUILayout.Button("Auto-wire MockExcavatorRig"))
        {
            AutoWireMockExcavatorRig();
        }

        if (GUILayout.Button("Auto-wire GradeMonitorSimulator"))
        {
            AutoWireGradeMonitorSimulator();
        }
    }

    private void AutoWireMockExcavatorRig()
    {
            // Find selected GameObject
            if (Selection.activeGameObject == null)
            {
                Debug.LogWarning("[SetupWizard] Select a GameObject first.");
                return;
            }

            var rig = Selection.activeGameObject.GetComponent<MockExcavatorRig>();
            if (rig == null)
            {
                Debug.LogWarning("[SetupWizard] Selected GameObject has no MockExcavatorRig.");
                return;
            }

            // Auto-find child transforms by name
            if (rig.cuttingEdgeReference == null)
                rig.cuttingEdgeReference = FindChildRecursive(Selection.activeGameObject.transform, "CuttingEdgeReference");
            if (rig.swingPivot == null)
                rig.swingPivot = FindChildRecursive(Selection.activeGameObject.transform, "SwingPivot");
            if (rig.boomPivot == null)
                rig.boomPivot = FindChildRecursive(Selection.activeGameObject.transform, "BoomPivot");
            if (rig.stickPivot == null)
                rig.stickPivot = FindChildRecursive(Selection.activeGameObject.transform, "StickPivot");
            if (rig.bucketPivot == null)
                rig.bucketPivot = FindChildRecursive(Selection.activeGameObject.transform, "BucketPivot");

            EditorUtility.SetDirty(rig);
            Debug.Log("[SetupWizard] MockExcavatorRig auto-wired.");
    }

    private void AutoWireGradeMonitorSimulator()
    {
        if (Selection.activeGameObject == null)
        {
            Debug.LogWarning("[SetupWizard] Select a GameObject first.");
            return;
        }

        var sim = Selection.activeGameObject.GetComponent<GradeMonitorSimulator>();
        if (sim == null)
        {
            Debug.LogWarning("[SetupWizard] Selected GameObject has no GradeMonitorSimulator.");
            return;
        }

        if (sim.rig == null)
            sim.rig = FindObjectOfType<MockExcavatorRig>();

        EditorUtility.SetDirty(sim);
        Debug.Log("[SetupWizard] GradeMonitorSimulator auto-wired.");
    }

    private static Transform FindChildRecursive(Transform parent, string name)
    {
        Transform found = parent.Find(name);
        if (found != null) return found;
        foreach (Transform child in parent)
        {
            found = FindChildRecursive(child, name);
            if (found != null) return found;
        }
        return null;
    }
}
