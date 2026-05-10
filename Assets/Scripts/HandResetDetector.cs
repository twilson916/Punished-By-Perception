using UnityEngine;
using System.Collections;

using MyGame.Resources;

public class HandResetDetector : MonoBehaviour
{
    [Header("Hand Tracking References")]
    [Tooltip("The Transform of the left hand/controller")]
    public Transform leftHand;
    [Tooltip("The Transform of the right hand/controller")]
    public Transform rightHand;

    [Header("Settings")]
    [Tooltip("Y position threshold - hands must be below this to count as 'on the floor'")]
    public float floorThreshold = 0.15f;
    [Tooltip("How long both hands must stay down")]
    public float holdDuration = 3f;
    [Tooltip("Cooldown after any reset before another can trigger")]
    public float resetCooldown = 3f;

    // Hold timers
    private float holdBothTimer = 0f;
    private float holdOneTimer = 0f;
    private bool resetTriggered = false;

    // Transition tracking - prevents carry-over from lost tracking
    private bool wasLeftDown;
    private bool wasRightDown;

    private void Update()
    {
        if (GameManager.Instance.currentState == GameState.Ending) return;
        if (resetTriggered) return;

        bool leftValid = IsHandTracked(leftHand, OVRInput.Controller.LTouch, OVRInput.Controller.LHand);
        bool rightValid = IsHandTracked(rightHand, OVRInput.Controller.RTouch, OVRInput.Controller.RHand);

        bool leftDown = leftValid && leftHand.position.y <= floorThreshold;
        bool rightDown = rightValid && rightHand.position.y <= floorThreshold;

        // Reset timers if either hand's state just changed (prevents carry-over)
        if (leftDown != wasLeftDown || rightDown != wasRightDown)
        {
            holdBothTimer = 0f;
            holdOneTimer = 0f;
        }
        wasLeftDown = leftDown;
        wasRightDown = rightDown;

        bool bothHandsDown = leftDown && rightDown;
        bool oneHandDown = leftDown != rightDown; // exactly one

        if (bothHandsDown && GameManager.Instance.currentState != GameState.FakeEnding)
        {
            holdOneTimer = 0f;
            holdBothTimer += Time.deltaTime;
            if (holdBothTimer >= holdDuration)
            {
                resetTriggered = true;
                StartCoroutine(CooldownRoutine());
                GameManager.Instance.ResetRun();
            }
        }
        else if (oneHandDown)
        {
            holdBothTimer = 0f;
            holdOneTimer += Time.deltaTime;
            if (holdOneTimer >= 5f)
            {
                // LMAO this is diabolical, you get a hint that your BOUGHT that tells you this is how to wipe out your punishments
                // ...which is like totally true, but not only does it reset the run which is horrendous it RESETS YOUR SAVE!!!
                // Also useful as a dev tool to reset the save easily
                RuleManager.Instance.ClearAllRulesAndSave();
                MetaRuleRegistry.Instance.ClearAllMetaRules();
                resetTriggered = true;
                StartCoroutine(CooldownRoutine());
                GameManager.Instance.ResetRun();
            }
        }
        else
        {
            holdBothTimer = 0f;
            holdOneTimer = 0f;
        }
    }

    private bool IsHandTracked(Transform hand, OVRInput.Controller touchController, OVRInput.Controller handController)
    {
        if (hand == null) return false;
        if (!hand.gameObject.activeInHierarchy) return false;

        bool b1 = OVRInput.GetControllerPositionTracked(touchController);
        bool b2 = OVRInput.GetControllerPositionTracked(handController);

        return OVRInput.GetControllerPositionTracked(touchController)
            || OVRInput.GetControllerPositionTracked(handController);
    }

    private IEnumerator CooldownRoutine()
    {
        yield return new WaitForSeconds(resetCooldown);
        resetTriggered = false;
        holdBothTimer = 0f;
        holdOneTimer = 0f;
        wasLeftDown = false;
        wasRightDown = false;
    }
}
