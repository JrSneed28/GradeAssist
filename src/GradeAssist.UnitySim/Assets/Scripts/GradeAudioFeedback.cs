using System.Collections;
using UnityEngine;

/// <summary>
/// Plays directional audio tones for grade status feedback.
/// Designed for eyes-free operation in a vibrating excavator cab.
/// </summary>
[RequireComponent(typeof(AudioSource))]
public sealed class GradeAudioFeedback : MonoBehaviour
{
    [Header("Status Tones")]
    [Tooltip("Tone frequency when AboveGrade (Hz)")]
    [Range(200f, 800f)]
    public float aboveGradeFrequency = 400f;

    [Tooltip("Tone frequency when BelowGrade (Hz)")]
    [Range(200f, 800f)]
    public float belowGradeFrequency = 300f;

    [Tooltip("Tone frequency when OnGrade (Hz); 0 for silence")]
    [Range(0f, 800f)]
    public float onGradeFrequency = 600f;

    [Header("Tone Timing")]
    [Tooltip("Duration of each tone pulse (seconds)")]
    [Range(0.05f, 0.5f)]
    public float toneDuration = 0.2f;

    [Tooltip("Duration of silence between tone pulses (seconds)")]
    [Range(0.05f, 0.5f)]
    public float pauseDuration = 0.2f;

    [Tooltip("Volume when OnGrade (set to 0 for silent on-grade)")]
    [Range(0f, 1f)]
    public float onGradeVolume = 0.1f;

    [Tooltip("Volume when off-grade")]
    [Range(0f, 1f)]
    public float offGradeVolume = 0.3f;

    [Header("Transition Chime")]
    [Tooltip("Play a brief chime on status change?")]
    public bool playTransitionChime = true;

    [Tooltip("Chime frequency (Hz)")]
    [Range(400f, 1200f)]
    public float chimeFrequency = 800f;

    [Tooltip("Chime duration (seconds)")]
    [Range(0.05f, 0.3f)]
    public float chimeDuration = 0.1f;

    private AudioSource audioSource = null!;
    private GradeStatus lastStatus = GradeStatus.OnGrade;
    private float toneTimer;
    private AudioClip? cachedAboveClip;
    private AudioClip? cachedBelowClip;
    private AudioClip? cachedOnGradeClip;
    private AudioClip? cachedChimeClip;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        audioSource.loop = false;
        audioSource.playOnAwake = false;
    }

    private void Start()
    {
        // Pre-allocate clips to avoid runtime allocation / GC pressure
        int aboveSamples = Mathf.CeilToInt(toneDuration * AudioSettings.outputSampleRate);
        int belowSamples = Mathf.CeilToInt(toneDuration * AudioSettings.outputSampleRate);
        int onGradeSamples = Mathf.CeilToInt(toneDuration * AudioSettings.outputSampleRate);
        int chimeSamples = Mathf.CeilToInt(chimeDuration * AudioSettings.outputSampleRate);

        cachedAboveClip = CreateSineClip("AboveTone", aboveGradeFrequency, aboveSamples);
        cachedBelowClip = CreateSineClip("BelowTone", belowGradeFrequency, belowSamples);
        cachedOnGradeClip = CreateSineClip("OnGradeTone", onGradeFrequency, onGradeSamples);
        cachedChimeClip = CreateSineClip("Chime", chimeFrequency, chimeSamples);
    }

    private void OnDestroy()
    {
        if (cachedAboveClip != null) Destroy(cachedAboveClip);
        if (cachedBelowClip != null) Destroy(cachedBelowClip);
        if (cachedOnGradeClip != null) Destroy(cachedOnGradeClip);
        if (cachedChimeClip != null) Destroy(cachedChimeClip);
    }

    /// <summary>
    /// Call this from GradeMonitorSimulator when grade status changes.
    /// </summary>
    public void OnGradeStatusChanged(GradeStatus status)
    {
        if (playTransitionChime && status != lastStatus && cachedChimeClip != null)
        {
            audioSource.PlayOneShot(cachedChimeClip, offGradeVolume);
        }
        lastStatus = status;
    }

    private void Update()
    {
        if (audioSource == null) return;

        if (lastStatus == GradeStatus.OnGrade && onGradeFrequency <= 0f)
        {
            if (audioSource.isPlaying) audioSource.Stop();
            return;
        }

        AudioClip? targetClip = lastStatus switch
        {
            GradeStatus.AboveGrade => cachedAboveClip,
            GradeStatus.BelowGrade => cachedBelowClip,
            GradeStatus.OnGrade => cachedOnGradeClip,
            _ => null
        };

        float targetVol = lastStatus == GradeStatus.OnGrade ? onGradeVolume : offGradeVolume;

        if (targetClip == null || targetVol <= 0f)
        {
            if (audioSource.isPlaying) audioSource.Stop();
            return;
        }

        toneTimer += Time.deltaTime;
        float cycle = toneDuration + pauseDuration;
        if (toneTimer >= cycle) toneTimer -= cycle;
        bool shouldPlay = toneTimer < toneDuration;

        if (shouldPlay && !audioSource.isPlaying)
        {
            audioSource.clip = targetClip;
            audioSource.volume = targetVol;
            audioSource.Play();
        }
        else if (!shouldPlay && audioSource.isPlaying)
        {
            audioSource.Stop();
        }
    }

    private static AudioClip CreateSineClip(string name, float frequency, int samples)
    {
        var clip = AudioClip.Create(name, samples, 1, AudioSettings.outputSampleRate, false);
        var data = new float[samples];
        for (int i = 0; i < samples; i++)
        {
            data[i] = Mathf.Sin(2f * Mathf.PI * frequency * i / AudioSettings.outputSampleRate);
        }
        clip.SetData(data, 0);
        return clip;
    }
}
