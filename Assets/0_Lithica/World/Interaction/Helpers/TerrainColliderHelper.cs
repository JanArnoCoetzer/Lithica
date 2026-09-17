using UnityEngine;
using UnityEngine.Tilemaps;

public static class TerrainColliderHelper
{
    public static void ProcessChunkColliders(WorldManager world, Vector2 minWorld, Vector2 maxWorld)
    {
        if (world == null)
            return;

        Transform spriteRoot = world.transform.Find("_SpriteChunks");
        if (spriteRoot == null)
            return;

        int minCellX = Mathf.FloorToInt((minWorld.x - world.transform.position.x) / world.cellSize);
        int maxCellX = Mathf.FloorToInt((maxWorld.x - world.transform.position.x) / world.cellSize);
        int minCellY = Mathf.FloorToInt((minWorld.y - world.transform.position.y) / world.cellSize);
        int maxCellY = Mathf.FloorToInt((maxWorld.y - world.transform.position.y) / world.cellSize);

        int minChunkX = Mathf.Clamp(Mathf.FloorToInt((float)minCellX / world.cellsPerChunk), 0, world.chunkCountX - 1);
        int maxChunkX = Mathf.Clamp(Mathf.FloorToInt((float)maxCellX / world.cellsPerChunk), 0, world.chunkCountX - 1);
        int minChunkY = Mathf.Clamp(Mathf.FloorToInt((float)minCellY / world.cellsPerChunk), 0, world.chunkCountY - 1);
        int maxChunkY = Mathf.Clamp(Mathf.FloorToInt((float)maxCellY / world.cellsPerChunk), 0, world.chunkCountY - 1);

        for (int cy = minChunkY; cy <= maxChunkY; cy++)
        {
            for (int cx = minChunkX; cx <= maxChunkX; cx++)
            {
                Transform chunkTransform = spriteRoot.Find($"SpriteChunk_{cx}_{cy}");
                if (chunkTransform == null)
                    continue;

                Transform tilemapTransform = chunkTransform.Find("Tilemap");
                if (tilemapTransform == null)
                    continue;

                TilemapCollider2D tilemapCollider = tilemapTransform.GetComponent<TilemapCollider2D>();
                if (tilemapCollider != null && tilemapCollider.enabled && tilemapCollider.hasTilemapChanges)
                    tilemapCollider.ProcessTilemapChanges();
            }
        }
    }
}