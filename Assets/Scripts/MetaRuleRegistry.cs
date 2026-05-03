using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// All meta rules in discovery order.
// Misc Factual rules come first, then Tricky rules.
public enum MetaRuleName
{
    // ── Misc Factual (available from shop) ──
    HandReset,
    Every3rdChallenge,
    FailedChallengeDoublePunish,
    Every7thQuiz,
    Every9thShop,
    TwentyRoomsToWin,
    FivePunishmentsReset,
    ThreeFailsReset,
    ChallengeShield,
    OneHandRemovesPunishments,
    EmptyShopPunishes,

    // ── Tricky (purchased from shop as double-punishment challenge) ──
    PlantsSwapped,
    LeftChairOut,
    CeilingLightColor,
    RightChairOut,
    LowPunishmentRestart,
    Every6thInvert,
    Every3rdAnswerLeft,
    OneHandWipesSave
}

public enum MetaRuleCategory
{
    Factual,
    Tricky
}

[System.Serializable]
public class MetaRuleDefinition
{
    public MetaRuleName name;
    public string description;
    public MetaRuleCategory category;
    public bool isDiscovered;

    public MetaRuleDefinition(MetaRuleName name, string description, MetaRuleCategory category)
    {
        this.name = name;
        this.description = description;
        this.category = category;
        this.isDiscovered = false;
    }
}

public class MetaRuleRegistry : MonoBehaviour
{
    public static MetaRuleRegistry Instance;

    [Header("UI Reference")]
    [Tooltip("Same ScrollRect as the main rulebook — meta rules appear at the bottom")]
    public ScrollRect rulebookScrollView;

    [Header("Dependencies")]
    [Tooltip("Drag in the punishment manager")]
    public PunishmentManager punisher;

    private string saveFileName = "meta_rule_save_state.txt";

    private List<MetaRuleDefinition> allMetaRules;
    private Dictionary<MetaRuleName, RuleEntryUI> metaRuleUIMap;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        // Run all this stuff in start so it always runs after the Awake call of the rule manager
        allMetaRules = BuildMetaRuleList();
        metaRuleUIMap = new Dictionary<MetaRuleName, RuleEntryUI>();

        // Create a separator header before meta rules
        CreateSeparatorEntry("— Strange Observations —");

        // Create UI entries for each meta rule at the bottom of the rulebook
        foreach (var rule in allMetaRules)
        {
            RuleEntryUI ui = CreateMetaRuleEntry(rule.description);
            metaRuleUIMap.Add(rule.name, ui);
        }

        LoadSaveState();
    }

    // ─────────────────────────────────────────────
    //  HARDCODED RULE DEFINITIONS (in exact reveal order)
    // ─────────────────────────────────────────────

    private List<MetaRuleDefinition> BuildMetaRuleList()
    {
        return new List<MetaRuleDefinition>
        {
            // Misc Factual
            new MetaRuleDefinition(MetaRuleName.HandReset,
                "Reset a run intentionally by touching the floor with both hands/controllers for 3 seconds",
                MetaRuleCategory.Factual),
            new MetaRuleDefinition(MetaRuleName.Every3rdChallenge,
                "Every 3rd room has an additional challenge",
                MetaRuleCategory.Factual),
            new MetaRuleDefinition(MetaRuleName.FailedChallengeDoublePunish,
                "Failing a challenge doles out a double punishment",
                MetaRuleCategory.Factual),
            new MetaRuleDefinition(MetaRuleName.Every7thQuiz,
                "Every 7th room has a quiz",
                MetaRuleCategory.Factual),
            new MetaRuleDefinition(MetaRuleName.Every9thShop,
                "Every 9th room is a shop",
                MetaRuleCategory.Factual),
            new MetaRuleDefinition(MetaRuleName.TwentyRoomsToWin,
                "To complete the game you must pass through 20 rooms",
                MetaRuleCategory.Factual),
            new MetaRuleDefinition(MetaRuleName.FivePunishmentsReset,
                "Hitting 5 active punishments automatically resets your run",
                MetaRuleCategory.Factual),
            new MetaRuleDefinition(MetaRuleName.ThreeFailsReset,
                "Failing 3 challenges automatically resets your run",
                MetaRuleCategory.Factual),
            new MetaRuleDefinition(MetaRuleName.ChallengeShield,
                "Completing a challenge gives you a shield that can absorb 1 punishment",
                MetaRuleCategory.Factual),
            new MetaRuleDefinition(MetaRuleName.OneHandRemovesPunishments,
                "Holding one hand down to the floor for 5 seconds removes all punishments",
                MetaRuleCategory.Factual),
            new MetaRuleDefinition(MetaRuleName.EmptyShopPunishes,
                "Attempting to buy something from the shop punishes you when there is nothing left to acquire",
                MetaRuleCategory.Factual),

            // Tricky
            new MetaRuleDefinition(MetaRuleName.PlantsSwapped,
                "If the plants on the table are swapped all safe rooms are punishments and all challenges are random",
                MetaRuleCategory.Tricky),
            new MetaRuleDefinition(MetaRuleName.LeftChairOut,
                "If the left chair is pulled out slightly then all safe rooms are punishments and all random doors are safe",
                MetaRuleCategory.Tricky),
            new MetaRuleDefinition(MetaRuleName.CeilingLightColor,
                "If the ceiling light is a different color then all safe and random rooms are punishments and all other rooms are safe",
                MetaRuleCategory.Tricky),
            new MetaRuleDefinition(MetaRuleName.RightChairOut,
                "If the right chair is pulled out slightly then the last guy must've forgotten to put it back",
                MetaRuleCategory.Tricky),
            new MetaRuleDefinition(MetaRuleName.LowPunishmentRestart,
                "Restarting the current run with less than 3 active punishments starts your next run with a punishment",
                MetaRuleCategory.Tricky),
            new MetaRuleDefinition(MetaRuleName.Every6thInvert,
                "Every 6th room has inverted logic, ie all safe rooms are punishments and vice versa",
                MetaRuleCategory.Tricky),
            new MetaRuleDefinition(MetaRuleName.Every3rdAnswerLeft,
                "Every 3rd challenge requires you to answer the left most option regardless of the true answer",
                MetaRuleCategory.Tricky),
            new MetaRuleDefinition(MetaRuleName.OneHandWipesSave,
                "Touching the floor with only one hand wipes your save lmao!!!",
                MetaRuleCategory.Tricky),
        };
    }

    // ─────────────────────────────────────────────
    //  PUBLIC API
    // ─────────────────────────────────────────────

    // Discovers the next undiscovered Factual meta rule in order
    public void DiscoverNextFactualRule()
    {
        var next = allMetaRules.FirstOrDefault(r => r.category == MetaRuleCategory.Factual && !r.isDiscovered);
        if (next != null)
            DiscoverMetaRule(next.name);
        else
            punisher.Punish(1);

        //Also punish them if they are just finding out about the last rule cause lmao
        if(next.name == MetaRuleName.EmptyShopPunishes)
            punisher.Punish(1);
    }

    // Discovers the next undiscovered Tricky meta rule in order
    public void DiscoverNextTrickyRule()
    {
        var next = allMetaRules.FirstOrDefault(r => r.category == MetaRuleCategory.Tricky && !r.isDiscovered);
        if (next != null)
            DiscoverMetaRule(next.name);
    }

    private void DiscoverMetaRule(MetaRuleName name)
    {
        var rule = allMetaRules.FirstOrDefault(r => r.name == name);
        if (rule == null || rule.isDiscovered) return;

        rule.isDiscovered = true;

        if (metaRuleUIMap.TryGetValue(name, out var ui))
        {
            AudioManager.Play(AudioManager.SoundCategory.Discover);
            StopAllCoroutines();
            StartCoroutine(RevealAndScroll(ui));
        }

        SaveProgress();
    }

    // Check if a specific meta rule has been discovered
    public bool IsDiscovered(MetaRuleName name)
    {
        var rule = allMetaRules.FirstOrDefault(r => r.name == name);
        return rule != null && rule.isDiscovered;
    }

    // Get all discovered meta rule names
    public HashSet<MetaRuleName> GetDiscoveredMetaRuleNames()
    {
        return new HashSet<MetaRuleName>(
            allMetaRules.Where(r => r.isDiscovered).Select(r => r.name)
        );
    }

    // Clear all meta rule discoveries (full save wipe)
    public void ClearAllMetaRules()
    {
        foreach (var rule in allMetaRules)
            rule.isDiscovered = false;

        foreach (var ui in metaRuleUIMap.Values)
        {
            ui.SetVisibleInstant(false);
            ui.isDiscovered = false;
        }

        string fullPath = Path.Combine(Application.persistentDataPath, saveFileName);
        if (File.Exists(fullPath))
            File.Delete(fullPath);

        Debug.Log("[MetaRuleRegistry] All meta rules cleared.");
    }

    // ─────────────────────────────────────────────
    //  UI CREATION
    // ─────────────────────────────────────────────

    private void CreateSeparatorEntry(string text)
    {
        GameObject entry = new GameObject("MetaRuleSeparator", typeof(RectTransform));
        entry.transform.SetParent(rulebookScrollView.content, false);

        ContentSizeFitter entryFitter = entry.AddComponent<ContentSizeFitter>();
        entryFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        entryFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

        VerticalLayoutGroup entryLayout = entry.AddComponent<VerticalLayoutGroup>();
        entryLayout.childControlHeight = true;
        entryLayout.childControlWidth = true;
        entryLayout.padding = new RectOffset(10, 10, 20, 10);

        GameObject textObj = new GameObject("SeparatorText", typeof(RectTransform));
        textObj.transform.SetParent(entry.transform, false);

        TextMeshProUGUI tmp = textObj.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = 12;
        tmp.fontStyle = FontStyles.Bold | FontStyles.Italic;
        tmp.color = new Color(0.4f, 0.1f, 0.1f); // dark red-ish
        tmp.enableWordWrapping = true;
        tmp.alignment = TextAlignmentOptions.Center;
    }

    private RuleEntryUI CreateMetaRuleEntry(string description)
    {
        GameObject entry = new GameObject("MetaRuleEntryUI", typeof(RectTransform));
        entry.transform.SetParent(rulebookScrollView.content, false);

        ContentSizeFitter entryFitter = entry.AddComponent<ContentSizeFitter>();
        entryFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        entryFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

        VerticalLayoutGroup entryLayout = entry.AddComponent<VerticalLayoutGroup>();
        entryLayout.childControlHeight = true;
        entryLayout.childControlWidth = true;
        entryLayout.padding = new RectOffset(10, 10, 10, 10);

        CanvasGroup cg = entry.AddComponent<CanvasGroup>();
        cg.alpha = 0f;

        GameObject textObj = new GameObject("Text", typeof(RectTransform));
        textObj.transform.SetParent(entry.transform, false);

        TextMeshProUGUI tmp = textObj.AddComponent<TextMeshProUGUI>();
        tmp.text = description;
        tmp.fontSize = 10;
        tmp.color = new Color(0.3f, 0f, 0f); // slightly different color to distinguish from normal rules
        tmp.enableWordWrapping = true;
        tmp.overflowMode = TextOverflowModes.Overflow;

        RuleEntryUI wrapper = entry.AddComponent<RuleEntryUI>();
        wrapper.text = tmp;
        wrapper.canvasGroup = cg;

        return wrapper;
    }

    // ─────────────────────────────────────────────
    //  SAVE / LOAD
    // ─────────────────────────────────────────────

    private void SaveProgress()
    {
        string fullPath = Path.Combine(Application.persistentDataPath, saveFileName);

        List<string> discoveredNames = allMetaRules
            .Where(r => r.isDiscovered)
            .Select(r => r.name.ToString())
            .ToList();

        File.WriteAllLines(fullPath, discoveredNames);
    }

    private void LoadSaveState()
    {
        string fullPath = Path.Combine(Application.persistentDataPath, saveFileName);
        if (!File.Exists(fullPath)) return;

        string[] discoveredNames = File.ReadAllLines(fullPath);

        foreach (string rawName in discoveredNames)
        {
            string trimmed = rawName.Trim();
            if (Enum.TryParse(trimmed, out MetaRuleName name))
            {
                var rule = allMetaRules.FirstOrDefault(r => r.name == name);
                if (rule != null)
                {
                    rule.isDiscovered = true;

                    if (metaRuleUIMap.TryGetValue(name, out var ui))
                        ui.SetVisibleInstant(true);
                }
            }
        }

        Canvas.ForceUpdateCanvases();
    }

    // ─────────────────────────────────────────────
    //  SCROLL ANIMATION (same approach as RuleManager)
    // ─────────────────────────────────────────────

    private System.Collections.IEnumerator RevealAndScroll(RuleEntryUI ui)
    {
        StartCoroutine(ui.FadeIn(0.5f));

        yield return new WaitForEndOfFrame();
        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(rulebookScrollView.content);

        RectTransform targetRT = ui.GetComponent<RectTransform>();
        RectTransform contentRT = rulebookScrollView.content;

        float contentHeight = contentRT.rect.height;
        float viewportHeight = rulebookScrollView.viewport.rect.height;

        if (contentHeight > viewportHeight)
        {
            float targetPos = contentRT.InverseTransformPoint(targetRT.position).y + (targetRT.rect.height * 0.5f);
            float normalizedPos = 1f - (Mathf.Abs(targetPos) / (contentHeight - viewportHeight));
            normalizedPos = Mathf.Clamp01(normalizedPos);

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