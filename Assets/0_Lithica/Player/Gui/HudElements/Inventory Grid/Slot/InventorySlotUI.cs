using UnityEngine;
using UnityEngine.UI;
using TMPro;

[RequireComponent(typeof(Image))]
public class InventorySlotUI : MonoBehaviour
{
    [Header("Background")]
    [SerializeField] private Texture2D backgroundTexture;
    [SerializeField] private float backgroundPixelsPerUnit = 100f;

    [Header("Visuals")]
    [SerializeField] private Image iconImage;      // assign the Material Icon image here
    [SerializeField] private TMP_Text amountLabel;
    [SerializeField] private TMP_Text hotkeyLabel;
    [SerializeField] private GameObject selectionHighlight;

    [Header("Debug")]
    [SerializeField] private string materialIdDebug;

    private Image backgroundImage;
    private string materialId;
    private int currentAmount;
    private string currentFormattedAmount;
    private Sprite backgroundSpriteInstance;

    public string MaterialId => materialId;
    public int CurrentAmount => currentAmount;
    public string CurrentFormattedAmount => currentFormattedAmount;

    private void Awake()
    {
        backgroundImage = GetComponent<Image>();
        SetupBackground();
    }

    private void OnValidate()
    {
        backgroundImage = GetComponent<Image>();
        SetupBackground();
    }

    private void SetupBackground()
    {
        if (backgroundImage == null)
            return;

        if (backgroundTexture != null)
        {
#if UNITY_EDITOR
            if (backgroundSpriteInstance != null)
            {
                if (!Application.isPlaying)
                    DestroyImmediate(backgroundSpriteInstance);
                else
                    Destroy(backgroundSpriteInstance);
            }
#endif

            backgroundSpriteInstance = Sprite.Create(
                backgroundTexture,
                new Rect(0, 0, backgroundTexture.width, backgroundTexture.height),
                new Vector2(0.5f, 0.5f),
                backgroundPixelsPerUnit
            );

            backgroundImage.sprite = backgroundSpriteInstance;
        }

        backgroundImage.type = Image.Type.Sliced;
        backgroundImage.enabled = true;
    }

    // -------- Public API --------

    public void SetItem(string id, Sprite icon, int amount, string formattedAmount)
    {
        materialId = id;
        materialIdDebug = id;
        currentAmount = amount;
        currentFormattedAmount = formattedAmount;

        if (iconImage != null)
        {
            if (!string.IsNullOrEmpty(id) && icon != null && amount > 0)
            {
                iconImage.enabled = true;
                iconImage.sprite = icon;
            }
            else
            {
                iconImage.enabled = false;
                iconImage.sprite = null;
            }
        }

        UpdateAmountLabel();
    }

    public void SetAmount(int amount, string formattedAmount)
    {
        currentAmount = amount;
        currentFormattedAmount = formattedAmount;
        UpdateAmountLabel();
    }

    public void SetHotkeyLabel(string text)
    {
        if (hotkeyLabel == null) return;

        hotkeyLabel.text = text;
        hotkeyLabel.gameObject.SetActive(!string.IsNullOrEmpty(text));
    }

    public void SetSelected(bool selected)
    {
        if (selectionHighlight != null)
            selectionHighlight.SetActive(selected);
    }

    public void Clear()
    {
        SetItem(null, null, 0, string.Empty);
        SetHotkeyLabel(string.Empty);
        SetSelected(false);
    }

    // -------- Internal --------

    private void UpdateAmountLabel()
    {
        if (amountLabel == null) return;

        if (!string.IsNullOrEmpty(materialId) && currentAmount > 0 && !string.IsNullOrEmpty(currentFormattedAmount))
            amountLabel.text = currentFormattedAmount;
        else
            amountLabel.text = string.Empty;
    }
}