using System;
using UnityEngine;

[Serializable]
public class TerraceFilter : TerrainFilter
{
    public int steps = 5;
    [Range(0f, 1f)] public float blend = 0.2f;

    public TerraceFilter()
    {
        displayName = "Terrace";
    }

    public override void Apply(WorldManager world, Vector2 worldPos, ref float density, ref int materialId)
    {
        if (!enabled)
            return;

        int safeSteps = Mathf.Max(1, steps);
        float stepped = Mathf.Round(density * safeSteps) / safeSteps;
        density = Mathf.Lerp(density, stepped, blend);
        // Material unchanged.
    }
}