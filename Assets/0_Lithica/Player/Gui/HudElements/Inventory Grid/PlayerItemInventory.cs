using System;
using UnityEngine;

public class PlayerItemInventory : MonoBehaviour
{
    [Serializable]
    public class InventoryItem
    {
        [Tooltip("Item / material identifier (e.g. Dirt, Stone).")]
        public string id;

        [Tooltip("Icon to show in the inventory slot.")]
        public Sprite icon;

        [Tooltip("Stack size in this slot.")]
        public int amount;
    }

    [Header("Inventory Slots")]
    [Tooltip("Fixed array of slots used by the inventory grid.")]
    [SerializeField] private InventoryItem[] slots;

    public int SlotCount => slots != null ? slots.Length : 0;

    private void Awake()
    {
        EnsureSlotsInitialized();
    }

    private void OnValidate()
    {
        EnsureSlotsInitialized();
    }

    private void EnsureSlotsInitialized()
    {
        if (slots == null || slots.Length == 0)
            return;

        for (int i = 0; i < slots.Length; i++)
        {
            if (slots[i] == null)
                slots[i] = new InventoryItem();
        }
    }

    public InventoryItem GetSlot(int index)
    {
        if (!IsValidIndex(index))
            return null;

        return slots[index];
    }

    public void SetSlot(int index, string id, Sprite icon, int amount)
    {
        if (!IsValidIndex(index))
            return;

        var slot = slots[index];
        if (slot == null)
            slots[index] = slot = new InventoryItem();

        slot.id = id;
        slot.icon = icon;
        slot.amount = amount;
        // Optional: raise an event here to notify UI listeners.
    }

    public void ClearSlot(int index)
    {
        if (!IsValidIndex(index))
            return;

        var slot = slots[index];
        if (slot == null)
            slots[index] = slot = new InventoryItem();

        slot.id = null;
        slot.icon = null;
        slot.amount = 0;
    }

    public void ClearAll()
    {
        if (slots == null)
            return;

        for (int i = 0; i < slots.Length; i++)
            ClearSlot(i);
    }

    public bool TryAddToSlot(int index, string id, Sprite icon, int amount)
    {
        if (!IsValidIndex(index) || amount <= 0)
            return false;

        var slot = slots[index];
        if (slot == null)
            slots[index] = slot = new InventoryItem();

        // If empty or same item, stack it.
        if (string.IsNullOrEmpty(slot.id) || slot.id == id)
        {
            slot.id = id;
            slot.icon = icon;
            slot.amount += amount;
            return true;
        }

        // Different item already in this slot.
        return false;
    }

    private bool IsValidIndex(int index)
    {
        return slots != null && index >= 0 && index < slots.Length;
    }
}