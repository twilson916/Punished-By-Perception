using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using Newtonsoft.Json;
using MyGame.Resources;

public class RuleManager : MonoBehaviour
{
    // THE SINGLETON INSTANCE
    // This allows any script in the game to say "RuleManager.Instance" to talk to it.
    public static RuleManager Instance;

    [Header("UI Reference")]
    public ScrollRect rulebookScrollView;

    [Header("Audio")]
    public AudioSource discoverySound;

    [Header("Text Assets")]
    public TextAsset rulesCSV;      // assign in inspector
    public TextAsset saveStateJSON; // assign in inspector

    private List<GameRule> allRules;
    private Dictionary<GameRule.RuleName, RuleEntryUI> ruleUIMap;

    private VerticalLayoutGroup layoutGroup;
    private ContentSizeFitter sizeFitter;

    private void Awake()
    {
        // Singleton Setup: Ensure there is only ever ONE RuleManager in the scene
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        allRules = ParseRulesFromFile();
        if (allRules == null) allRules = new List<GameRule>();

        ruleUIMap = new Dictionary<GameRule.RuleName, RuleEntryUI>();

        SetupContentLayout();

        foreach (var rule in allRules)
        {
            RuleEntryUI ui = CreateRuleEntry(rule.description);

            //ui.SetVisibleInstant(false);

            ruleUIMap.Add(rule.ruleName, ui);
        }
    }

    private void SetupContentLayout()
    {
        var content = rulebookScrollView.content;

        layoutGroup = content.GetComponent<VerticalLayoutGroup>();
        if (layoutGroup == null)
            layoutGroup = content.gameObject.AddComponent<VerticalLayoutGroup>();

        layoutGroup.childControlHeight = true;
        layoutGroup.childControlWidth = true;
        layoutGroup.childForceExpandWidth = true;
        layoutGroup.childForceExpandHeight = false;
        layoutGroup.spacing = 2f;

        sizeFitter = content.GetComponent<ContentSizeFitter>();
        if (sizeFitter == null)
            sizeFitter = content.gameObject.AddComponent<ContentSizeFitter>();

        sizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
    }

    private RuleEntryUI CreateRuleEntry(string description)
    {
        // 1. Create the Entry (The Container)
        GameObject entry = new GameObject("RuleEntryUI", typeof(RectTransform));
        entry.transform.SetParent(rulebookScrollView.content, false);

        // 2. Add Layout Components to the Entry
        // This makes the container follow the size of its text child
        ContentSizeFitter entryFitter = entry.AddComponent<ContentSizeFitter>();
        entryFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        entryFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

        VerticalLayoutGroup entryLayout = entry.AddComponent<VerticalLayoutGroup>();
        entryLayout.childControlHeight = true;
        entryLayout.childControlWidth = true;
        entryLayout.padding = new RectOffset(10, 10, 10, 10); // Nice breathing room

        // 3. Add Visual/Logic Components
        CanvasGroup cg = entry.AddComponent<CanvasGroup>();
        cg.alpha = 0f;

        // 4. Create the Text Object
        GameObject textObj = new GameObject("Text", typeof(RectTransform));
        textObj.transform.SetParent(entry.transform, false);

        TextMeshProUGUI tmp = textObj.AddComponent<TextMeshProUGUI>();
        tmp.text = description;
        tmp.fontSize = 10; // Set your desired size here
        tmp.enableWordWrapping = true;
        tmp.overflowMode = TextOverflowModes.Overflow;

        // 5. Setup the Wrapper
        RuleEntryUI wrapper = entry.AddComponent<RuleEntryUI>();
        wrapper.text = tmp;
        wrapper.canvasGroup = cg;

        return wrapper;
    }

    private List<GameRule> ParseRulesFromFile()
    {
        var rules = new List<GameRule>();

        if (rulesCSV == null)
        {
            Debug.LogError("No CSV file assigned to RuleManager");
            return rules;
        }

        string[] lines = rulesCSV.text.Split('\n');

        for (int i = 1; i < lines.Length; i++) // skip header
        {
            string line = lines[i].Trim();

            if (string.IsNullOrEmpty(line))
                continue;

            string[] cols = line.Split(',');

            if (cols.Length < 6)
            {
                Debug.LogWarning($"Malformed CSV line: {line}");
                continue;
            }

            string nameStr = cols[0].Trim();
            string desc = cols[1].Trim();
            string doorStr = cols[2].Trim();
            string handleStr = cols[3].Trim();
            string resultStr = cols[4].Trim();
            string parentStr = cols[5].Trim();

            // Parse RuleName
            if (!System.Enum.TryParse(nameStr, out GameRule.RuleName ruleName))
            {
                Debug.LogError($"Invalid RuleName: {nameStr}");
                continue;
            }

            // Parse Result
            if (!System.Enum.TryParse(resultStr, out RuleResultType result))
            {
                Debug.LogError($"Invalid ResultType: {resultStr}");
                continue;
            }

            // Parse Parent
            GameRule.RuleName parent = GameRule.RuleName.None;
            if (!string.IsNullOrEmpty(parentStr) && parentStr != "None")
            {
                if (!System.Enum.TryParse(parentStr, out parent))
                {
                    Debug.LogError($"Invalid Parent RuleName: {parentStr}");
                    parent = GameRule.RuleName.None;
                }
            }

            // Create GameObject + component (since GameRule is MonoBehaviour)
            GameObject obj = new GameObject($"Rule_{ruleName}");
            GameRule rule = obj.AddComponent<GameRule>();

            rule.ruleName = ruleName;
            rule.description = desc;
            rule.parent = parent;
            rule.result = result;

            rule.doorColor = ColorUtils.ParseColor(doorStr);
            rule.handleColor = ColorUtils.ParseColor(handleStr);

            rule.resetRule();

            rules.Add(rule);
        }

        return rules;
    }

    public void DiscoverRuleByTitle(GameRule.RuleName name)
    {
        if (!ruleUIMap.TryGetValue(name, out var ui)) return;

        // Play Sound
        if (discoverySound != null) discoverySound.Play();

        // Start the Reveal and Scroll
        StopAllCoroutines(); // Optional: prevent overlapping scrolls
        StartCoroutine(RevealAndScroll(ui));
    }

    private IEnumerator RevealAndScroll(RuleEntryUI ui)
    {
        // Start fading the UI element
        StartCoroutine(ui.FadeIn(0.5f));

        // WAIT for Unity to realize the UI has changed
        yield return new WaitForEndOfFrame();
        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(rulebookScrollView.content);

        // Calculate Normalized Position
        RectTransform targetRT = ui.GetComponent<RectTransform>();
        RectTransform contentRT = rulebookScrollView.content;

        // We want to find where the target is relative to the content's top
        // Unity UI scrolls from 1 (top) to 0 (bottom)
        float contentHeight = contentRT.rect.height;
        float viewportHeight = rulebookScrollView.viewport.rect.height;

        if (contentHeight > viewportHeight)
        {
            // Get target position in local space of content
            // Subtract half the height of the rule to find its top edge
            float targetPos = contentRT.InverseTransformPoint(targetRT.position).y + (targetRT.rect.height * 0.5f);
            // Center the item or just scroll to it (this math scrolls to top-align the item)
            float normalizedPos = 1f - (Mathf.Abs(targetPos) / (contentHeight - viewportHeight));
            normalizedPos = Mathf.Clamp01(normalizedPos);

            // Smooth Scroll
            float t = 0f;
            float duration = 0.5f;
            float startPos = rulebookScrollView.verticalNormalizedPosition;

            while (t < duration)
            {
                t += Time.deltaTime;
                rulebookScrollView.verticalNormalizedPosition = Mathf.Lerp(startPos, normalizedPos, t / duration);
                yield return null;
            }
            rulebookScrollView.verticalNormalizedPosition = normalizedPos;
        }
    }
}

public class RuleEntryUI : MonoBehaviour
{
    public TextMeshProUGUI text;
    public CanvasGroup canvasGroup;
    private bool isDiscovered = false;

    public void Initialize(string description)
    {
        text.text = description;
        // Start invisible and non-interactive, but occupying space
        canvasGroup.alpha = 0f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
    }

    public IEnumerator FadeIn(float duration)
    {
        if (isDiscovered) yield break;
        isDiscovered = true;

        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            canvasGroup.alpha = t / duration;
            yield return null;
        }
        canvasGroup.alpha = 1f;
        canvasGroup.interactable = true;
        canvasGroup.blocksRaycasts = true;
    }
}