using UnityEngine;
using UnityEngine.Tilemaps;

public class SpriteBuilder : MonoBehaviour
{
    [Header("Runtime")]
    public bool buildSpritesOnStart = true;

    [Header("Generated Tile")]
    [Min(1)] public int tilePixelSize = 32;

    [Header("Curve Cutting")]
    public bool cutToTerrainShape = true;
    [Min(1)] public int pixelSamplesPerAxis = 1;
    public bool useBilinearDensityForCutting = true;
    [Range(0f, 1f)] public float alphaClipThreshold = 0.5f;

    [Header("Destruction")]
    public bool applyDestructionMask = true;

    [Header("Material Generation")]
    public MaterialGenerator materialGenerator;

    [Header("Rendering")]
    public string sortingLayerName = "line";
    public int orderInLayer = 0;

    [Header("Layers")]
    public string objectLayerName = "Ground";

    [Header("Generation")]
    public bool clearPreviousSpriteChunks = true;

    [Header("Debug Target")]
    public Vector2Int debugTileTimingChunk = Vector2Int.zero;

    private DebugManager debugManager;

    [Header("Sprite Alignment")]
    public float spriteVerticalOffset = 0f;   // in world units, can be ±cellSize/2
    public void SetDebugManager(DebugManager manager)
    {
        debugManager = manager;
    }

    private bool SpriteBuilderDebugEnabled =>
        debugManager != null && debugManager.SpriteBuilderDebug;

    private bool SpriteTileTimingDebugEnabled =>
        debugManager != null && debugManager.SpriteTileTimingDebug;

    private bool SpriteEveryTileTimingDebugEnabled =>
        debugManager != null && debugManager.SpriteEveryTileTimingDebug;

    private void Start()
    {
        if (!Application.isPlaying)
            return;

        if (!buildSpritesOnStart)
            return;

        WorldManager world = GetComponent<WorldManager>();
        if (world == null)
        {
            Debug.LogWarning("SpriteBuilder: No WorldManager found.", this);
            return;
        }

        if (debugManager == null)
            SetDebugManager(world.DebugManager);

        if (materialGenerator == null)
            materialGenerator = GetComponent<MaterialGenerator>();
    }

    private void OnValidate()
    {
        tilePixelSize = Mathf.Max(1, tilePixelSize);
        pixelSamplesPerAxis = Mathf.Max(1, pixelSamplesPerAxis);
        alphaClipThreshold = Mathf.Clamp01(alphaClipThreshold);

        if (string.IsNullOrWhiteSpace(sortingLayerName))
            sortingLayerName = "Default";

        if (string.IsNullOrWhiteSpace(objectLayerName))
            objectLayerName = "Ground";

        if (materialGenerator == null)
            materialGenerator = GetComponent<MaterialGenerator>();

        WorldManager world = GetComponent<WorldManager>();
        if (world != null)
            world.NotifyGenerationSettingsChanged();
    }

    public void GenerateSprites()
    {
        WorldManager world = GetComponent<WorldManager>();
        if (world == null)
        {
            Debug.LogWarning("SpriteBuilder: No WorldManager found.", this);
            return;
        }

        if (debugManager == null)
            SetDebugManager(world.DebugManager);

        if (materialGenerator == null)
            materialGenerator = GetComponent<MaterialGenerator>();

        BuildSpriteChunks(world);
    }

    public void BuildSpriteChunks(WorldManager world)
    {
        if (debugManager == null && world != null)
            SetDebugManager(world.DebugManager);

        bool timingEnabled = SpriteBuilderDebugEnabled;
        StepTiming timing = new StepTiming("BuildSpriteChunks", timingEnabled);
        BuildSpriteChunks(world, timing);
        timing.End(this);
    }

    public void BuildSpriteChunks(WorldManager world, StepTiming timing)
    {
        if (world == null)
        {
            timing.Mark("aborted: world null");
            return;
        }

        if (clearPreviousSpriteChunks)
        {
            ClearSpriteChunks();
            timing.Step("ClearSpriteChunks");
        }

        Transform spriteRoot = GetOrCreateSpriteRoot();
        timing.Step("GetOrCreateSpriteRoot");

        for (int cy = 0; cy < world.chunkCountY; cy++)
        {
            for (int cx = 0; cx < world.chunkCountX; cx++)
            {
                BuildSingleSpriteChunk(world, cx, cy, spriteRoot, timing.Child($"SpriteChunk({cx},{cy})"));
            }
        }

        timing.Step("all sprite chunks built");
    }

    public SpriteChunk BuildSingleSpriteChunk(WorldManager world, int chunkX, int chunkY)
    {
        if (world == null)
        {
            Debug.LogWarning("SpriteBuilder: Cannot build chunk, world is null.", this);
            return null;
        }

        if (debugManager == null)
            SetDebugManager(world.DebugManager);

        StepTiming timing = new StepTiming($"BuildSingleSpriteChunk({chunkX},{chunkY})", SpriteBuilderDebugEnabled);
        Transform spriteRoot = GetOrCreateSpriteRoot();
        SpriteChunk chunk = BuildSingleSpriteChunk(world, chunkX, chunkY, spriteRoot, timing);
        timing.End(this);
        return chunk;
    }

    public SpriteChunk BuildSingleSpriteChunk(WorldManager world, int chunkX, int chunkY, Transform spriteRoot, StepTiming timing)
    {
        if (world == null)
        {
            timing.Mark("aborted: world null");
            return null;
        }

        if (spriteRoot == null)
        {
            spriteRoot = GetOrCreateSpriteRoot();
            timing.Step("GetOrCreateSpriteRoot");
        }

        Transform existing = spriteRoot.Find($"SpriteChunk_{chunkX}_{chunkY}");
        if (existing != null)
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
                Object.DestroyImmediate(existing.gameObject);
            else
                Object.Destroy(existing.gameObject);
#else
            Object.Destroy(existing.gameObject);
#endif
            timing.Step("destroy existing chunk");
        }

        GameObject chunkObj = new GameObject($"SpriteChunk_{chunkX}_{chunkY}");
        chunkObj.transform.SetParent(spriteRoot, false);
        chunkObj.transform.localPosition = new Vector3(
            chunkX * world.ChunkWorldSize,
            chunkY * world.ChunkWorldSize,
            0f
        );
        timing.Step("create obj");

        SpriteChunk chunk = chunkObj.AddComponent<SpriteChunk>();
        chunk.Initialize(world, this, chunkX, chunkY);
        timing.Step("initialize");

        chunk.BuildSprites(timing.Child("BuildSprites"));
        timing.Step("BuildSprites");

        timing.End();
        return chunk;
    }

    public Transform GetOrCreateSpriteRootPublic()
    {
        return GetOrCreateSpriteRoot();
    }

    public TileBase CreateTile(WorldManager world, int globalCellX, int globalCellY)
    {
        return CreateTile(world, globalCellX, globalCellY, default);
    }

    public TileBase CreateTile(WorldManager world, int globalCellX, int globalCellY, StepTiming timing)
    {
        StepTiming tileTiming = timing.IsValid ? timing.Child($"Tile({globalCellX},{globalCellY})") : default;

        Sprite sprite = CreateTileSprite(world, globalCellX, globalCellY, tileTiming.Child("CreateTileSprite"));
        tileTiming.Step("CreateTileSprite");

        if (sprite == null)
        {
            tileTiming.Mark("sprite null");
            tileTiming.End();
            return null;
        }

        Tile tile = ScriptableObject.CreateInstance<Tile>();
        tileTiming.Step("CreateInstance<Tile>");

        tile.sprite = sprite;
        tile.color = Color.white;
        tile.transform = Matrix4x4.identity;
        tile.flags = TileFlags.LockColor;
        tile.colliderType = Tile.ColliderType.Sprite;
        tileTiming.Step("assign tile data");

        tileTiming.End();
        return tile;
    }

    public Sprite CreateTileSprite(WorldManager world, int globalCellX, int globalCellY)
    {
        return CreateTileSprite(world, globalCellX, globalCellY, default);
    }

    public Sprite CreateTileSprite(WorldManager world, int globalCellX, int globalCellY, StepTiming timing)
    {
        if (debugManager == null && world != null)
            SetDebugManager(world.DebugManager);

        if (materialGenerator == null)
            materialGenerator = GetComponent<MaterialGenerator>();

        bool localTimingEnabled =
            timing.IsValid ||
            SpriteTileTimingDebugEnabled;

        StepTiming localTiming = timing.IsValid
            ? timing
            : new StepTiming($"CreateTileSprite({globalCellX},{globalCellY})", localTimingEnabled);

        int size = Mathf.Max(1, tilePixelSize);

        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        texture.filterMode = FilterMode.Point;
        texture.wrapMode = TextureWrapMode.Clamp;
        localTiming.Step("alloc texture");

        Color[] pixels = new Color[size * size];
        localTiming.Step("alloc pixels");

        for (int i = 0; i < pixels.Length; i++)
            pixels[i] = Color.clear;
        localTiming.Step("clear pixels");

        bool hasVisiblePixel = false;

        for (int py = 0; py < size; py++)
        {
            for (int px = 0; px < size; px++)
            {
                bool keepPixel = true;

                if (cutToTerrainShape)
                {
                    float density = SamplePixelDensity(world, globalCellX, globalCellY, px, py, size);
                    keepPixel = density >= alphaClipThreshold;
                }

                if (keepPixel && applyDestructionMask && world != null)
                {
                    if (world.IsDestroyedPixelInCell(globalCellX, globalCellY, px, py))
                        keepPixel = false;
                }

                if (!keepPixel)
                    continue;

                int index = py * size + px;
                pixels[index] = EvaluatePixelColor(world, globalCellX, globalCellY, px, py, size);
                hasVisiblePixel = true;
            }
        }
        localTiming.Step("fill pixels");

        if (!hasVisiblePixel)
        {
            DestroyTextureSafe(texture);
            localTiming.Mark("no visible pixels");
            localTiming.End();
            return null;
        }

        texture.SetPixels(pixels);
        localTiming.Step("texture.SetPixels");

        texture.Apply();
        localTiming.Step("texture.Apply");

        Sprite sprite = Sprite.Create(
            texture,
            new Rect(0f, 0f, size, size),
            new Vector2(0.5f, 0.5f),
            size,
            0,
            SpriteMeshType.FullRect
        );
        localTiming.Step("Sprite.Create");

        localTiming.End();
        return sprite;
    }

    private Color EvaluatePixelColor(WorldManager world, int globalCellX, int globalCellY, int px, int py, int size)
    {
        float u = (px + 0.5f) / size;
        float v = (py + 0.5f) / size;

        Vector2 worldPos = new Vector2(
            world.transform.position.x + (globalCellX + u) * world.cellSize,
            world.transform.position.y + (globalCellY + v) * world.cellSize
        );

        TerrainPixelMaterial material = materialGenerator != null
            ? materialGenerator.ResolveMaterial(world, globalCellX, globalCellY, u, v)
            : null;

        if (material != null)
            return material.EvaluateColor(px, py, size, worldPos);

        return Color.white;
    }

    public bool ShouldDebugTileTiming(int chunkX, int chunkY, int localCellX, int localCellY)
    {
        if (!SpriteTileTimingDebugEnabled)
            return false;

        if (SpriteEveryTileTimingDebugEnabled)
            return true;

        return chunkX == debugTileTimingChunk.x &&
               chunkY == debugTileTimingChunk.y &&
               localCellX == 0 &&
               localCellY == 0;
    }

    private float SamplePixelDensity(WorldManager world, int globalCellX, int globalCellY, int px, int py, int size)
    {
        int samples = Mathf.Max(1, pixelSamplesPerAxis);

        if (samples == 1)
        {
            float u = (px + 0.5f) / size;
            float v = (py + 0.5f) / size;
            return EvaluateDensityAtCellUV(world, globalCellX, globalCellY, u, v);
        }

        float total = 0f;
        int count = 0;

        for (int sy = 0; sy < samples; sy++)
        {
            for (int sx = 0; sx < samples; sx++)
            {
                float u = (px + (sx + 0.5f) / samples) / size;
                float v = (py + (sy + 0.5f) / samples) / size;
                total += EvaluateDensityAtCellUV(world, globalCellX, globalCellY, u, v);
                count++;
            }
        }

        return count > 0 ? total / count : 0f;
    }

    private float EvaluateDensityAtCellUV(WorldManager world, int globalCellX, int globalCellY, float u, float v)
    {
        // Always sample from the precomputed densityField used by the debug mesh
        float bl = world.GetDensityPoint(globalCellX, globalCellY);
        float br = world.GetDensityPoint(globalCellX + 1, globalCellY);
        float tl = world.GetDensityPoint(globalCellX, globalCellY + 1);
        float tr = world.GetDensityPoint(globalCellX + 1, globalCellY + 1);

        float bottom = Mathf.Lerp(bl, br, u);
        float top = Mathf.Lerp(tl, tr, u);
        return Mathf.Lerp(bottom, top, v);
    }

    public void ClearSpriteChunks()
    {
        Transform spriteRoot = transform.Find("_SpriteChunks");
        if (spriteRoot == null)
            return;

#if UNITY_EDITOR
        if (!Application.isPlaying)
            Object.DestroyImmediate(spriteRoot.gameObject);
        else
            Object.Destroy(spriteRoot.gameObject);
#else
        Object.Destroy(spriteRoot.gameObject);
#endif
    }

    public string GetValidatedSortingLayerName()
    {
        if (string.IsNullOrWhiteSpace(sortingLayerName))
            return "Default";

        int id = SortingLayer.NameToID(sortingLayerName);
        return id == 0 && sortingLayerName != "Default" ? "Default" : sortingLayerName;
    }

    public int GetValidatedObjectLayer()
    {
        if (string.IsNullOrWhiteSpace(objectLayerName))
            return LayerMask.NameToLayer("Ground");

        int layer = LayerMask.NameToLayer(objectLayerName);
        if (layer == -1)
            layer = LayerMask.NameToLayer("Default");

        return layer;
    }

    private Transform GetOrCreateSpriteRoot()
    {
        Transform spriteRoot = transform.Find("_SpriteChunks");
        if (spriteRoot != null)
            return spriteRoot;

        GameObject root = new GameObject("_SpriteChunks");
        root.transform.SetParent(transform, false);
        return root.transform;
    }

    private void DestroyTextureSafe(Texture2D texture)
    {
        if (texture == null)
            return;

#if UNITY_EDITOR
        if (!Application.isPlaying)
            Object.DestroyImmediate(texture);
        else
            Object.Destroy(texture);
#else
        Object.Destroy(texture);
#endif
    }
}