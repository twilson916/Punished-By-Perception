using System.Collections;
using UnityEngine;
using Oculus.Interaction.Locomotion;

[RequireComponent(typeof(BoxCollider))]
public class MetaSeamlessPortal : MonoBehaviour
{
    [Header("Portal Connections")]
    [Tooltip("The invisible cube the player will be seamlessly teleported TO.")]
    public Transform destinationCube;

    [Tooltip("Drag the GameObject from your hierarchy that contains the FirstPersonLocomotor script.")]
    public FirstPersonLocomotor locomotor;

    [Header("Settings")]
    [Tooltip("Prevents infinite bouncing if the destination also has a portal script.")]
    public float cooldownSeconds = 1.0f;
    private float lastTeleportTime = -1f;

    private void Start()
    {
        // Initial Dependency Checks
        if (locomotor == null || destinationCube == null)
        {
            Debug.LogError($"Portal on {gameObject.name} is missing references!");
            return;
        }

        // Validate the geometry (Ensures the illusion won't break)
        // We check if the destination cube has the exact same scale as this cube.
        if (transform.localScale != destinationCube.localScale)
        {
            Debug.LogWarning($"Portal Warning: {gameObject.name} and {destinationCube.name} are different sizes! The relative math might feel slightly off to the player.");
        }

        // Ensure this cube is actually set up as a trigger
        GetComponent<BoxCollider>().isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        // Cooldown Check
        if (Time.time < lastTeleportTime + cooldownSeconds) return;

        // Check if the thing that entered the box is actually the player
        // We do this by looking to see if the collider belongs to the Locomotor rig
        if (other.GetComponentInParent<FirstPersonLocomotor>() == locomotor)
        {
            ExecuteSeamlessTeleport();

            GameManager.Instance.OnLoopTeleport();
        }
    }

    private void ExecuteSeamlessTeleport()
    {
        // Mark the time so we don't instantly bounce back
        lastTeleportTime = Time.time;

        // Get the player's HMD/Controller position
        Vector3 currentWorldPos = locomotor.transform.position;
        Quaternion currentWorldRot = locomotor.transform.rotation;

        // Calculate the relative X/Z math
        Vector3 relativeLocalPos = transform.InverseTransformPoint(currentWorldPos);
        Vector3 targetWorldPos = destinationCube.TransformPoint(relativeLocalPos);

        // We find out exactly how high your head/center is from the floor of your rig.
        float heightFromFloor = locomotor.transform.localPosition.y;

        // We subtract that height from the target Y. 
        // This guarantees the teleport event receives the exact coordinates of the FLOOR, not your head!
        targetWorldPos.y = currentWorldPos.y - heightFromFloor;

        // Calculate the relative rotation
        Quaternion relativeLocalRot = Quaternion.Inverse(transform.rotation) * currentWorldRot;
        Quaternion targetWorldRot = destinationCube.rotation * relativeLocalRot;

        // Send teleport request event
        Pose targetPose = new Pose(targetWorldPos, targetWorldRot);

        LocomotionEvent teleportEvent = new LocomotionEvent(
            this.GetInstanceID(),
            targetPose,
            LocomotionEvent.TranslationType.Absolute,
            LocomotionEvent.RotationType.Absolute
        );

        locomotor.HandleLocomotionEvent(teleportEvent);

        Debug.Log($"Seamless teleport executed from {gameObject.name} to {destinationCube.name}");
    }
}