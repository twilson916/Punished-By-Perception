/*using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro; //for UI

public class RuleManager : MonoBehaviour
{
    public static RuleManager instance; //allows other scripts to call RuleManager.instance.discoverRule(..)

    [Header("UI Reference")]
    public TextMeshProUGUI rulebookContentText; // Drag your UI Text here

    [Header("The Rules Pile")]
    // Add the rules in the "rules" folder here
    public List<GameRule> allRules;

    // This is the librarian's index. Key: ruleTitle enum in GameRules.cs | Value: the rule created in "rules"
    private Dictionary<ruleTitle, GameRule> ruleMap = new Dictionary<ruleTitle, GameRule>();

    // Awake runs as soon as object initialized and even if its disabled
    private void Awake()
    {
        // Setup the Singleton
        if (instance == null) instance = this;
        else Destroy(gameObject);

        foreach (GameRule rule in allRules)
        {
            // Reset the rule so it's not "already discovered" from a previous play session
            rule.resetRule();

            // Check if the rule has a valid title and isn't a duplicate
            if (rule.title != ruleTitle.None && !ruleMap.ContainsKey(rule.title))
            {
                ruleMap.Add(rule.title, rule);
            }
        }

        UpdateUI();
    }

    public void DiscoverRuleByTitle(ruleTitle titleKey)
    {
        // Use the Dictionary to find the asset instantly
        if (ruleMap.ContainsKey(titleKey))
        {
            GameRule rule = ruleMap[titleKey];

            if (!rule.isDiscovered)
            {
                rule.isDiscovered = true;
                UpdateUI(); // Refresh the Rulebook UI
            }
        }
    }

    void UpdateUI()
    {
        if (rulebookContentText == null) return;

        rulebookContentText.text = "<b>THE RULEBOOK:</b>\n\n";

        foreach (GameRule rule in allRules)
        {
            if (rule.isDiscovered)
            {
                rulebookContentText.text += "- " + rule.description + "\n";
            }
        }
    }


}
*/

//Second iteration  where the string doesn't glitch
/*
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro; // Needed again for the single UI text!

public class RuleManager : MonoBehaviour
{
    public static RuleManager instance;

    [Header("UI Reference")]
    public TextMeshProUGUI rulebookContentText;

    [Header("Audio")]
    public AudioSource discoverySound; // Drag an AudioSource here to play the sound

    [Header("The Rules Pile")]
    // The order you place the rules in this list determines their line number!
    public List<GameRule> allRules;

    private Dictionary<ruleTitle, GameRule> ruleMap = new Dictionary<ruleTitle, GameRule>();

    // This new dictionary remembers which line number (index) each rule belongs to
    private Dictionary<ruleTitle, int> lineIndexMap = new Dictionary<ruleTitle, int>();

    // This array holds exactly what is written on each line at any given moment
    private string[] displayLines;

    private void Awake()
    {
        if (instance == null) instance = this;
        else Destroy(gameObject);

        // Create enough blank lines for however many rules you have
        displayLines = new string[allRules.Count];

        for (int i = 0; i < allRules.Count; i++)
        {
            GameRule rule = allRules[i];
            rule.resetRule();

            if (rule.title != ruleTitle.None && !ruleMap.ContainsKey(rule.title))
            {
                ruleMap.Add(rule.title, rule);
                lineIndexMap.Add(rule.title, i); // Save the line number (i)
            }

            // Set up the default "hidden" text for every line
            // Adding + 1 so the list starts at "1." instead of "0."
            displayLines[i] = (i + 1) + ". ???";
        }

        UpdateUI();
    }

    public void DiscoverRuleByTitle(ruleTitle titleKey)
    {
        if (ruleMap.ContainsKey(titleKey))
        {
            GameRule rule = ruleMap[titleKey];

            if (!rule.isDiscovered)
            {
                rule.isDiscovered = true;

                // 1. Find out which line this rule belongs on
                int lineNumber = lineIndexMap[titleKey];

                // 2. Erase the "???" and write the actual description
                displayLines[lineNumber] = (lineNumber + 1) + ". " + rule.description;

                // 3. Play the sound effect
                if (discoverySound != null)
                {
                    discoverySound.Play();
                }

                // 4. Push the updated lines to the Canvas
                UpdateUI();
            }
        }
    }

    void UpdateUI()
    {
        if (rulebookContentText == null) return;

        // string.Join takes all the individual lines in our array and glues them 
        // together into one giant string, separating them with an Enter key (\n)
        rulebookContentText.text = "<b>THE RULEBOOK:</b>\n\n" + string.Join("\n", displayLines);
    }
}*/

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI; // NEW: We need this to talk to the Scroll View!

public class RuleManager : MonoBehaviour
{
    public static RuleManager instance;

    [Header("UI Reference")]
    public TextMeshProUGUI rulebookContentText;

    // NEW: The reference to your Scroll View window
    public ScrollRect rulebookScrollView;

    [Header("Audio")]
    public AudioSource discoverySound;

    [Header("The Rules Pile")]
    public List<GameRule> allRules;

    private Dictionary<ruleTitle, GameRule> ruleMap = new Dictionary<ruleTitle, GameRule>();
    private Dictionary<ruleTitle, int> lineIndexMap = new Dictionary<ruleTitle, int>();
    private string[] displayLines;

    // NEW: The Coroutine to fade in the text using TMP Rich Text Tags
    private IEnumerator FadeInRuleText(int lineNumber, string actualDescription)
    {
        float transitionDuration = 1.5f; // How long the fade lasts in seconds
        float timer = 0f;

        while (timer < transitionDuration)
        {
            timer += Time.deltaTime;

            // Calculate percentage of completion (0.0 to 1.0)
            float alphaPercentage = Mathf.Clamp01(timer / transitionDuration);

            // Convert that percentage to a Hex code (00 to FF)
            int alphaInt = Mathf.RoundToInt(alphaPercentage * 255);
            string hexAlpha = alphaInt.ToString("X2");

            // Inject the TMP Alpha tag right before the description
            // The <alpha=#FF> at the end ensures the rules below it stay solid!
            displayLines[lineNumber] = (lineNumber + 1) + $". <alpha=#{hexAlpha}>" + actualDescription + "<alpha=#FF>";

            UpdateUI();

            yield return null; // Wait for the next frame and loop again
        }

        // Once the timer is done, remove the tags to keep the string clean
        displayLines[lineNumber] = (lineNumber + 1) + ". " + actualDescription;
        UpdateUI();
    }


    private void Awake()
    {
        if (instance == null) instance = this;
        else Destroy(gameObject);

        displayLines = new string[allRules.Count];

        for (int i = 0; i < allRules.Count; i++)
        {
            GameRule rule = allRules[i];
            rule.resetRule();

            if (rule.title != ruleTitle.None && !ruleMap.ContainsKey(rule.title))
            {
                ruleMap.Add(rule.title, rule);
                lineIndexMap.Add(rule.title, i);
            }

            displayLines[i] = (i + 1) + ". ???";
        }

        UpdateUI();
    }

    public void DiscoverRuleByTitle(ruleTitle titleKey)
    {
        if (ruleMap.ContainsKey(titleKey))
        {
            GameRule rule = ruleMap[titleKey];

            if (!rule.isDiscovered)
            {
                rule.isDiscovered = true;

                int lineNumber = lineIndexMap[titleKey];

               // displayLines[lineNumber] = (lineNumber + 1) + ". " + rule.description;

                if (discoverySound != null)
                {
                    discoverySound.Play();
                }
                
                //UpdateUI();

                StartCoroutine(FadeInRuleText(lineNumber, rule.description));

                // NEW: Trigger the Coroutine to jump to the newly discovered rule
                if (rulebookScrollView != null)
                {
                    StartCoroutine(SnapToRuleLine(lineNumber));
                }
            }
        }
    }

    void UpdateUI()
    {
        if (rulebookContentText == null) return;
        rulebookContentText.text = "<b>THE RULEBOOK:</b>\n\n" + string.Join("\n", displayLines);
    }

    // NEW: The Coroutine that waits for the UI to update, then scrolls
    private IEnumerator SnapToRuleLine(int lineNumber)
    {
        // 1. Wait until the end of the frame so the Content Size Fitter stretches the box
        yield return new WaitForEndOfFrame();

        // 2. Prevent division by zero if you only have 1 rule in the list
        if (allRules.Count > 1)
        {
            // 3. Calculate the target position.
            // verticalNormalizedPosition uses 1.0f for the absolute Top, and 0.0f for the Bottom.
            float scrollPercentage = 1f - ((float)lineNumber / (allRules.Count - 1));

            // 4. Snap the scrollbar to that exact percentage
            rulebookScrollView.verticalNormalizedPosition = scrollPercentage;
        }
    }
}