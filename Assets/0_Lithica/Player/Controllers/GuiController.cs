using UnityEngine;
using UnityEngine.InputSystem;

public class GuiController : MonoBehaviour
{
    [Header("UI Panels")]
    [SerializeField] private GameObject playerInventoryPanel;
    [SerializeField] private GameObject craftingPanel;
    [SerializeField] private GameObject mapPanel;

    private GameObject currentOpenPanel;

    private void Start()
    {
        // Initial state
        SetPanelActive(playerInventoryPanel, false);
        SetPanelActive(craftingPanel, false);
        SetPanelActive(mapPanel, false);
        currentOpenPanel = null;
    }

    // Called by Input System action (e.g. "UI/Escape")
    public void OnEscape(InputValue value)
    {
        // We only care about button *down* transitions
        if (!value.isPressed)
            return;

        HandleEscape();
    }

    private void HandleEscape()
    {
        if (currentOpenPanel != null)
        {
            // Close whatever is open
            SetPanelActive(currentOpenPanel, false);
            currentOpenPanel = null;
        }
        else
        {
            // Nothing open -> open player inventory
            OpenPlayerInventory();
        }
    }

    public void OpenPlayerInventory()
    {
        CloseAllPanels();
        SetPanelActive(playerInventoryPanel, true);
        currentOpenPanel = playerInventoryPanel;
    }

    public void OpenCrafting()
    {
        CloseAllPanels();
        SetPanelActive(craftingPanel, true);
        currentOpenPanel = craftingPanel;
    }

    public void OpenMap()
    {
        CloseAllPanels();
        SetPanelActive(mapPanel, true);
        currentOpenPanel = mapPanel;
    }

    private void CloseAllPanels()
    {
        SetPanelActive(playerInventoryPanel, false);
        SetPanelActive(craftingPanel, false);
        SetPanelActive(mapPanel, false);
        currentOpenPanel = null;
    }

    private void SetPanelActive(GameObject panel, bool active)
    {
        if (panel != null)
            panel.SetActive(active);
    }
}