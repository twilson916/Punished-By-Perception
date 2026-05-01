using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MyGame.Resources;

public class RoomController : MonoBehaviour
{
    // The room keeps track of its own doors
    [Header("Dependencies")]
    [Tooltip("Drag the root GameObject for the doors (from left to right) into the three slots of this array")]
    public DoorController[] doors;

    // The GameManager can call this to easily lock/unlock a room
    public void SetLockDoors(bool isLocked)
    {
        foreach (DoorController door in doors)
        {
            door.isLocked = isLocked;
        }
    }

    public void OpenDoor(DoorPos pos)
    {
        doors[(int)pos].OpenDoor();
    }

    public void OpenAllDoors()
    {
        foreach(DoorController door in doors)
        {
            door.OpenDoor();
        }
    }

    public void CloseDoor(DoorPos pos)
    {
        doors[(int)pos].CloseDoor();
    }

    public void CloseAllDoors()
    {
        foreach (DoorController door in doors)
        {
            door.CloseDoor();
        }
    }

    private void ChangeWallColor(Color newColor)
    {
        // Change material color, swap props, etc.
    }

    public void ChangeRoomColor(RoomColors colors)
    {
        //Change door and handle colors
        for(int i = 0; i < 3; i++)
        {
            doors[i].ChangeDoorColor(colors.doors[i], colors.doorHandles[i]);
        }

        //TODO add logic for other colors
    }

    public void ApplyEnvironmentModifiers(Dictionary<string, object> modifiers)
    {
        if (modifiers == null || modifiers.Count == 0) return;

        // Example: vase positioning
        if (modifiers.TryGetValue("vase_side", out object vaseSide))
        {
            // TODO: move your vase GameObject to the left or right side FIXME
            // e.g. vaseTransform.localPosition = (string)vaseSide == "right" ? rightPos : leftPos;
            Debug.Log($"[RoomController] Vase moved to {vaseSide}");
        }

        // Add more modifier handlers here as you create meta-rules
    }
}
