using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ShopUIController : MonoBehaviour
{
    public enum ShopItem { Mulligan, PunishmentShield, RulebookSilver, RulebookGold }

    [Header("Buttons")]
    [Tooltip("Assign the 4 shop buttons in order: Mulligan, Shield, Silver Book, Gold Book")]
    [SerializeField] private Button[] shopButtons;

    [Header("Sold Out Appearance")]
    [SerializeField] private Color soldOutColor = new Color(0.3f, 0.3f, 0.3f, 1f);
    [SerializeField] private Color soldOutTextColor = new Color(0.5f, 0.5f, 0.5f, 1f);

    private bool[] purchased;
    private Color[] originalButtonColors;
    private TMP_Text[][] buttonTexts;
    private Color[][] originalTextColors;

    private System.Action<ShopItem> _onItemPurchased;

    public void Initialize(System.Action<ShopItem> onItemPurchased)
    {
        _onItemPurchased = onItemPurchased;

        int count = shopButtons.Length;
        purchased = new bool[count];
        originalButtonColors = new Color[count];
        buttonTexts = new TMP_Text[count][];
        originalTextColors = new Color[count][];

        for (int i = 0; i < count; i++)
        {
            int index = i; // capture for closure
            purchased[i] = false;

            // Cache button background color
            originalButtonColors[i] = shopButtons[i].GetComponent<Image>().color;

            // Cache all child texts and their colors
            buttonTexts[i] = shopButtons[i].GetComponentsInChildren<TMP_Text>();
            originalTextColors[i] = new Color[buttonTexts[i].Length];
            for (int t = 0; t < buttonTexts[i].Length; t++)
                originalTextColors[i][t] = buttonTexts[i][t].color;

            shopButtons[i].onClick.RemoveAllListeners();
            shopButtons[i].onClick.AddListener(() => OnButtonClicked(index));
        }
    }

    // Call this every time the shop opens to reset all items to purchasable
    public void ResetShop()
    {
        for (int i = 0; i < shopButtons.Length; i++)
        {
            purchased[i] = false;
            shopButtons[i].interactable = true;
            shopButtons[i].GetComponent<Image>().color = originalButtonColors[i];

            for (int t = 0; t < buttonTexts[i].Length; t++)
                buttonTexts[i][t].color = originalTextColors[i][t];
        }
    }

    private void OnButtonClicked(int index)
    {
        if (index < 0 || index >= shopButtons.Length) return;
        if (purchased[index]) return; // safety check

        // Grey out immediately
        purchased[index] = true;
        shopButtons[index].interactable = false;
        shopButtons[index].GetComponent<Image>().color = soldOutColor;

        foreach (var tmp in buttonTexts[index])
            tmp.color = soldOutTextColor;

        ShopItem item = (ShopItem)index;
        Debug.Log($"[ShopUIController] Purchased: {item}");
        _onItemPurchased?.Invoke(item);
    }
}