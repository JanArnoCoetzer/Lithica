using System;
using UnityEngine;

[Serializable]
public class HeightMaskFilter : TerrainFilter
{
    public float minHeight = 0f;
    public float maxHeight = 20f;
    public bool invert = false;

    public HeightMaskFilter()
    {
        displayName = "Height Mask";
    }

    public override void Apply(WorldManager world, Vector2 worldPos, ref float density, ref int materialId)
    {
        if (!enabled)
            return;

        float localY = worldPos.y - world.transform.position.y;
        float mask = Mathf.InverseLerp(minHeight, maxHeight, localY);
        mask = invert ? 1f - mask : mask;

        density *= mask;

        // Optionally clear material when fully masked out:
        // if (density <= 0f) materialId = 0;
    }
}