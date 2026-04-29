# Grade Math

## Convention

- `targetCutDepthMeters` is positive when desired grade is below benchmark.
- `slopeDecimal = slopePercent / 100`.
- `crossSlopeDecimal = crossSlopePercent / 100`.

## Formula

```text
targetY = benchmarkY - targetCutDepthMeters
          + slopeDecimal * alongDistance
          + crossSlopeDecimal * crossDistance
```

## Error

```text
errorMeters = referencePoint.y - targetY
```

Interpretation:

| Error | Meaning |
|---|---|
| `> tolerance` | Above grade |
| `abs(error) <= tolerance` | On grade |
| `< -tolerance` | Below grade / overcut |
