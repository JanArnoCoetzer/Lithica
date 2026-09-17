using System.Collections.Generic;
using UnityEngine;

public sealed class TerrainGizmoVisualizer
{
    public bool Enabled;
    public bool DrawOnlyWhenSelected;
    public float PointRadius;
    public float CenterRadius;
    public float HitRadius;
    public float DestroyedRadius;
    public Color StartColor;
    public Color TargetColor;
    public Color RayColor;
    public Color CenterColor;
    public Color SampleColor;
    public Color MissColor;
    public Color SolidColor;
    public Color DestroyedColor;

    private readonly List<Vector3> directionalCenters = new();
    private readonly List<Vector3> directionalSamples = new();
    private readonly List<Vector3> directionalMissSamples = new();
    private readonly List<Vector3> directionalSolidSamples = new();
    private readonly List<Vector3> directionalDestroyedSamples = new();

    private Vector3 lastDirectionalStart;
    private Vector3 lastDirectionalTarget;
    private Vector3 lastDirectionalFinalHitCenter;
    private bool lastDirectionalHadHit;

    public void Clear()
    {
        directionalCenters.Clear();
        directionalSamples.Clear();
        directionalMissSamples.Clear();
        directionalSolidSamples.Clear();
        directionalDestroyedSamples.Clear();

        lastDirectionalHadHit = false;
        lastDirectionalFinalHitCenter = Vector3.zero;
        lastDirectionalStart = Vector3.zero;
        lastDirectionalTarget = Vector3.zero;
    }

    public void SetRayEndpoints(Vector3 start, Vector3 target)
    {
        lastDirectionalStart = start;
        lastDirectionalTarget = target;
    }

    public void RecordCenter(Vector2 point)
    {
        directionalCenters.Add(point);
    }

    public void RecordSample(Vector2 point, bool solid, bool destroyed)
    {
        directionalSamples.Add(point);

        if (destroyed)
            directionalDestroyedSamples.Add(point);
        else if (solid)
            directionalSolidSamples.Add(point);
        else
            directionalMissSamples.Add(point);
    }

    public void MarkHit(Vector2 hitCenter)
    {
        lastDirectionalHadHit = true;
        lastDirectionalFinalHitCenter = hitCenter;
    }

    public void Draw()
    {
        if (!Enabled)
            return;

        if (directionalCenters.Count == 0 &&
            directionalSamples.Count == 0 &&
            !lastDirectionalHadHit &&
            lastDirectionalStart == Vector3.zero &&
            lastDirectionalTarget == Vector3.zero)
            return;

        Gizmos.color = RayColor;
        Gizmos.DrawLine(lastDirectionalStart, lastDirectionalTarget);

        Gizmos.color = StartColor;
        Gizmos.DrawSphere(lastDirectionalStart, DestroyedRadius);

        Gizmos.color = TargetColor;
        Gizmos.DrawSphere(lastDirectionalTarget, CenterRadius);

        Gizmos.color = CenterColor;
        for (int i = 0; i < directionalCenters.Count; i++)
            Gizmos.DrawSphere(directionalCenters[i], CenterRadius);

        Gizmos.color = RayColor;
        for (int i = 1; i < directionalCenters.Count; i++)
            Gizmos.DrawLine(directionalCenters[i - 1], directionalCenters[i]);

        Gizmos.color = SampleColor;
        for (int i = 0; i < directionalSamples.Count; i++)
            Gizmos.DrawSphere(directionalSamples[i], PointRadius);

        Gizmos.color = MissColor;
        for (int i = 0; i < directionalMissSamples.Count; i++)
            Gizmos.DrawSphere(directionalMissSamples[i], PointRadius * 0.9f);

        Gizmos.color = SolidColor;
        for (int i = 0; i < directionalSolidSamples.Count; i++)
            Gizmos.DrawSphere(directionalSolidSamples[i], HitRadius);

        Gizmos.color = DestroyedColor;
        for (int i = 0; i < directionalDestroyedSamples.Count; i++)
            Gizmos.DrawSphere(directionalDestroyedSamples[i], DestroyedRadius);

        if (lastDirectionalHadHit)
        {
            Gizmos.color = DestroyedColor;
            Gizmos.DrawWireSphere(lastDirectionalFinalHitCenter, DestroyedRadius * 1.75f);
        }
    }
}