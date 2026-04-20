using UnityEngine;

public class RuleTrigger : MonoBehaviour
{
    [Header("ruleTitle enum")]
    // This dropdown will now show your 'ruleTitle' enum list
    public ruleTitle ruleToBreak;

    [Header("Filter Settings (case sensitive), e.g: 'Player', etc. ")]
    // Set this to "Card" or "Player" in the Inspector to only trigger for those objects
    public string targetTag;

    // This runs when something enters the trigger zone
    private void OnTriggerEnter(Collider other)
    {
        // 1. Check if the object has the right tag
        if (other.CompareTag(targetTag))
        {
            // 2. Tell the Manager to unlock this specific rule
            // We use the 'instance' shortcut we set up in RuleManager
            RuleManager.instance.DiscoverRuleByTitle(ruleToBreak);

            // 3. Optional: Feedback
            Debug.Log($"Object {other.name} broke rule: {ruleToBreak}");

            // 4. Optional: Disable this trigger so the rule doesn't "re-unlock" constantly
            // gameObject.SetActive(false); 
        }
    }
}