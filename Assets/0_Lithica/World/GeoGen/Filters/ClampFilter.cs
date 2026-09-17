using System;
using UnityEngine;

[Serializable]
public class ClampFilter : TerrainFilter
{
    public float minValue = 0f;
    public float maxValue = 1f;

    public ClampFilter()
    {
        displayName = "Clamp";
    }

    public override void Apply(WorldManager world, Vector2 worldPos, ref float density, ref int materialId)
    {
        if (!enabled)
            return;

        density = Mathf.Clamp(density, minValue, maxValue);

        // If you want to clear material when clamped to zero:
        // if (density <= 0f) materialId = 0;
    }
}