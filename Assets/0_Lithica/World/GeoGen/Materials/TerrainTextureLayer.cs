using UnityEngine;

[System.Serializable]
public class TerrainTextureLayer
{
    public string layerName = "Layer";
    public Texture2D texture;
    public Color tint = Color.white;

    [Header("World Tiling")]
    [Min(0.0001f)] public float textureScale = 0.25f;
    public Vector2 uvOffset = Vector2.zero;

    [Header("Blend Noise")]
    public bool useBlendNoise = true;
    [Min(0.0001f)] public float blendNoiseScale = 0.15f;
    [Range(0f, 1f)] public float blendThreshold = 0.5f;
    [Range(0.001f, 1f)] public float blendSoftness = 0.15f;
    public Vector2 blendNoiseOffset = Vector2.zero;

    [Header("Strength")]
    [Range(0f, 1f)] public float opacity = 1f;

    public Color SampleTextureWorld(Vector2 worldPos)
    {
        if (texture == null)
            return Color.clear;

        float u = worldPos.x * textureScale + uvOffset.x;
        float v = worldPos.y * textureScale + uvOffset.y;

        Color sampled = texture.GetPixelBilinear(
            Mathf.Repeat(u, 1f),
            Mathf.Repeat(v, 1f)
        );

        return new Color(
            sampled.r * tint.r,
            sampled.g * tint.g,
            sampled.b * tint.b,
            sampled.a * tint.a
        );
    }

    public float EvaluateBlend(Vector2 worldPos)
    {
        if (!useBlendNoise)
            return opacity;

        float n = Mathf.PerlinNoise(
            worldPos.x * blendNoiseScale + blendNoiseOffset.x,
            worldPos.y * blendNoiseScale + blendNoiseOffset.y
        );

        float min = blendThreshold - blendSoftness * 0.5f;
        float max = blendThreshold + blendSoftness * 0.5f;
        float t = Mathf.InverseLerp(min, max, n);
        return t * opacity;
    }
}