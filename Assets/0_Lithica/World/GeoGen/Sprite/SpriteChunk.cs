using UnityEngine;
using UnityEngine.Tilemaps;

#if UNITY_EDITOR
using UnityEditor;
#endif

[ExecuteAlways]
public class SpriteChunk : MonoBehaviour
{
    private WorldManager world;
    private SpriteBuilder builder;
    private int chunkX;
    private int chunkY;

    private Grid grid;
    private Tilemap tilemap;
    private TilemapRenderer tilemapRenderer;
    private TilemapCollider2D tilemapCollider;
    private Rigidbody2D rb;
    private CompositeCollider2D compositeCollider;

    private bool setupComplete;

    public int ChunkX => chunkX;
    public int ChunkY => chunkY;
    public WorldManager World => world;
    public SpriteBuilder Builder => builder;
    public Tilemap Tilemap => tilemap;

    public void Initialize(WorldManager world, SpriteBuilder builder, int chunkX, int chunkY)
    {
        this.world = world;
        this.builder = builder;
        this.chunkX = chunkX;
        this.chunkY = chunkY;

        setupComplete = false;
        EnsureTilemapSetup();
    }

    public void BuildSprites()
    {
        BuildSprites(default);
    }

    public void BuildSprites(StepTiming timing)
    {
        if (world == null || builder == null)
        {
            timing.Mark("aborted: missing refs");
            return;
        }

        EnsureTilemapSetup(timing.Child("EnsureTilemapSetup"));
        timing.Step("EnsureTilemapSetup");

        int width = world.cellsPerChunk;
        int height = world.cellsPerChunk;
        int total = width * height;

        Vector3Int[] positions = new Vector3Int[total];
        TileBase[] tiles = new TileBase[total];
        timing.Step("alloc arrays");

        int startX = chunkX * width;
        int startY = chunkY * height;

        int index = 0;

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                int gx = startX + x;
                int gy = startY + y;

                positions[index] = new Vector3Int(x, y, 0);

                if (TileTouchesTerrain(gx, gy))
                {
                    bool debugThisTile = builder.ShouldDebugTileTiming(chunkX, chunkY, x, y);
                    StepTiming tileTiming = debugThisTile
                        ? timing.Child($"Tile({gx},{gy})")
                        : default;

                    tiles[index] = builder.CreateTile(world, gx, gy, tileTiming);
                }
                else
                {
                    tiles[index] = null;
                }

                index++;
            }
        }
        timing.Step("build tile array");

        tilemap.SetTiles(positions, tiles);
        timing.Step("tilemap.SetTiles");

        tilemap.RefreshAllTiles();
        timing.Step("tilemap.RefreshAllTiles");

        if (tilemapCollider != null && tilemapCollider.enabled && tilemapCollider.hasTilemapChanges)
        {
            tilemapCollider.ProcessTilemapChanges();
            timing.Step("tilemapCollider.ProcessTilemapChanges");
        }

#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            SceneView.RepaintAll();
            timing.Step("SceneView.RepaintAll");
        }
#endif

        timing.End();
    }

    public void ClearTiles()
    {
        ClearTiles(default);
    }

    public void ClearTiles(StepTiming timing)
    {
        EnsureTilemapSetup(timing.Child("EnsureTilemapSetup"));
        timing.Step("EnsureTilemapSetup");

        if (tilemap != null)
        {
            tilemap.ClearAllTiles();
            timing.Step("tilemap.ClearAllTiles");

            tilemap.RefreshAllTiles();
            timing.Step("tilemap.RefreshAllTiles");
        }

        if (tilemapCollider != null && tilemapCollider.enabled && tilemapCollider.hasTilemapChanges)
        {
            tilemapCollider.ProcessTilemapChanges();
            timing.Step("tilemapCollider.ProcessTilemapChanges");
        }

#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            SceneView.RepaintAll();
            timing.Step("SceneView.RepaintAll");
        }
#endif

        timing.End();
    }

    public void BuildSpritesPartial(int minX, int maxX, int minY, int maxY)
    {
        BuildSpritesPartial(minX, maxX, minY, maxY, default);
    }

    public void BuildSpritesPartial(int minX, int maxX, int minY, int maxY, StepTiming timing)
    {
        if (world == null || builder == null)
        {
            timing.Mark("aborted: missing refs");
            return;
        }

        EnsureTilemapSetup(timing.Child("EnsureTilemapSetup"));
        timing.Step("EnsureTilemapSetup");

        minX = Mathf.Clamp(minX, 0, world.cellsPerChunk - 1);
        maxX = Mathf.Clamp(maxX, 0, world.cellsPerChunk - 1);
        minY = Mathf.Clamp(minY, 0, world.cellsPerChunk - 1);
        maxY = Mathf.Clamp(maxY, 0, world.cellsPerChunk - 1);
        timing.Step("clamp bounds");

        if (minX > maxX || minY > maxY)
        {
            timing.Mark("aborted: invalid bounds");
            timing.End();
            return;
        }

        int startX = chunkX * world.cellsPerChunk;
        int startY = chunkY * world.cellsPerChunk;

        int width = maxX - minX + 1;
        int height = maxY - minY + 1;
        int total = width * height;

        Vector3Int[] positions = new Vector3Int[total];
        TileBase[] tiles = new TileBase[total];
        timing.Step("alloc arrays");

        int index = 0;

        for (int y = minY; y <= maxY; y++)
        {
            for (int x = minX; x <= maxX; x++)
            {
                int gx = startX + x;
                int gy = startY + y;

                positions[index] = new Vector3Int(x, y, 0);

                if (TileTouchesTerrain(gx, gy))
                {
                    bool debugThisTile = builder.ShouldDebugTileTiming(chunkX, chunkY, x, y);
                    StepTiming tileTiming = debugThisTile
                        ? timing.Child($"Tile({gx},{gy})")
                        : default;

                    tiles[index] = builder.CreateTile(world, gx, gy, tileTiming);
                }
                else
                {
                    tiles[index] = null;
                }

                index++;
            }
        }
        timing.Step("build tile array");

        tilemap.SetTiles(positions, tiles);
        timing.Step("tilemap.SetTiles");

        tilemap.RefreshAllTiles();
        timing.Step("tilemap.RefreshAllTiles");

        if (tilemapCollider != null && tilemapCollider.enabled && tilemapCollider.hasTilemapChanges)
        {
            tilemapCollider.ProcessTilemapChanges();
            timing.Step("tilemapCollider.ProcessTilemapChanges");
        }

#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            SceneView.RepaintAll();
            timing.Step("SceneView.RepaintAll");
        }
#endif

        timing.End();
    }

    public Bounds GetChunkWorldBounds()
    {
        if (world == null)
            return new Bounds(transform.position, Vector3.zero);

        Vector3 size = new Vector3(world.ChunkWorldSize, world.ChunkWorldSize, 0f);
        Vector3 center = transform.position + new Vector3(size.x * 0.5f, size.y * 0.5f, 0f);
        return new Bounds(center, size);
    }

    public bool ContainsGlobalCell(int globalCellX, int globalCellY)
    {
        if (world == null)
            return false;

        int startX = chunkX * world.cellsPerChunk;
        int startY = chunkY * world.cellsPerChunk;
        int endX = startX + world.cellsPerChunk;
        int endY = startY + world.cellsPerChunk;

        return globalCellX >= startX && globalCellX < endX &&
               globalCellY >= startY && globalCellY < endY;
    }

    private bool TileTouchesTerrain(int gx, int gy)
    {
        float bl = world.GetDensityPoint(gx, gy);
        float br = world.GetDensityPoint(gx + 1, gy);
        float tr = world.GetDensityPoint(gx + 1, gy + 1);
        float tl = world.GetDensityPoint(gx, gy + 1);

        return world.IsSolid(bl) || world.IsSolid(br) || world.IsSolid(tr) || world.IsSolid(tl);
    }

    private void EnsureTilemapSetup()
    {
        EnsureTilemapSetup(default);
    }

    private void EnsureTilemapSetup(StepTiming timing)
    {
        if (setupComplete &&
            grid != null &&
            tilemap != null &&
            tilemapRenderer != null &&
            tilemapCollider != null &&
            rb != null &&
            compositeCollider != null)
        {
            timing.Mark("already setup");
            return;
        }

        grid = GetComponent<Grid>();
        if (grid == null)
            grid = gameObject.AddComponent<Grid>();
        timing.Step("grid");

        float size = world != null ? world.cellSize : 1f;
        grid.cellSize = new Vector3(size, size, 1f);
        timing.Step("grid.cellSize");

        Transform tilemapChild = transform.Find("Tilemap");
        GameObject tilemapObj;

        if (tilemapChild == null)
        {
            tilemapObj = new GameObject("Tilemap");
            tilemapObj.transform.SetParent(transform, false);
        }
        else
        {
            tilemapObj = tilemapChild.gameObject;
        }
        timing.Step("tilemap child");

        ApplyConfiguredLayer(gameObject);
        ApplyConfiguredLayer(tilemapObj);
        timing.Step("ApplyConfiguredLayer");

        tilemap = tilemapObj.GetComponent<Tilemap>();
        if (tilemap == null)
            tilemap = tilemapObj.AddComponent<Tilemap>();
        timing.Step("Tilemap");

        TilemapRenderer existingRenderer = tilemapObj.GetComponent<TilemapRenderer>();
        if (existingRenderer == null)
            existingRenderer = tilemapObj.AddComponent<TilemapRenderer>();
        tilemapRenderer = existingRenderer;
        timing.Step("TilemapRenderer");

        TilemapCollider2D existingCollider = tilemapObj.GetComponent<TilemapCollider2D>();
        if (existingCollider == null)
            existingCollider = tilemapObj.AddComponent<TilemapCollider2D>();
        tilemapCollider = existingCollider;
        timing.Step("TilemapCollider2D");

        Rigidbody2D existingRb = tilemapObj.GetComponent<Rigidbody2D>();
        if (existingRb == null)
            existingRb = tilemapObj.AddComponent<Rigidbody2D>();
        rb = existingRb;
        timing.Step("Rigidbody2D");

        rb.bodyType = RigidbodyType2D.Static;
        rb.simulated = true;
        timing.Step("Rigidbody2D config");

        CompositeCollider2D existingComposite = tilemapObj.GetComponent<CompositeCollider2D>();
        if (existingComposite == null)
            existingComposite = tilemapObj.AddComponent<CompositeCollider2D>();
        compositeCollider = existingComposite;
        timing.Step("CompositeCollider2D");

        tilemapCollider.usedByComposite = true;
        compositeCollider.geometryType = CompositeCollider2D.GeometryType.Polygons;
        compositeCollider.generationType = CompositeCollider2D.GenerationType.Synchronous;
        timing.Step("collider config");

        tilemapRenderer.sortingLayerName = builder != null ? builder.GetValidatedSortingLayerName() : "Default";
        tilemapRenderer.sortingOrder = builder != null ? builder.orderInLayer : 0;
        timing.Step("renderer config");

        setupComplete = true;
        timing.Mark("setup complete");
    }

    private void ApplyConfiguredLayer(GameObject target)
    {
        if (target == null)
            return;

        int layer = builder != null ? builder.GetValidatedObjectLayer() : LayerMask.NameToLayer("Ground");
        if (layer == -1)
            layer = LayerMask.NameToLayer("Default");

        target.layer = layer;
    }
}