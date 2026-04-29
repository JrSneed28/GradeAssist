using UnityEngine;
using UnityEngine.UI;

public sealed class GradeDirectionIndicator : MonoBehaviour
{
    [Header("Visuals")]
    public Image arrow = null!;
    public Text cardinalText = null!;

    public void SetDirection(Vector3 gradeDirection)
    {
        var flat = new Vector3(gradeDirection.x, 0, gradeDirection.z);
        if (flat.sqrMagnitude < 0.0001f) return;

        float yaw = Mathf.Atan2(flat.x, flat.z) * Mathf.Rad2Deg;
        if (arrow != null)
            arrow.rectTransform.rotation = Quaternion.Euler(0, 0, -yaw);

        if (cardinalText != null)
            cardinalText.text = CardinalFromYaw(yaw);
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
}
