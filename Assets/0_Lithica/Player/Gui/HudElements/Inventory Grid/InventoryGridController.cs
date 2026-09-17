using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class InventoryGridController : MonoBehaviour
{
    [Header("Panel & Layout Roots")]
    [SerializeField] private RectTransform panelRectTransform;
    [SerializeField] private Image inventoryBackgroundImage;

    [Tooltip("Optional inner image under the main inventory background (same size & slice).")]
    [SerializeField] private Image inventoryInnerImage;

    [Tooltip("Optional border image around the main inventory background.")]
    [SerializeField] private Image inventoryBorderImage;

    [SerializeField] private GridLayoutGroup gridLayout;

    [Header("Layout")]
    [SerializeField] private int columns = 10;
    [SerializeField] private int rows = 4;
    [SerializeField] private Vector2 slotSize = new Vector2(50f, 50f);
    [SerializeField] private Vector2 slotSpacing = new Vector2(4f, 4f);
    [SerializeField] private RectOffset padding;

    [Header("Border Padding")]
    [Tooltip("Extra inset/outset for the border relative to the black background.")]
    [SerializeField] private RectOffset borderPadding;

    [Header("Rounded Borders (Pixels Per Unit Multipliers)")]
    [SerializeField] private float slotPixelsPerUnitMultiplier = 15f;
    [SerializeField] private float inventoryPixelsPerUnitMultiplier = 15f;
    [SerializeField] private float borderPixelsPerUnitMultiplier = 15f;

    [Header("Colors")]
    [SerializeField] private Color slotColor = new Color(1f, 1f, 1f, 1f);
    [SerializeField] private Color inventoryBackgroundColor = new Color(0f, 0f, 0f, 1f);
    [SerializeField] private Color inventoryInnerColor = new Color(1f, 1f, 1f, 0.25f);
    [SerializeField] private Color inventoryBorderColor = new Color(0f, 1f, 0f, 1f);

    [Header("Prefabs & Data")]
    [SerializeField] private InventorySlotUI slotPrefab;
    [SerializeField] private PlayerInventoryManager inventory;

    [Header("Debug Copy of Material Counts")]
    [SerializeField]
    private List<PlayerInventoryManager.MaterialCountEntry> materialCountsDisplay =
        new List<PlayerInventoryManager.MaterialCountEntry>();

#if UNITY_EDITOR
    [Header("Editor Preview")]
    [Tooltip("If enabled, shows the inventory grid preview (slots, background, border) in the editor (scene instances only).")]
    [SerializeField] private bool showPreviewInEditor = true;
#endif

    private readonly List<InventorySlotUI> slots = new();

    private void Awake()
    {
        EnsureRefs();
        ApplyLayoutToGrid();
        ResizePanelToFitGrid();
        ApplyInventoryBackgroundAppearance();

        if (Application.isPlaying)
        {
            if (inventoryBackgroundImage != null) inventoryBackgroundImage.enabled = true;
            if (inventoryInnerImage != null) inventoryInnerImage.enabled = true;
            if (inventoryBorderImage != null) inventoryBorderImage.enabled = true;

            BuildGrid();
            RefreshAllSlots();
        }
    }

    private void OnEnable()
    {
        EnsureRefs();
        ApplyLayoutToGrid();
        ResizePanelToFitGrid();
        ApplyInventoryBackgroundAppearance();

        if (Application.isPlaying)
        {
            if (inventoryBackgroundImage != null) inventoryBackgroundImage.enabled = true;
            if (inventoryInnerImage != null) inventoryInnerImage.enabled = true;
            if (inventoryBorderImage != null) inventoryBorderImage.enabled = true;

            if (slots.Count == 0)
            {
                BuildGrid();
                RefreshAllSlots();
            }
        }
    }

    private void EnsureRefs()
    {
        if (panelRectTransform == null)
            panelRectTransform = GetComponent<RectTransform>();

        if (inventoryBackgroundImage == null && panelRectTransform != null)
            inventoryBackgroundImage = panelRectTransform.GetComponent<Image>();

        if (gridLayout == null && panelRectTransform != null)
            gridLayout = panelRectTransform.GetComponentInChildren<GridLayoutGroup>();

        if (inventory == null)
            inventory = FindObjectOfType<PlayerInventoryManager>();

        if (padding == null)
            padding = new RectOffset(4, 4, 4, 4);

        if (borderPadding == null)
            borderPadding = new RectOffset(0, 0, 0, 0);
    }

    private void ApplyLayoutToGrid()
    {
        if (gridLayout == null)
            return;

        gridLayout.cellSize = slotSize;
        gridLayout.spacing = slotSpacing;
        gridLayout.padding = padding;
        gridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        gridLayout.constraintCount = Mathf.Max(1, columns);
    }

    private void ApplyInventoryBackgroundAppearance()
    {
        if (inventoryBackgroundImage != null)
        {
            inventoryBackgroundImage.type = Image.Type.Sliced;
            inventoryBackgroundImage.pixelsPerUnitMultiplier = inventoryPixelsPerUnitMultiplier;
            inventoryBackgroundImage.color = inventoryBackgroundColor;
        }

        if (inventoryInnerImage != null && panelRectTransform != null)
        {
            var innerRect = inventoryInnerImage.rectTransform;
            RectTransform bgRect = inventoryBackgroundImage != null
                ? inventoryBackgroundImage.rectTransform
                : panelRectTransform;

            innerRect.anchorMin = new Vector2(0.5f, 0.5f);
            innerRect.anchorMax = new Vector2(0.5f, 0.5f);
            innerRect.pivot = new Vector2(0.5f, 0.5f);
            innerRect.anchoredPosition = bgRect.anchoredPosition;
            innerRect.sizeDelta = bgRect.sizeDelta;

            inventoryInnerImage.type = Image.Type.Sliced;
            inventoryInnerImage.pixelsPerUnitMultiplier = inventoryPixelsPerUnitMultiplier;
            inventoryInnerImage.color = inventoryInnerColor;

            if (inventoryInnerImage.sprite == null && inventoryBackgroundImage != null)
                inventoryInnerImage.sprite = inventoryBackgroundImage.sprite;

            inventoryInnerImage.enabled = true;
        }

        if (inventoryBorderImage != null && inventoryBackgroundImage != null)
        {
            var borderRect = inventoryBorderImage.rectTransform;
            var bgRect = inventoryBackgroundImage.rectTransform;

            borderRect.anchorMin = bgRect.anchorMin;
            borderRect.anchorMax = bgRect.anchorMax;
            borderRect.pivot = bgRect.pivot;
            borderRect.anchoredPosition = bgRect.anchoredPosition;

            Vector2 size = bgRect.sizeDelta;

            float width = size.x - borderPadding.left - borderPadding.right;
            float height = size.y - borderPadding.top - borderPadding.bottom;
            borderRect.sizeDelta = new Vector2(width, height);

            inventoryBorderImage.type = Image.Type.Sliced;
            inventoryBorderImage.pixelsPerUnitMultiplier = borderPixelsPerUnitMultiplier;
            inventoryBorderImage.color = inventoryBorderColor;

            if (inventoryBorderImage.sprite == null)
                inventoryBorderImage.sprite = inventoryBackgroundImage.sprite;

            inventoryBorderImage.enabled = true;

            borderRect.SetSiblingIndex(borderRect.parent.childCount - 1);
        }
    }

    private void ResizePanelToFitGrid()
    {
        if (panelRectTransform == null)
            return;

        float width =
            padding.left +
            padding.right +
            columns * slotSize.x +
            Mathf.Max(0, columns - 1) * slotSpacing.x;

        float height =
            padding.top +
            padding.bottom +
            rows * slotSize.y +
            Mathf.Max(0, rows - 1) * slotSpacing.y;

        panelRectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width);
        panelRectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, height);
    }

    private void BuildGrid()
    {
        if (gridLayout == null || slotPrefab == null)
            return;

        int totalSlots = Mathf.Max(0, columns * rows);

        for (int i = gridLayout.transform.childCount - 1; i >= 0; i--)
        {
            var child = gridLayout.transform.GetChild(i);
            var slotUI = child.GetComponent<InventorySlotUI>();
            if (slotUI != null)
                Destroy(child.gameObject);
        }

        slots.Clear();

        for (int i = 0; i < totalSlots; i++)
        {
            var slotInstance = Instantiate(slotPrefab, gridLayout.transform);
            slotInstance.name = $"Slot_{i}";
            slots.Add(slotInstance);

            ApplySlotAppearance(slotInstance);

            int row = i / columns;
            int col = i % columns;

            if (row == 0)
            {
                string key = col <= 8 ? (col + 1).ToString() : "0";
                slotInstance.SetHotkeyLabel(key);
            }
            else
            {
                slotInstance.SetHotkeyLabel(string.Empty);
            }
        }
    }

    private void ApplySlotAppearance(InventorySlotUI slot)
    {
        if (slot == null) return;

        var bgImage = slot.GetComponent<Image>();
        if (bgImage != null)
        {
            bgImage.type = Image.Type.Sliced;
            bgImage.pixelsPerUnitMultiplier = slotPixelsPerUnitMultiplier;
            bgImage.color = slotColor;
            bgImage.SetAllDirty();
        }
    }

    public void RefreshAllSlots()
    {
        if (inventory == null || slots.Count == 0)
            return;

        SyncMaterialCountsDisplay();

        for (int i = 0; i < slots.Count; i++)
        {
            var slotUI = slots[i];

            if (i < materialCountsDisplay.Count)
            {
                var entry = materialCountsDisplay[i];
                slotUI.SetItem(entry.materialId, entry.icon, entry.count, entry.formattedCount);
            }
            else
            {
                slotUI.Clear();
            }
        }
    }

    private void SyncMaterialCountsDisplay()
    {
        materialCountsDisplay.Clear();

        if (inventory == null)
            return;

        var src = inventory.MaterialCountsDisplay;
        for (int i = 0; i < src.Count; i++)
        {
            var e = src[i];
            materialCountsDisplay.Add(new PlayerInventoryManager.MaterialCountEntry
            {
                materialId = e.materialId,
                count = e.count,
                formattedCount = e.formattedCount,
                icon = e.icon
            });
        }
    }

#if UNITY_EDITOR
    private bool IsOnPrefabAsset()
    {
        return PrefabUtility.IsPartOfPrefabAsset(this);
    }

    [ContextMenu("Generate Inventory Preview")]
    public void GenerateInventoryPreview()
    {
        if (IsOnPrefabAsset())
        {
            Debug.LogWarning("[InventoryGrid] Preview skipped: running on a prefab asset, not a scene instance.", this);
            return;
        }

        EnsureRefs();
        ApplyLayoutToGrid();
        ResizePanelToFitGrid();
        ApplyInventoryBackgroundAppearance();

        if (gridLayout == null || slotPrefab == null)
        {
            Debug.LogWarning("[InventoryGrid] Missing GridLayout or Slot Prefab, cannot generate preview.", this);
            return;
        }

        EditorApplication.delayCall += () =>
        {
            if (this == null || gridLayout == null)
                return;

            for (int i = gridLayout.transform.childCount - 1; i >= 0; i--)
            {
                var child = gridLayout.transform.GetChild(i);
                var slotUI = child.GetComponent<InventorySlotUI>();
                if (slotUI != null && !PrefabUtility.IsPartOfPrefabAsset(child))
                    DestroyImmediate(child.gameObject);
            }

            slots.Clear();

            int totalSlots = Mathf.Max(0, columns * rows);
            for (int i = 0; i < totalSlots; i++)
            {
                var slotInstance = Instantiate(slotPrefab, gridLayout.transform);
                if (slotInstance == null)
                    continue;

                slotInstance.name = $"Slot_{i}";
                slots.Add(slotInstance);

                ApplySlotAppearance(slotInstance);

                int row = i / columns;
                int col = i % columns;

                if (row == 0)
                {
                    string key = col <= 8 ? (col + 1).ToString() : "0";
                    slotInstance.SetHotkeyLabel(key);
                }
                else
                {
                    slotInstance.SetHotkeyLabel(string.Empty);
                }
            }

            RefreshAllSlots();
        };
    }

    private void OnValidate()
    {
        if (Application.isPlaying)
            return;

        if (IsOnPrefabAsset())
            return;

        EnsureRefs();
        ApplyLayoutToGrid();
        ResizePanelToFitGrid();
        ApplyInventoryBackgroundAppearance();

        EditorApplication.delayCall += () =>
        {
            if (this == null)
                return;

            bool visible = showPreviewInEditor;

            if (inventoryBackgroundImage != null)
                inventoryBackgroundImage.enabled = visible;
            if (inventoryInnerImage != null)
                inventoryInnerImage.enabled = visible;
            if (inventoryBorderImage != null)
                inventoryBorderImage.enabled = visible;

            if (gridLayout == null)
                return;

            if (!visible)
            {
                for (int i = gridLayout.transform.childCount - 1; i >= 0; i--)
                {
                    var child = gridLayout.transform.GetChild(i);
                    var slotUI = child.GetComponent<InventorySlotUI>();
                    if (slotUI != null && !PrefabUtility.IsPartOfPrefabAsset(child))
                        DestroyImmediate(child.gameObject);
                }

                slots.Clear();
            }
            else
            {
                GenerateInventoryPreview();
            }
        };
    }
#endif
}