using UnityEngine;
using UnityEngine.UI;

public sealed class GradeStatusDisplay : MonoBehaviour
{
    [Header("Visuals")]
    public Image statusIcon = null!;
    public Sprite[] statusSprites = new Sprite[3]; // Below, On, Above

    [Header("Animation")]
    public bool enableBobAnimation = true;
    public float bobAmplitude = 3f;
    public float bobFrequency = 2f;

    private GradeStatus currentStatus = GradeStatus.OnGrade;
    private RectTransform rectTransform;
    private Vector2 baseAnchoredPosition;

    // Color-blind safe palette
    private static readonly Color ColorOnGrade = new Color32(0x2A, 0x9D, 0x8F, 0xFF);
    private static readonly Color ColorAboveGrade = new Color32(0xE6, 0x39, 0x46, 0xFF);
    private static readonly Color ColorBelowGrade = new Color32(0x45, 0x7B, 0x9D, 0xFF);

    private void Awake()
    {
        rectTransform = statusIcon != null ? statusIcon.GetComponent<RectTransform>() : null;
        if (rectTransform != null) baseAnchoredPosition = rectTransform.anchoredPosition;
    }

    private void Update()
    {
        if (!enableBobAnimation || rectTransform == null) return;
        float bob = Mathf.Sin(Time.time * bobFrequency) * bobAmplitude;
        rectTransform.anchoredPosition = baseAnchoredPosition + new Vector2(0, bob);
    }

    public void SetStatus(GradeStatus status)
    {
        currentStatus = status;
        if (statusIcon == null) return;

        int spriteIndex = status switch
        {
            GradeStatus.BelowGrade => 0,
            GradeStatus.OnGrade => 1,
            GradeStatus.AboveGrade => 2,
            _ => 1
        };

        if (spriteIndex < statusSprites.Length && statusSprites[spriteIndex] != null)
            statusIcon.sprite = statusSprites[spriteIndex];

        statusIcon.color = status switch
        {
            GradeStatus.OnGrade => ColorOnGrade,
            GradeStatus.AboveGrade => ColorAboveGrade,
            GradeStatus.BelowGrade => ColorBelowGrade,
            _ => Color.white
        };
    }
}
