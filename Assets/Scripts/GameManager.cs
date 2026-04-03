using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    // THE SINGLETON INSTANCE
    // This allows any script in the game to say "GameManager.Instance" to talk to it.
    public static GameManager Instance;

    [Header("Dependencies")]
    [Tooltip("Drag the rooms into this list from rooms 1->3")]
    public List<RoomController> sceneRooms;

    // game state enum
    public enum GameState { Exploring, Puzzle, Ending }; //placeholders for now
    private GameState currentState;

    // keep track of room count/progress
    private int totalRoomsVisited = 1;
    private enum RoomNumber { One=0, Two=1, Three=2 }; //note that the player is only in room 3 to be teleported (for game logic rooms 1 and 3 can be considered the same room)
    private RoomNumber currentRoom = RoomNumber.One;

    private void Awake()
    {
        // Singleton Setup: Ensure there is only ever ONE GameManager in the scene
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        currentState = GameState.Exploring;

        //unlock first rooms doors
        sceneRooms[(int)currentRoom].SetLockDoors(false);
    }

    // signal receivers for other scripts to signal manager

    // when loop teleport takes user from room 3 to 1
    public void OnLoopTeleport()
    {
        currentRoom = RoomNumber.One;

        // update logic and door/room decor/color
    }

    public void OnDoorClicked(DoorController.DoorPos pos)
    {
        sceneRooms[(int)currentRoom].SetLockDoors(true);

        sceneRooms[(int)currentRoom].OpenDoor(pos);

        //TODO add logic
    }

    public void OnDoorCloseTrigger(GameObject Trigger)
    {

    }
}