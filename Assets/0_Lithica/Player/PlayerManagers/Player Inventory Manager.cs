using System.Collections.Generic;
using UnityEngine;

public class PlayerInventoryManager : MonoBehaviour
{
    [System.Serializable]
    public class MaterialCountEntry
    {
        [Tooltip("Identifier for this material / item.")]
        public string materialId;

        [Tooltip("Number of pixels/items collected for this id.")]
        public int count;

        [Tooltip("Formatted string version of count (e.g. 9.5k).")]
        public string formattedCount;

        [Tooltip("Sprite used to represent this id in the inventory UI.")]
        public Sprite icon;
    }

    // Raw counts and icon lookup per materialId
    private readonly Dictionary<string, int> materialPixelCounts = new();
    private readonly Dictionary<string, Sprite> materialIcons = new();

    [Header("Runtime Counts")]
    [SerializeField] private int totalBrokenPixels;
    [SerializeField] private string formattedTotalBrokenPixels;
    [SerializeField] private List<MaterialCountEntry> materialCountsDisplay = new();

    public int TotalBrokenPixels => totalBrokenPixels;
    public string FormattedTotalBrokenPixels => formattedTotalBrokenPixels;
    public IReadOnlyDictionary<string, int> MaterialPixelCounts => materialPixelCounts;
    public IReadOnlyList<MaterialCountEntry> MaterialCountsDisplay => materialCountsDisplay;

    /// <summary>
    /// Register a broken pixel with its id and the sprite that should represent it in the inventory.
    /// Call this from your terrain-breaking code.
    /// </summary>
    public void RegisterBrokenPixel(string materialId, Sprite icon)
    {
        if (string.IsNullOrWhiteSpace(materialId))
            materialId = "Unknown";

        totalBrokenPixels++;

        if (!materialPixelCounts.TryGetValue(materialId, out int current))
            current = 0;

        materialPixelCounts[materialId] = current + 1;

        // Store icon for this id if we don't already have one, or if existing is null.
        if (icon != null)
        {
            if (!materialIcons.TryGetValue(materialId, out var existing) || existing == null)
                materialIcons[materialId] = icon;
        }

        RebuildDisplayList();
    }

    public int GetMaterialPixelCount(string materialId)
    {
        if (string.IsNullOrWhiteSpace(materialId))
            materialId = "Unknown";

        return materialPixelCounts.TryGetValue(materialId, out int count) ? count : 0;
    }

    public string GetFormattedMaterialPixelCount(string materialId)
    {
        return FormatCount(GetMaterialPixelCount(materialId));
    }

    public void ClearInventory()
    {
        totalBrokenPixels = 0;
        formattedTotalBrokenPixels = FormatCount(0);
        materialPixelCounts.Clear();
        materialIcons.Clear();
        materialCountsDisplay.Clear();
    }

    private void RebuildDisplayList()
    {
        formattedTotalBrokenPixels = FormatCount(totalBrokenPixels);
        materialCountsDisplay.Clear();

        foreach (KeyValuePair<string, int> pair in materialPixelCounts)
        {
            materialIcons.TryGetValue(pair.Key, out var icon);

            materialCountsDisplay.Add(new MaterialCountEntry
            {
                materialId = pair.Key,
                count = pair.Value,
                formattedCount = FormatCount(pair.Value),
                icon = icon
            });
        }

        // Highest count first
        materialCountsDisplay.Sort((a, b) => b.count.CompareTo(a.count));

        // NEW: tell the grid to sync and redraw
        var grid = FindObjectOfType<InventoryGridController>();
        if (grid != null)
            grid.RefreshAllSlots();
    }

    public static string FormatCount(int value)
    {
        if (value < 1000)
            return value.ToString();

        if (value < 1_000_000)
            return (value / 1000f).ToString("0.##") + "k";

        if (value < 1_000_000_000)
            return (value / 1_000_000f).ToString("0.##") + "m";

        return (value / 1_000_000_000f).ToString("0.##") + "b";
    }

    [ContextMenu("Log Inventory Counts")]
    private void LogInventoryCounts()
    {
        Debug.Log($"[Inventory] TotalBrokenPixels = {formattedTotalBrokenPixels} ({totalBrokenPixels})", this);

        foreach (MaterialCountEntry entry in materialCountsDisplay)
            Debug.Log($"[Inventory] {entry.materialId} = {entry.formattedCount} ({entry.count})", this);
    }
}