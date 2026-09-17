using UnityEngine;

public class DebugManager : MonoBehaviour
{
    [Header("Player Interaction")]
    [SerializeField] private bool playerInteractionDebug = true;
    [SerializeField] private bool playerInteractionToolGizmos = true;

    [Header("Terrain Interaction")]
    [SerializeField] private bool terrainInteractionDebug = true;

    [Header("World Manager")]
    [SerializeField] private bool worldRuntimeDebug = true;
    [SerializeField] private bool worldBreakDebug = true;

    [Header("Sprite Builder")]
    [SerializeField] private bool spriteBuilderDebug = false;
    [SerializeField] private bool spriteTileTimingDebug = false;
    [SerializeField] private bool spriteEveryTileTimingDebug = false;

    public bool PlayerInteractionDebug => playerInteractionDebug;
    public bool PlayerInteractionToolGizmos => playerInteractionToolGizmos;
    public bool TerrainInteractionDebug => terrainInteractionDebug;
    public bool WorldRuntimeDebug => worldRuntimeDebug;
    public bool WorldBreakDebug => worldBreakDebug;
    public bool SpriteBuilderDebug => spriteBuilderDebug;
    public bool SpriteTileTimingDebug => spriteTileTimingDebug;
    public bool SpriteEveryTileTimingDebug => spriteEveryTileTimingDebug;
}