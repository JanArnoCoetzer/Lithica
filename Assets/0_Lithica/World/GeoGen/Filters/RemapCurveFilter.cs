using System;
using UnityEngine;

[Serializable]
public class RemapCurveFilter : TerrainFilter
{
    public AnimationCurve curve = AnimationCurve.Linear(0f, 0f, 1f, 1f);

    public RemapCurveFilter()
    {
        displayName = "Remap Curve";
    }

    public override void Apply(WorldManager world, Vector2 worldPos, ref float density, ref int materialId)
    {
        if (!enabled)
            return;

        density = curve.Evaluate(density);
        // Material unchanged.
    }
}