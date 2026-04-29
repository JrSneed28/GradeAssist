using UnityEngine;

public sealed class BucketReferenceTracker : MonoBehaviour
{
    public Vector3 WorldPosition => transform.position;
    public event System.Action<Vector3> OnPositionChanged;

    private Vector3 lastPosition;

    private void Start()
    {
        lastPosition = transform.position;
    }

    private void Update()
    {
        Vector3 current = transform.position;
        if (current != lastPosition)
        {
            OnPositionChanged?.Invoke(current);
            lastPosition = current;
        }
    }
}
