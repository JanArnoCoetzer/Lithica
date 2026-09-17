using UnityEngine;

public class TerrainInteractionManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private WorldManager world;
    [SerializeField] private PlayerInventoryManager playerInventory;
    [SerializeField] private MaterialGenerator materialGenerator;

    [Header("Processing")]
    [SerializeField] private bool processColliderImmediately = true;

    [Header("Async Breaking")]
    [SerializeField] private bool useAsyncBrushBreaking = true;
    [SerializeField] private bool allowOnlyOnePendingBreakJob = true;

    [Header("Directional Gizmos")]
    [SerializeField] private bool drawDirectionalBreakGizmos = true;
    [SerializeField] private bool drawGizmosOnlyWhenSelected = true;
    [SerializeField] private float gizmoPointRadius = 0.04f;
    [SerializeField] private float gizmoCenterRadius = 0.05f;
    [SerializeField] private float gizmoHitRadius = 0.07f;
    [SerializeField] private float gizmoDestroyedRadius = 0.09f;
    [SerializeField] private Color gizmoStartColor = Color.cyan;
    [SerializeField] private Color gizmoTargetColor = Color.magenta;
    [SerializeField] private Color gizmoRayColor = Color.white;
    [SerializeField] private Color gizmoCenterColor = new Color(0.25f, 0.9f, 1f, 1f);
    [SerializeField] private Color gizmoSampleColor = Color.yellow;
    [SerializeField] private Color gizmoMissColor = new Color(0.45f, 0.45f, 0.45f, 1f);
    [SerializeField] private Color gizmoSolidColor = new Color(1f, 0.55f, 0f, 1f);
    [SerializeField] private Color gizmoDestroyedColor = Color.red;

    private DebugManager debugManager;
    private TerrainBreakContext breakContext = new TerrainBreakContext();
    private TerrainBreakGizmos breakGizmos = new TerrainBreakGizmos();

    private TerrainBreakJobResult pendingBrushJob;
    private bool hasPendingBrushJob;

    private bool DebugEnabled =>
        debugManager != null && debugManager.TerrainInteractionDebug;

    private void Awake()
    {
        ResolveReferences();
        SyncContext();
        DebugLog($"Awake | world={(world != null ? world.name : "null")}");
    }

    private void OnValidate()
    {
        ResolveReferences();
        SyncContext();
    }

    private void OnDestroy()
    {
        DisposePendingBrushJob();
    }

    public void SetDebugManager(DebugManager manager)
    {
        debugManager = manager;
        SyncContext();
    }

    private void LateUpdate()
    {
        SyncContext();

        if (!hasPendingBrushJob)
            return;

        if (!pendingBrushJob.Handle.IsCompleted)
            return;

        pendingBrushJob.Handle.Complete();
        ApplyPendingBrushJob();
        DisposePendingBrushJob();
    }

    private void ResolveReferences()
    {
        if (world == null)
            world = FindAnyObjectByType<WorldManager>();

        if (playerInventory == null)
            playerInventory = GetComponentInParent<PlayerInventoryManager>(true);

        if (playerInventory == null && world != null)
            playerInventory = world.GetComponentInChildren<PlayerInventoryManager>(true);

        if (playerInventory == null)
            playerInventory = FindAnyObjectByType<PlayerInventoryManager>();

        if (materialGenerator == null && world != null)
            materialGenerator = world.GetComponent<MaterialGenerator>();

        if (materialGenerator == null)
            materialGenerator = FindAnyObjectByType<MaterialGenerator>();
    }

    private void SyncContext()
    {
        breakContext.World = world;
        breakContext.PlayerInventory = playerInventory;
        breakContext.MaterialGenerator = materialGenerator;
        breakContext.ProcessColliderImmediately = processColliderImmediately;
        breakContext.DebugManager = debugManager;
    }

    private void DebugLog(string message)
    {
        if (!DebugEnabled)
            return;

        Debug.Log($"[TerrainInteraction] {message}", this);
    }

    public void BreakBrushAtWorld(Vector2 worldPos, float breakRadius, Vector3 playerPosition)
    {
        SyncContext();

        if (!useAsyncBrushBreaking)
        {
            int changedCount;
            bool changed = TerrainBreakProcessor.BreakBrushAtWorld(
                breakContext,
                worldPos,
                breakRadius,
                breakGizmos,
                this,
                out changedCount);

            if (DebugEnabled && changed)
                Debug.Log($"[BreakBrushAtWorld] changedCount={changedCount}", this);

            return;
        }

        if (world == null || !world.enableDestruction)
            return;

        if (!breakContext.BrushTouchesSolidTerrain(worldPos, breakRadius))
            return;

        if (hasPendingBrushJob)
        {
            if (allowOnlyOnePendingBreakJob)
                return;

            if (pendingBrushJob.Handle.IsCompleted)
            {
                pendingBrushJob.Handle.Complete();
                ApplyPendingBrushJob();
                DisposePendingBrushJob();
            }
            else
            {
                return;
            }
        }

        world.EnsureDamageMask();
        pendingBrushJob = TerrainBreakJobScheduler.ScheduleBrush(world, worldPos, breakRadius);
        hasPendingBrushJob = true;

        if (DebugEnabled)
            Debug.Log("[BreakBrushAtWorld] scheduled async brush job", this);
    }

    public void BreakTowardMouse(Vector2 targetWorldPos, Vector3 playerPosition, float breakRange, int breakWidth)
    {
        SyncContext();

        bool changed = TerrainBreakProcessor.BreakTowardMouse(
            breakContext,
            targetWorldPos,
            playerPosition,
            breakRange,
            breakWidth,
            breakGizmos);

        if (DebugEnabled && changed)
            Debug.Log("[BreakTowardMouse] terrain changed", this);
    }

    public void BreakInCone(
        Vector2 targetWorldPos,
        Vector3 playerPosition,
        float breakRange,
        int breakWidth,
        float coneHalfAngle,
        int coneRayCount,
        int forwardRemovalPixels)
    {
        SyncContext();

        bool changed = TerrainBreakProcessor.BreakInCone(
            breakContext,
            targetWorldPos,
            playerPosition,
            breakRange,
            breakWidth,
            coneHalfAngle,
            coneRayCount,
            forwardRemovalPixels,
            breakGizmos);

        if (DebugEnabled && changed)
            Debug.Log("[BreakInCone] terrain changed", this);
    }

    private void ApplyPendingBrushJob()
    {
        if (!hasPendingBrushJob || world == null)
            return;

        bool changed = false;
        int changedCount = 0;

        Vector2 changedMinWorld = Vector2.positiveInfinity;
        Vector2 changedMaxWorld = Vector2.negativeInfinity;

        for (int i = 0; i < pendingBrushJob.Count; i++)
        {
            if (pendingBrushJob.HitMask[i] == 0)
                continue;

            int px = pendingBrushJob.PixelCoords[i].x;
            int py = pendingBrushJob.PixelCoords[i].y;

            if (!breakContext.IsSolidDamagePixel(px, py))
                continue;

            if (breakContext.TryDestroyPixelAndRegisterInventory(px, py, this))
            {
                changed = true;
                changedCount++;

                Vector2 pixelWorld = TerrainSamplingHelper.DamagePixelToWorldCenter(world, px, py);
                changedMinWorld = Vector2.Min(changedMinWorld, pixelWorld);
                changedMaxWorld = Vector2.Max(changedMaxWorld, pixelWorld);
            }
        }

        if (!changed)
            return;

        float pixelWorldSize = world.cellSize / world.TilePixelSize;
        Vector2 padding = Vector2.one * pixelWorldSize;
        changedMinWorld -= padding;
        changedMaxWorld += padding;

        breakContext.RebuildChangedArea(changedMinWorld, changedMaxWorld);

        if (DebugEnabled)
            Debug.Log($"[BreakBrushAtWorld Async] changedCount={changedCount}", this);
    }

    private void DisposePendingBrushJob()
    {
        if (!hasPendingBrushJob)
            return;

        pendingBrushJob.Dispose();
        hasPendingBrushJob = false;
    }

    private void DrawDirectionalGizmos()
    {
        breakGizmos.Draw(
            DebugEnabled,
            drawDirectionalBreakGizmos,
            gizmoRayColor,
            gizmoStartColor,
            gizmoTargetColor,
            gizmoCenterColor,
            gizmoSampleColor,
            gizmoMissColor,
            gizmoSolidColor,
            gizmoDestroyedColor,
            gizmoPointRadius,
            gizmoCenterRadius,
            gizmoHitRadius,
            gizmoDestroyedRadius);
    }

    private void OnDrawGizmos()
    {
        if (drawGizmosOnlyWhenSelected)
            return;

        DrawDirectionalGizmos();
    }

    private void OnDrawGizmosSelected()
    {
        if (!drawGizmosOnlyWhenSelected)
            return;

        DrawDirectionalGizmos();
    }
}