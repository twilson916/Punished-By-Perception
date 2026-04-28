using MyGame.Resources;
using Newtonsoft.Json;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

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

    private string saveFileName = "rule_save_state.txt";

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

        LoadSaveState(); //load save file
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

    private void LoadSaveState()
    {
        string fullPath = Path.Combine(Application.persistentDataPath, saveFileName);

        if (!File.Exists(fullPath))
        {
            Debug.Log("No save file found. Starting fresh.");
            return;
        }

        // 1. Read the text file lines
        string[] discoveredNames = File.ReadAllLines(fullPath);

        foreach (string rawName in discoveredNames)
        {
            string trimmedName = rawName.Trim();

            if (System.Enum.TryParse(trimmedName, out GameRule.RuleName ruleName))
            {
                // A. Update the UI Wrapper
                if (ruleUIMap.TryGetValue(ruleName, out var ui))
                {
                    ui.SetVisibleInstant(true);
                }

                // B. Update the actual GameRule Logic Object
                // We search the list for the rule with the matching name
                GameRule logicRule = allRules.FirstOrDefault(r => r.ruleName == ruleName);
                if (logicRule != null)
                {
                    // Assuming GameRule has a 'isFound' or similar boolean
                    // Adjust 'isFound' to whatever variable name you use in GameRule.cs
                    logicRule.isDiscovered = true;

                    // If your logic needs to trigger something specific when "found" 
                    // (like updating a door state), call that here.
                }
            }
        }

        Canvas.ForceUpdateCanvases();
    }

    public void SaveProgress()
    {
        string fullPath = Path.Combine(Application.persistentDataPath, saveFileName);

        // Gather all names of rules where isFound is true
        List<string> foundRuleNames = allRules
            .Where(r => r.isDiscovered)
            .Select(r => r.ruleName.ToString())
            .ToList();

        // Overwrite the file with the new list
        File.WriteAllLines(fullPath, foundRuleNames);
    }

    public void ClearAllRulesAndSave()
    {
        // 1. Wipe the logic flags in the master list
        foreach (var rule in allRules)
        {
            rule.isDiscovered = false;
        }

        // 2. Wipe the UI visibility and discovery flags
        foreach (var ui in ruleUIMap.Values)
        {
            ui.SetVisibleInstant(false);
            ui.isDiscovered = false; // Important: so they can be "discovered" again
        }

        // 3. Delete the physical save file from the disk
        string fullPath = Path.Combine(Application.persistentDataPath, saveFileName);

        if (File.Exists(fullPath))
        {
            File.Delete(fullPath);
            Debug.Log("Save file deleted successfully.");
        }

        // 4. Reset the Scroll position to the top
        rulebookScrollView.verticalNormalizedPosition = 1f;

        // 5. Force UI refresh
        Canvas.ForceUpdateCanvases();

        Debug.Log("All rules cleared for testing.");
    }

    public void DiscoverRuleByTitle(GameRule.RuleName name)
    {
        if (!ruleUIMap.TryGetValue(name, out var ui)) return;

        if (ui.isDiscovered) return;

        GameRule logicRule = allRules.FirstOrDefault(r => r.ruleName == name);
        if (logicRule != null)
        {
            logicRule.isDiscovered = true;
        }

        // Play Sound
        if (discoverySound != null) discoverySound.Play();

        // Start the Reveal and Scroll
        StopAllCoroutines(); // Optional: prevent overlapping scrolls
        StartCoroutine(RevealAndScroll(ui));

        SaveProgress();
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
    public bool isDiscovered = false;

    public void Initialize(string description)
    {
        text.text = description;
        // Start invisible and non-interactive, but occupying space
        canvasGroup.alpha = 0f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
    }

    public void SetVisibleInstant(bool visible)
    {
        isDiscovered = visible;
        canvasGroup.alpha = visible ? 1f : 0f;
        canvasGroup.interactable = visible;
        canvasGroup.blocksRaycasts = visible;
        gameObject.SetActive(true); // Ensure it's active in the hierarchy
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