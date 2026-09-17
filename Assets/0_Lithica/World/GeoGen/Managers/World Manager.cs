using System.Collections.Generic;
using UnityEngine;
using Unity.Collections;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

public class WorldManager : MonoBehaviour
{
    [Header("Chunk Settings")]
    [Min(1)] public int chunkCountX = 4;
    [Min(1)] public int chunkCountY = 4;
    [Min(2)] public int cellsPerChunk = 16;
    [Min(0.05f)] public float cellSize = 1f;

    [Header("References")]
    public Material terrainMaterial;
    public TerrainGenerationManager generationManager;
    public SpriteBuilder spriteBuilder;
    public DebugManager debugManager;
    public PlayerInteraction playerInteraction;

    public DebugManager DebugManager => debugManager;

    [Header("Runtime")]
    public bool generateSpritesOnRun = true;
    public bool RuntimeSpritesBuilt => runtimeSpritesBuilt;

    [Header("Generation")]
    public bool clearPreviousChunksBeforeGeneration = true;

    [Header("Mesh Debug")]
    public bool buildDebugMeshInEditor = true;
    public float meshZ = 0f;

    [Header("Editor")]
    public bool autoRegenerateInEditor = true;
    public bool drawDensityPoints = true;
    public bool drawChunkBounds = true;
    [Min(0.001f)] public float densityPointRadius = 0.05f;

    [Header("Gizmos")]
    public Color solidPointColor = new Color(0.2f, 1f, 0.2f, 1f);
    public Color emptyPointColor = new Color(1f, 0.35f, 0.35f, 0.8f);
    public Color chunkLineColor = new Color(0.2f, 0.85f, 1f, 1f);

    [Header("Destruction")]
    public bool enableDestruction = true;

    [HideInInspector] public List<MarchingSquaresChunk> chunks = new List<MarchingSquaresChunk>();
    [HideInInspector] public bool[,] destroyedPixels;

    // NOTE: points for marching squares, cells = points - 1
    private float[,] densityField;

    // NEW: per-cell material ids written by filters.
    // Layout: [cellX, cellY] with dimensions TotalCellsX x TotalCellsY.
    [HideInInspector] public int[,] materialIds;

    private bool runtimeSpritesBuilt;

#if UNITY_EDITOR
    private bool regenerationQueued;
#endif

    private bool WorldRuntimeDebugEnabled =>
        debugManager != null && debugManager.WorldRuntimeDebug;

    private bool WorldBreakDebugEnabled =>
        debugManager != null && debugManager.WorldBreakDebug;

    public float IsoLevel => generationManager != null ? generationManager.isoLevel : 0.5f;
    public float ChunkWorldSize => cellsPerChunk * cellSize;
    public float TotalMapWidth => chunkCountX * cellsPerChunk * cellSize;
    public float TotalMapHeight => chunkCountY * cellsPerChunk * cellSize;

    // Points = cells + 1
    public int TotalPointsX => chunkCountX * cellsPerChunk + 1;
    public int TotalPointsY => chunkCountY * cellsPerChunk + 1;

    // Cells
    public int TotalCellsX => chunkCountX * cellsPerChunk;
    public int TotalCellsY => chunkCountY * cellsPerChunk;

    public int DamageResolutionX => TotalCellsX * TilePixelSize;
    public int DamageResolutionY => TotalCellsY * TilePixelSize;

    public int TilePixelSize
    {
        get
        {
            if (spriteBuilder == null)
                spriteBuilder = GetComponent<SpriteBuilder>();

            return spriteBuilder != null ? Mathf.Max(1, spriteBuilder.tilePixelSize) : 1;
        }
    }

    private void Awake()
    {
        ResolveReferences();
        PushSharedReferences();
    }

    private void OnEnable()
    {
        ResolveReferences();
        PushSharedReferences();
    }

    private void Start()
    {
        ClearAllGenerated();
        if (!Application.isPlaying)
            return;

        if (generateSpritesOnRun)
        {
            GenerateSprites();
        }
    }

    public void ResolveGenerationDependenciesForRuntime()
    {
        ResolveReferences();
        PushSharedReferences();
    }

    public void BuildDensityFieldForRuntime()

    {
        BuildDensityField();
    }

    public void MarkRuntimeSpritesBuilt(bool built)
    {
        runtimeSpritesBuilt = built;
    }

    private void OnDisable()
    {
        if (!Application.isPlaying)
            return;

        runtimeSpritesBuilt = false;
        RuntimeLog("OnDisable | runtimeSpritesBuilt reset to false.");
    }

    private void OnValidate()
    {
        chunkCountX = Mathf.Max(1, chunkCountX);
        chunkCountY = Mathf.Max(1, chunkCountY);
        cellsPerChunk = Mathf.Max(2, cellsPerChunk);
        cellSize = Mathf.Max(0.05f, cellSize);
        densityPointRadius = Mathf.Max(0.001f, densityPointRadius);

        ResolveReferences();
        PushSharedReferences();

#if UNITY_EDITOR
        if (!Application.isPlaying && autoRegenerateInEditor)
            QueueEditorRegeneration();
#endif
    }

    private void ResolveReferences()
    {
        if (generationManager == null)
            generationManager = GetComponent<TerrainGenerationManager>();

        if (spriteBuilder == null)
            spriteBuilder = GetComponent<SpriteBuilder>();

        if (debugManager == null)
            debugManager = GetComponentInParent<DebugManager>();

        if (playerInteraction == null)
            playerInteraction = GetComponentInChildren<PlayerInteraction>(true);
    }

    private void PushSharedReferences()
    {
        if (spriteBuilder != null)
            spriteBuilder.SetDebugManager(debugManager);

        TerrainInteractionManager terrainInteraction = GetComponentInChildren<TerrainInteractionManager>(true);
        if (terrainInteraction != null)
            terrainInteraction.SetDebugManager(debugManager);

        if (playerInteraction != null)
            playerInteraction.SetDebugManager(debugManager);

        if (playerInteraction != null && terrainInteraction != null)
            playerInteraction.SetTerrainInteraction(terrainInteraction);
    }

    [ContextMenu("Generate Debug Mesh")]
    public void GenerateDebugMesh()
    {
        StepTiming timing = new StepTiming("GenerateDebugMesh", WorldRuntimeDebugEnabled);

        if (generationManager == null)
            generationManager = GetComponent<TerrainGenerationManager>();
        timing.Step("resolve generationManager");

        if (generationManager == null)
        {
            Debug.LogWarning("WorldManager: No TerrainGenerationManager assigned.", this);
            timing.Mark("aborted: no generationManager");
            timing.End(this);
            return;
        }

        BuildDensityField(timing.Child("BuildDensityField"));

        if (buildDebugMeshInEditor)
            RebuildChunks(timing.Child("RebuildChunks"));

#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            SceneView.RepaintAll();
            timing.Step("SceneView.RepaintAll");
        }
#endif

        timing.End(this);
    }

    [ContextMenu("Clear Debug Mesh")]
    public void ClearDebugMesh()
    {
        chunks.Clear();

        Transform debugRoot = transform.Find("_DebugMesh");
        if (debugRoot != null)
            DestroyHierarchyRoot(debugRoot.gameObject);

        MarkEditorDirty();
    }

    [ContextMenu("Generate Sprites")]
    public void GenerateSprites(bool repaintSceneView = false)
    {
        StepTiming timing = new StepTiming("GenerateSprites", WorldRuntimeDebugEnabled);

        RuntimeLog("GenerateSprites() called.");

        if (generationManager == null)
            generationManager = GetComponent<TerrainGenerationManager>();
        timing.Step("resolve generationManager");

        if (spriteBuilder == null)
            spriteBuilder = GetComponent<SpriteBuilder>();
        timing.Step("resolve spriteBuilder");

        if (generationManager == null)
        {
            Debug.LogWarning("WorldManager: No TerrainGenerationManager assigned.", this);
            timing.Mark("aborted: no generationManager");
            timing.End(this);
            return;
        }

        if (spriteBuilder == null)
        {
            Debug.LogWarning("WorldManager: No SpriteBuilder assigned.", this);
            timing.Mark("aborted: no spriteBuilder");
            timing.End(this);
            return;
        }

        spriteBuilder.SetDebugManager(debugManager);

        BuildDensityField(timing.Child("BuildDensityField"));

        if (clearPreviousChunksBeforeGeneration)
            spriteBuilder.ClearSpriteChunks();

        spriteBuilder.BuildSpriteChunks(this, timing.Child("BuildSpriteChunks"));

        Transform spriteRoot = transform.Find("_SpriteChunks");
        RuntimeLog($"GenerateSprites | spriteRoot found = {spriteRoot != null}");
        if (spriteRoot != null)
            RuntimeLog($"GenerateSprites | spriteRoot child count = {spriteRoot.childCount}");

        runtimeSpritesBuilt = true;

#if UNITY_EDITOR
        if (repaintSceneView && !Application.isPlaying)
        {
            SceneView.lastActiveSceneView?.Repaint();
            timing.Step("SceneView.Repaint");
        }
#endif

        timing.End(this);
    }

    [ContextMenu("Clear Sprites")]
    public void ClearSprites()
    {
        if (spriteBuilder == null)
            spriteBuilder = GetComponent<SpriteBuilder>();

        if (spriteBuilder == null)
        {
            Debug.LogWarning("WorldManager: No SpriteBuilder assigned.", this);
            RuntimeLog("ClearSprites() aborted: spriteBuilder missing.");
            return;
        }

        RuntimeLog("Clearing sprite chunks.");
        spriteBuilder.ClearSpriteChunks();
        runtimeSpritesBuilt = false;
        MarkEditorDirty();
    }

    [ContextMenu("Clear All Generated")]
    public void ClearAllGenerated()
    {
        RuntimeLog("ClearAllGenerated() called.");
        ClearDebugMesh();
        ClearSprites();
        MarkEditorDirty();
    }

    public void NotifyGenerationSettingsChanged()
    {
#if UNITY_EDITOR
        if (Application.isPlaying)
            return;

        if (!autoRegenerateInEditor)
            return;

        QueueEditorRegeneration();
#endif
    }

#if UNITY_EDITOR
    private void QueueEditorRegeneration()
    {
        if (regenerationQueued)
            return;

        regenerationQueued = true;
        EditorApplication.delayCall += DelayedEditorRegenerate;
    }

    private void DelayedEditorRegenerate()
    {
        regenerationQueued = false;

        if (this == null)
            return;

        if (Application.isPlaying)
            return;

        if (!autoRegenerateInEditor)
            return;

        GenerateDebugMesh();

        EditorUtility.SetDirty(this);
        EditorSceneManager.MarkSceneDirty(gameObject.scene);
        SceneView.RepaintAll();
    }
#endif

    public float GetDensityPoint(int globalX, int globalY)
    {
        if (densityField == null || densityField.GetLength(0) != TotalPointsX || densityField.GetLength(1) != TotalPointsY)
            BuildDensityField();

        globalX = Mathf.Clamp(globalX, 0, TotalPointsX - 1);
        globalY = Mathf.Clamp(globalY, 0, TotalPointsY - 1);
        return densityField[globalX, globalY];
    }

    public Vector3 GetWorldPointPosition(int globalX, int globalY)
    {
        return transform.position + new Vector3(globalX * cellSize, globalY * cellSize, 0f);
    }

    public Vector3 GetCellCenterWorldPosition(int globalCellX, int globalCellY)
    {
        return transform.position + new Vector3(
            (globalCellX + 0.5f) * cellSize,
            (globalCellY + 0.5f) * cellSize,
            0f
        );
    }

    public bool IsSolid(float density)
    {
        return density >= IsoLevel;
    }

    // NEW: ensure materialIds array matches current cell resolution.
    public void EnsureMaterialField()
    {
        if (materialIds != null &&
            materialIds.GetLength(0) == TotalCellsX &&
            materialIds.GetLength(1) == TotalCellsY)
            return;

        materialIds = new int[TotalCellsX, TotalCellsY];
    }

    // NEW: set material id for a cell (called from filters).
    public void SetCellMaterialId(int cellX, int cellY, int materialId)
    {
        EnsureMaterialField();

        if (cellX < 0 || cellX >= TotalCellsX ||
            cellY < 0 || cellY >= TotalCellsY)
            return;

        materialIds[cellX, cellY] = materialId;
    }

    // NEW: get material id for a cell (used by MaterialGenerator).
    public int GetCellMaterialId(int cellX, int cellY)
    {
        if (materialIds == null)
            return 0;

        if (cellX < 0 || cellX >= TotalCellsX ||
            cellY < 0 || cellY >= TotalCellsY)
            return 0;

        return materialIds[cellX, cellY];
    }

    private void BuildDensityField()
    {
        StepTiming timing = new StepTiming("BuildDensityField", WorldRuntimeDebugEnabled);
        BuildDensityField(timing);
        timing.End(this);
    }

    private void BuildDensityField(StepTiming timing)
    {
        densityField = new float[TotalPointsX, TotalPointsY];
        timing.Step("alloc densityField");


        for (int y = 0; y < TotalPointsY; y++)
        {
            for (int x = 0; x < TotalPointsX; x++)
            {
                Vector2 worldPos = new Vector2(
                    transform.position.x + x * cellSize,
                    transform.position.y + y * cellSize
                );

                densityField[x, y] = generationManager.EvaluateDensity(worldPos, this);
            }
        }

        timing.Step("fill densityField");
    }

    private void RebuildChunks()
    {
        StepTiming timing = new StepTiming("RebuildChunks", WorldRuntimeDebugEnabled);
        RebuildChunks(timing);
        timing.End(this);
    }

    private void RebuildChunks(StepTiming timing)
    {
        ClearDebugMesh();
        timing.Step("ClearDebugMesh");

        GameObject debugRootObj = new GameObject("_DebugMesh");
        debugRootObj.transform.SetParent(transform, false);
        Transform debugRoot = debugRootObj.transform;
        timing.Step("create debug root");

        for (int cy = 0; cy < chunkCountY; cy++)
        {
            for (int cx = 0; cx < chunkCountX; cx++)
            {
                StepTiming chunkTiming = timing.Child($"Chunk({cx},{cy})");

                GameObject chunkObj = new GameObject($"Chunk_{cx}_{cy}");
                chunkObj.transform.SetParent(debugRoot, false);
                chunkObj.transform.localPosition = new Vector3(cx * ChunkWorldSize, cy * ChunkWorldSize, 0f);
                chunkTiming.Step("create obj");

                MarchingSquaresChunk chunk = chunkObj.AddComponent<MarchingSquaresChunk>();
                chunk.Initialize(this, cx, cy, terrainMaterial, meshZ);
                chunkTiming.Step("init");

                chunk.BuildMesh(chunkTiming.Child("BuildMesh"));

                chunks.Add(chunk);
                chunkTiming.Step("add chunk");
            }
        }

        timing.Step("all chunks built");
    }

    private void DestroyHierarchyRoot(GameObject root)
    {
        if (root == null)
            return;

#if UNITY_EDITOR
        if (!Application.isPlaying)
            DestroyImmediate(root);
        else
            Destroy(root);
#else
        Destroy(root);
#endif
    }

    private void OnDrawGizmos()
    {
        if (generationManager == null)
            generationManager = GetComponent<TerrainGenerationManager>();

        if (generationManager == null)
            return;

        if ((drawDensityPoints || drawChunkBounds) &&
            (densityField == null || densityField.GetLength(0) != TotalPointsX || densityField.GetLength(1) != TotalPointsY))
        {
            BuildDensityField();
        }

        if (drawDensityPoints)
        {
            for (int y = 0; y < TotalPointsY; y++)
            {
                for (int x = 0; x < TotalPointsX; x++)
                {
                    Gizmos.color = densityField[x, y] >= IsoLevel ? solidPointColor : emptyPointColor;
                    Gizmos.DrawSphere(GetWorldPointPosition(x, y), densityPointRadius);
                }
            }
        }

        if (drawChunkBounds)
        {
            Gizmos.color = chunkLineColor;

            for (int cy = 0; cy < chunkCountY; cy++)
            {
                for (int cx = 0; cx < chunkCountX; cx++)
                {
                    Vector3 min = transform.position + new Vector3(cx * ChunkWorldSize, cy * ChunkWorldSize, 0f);
                    Vector3 size = new Vector3(ChunkWorldSize, ChunkWorldSize, 0f);
                    Vector3 center = min + new Vector3(size.x * 0.5f, size.y * 0.5f, 0f);
                    Gizmos.DrawWireCube(center, size);
                }
            }
        }
    }

    private void MarkEditorDirty()
    {
#if UNITY_EDITOR
        if (Application.isPlaying)
            return;

        EditorUtility.SetDirty(this);
        EditorSceneManager.MarkSceneDirty(gameObject.scene);
        SceneView.RepaintAll();
#endif
    }

    private void RuntimeLog(string message)
    {
        if (!WorldRuntimeDebugEnabled)
            return;

        Debug.Log($"[WorldManager] {message}", this);
    }

    public void EnsureDamageMask()
    {
        if (!enableDestruction)
            return;

        if (destroyedPixels != null &&
            destroyedPixels.GetLength(0) == DamageResolutionX &&
            destroyedPixels.GetLength(1) == DamageResolutionY)
            return;

        destroyedPixels = new bool[DamageResolutionX, DamageResolutionY];
    }

    public void ClearDamageMask()
    {
        destroyedPixels = null;
    }

    public bool SetDestroyedPixel(int px, int py, bool destroyed)
    {
        if (!enableDestruction)
            return false;

        EnsureDamageMask();

        if (px < 0 || py < 0 || px >= DamageResolutionX || py >= DamageResolutionY)
            return false;

        if (destroyedPixels[px, py] == destroyed)
            return false;

        destroyedPixels[px, py] = destroyed;
        return true;
    }

    public bool IsDestroyedPixel(int px, int py)
    {
        if (!enableDestruction || destroyedPixels == null)
            return false;

        if (px < 0 || py < 0 || px >= DamageResolutionX || py >= DamageResolutionY)
            return false;

        return destroyedPixels[px, py];
    }

    public bool IsDestroyedPixelInCell(int globalCellX, int globalCellY, int localPixelX, int localPixelY)
    {
        int px = globalCellX * TilePixelSize + localPixelX;
        int py = globalCellY * TilePixelSize + localPixelY;
        return IsDestroyedPixel(px, py);
    }

    public int WorldToDamagePixelX(float worldX)
    {
        float localX = worldX - transform.position.x;
        float pixelsPerWorldUnit = TilePixelSize / cellSize;
        return Mathf.FloorToInt(localX * pixelsPerWorldUnit);
    }

    public int WorldToDamagePixelY(float worldY)
    {
        float localY = worldY - transform.position.y;
        float pixelsPerWorldUnit = TilePixelSize / cellSize;
        return Mathf.FloorToInt(localY * pixelsPerWorldUnit);
    }

    public Vector2 DamagePixelToWorldCenter(int px, int py)
    {
        float pixelSize = cellSize / TilePixelSize;

        return new Vector2(
            transform.position.x + (px + 0.5f) * pixelSize,
            transform.position.y + (py + 0.5f) * pixelSize
        );
    }

    public void RebuildSpriteChunksOverlappingWorldBounds(Vector2 minWorld, Vector2 maxWorld)
    {
        StepTiming timing = new StepTiming("RebuildSpriteChunksOverlappingWorldBounds", WorldBreakDebugEnabled);

        if (spriteBuilder == null)
            spriteBuilder = GetComponent<SpriteBuilder>();
        timing.Step("resolve spriteBuilder");

        if (spriteBuilder == null)
        {
            timing.End(this);
            return;
        }

        Transform spriteRoot = transform.Find("_SpriteChunks");
        timing.Step("find sprite root");

        if (spriteRoot == null)
        {
            timing.End(this);
            return;
        }

        int totalCellsX = chunkCountX * cellsPerChunk;
        int totalCellsY = chunkCountY * cellsPerChunk;

        int minCellX = Mathf.FloorToInt((minWorld.x - transform.position.x) / cellSize);
        int maxCellX = Mathf.FloorToInt((maxWorld.x - transform.position.x) / cellSize);
        int minCellY = Mathf.FloorToInt((minWorld.y - transform.position.y) / cellSize);
        int maxCellY = Mathf.FloorToInt((maxWorld.y - transform.position.y) / cellSize);

        minCellX = Mathf.Clamp(minCellX, 0, totalCellsX - 1);
        maxCellX = Mathf.Clamp(maxCellX, 0, totalCellsX - 1);
        minCellY = Mathf.Clamp(minCellY, 0, totalCellsY - 1);
        maxCellY = Mathf.Clamp(maxCellY, 0, totalCellsY - 1);
        timing.Step("calc cell bounds");

        int minChunkX = Mathf.FloorToInt((float)minCellX / cellsPerChunk);
        int maxChunkX = Mathf.FloorToInt((float)maxCellX / cellsPerChunk);
        int minChunkY = Mathf.FloorToInt((float)minCellY / cellsPerChunk);
        int maxChunkY = Mathf.FloorToInt((float)maxCellY / cellsPerChunk);

        minChunkX = Mathf.Clamp(minChunkX, 0, chunkCountX - 1);
        maxChunkX = Mathf.Clamp(maxChunkX, 0, chunkCountX - 1);
        minChunkY = Mathf.Clamp(minChunkY, 0, chunkCountY - 1);
        maxChunkY = Mathf.Clamp(maxChunkY, 0, chunkCountY - 1);
        timing.Step("calc chunk bounds");

        for (int cy = minChunkY; cy <= maxChunkY; cy++)
        {
            for (int cx = minChunkX; cx <= maxChunkX; cx++)
            {
                Transform chunkTransform = spriteRoot.Find($"SpriteChunk_{cx}_{cy}");
                if (chunkTransform == null)
                    continue;

                SpriteChunk chunk = chunkTransform.GetComponent<SpriteChunk>();
                if (chunk == null)
                    continue;

                int chunkStartX = cx * cellsPerChunk;
                int chunkStartY = cy * cellsPerChunk;

                int localMinX = Mathf.Max(0, minCellX - chunkStartX);
                int localMaxX = Mathf.Min(cellsPerChunk - 1, maxCellX - chunkStartX);
                int localMinY = Mathf.Max(0, minCellY - chunkStartY);
                int localMaxY = Mathf.Min(cellsPerChunk - 1, maxCellY - chunkStartY);

                chunk.BuildSpritesPartial(localMinX, localMaxX, localMinY, localMaxY);
            }
        }

        timing.Step("rebuild partial sprite chunks");
        timing.End(this);
    }

    public NativeArray<float> GetDensityFieldNative(Allocator allocator)
    {
        StepTiming timing = new StepTiming("GetDensityFieldNative", WorldRuntimeDebugEnabled);
        NativeArray<float> result = GetDensityFieldNative(allocator, timing);
        timing.End(this);
        return result;
    }

    public NativeArray<float> GetDensityFieldNative(Allocator allocator, StepTiming timing)
    {
        if (densityField == null || densityField.GetLength(0) != TotalPointsX || densityField.GetLength(1) != TotalPointsY)
            BuildDensityField(timing.Child("BuildDensityField"));
        timing.Step("ensure densityField");

        NativeArray<float> result = new NativeArray<float>(TotalPointsX * TotalPointsY, allocator);
        timing.Step("alloc native");

        for (int y = 0; y < TotalPointsY; y++)
        {
            for (int x = 0; x < TotalPointsX; x++)
            {
                result[y * TotalPointsX + x] = densityField[x, y];
            }
        }

        timing.Step("copy densityField");
        return result;
    }
}