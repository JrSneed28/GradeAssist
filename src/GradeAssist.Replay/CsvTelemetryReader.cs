using System.Globalization;

namespace GradeAssist.Replay;

public sealed class CsvTelemetryReader
{
    private readonly string _path;

    public CsvTelemetryReader(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            throw new ArgumentException("Path must not be null or empty.", nameof(path));
        }
        _path = path;
    }

    public IReadOnlyList<TelemetrySample> ReadSamples(out IReadOnlyList<string> invalidRows)
    {
        var samples = new List<TelemetrySample>();
        var badRows = new List<string>();
        var lines = File.ReadAllLines(_path);

        if (lines.Length == 0)
        {
            invalidRows = badRows;
            return samples;
        }

        var headerLine = lines[0].Trim();
        bool hasHeader = headerLine.Contains("timeSeconds", StringComparison.OrdinalIgnoreCase)
            || headerLine.Contains("x", StringComparison.OrdinalIgnoreCase)
            || headerLine.Contains("y", StringComparison.OrdinalIgnoreCase)
            || headerLine.Contains("z", StringComparison.OrdinalIgnoreCase);

        var dataStart = hasHeader ? 1 : 0;
        var headerCols = hasHeader ? headerLine.Split(',') : Array.Empty<string>();

        for (int i = dataStart; i < lines.Length; i++)
        {
            var line = lines[i].Trim();
            if (string.IsNullOrWhiteSpace(line) || line.StartsWith('#'))
            {
                continue;
            }

            var parts = line.Split(',');
            if (parts.Length < 4)
            {
                badRows.Add($"Line {i + 1}: insufficient columns ({parts.Length})");
                continue;
            }

            if (!TryParseDouble(parts[0], out var timeSeconds))
            {
                badRows.Add($"Line {i + 1}: invalid timeSeconds '{parts[0]}'");
                continue;
            }
            if (!TryParseDouble(parts[1], out var bucketX))
            {
                badRows.Add($"Line {i + 1}: invalid x '{parts[1]}'");
                continue;
            }
            if (!TryParseDouble(parts[2], out var bucketY))
            {
                badRows.Add($"Line {i + 1}: invalid y '{parts[2]}'");
                continue;
            }
            if (!TryParseDouble(parts[3], out var bucketZ))
            {
                badRows.Add($"Line {i + 1}: invalid z '{parts[3]}'");
                continue;
            }

            double? boom = null, stick = null, bucket = null, swing = null;
            if (parts.Length > 4 && TryParseDouble(parts[4], out var bVal)) boom = bVal;
            if (parts.Length > 5 && TryParseDouble(parts[5], out var sVal)) stick = sVal;
            if (parts.Length > 6 && TryParseDouble(parts[6], out var baVal)) bucket = baVal;
            if (parts.Length > 7 && TryParseDouble(parts[7], out var swVal)) swing = swVal;

            samples.Add(new TelemetrySample(timeSeconds, bucketX, bucketY, bucketZ, boom, stick, bucket, swing));
        }

        invalidRows = badRows;
        return samples;
    }

    private static bool TryParseDouble(string s, out double result)
    {
        return double.TryParse(s.Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out result)
            && double.IsFinite(result);
    }
}
