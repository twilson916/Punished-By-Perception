using UnityEngine;

public class HandResetDetector : MonoBehaviour
{
    [Header("Hand Tracking References")]
    [Tooltip("The Transform of the left hand/controller")]
    public Transform leftHand;
    [Tooltip("The Transform of the right hand/controller")]
    public Transform rightHand;

    [Header("Settings")]
    [Tooltip("Y position threshold — hands must be below this to count as 'on the floor'")]
    public float floorThreshold = 0.15f;
    [Tooltip("How long both hands must stay down")]
    public float holdDuration = 3f;

    private float holdBothTimer = 0f;
    private float holdOneTimer = 0f;
    private bool resetTriggered = false;

    private void Update()
    {
        if (resetTriggered) return;

        bool bothHandsDown = leftHand.position.y <= floorThreshold
                          && rightHand.position.y <= floorThreshold;
        bool oneHandDown = leftHand.position.y <= floorThreshold
                          || rightHand.position.y <= floorThreshold;

        if (bothHandsDown)
        {
            holdBothTimer += Time.deltaTime;

            if (holdBothTimer >= holdDuration)
            {
                resetTriggered = true;
                GameManager.Instance.ResetRun();
            }
        }
        else if(oneHandDown) {
            holdOneTimer += Time.deltaTime;

            if (holdOneTimer >= 5f)
            {
                // LMAO this is diabolical, you get a hint that your BOUGHT that tells you this is how to wipe out your punishments
                // ...which is like totally true, but not only does it reset the run which is horrendous it RESETS YOUR SAVE!!!
                // Also useful asa dev tool to reset the save easily
                RuleManager.Instance.ClearAllRulesAndSave();
                MetaRuleRegistry.Instance.ClearAllMetaRules();


                resetTriggered = true;
                GameManager.Instance.ResetRun();
            }
        }
        else {
            holdBothTimer = 0f;
        }
    }

    // Called by GameManager.ResetRun() finishing so the detector can fire again.
    // Hook this up at the end of ResetRun or just call it after.
    public void ResetDetector()
    {
        resetTriggered = false;
        holdBothTimer = 0f;
    }
}