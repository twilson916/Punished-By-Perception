using System.Collections;
using UnityEngine;
using Oculus.Interaction.Locomotion;

public class MetaTeleportNode : MonoBehaviour
{
    [Header("Dependencies")]
    [Tooltip("Drag the GameObject from your hierarchy that contains the FirstPersonLocomotor script.")]
    public FirstPersonLocomotor locomotor;

    [Header("Startup Settings")]
    [Tooltip("Check this if this specific cube is the initial spawn point for the game.")]
    public bool isStartupSpawn = true;

    [Tooltip("Required delay for startup to allow headset tracking to initialize.")]
    public float startupDelay = 0.1f;

    private void Start()
    {
        if (locomotor == null)
        {
            Debug.LogError($"TeleportNode on {gameObject.name} is missing the FirstPersonLocomotor reference!");
            return;
        }

        if (isStartupSpawn)
        {
            StartCoroutine(TeleportRoutine(startupDelay));
        }
    }

    // Call this method from a trigger collider for your looping rooms
    public void TriggerInstantTeleport()
    {
        StartCoroutine(TeleportRoutine(0f));
    }

    private IEnumerator TeleportRoutine(float delay)
    {
        if (delay > 0)
        {
            // Give the physical headset a split second to report its tracking offset to Unity
            yield return new WaitForSeconds(delay);
        }

        // Grab the exact position and rotation of THIS cube
        Pose targetPose = new Pose(transform.position, transform.rotation);

        // Format the event using Meta's specific Enums
        // Absolute Translation = Move feet to the exact XZ coordinates of the target
        // Absolute Rotation = Snap the Yaw to perfectly match the target's rotation
        LocomotionEvent teleportEvent = new LocomotionEvent(
            this.GetInstanceID(),
            targetPose,
            LocomotionEvent.TranslationType.Absolute,
            LocomotionEvent.RotationType.Absolute
        );

        // Fire the event natively into Meta's locomotor
        locomotor.HandleLocomotionEvent(teleportEvent);

        Debug.Log($"Player safely teleported to {gameObject.name} via LocomotionEvent.");
    }
}