using UnityEngine;
using System.Collections;

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
    [Tooltip("Cooldown after any reset before another can trigger")]
    public float resetCooldown = 3f;
    [Tooltip("Hands at origin means tracking lost -> ignore")]
    public float trackingLostThreshold = 0.001f;

    // Hold timers
    private float holdBothTimer = 0f;
    private float holdOneTimer = 0f;
    private bool resetTriggered = false;

    // Stale detection — catches frozen/idle controllers
    private Vector3 lastLeftPos;
    private Vector3 lastRightPos;
    private float leftStaleTime;
    private float rightStaleTime;
    private const float STALE_THRESHOLD = 0.5f;
    private const float MOVE_EPSILON = 1e-8f;

    // Transition tracking — prevents carry-over from lost tracking
    private bool wasLeftDown;
    private bool wasRightDown;

    private void Update()
    {
        if (resetTriggered) return;

        bool leftValid = IsHandTracked(leftHand) && !IsHandStale(leftHand, ref lastLeftPos, ref leftStaleTime);
        bool rightValid = IsHandTracked(rightHand) && !IsHandStale(rightHand, ref lastRightPos, ref rightStaleTime);

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

        if (bothHandsDown)
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

    private bool IsHandTracked(Transform hand)
    {
        if (hand == null) return false;
        if (!hand.gameObject.activeInHierarchy) return false;
        if (hand.localPosition.sqrMagnitude < trackingLostThreshold * trackingLostThreshold) return false;
        return true;
    }

    private bool IsHandStale(Transform hand, ref Vector3 lastPos, ref float staleTime)
    {
        float delta = (hand.position - lastPos).sqrMagnitude;
        lastPos = hand.position;

        if (delta < MOVE_EPSILON * MOVE_EPSILON)
        {
            staleTime += Time.deltaTime;
            return staleTime >= STALE_THRESHOLD;
        }

        staleTime = 0f;
        return false;
    }

    private IEnumerator CooldownRoutine()
    {
        yield return new WaitForSeconds(resetCooldown);
        resetTriggered = false;
        holdBothTimer = 0f;
        holdOneTimer = 0f;
        leftStaleTime = 0f;
        rightStaleTime = 0f;
        wasLeftDown = false;
        wasRightDown = false;
    }
}