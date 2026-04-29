using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEngine;

public sealed class TelemetryReplayDriver : MonoBehaviour
{
    [Header("CSV Source")]
    public string csvFileName = "TelemetrySamples/sample_run_01.csv";

    [Header("Playback")]
    public float playbackSpeed = 1.0f;
    public bool loop = false;
    public Transform targetTransform = null!;

    [Header("Grade Monitor")]
    public GradeMonitorSimulator monitor = null!;

    public event Action<int, int, float> OnSampleProcessed; // sampleIndex, totalSamples, currentErrorMeters

    private List<TelemetrySample> samples = new List<TelemetrySample>();
    private float playbackTime;
    private int currentIndex;
    private bool isPlaying;

    [System.Serializable]
    public struct TelemetrySample
    {
        public float timeSeconds;
        public float bucketX;
        public float bucketY;
        public float bucketZ;

        public TelemetrySample(float time, float x, float y, float z)
        {
            timeSeconds = time;
            bucketX = x;
            bucketY = y;
            bucketZ = z;
        }
    }

    private void Start()
    {
        string path = Path.Combine(Application.streamingAssetsPath, csvFileName);
        if (File.Exists(path))
        {
            ReadCsv(path);
            isPlaying = samples.Count > 0;
            currentIndex = 0;
            playbackTime = samples.Count > 0 ? samples[0].timeSeconds : 0f;
        }
        else
        {
            Debug.LogWarning($"[TelemetryReplayDriver] CSV not found: {path}");
            isPlaying = false;
        }
    }

    private void Update()
    {
        if (!isPlaying || samples.Count == 0) return;
        if (targetTransform == null) return;

        playbackTime += Time.deltaTime * playbackSpeed;

        // Find current sample by time
        while (currentIndex < samples.Count - 1 && samples[currentIndex + 1].timeSeconds <= playbackTime)
        {
            currentIndex++;
        }

        if (currentIndex >= samples.Count)
        {
            if (loop)
            {
                currentIndex = 0;
                playbackTime = samples[0].timeSeconds;
            }
            else
            {
                isPlaying = false;
                return;
            }
        }

        var sample = samples[currentIndex];
        targetTransform.position = new Vector3(sample.bucketX, sample.bucketY, sample.bucketZ);

        float error = 0f;
        if (monitor != null && monitor.gradePlane != null)
        {
            error = monitor.gradePlane.ComputeError(targetTransform.position).ErrorMeters;
        }

        OnSampleProcessed?.Invoke(currentIndex, samples.Count, error);
    }

    private void ReadCsv(string path)
    {
        var lines = File.ReadAllLines(path);
        if (lines.Length == 0) return;

        var headerLine = lines[0].Trim();
        bool hasHeader = headerLine.Contains("timeSeconds", StringComparison.OrdinalIgnoreCase)
            || headerLine.Contains("x", StringComparison.OrdinalIgnoreCase)
            || headerLine.Contains("y", StringComparison.OrdinalIgnoreCase)
            || headerLine.Contains("z", StringComparison.OrdinalIgnoreCase);

        int start = hasHeader ? 1 : 0;

        for (int i = start; i < lines.Length; i++)
        {
            var line = lines[i].Trim();
            if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#")) continue;

            var parts = line.Split(',');
            if (parts.Length < 4) continue;

            if (!TryParseFloat(parts[0], out var t)) continue;
            if (!TryParseFloat(parts[1], out var x)) continue;
            if (!TryParseFloat(parts[2], out var y)) continue;
            if (!TryParseFloat(parts[3], out var z)) continue;

            samples.Add(new TelemetrySample(t, x, y, z));
        }

        Debug.Log($"[TelemetryReplayDriver] Loaded {samples.Count} samples from {path}");
    }

    private static bool TryParseFloat(string s, out float result)
    {
        return float.TryParse(s.Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out result)
            && !float.IsNaN(result) && !float.IsInfinity(result);
    }
}
