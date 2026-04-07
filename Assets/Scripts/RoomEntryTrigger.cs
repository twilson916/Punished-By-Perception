using Oculus.Interaction.Locomotion;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MyGame.Resources;

public class RoomEntryTrigger : MonoBehaviour
{
    [Header("Connections")]
    [Tooltip("Drag the GameObject from your hierarchy that contains the FirstPersonLocomotor script.")]
    public FirstPersonLocomotor locomotor;

    [Header("Settings")]
    [Tooltip("Room number that this trigger is part of")]
    public RoomNumber roomNum;

    private void Start()
    {
        // Initial Dependency Checks
        if (locomotor == null)
        {
            Debug.LogError($"Trigger on {gameObject.name} is missing references!");
            return;
        }

        // Ensure this cube is actually set up as a trigger
        GetComponent<BoxCollider>().isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        // Check if the thing that entered the box is actually the player
        // We do this by looking to see if the collider belongs to the Locomotor rig
        if (other.GetComponentInParent<FirstPersonLocomotor>() == locomotor)
        {
            GameManager.Instance.OnRoomEntry(roomNum);
        }
    }
}
