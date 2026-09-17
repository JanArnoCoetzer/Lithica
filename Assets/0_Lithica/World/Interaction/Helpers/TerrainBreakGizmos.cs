using System.Collections.Generic;
using UnityEngine;

public class TerrainBreakGizmos
{
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

    public void SetRay(Vector3 start, Vector3 target)
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

    public void MarkHit(Vector2 pixelWorld)
    {
        lastDirectionalHadHit = true;
        lastDirectionalFinalHitCenter = pixelWorld;
    }

    public void Draw(
        bool debugEnabled,
        bool drawDirectionalBreakGizmos,
        Color gizmoRayColor,
        Color gizmoStartColor,
        Color gizmoTargetColor,
        Color gizmoCenterColor,
        Color gizmoSampleColor,
        Color gizmoMissColor,
        Color gizmoSolidColor,
        Color gizmoDestroyedColor,
        float gizmoPointRadius,
        float gizmoCenterRadius,
        float gizmoHitRadius,
        float gizmoDestroyedRadius)
    {
        if (!debugEnabled || !drawDirectionalBreakGizmos)
            return;

        if (directionalCenters.Count == 0 &&
            directionalSamples.Count == 0 &&
            !lastDirectionalHadHit &&
            lastDirectionalStart == Vector3.zero &&
            lastDirectionalTarget == Vector3.zero)
            return;

        Gizmos.color = gizmoRayColor;
        Gizmos.DrawLine(lastDirectionalStart, lastDirectionalTarget);

        Gizmos.color = gizmoStartColor;
        Gizmos.DrawSphere(lastDirectionalStart, gizmoDestroyedRadius);

        Gizmos.color = gizmoTargetColor;
        Gizmos.DrawSphere(lastDirectionalTarget, gizmoCenterRadius);

        Gizmos.color = gizmoCenterColor;
        for (int i = 0; i < directionalCenters.Count; i++)
            Gizmos.DrawSphere(directionalCenters[i], gizmoCenterRadius);

        Gizmos.color = gizmoRayColor;
        for (int i = 1; i < directionalCenters.Count; i++)
            Gizmos.DrawLine(directionalCenters[i - 1], directionalCenters[i]);

        Gizmos.color = gizmoSampleColor;
        for (int i = 0; i < directionalSamples.Count; i++)
            Gizmos.DrawSphere(directionalSamples[i], gizmoPointRadius);

        Gizmos.color = gizmoMissColor;
        for (int i = 0; i < directionalMissSamples.Count; i++)
            Gizmos.DrawSphere(directionalMissSamples[i], gizmoPointRadius * 0.9f);

        Gizmos.color = gizmoSolidColor;
        for (int i = 0; i < directionalSolidSamples.Count; i++)
            Gizmos.DrawSphere(directionalSolidSamples[i], gizmoHitRadius);

        Gizmos.color = gizmoDestroyedColor;
        for (int i = 0; i < directionalDestroyedSamples.Count; i++)
            Gizmos.DrawSphere(directionalDestroyedSamples[i], gizmoDestroyedRadius);

        if (lastDirectionalHadHit)
        {
            Gizmos.color = gizmoDestroyedColor;
            Gizmos.DrawWireSphere(lastDirectionalFinalHitCenter, gizmoDestroyedRadius * 1.75f);
        }
    }
}