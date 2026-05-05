using System.Collections.Generic;
using UnityEngine;

public class ModularButtonRow : MonoBehaviour
{
    [Header("Drag your buttons here:")]
    public List<RectTransform> buttons = new List<RectTransform>();

    [Header("Layout Settings")]
    [Range(0f, 0.5f)]
    public float heightPercentage = 0.5f;

    [Range(0f, 0.1f)]
    public float gapBetweenButtons = 0.01f;

    private void OnValidate()
    {
        if (buttons == null || buttons.Count == 0) return;

        // 1. Make a temporary list of ONLY the slots you have filled in so far
        // This stops the script from deleting your empty "+" slots!
        List<RectTransform> validButtons = new List<RectTransform>();
        foreach (var btn in buttons)
        {
            if (btn != null) validButtons.Add(btn);
        }

        int count = validButtons.Count;

        // If there are no valid buttons yet, stop here and wait for you to add one
        if (count == 0) return;

        // 2. Calculate the exact width percentage each button gets
        float totalGapSpace = gapBetweenButtons * (count - 1);
        float widthPerButton = (1f - totalGapSpace) / count;
        float currentX = 0f;

        // 3. Loop through every valid button and assign its new Vector anchors
        for (int i = 0; i < count; i++)
        {
            RectTransform btn = validButtons[i];

            btn.anchorMin = new Vector2(currentX, 0f);

            float nextX = currentX + widthPerButton;
            btn.anchorMax = new Vector2(nextX, heightPercentage);

            btn.offsetMin = Vector2.zero;
            btn.offsetMax = Vector2.zero;

            currentX = nextX + gapBetweenButtons;
        }
    }
}