using UnityEngine;

[CreateAssetMenu(fileName = "TerrainPixelMaterial", menuName = "Terrain/Pixel Material (Natural Diagonal Layers)")]
public class TerrainPixelMaterial : ScriptableObject
{
    [Header("Inventory / UI")]
    [Tooltip("Sprite used as icon for this terrain material in the inventory.")]
    public Sprite inventoryIcon;

    [Header("Palette (max 8 colors)")]
    public Color[] palette = new Color[4]
    {
        new Color(0.30f, 0.18f, 0.10f, 1f),
        new Color(0.34f, 0.20f, 0.11f, 1f),
        new Color(0.38f, 0.22f, 0.13f, 1f),
        new Color(0.42f, 0.24f, 0.14f, 1f)
    };

    [Header("Layer Brightness")]
    [Range(1, 8)] public int layerCount = 4;
    public float minBrightness = 0.6f;
    public float maxBrightness = 1.1f;

    [Header("Diagonal Layer Noise")]
    [Tooltip("Controls the thickness / frequency of layers along the diagonal.")]
    public float layerNoiseScale = 0.15f;

    [Tooltip("Controls diagonal direction: d = x + y * slopeFactor (1 = 45°).")]
    public float slopeFactor = 1.0f;

    [Tooltip("How much to stretch the noise sideways (larger = more horizontal feeling).")]
    public float sidewaysStretch = 3.0f;

    [Header("Naturalness Controls")]
    [Tooltip("How much the bands bend and wobble.")]
    public float bandWarpScale = 0.05f;

    public float bandWarpAmount = 1.5f;

    [Tooltip("How much band contrast varies across space.")]
    public float bandContrastVariationScale = 0.03f;

    [Header("Pixel Palette Noise (white noise)")]
    [Tooltip("World-space scale for grouping pixels into noise 'cells'.")]
    public float pixelCellScale = 8f;

    [Range(0, 7)] public int maxIndexOffset = 1;

    [Tooltip("0 = no pixel variation, 1 = full offset within maxIndexOffset.")]
    [Range(0f, 1f)] public float pixelNoiseStrength = 0.8f;

    [Tooltip("How much the grain strength varies across space.")]
    public float grainVariationScale = 0.08f;

    public int pixelNoiseSeed = 98765;

    [Header("Pixel Shuffle (jitter)")]
    [Tooltip("0 = no shuffle, 1 = maximum pixel jitter in world space.")]
    [Range(0f, 1f)] public float shuffleStrength = 0.5f;

    [Tooltip("Base world-space distance used for maximum jitter.")]
    public float shuffleMaxOffset = 0.15f;

    public int shuffleSeed = 32123;

    [Header("Generation (Dirt vs Stone etc.)")]
    public TerrainGenerationSettings generation = new TerrainGenerationSettings();

    /// <summary>
    /// Should this material be allowed at this world position and base density?
    /// </summary>
    public bool SupportsPosition(Vector2 worldPos, float baseDensity)
    {
        // Vertical band.
        if (worldPos.y < generation.minWorldY || worldPos.y > generation.maxWorldY)
            return false;

        // Density constraint.
        if (baseDensity < generation.minAllowedDensity)
            return false;

        // Material noise mask.
        if (generation.materialNoiseScale > 0f)
        {
            float n = Mathf.PerlinNoise(
                worldPos.x * generation.materialNoiseScale,
                worldPos.y * generation.materialNoiseScale
            );

            if (n < generation.materialThreshold)
                return false;
        }

        // Optional veins / pockets.
        if (generation.enableVeins && generation.veinNoiseScale > 0f)
        {
            float nv = Mathf.PerlinNoise(
                worldPos.x * generation.veinNoiseScale + 100.123f,
                worldPos.y * generation.veinNoiseScale + 291.789f
            );

            if (nv < generation.veinThreshold)
                return false;
        }

        return true;
    }

    /// <summary>
    /// Heuristic "score" for how much this material wants this point.
    /// Higher score means better match.
    /// </summary>
    public float EvaluateSuitability(Vector2 worldPos, float baseDensity)
    {
        if (!SupportsPosition(worldPos, baseDensity))
            return float.NegativeInfinity;

        // Prefer densities close to preferredDensity.
        float diff = Mathf.Abs(baseDensity - generation.preferredDensity);
        return -diff;
    }

    public Color EvaluateColor(int px, int py, int size, Vector2 worldPos)
    {
        if (palette == null || palette.Length == 0)
            return Color.magenta;

        int paletteCount = Mathf.Min(palette.Length, 8);

        // -------- 0) Optional pixel shuffle: adjust the sample position --------
        Vector2 samplePos = worldPos;
        if (shuffleStrength > 0f)
        {
            int hx = HashInt(shuffleSeed ^
                             px * 73856093 ^
                             py * 19349663);

            int hy = HashInt(shuffleSeed * 31 ^
                             px * 83492791 ^
                             py * 297121507);

            float rx = (hx & 0xFFFF) / 65535f;
            float ry = (hy & 0xFFFF) / 65535f;

            float maxOffset = shuffleMaxOffset * shuffleStrength;
            float offX = (rx - 0.5f) * 2f * maxOffset;
            float offY = (ry - 0.5f) * 2f * maxOffset;

            samplePos += new Vector2(offX, offY);
        }

        // -------- 1) Band warp + diagonal layer value via Perlin noise --------
        float warp = 0f;
        if (bandWarpAmount != 0f)
        {
            float nWarp = Mathf.PerlinNoise(
                samplePos.x * bandWarpScale,
                samplePos.y * bandWarpScale
            );
            warp = (nWarp - 0.5f) * 2f * bandWarpAmount;
        }

        float diag = samplePos.x + samplePos.y * slopeFactor + warp;
        float side = samplePos.y * sidewaysStretch;

        float nLayer = Mathf.PerlinNoise(diag * layerNoiseScale, side * layerNoiseScale);
        int layers = Mathf.Max(1, layerCount);
        float layerFloat = nLayer * layers;
        int layerIndex = Mathf.Clamp(Mathf.FloorToInt(layerFloat), 0, layers - 1);

        float tLayer = layers > 1 ? (float)layerIndex / (layers - 1) : 0f;

        // Band contrast variation.
        float localMin = minBrightness;
        float localMax = maxBrightness;
        if (bandContrastVariationScale > 0f)
        {
            float cNoise = Mathf.PerlinNoise(
                samplePos.x * bandContrastVariationScale,
                samplePos.y * bandContrastVariationScale
            );

            float contrastFactor = Mathf.Lerp(0.6f, 1.2f, cNoise);
            float mid = (minBrightness + maxBrightness) * 0.5f;
            localMin = Mathf.Lerp(mid, minBrightness, contrastFactor);
            localMax = Mathf.Lerp(mid, maxBrightness, contrastFactor);
        }

        float brightness = Mathf.Lerp(localMin, localMax, tLayer);

        // -------- 2) Pixel-level palette noise --------
        int finalIndex;
        if (pixelNoiseStrength > 0f && maxIndexOffset > 0)
        {
            float localPixelStrength = pixelNoiseStrength;
            if (grainVariationScale > 0f)
            {
                float gNoise = Mathf.PerlinNoise(
                    samplePos.x * grainVariationScale,
                    samplePos.y * grainVariationScale
                );

                localPixelStrength *= Mathf.Lerp(0.4f, 1.0f, gNoise);
            }

            int cx = Mathf.FloorToInt(samplePos.x * pixelCellScale);
            int cy = Mathf.FloorToInt(samplePos.y * pixelCellScale);

            int h = HashInt(
                pixelNoiseSeed ^
                cx ^
                (cy * 73856093) ^
                (px * 19349663) ^
                (py * 83492791)
            );

            float nWhite = (h & 0xFFFF) / 65535f;
            float centered = (nWhite - 0.5f) * 2f * localPixelStrength;

            int offsetRange = Mathf.Max(0, maxIndexOffset);
            int baseIndex = paletteCount / 2;
            int rawOffset = Mathf.RoundToInt(centered * offsetRange);

            finalIndex = Mathf.Clamp(baseIndex + rawOffset, 0, paletteCount - 1);
        }
        else
        {
            finalIndex = paletteCount / 2;
        }

        Color c = palette[finalIndex];

        // -------- 3) Apply layer brightness --------
        c.r *= brightness;
        c.g *= brightness;
        c.b *= brightness;
        c.a = 1f;

        return c;
    }

    private int HashInt(int x)
    {
        unchecked
        {
            x += (x << 10);
            x ^= (x >> 6);
            x += (x << 3);
            x ^= (x >> 11);
            x += (x << 15);
            return x;
        }
    }
}