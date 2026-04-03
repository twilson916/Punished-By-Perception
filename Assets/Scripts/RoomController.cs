using System.Collections;
using System.Collections.Generic;
using UnityEngine;
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

    public void OpenDoor(DoorController.DoorPos pos)
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

    public void CloseDoor(DoorController.DoorPos pos)
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

    public void ChangeWallColor(Color newColor)
    {
        // Change material color, swap props, etc.
    }
}
