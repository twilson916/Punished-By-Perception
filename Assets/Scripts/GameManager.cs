using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MyGame.Resources;

public class GameManager : MonoBehaviour
{
    // THE SINGLETON INSTANCE
    // This allows any script in the game to say "GameManager.Instance" to talk to it.
    public static GameManager Instance;

    [Header("Dependencies")]
    [Tooltip("Drag the rooms into this list from rooms 1->3")]
    public List<RoomController> sceneRooms;
    [Tooltip("Drag in the punishment manager")]
    public PunishmentManager punisher;

    // game state enum
    private GameState currentState;

    // keep track of room count/progress
    private int totalRoomsVisited = 1;
    //note that the player is only in room 3 to be teleported (for game logic rooms 1 and 3 can be considered the same room)
    private RoomNumber currentRoom = RoomNumber.One;

    // queue of future rooms
    Queue<RoomColors> roomColorQueue = new Queue<RoomColors>();

    private void Awake()
    {
        // Singleton Setup: Ensure there is only ever ONE GameManager in the scene
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        //FIXME testing only delete save state
        RuleManager.Instance.ClearAllRulesAndSave();

        currentState = GameState.Exploring;

        //unlock first rooms doors
        sceneRooms[(int)currentRoom].SetLockDoors(false);

        //FIXME TESTING ONLY
        for (int i = 0; i < 25; i++)
        {
            RoomColors roomColors = new RoomColors
            (
                doors: new Color[] { ColorUtils.GetRandColor(), ColorUtils.GetRandColor(), ColorUtils.GetRandColor() },
                doorHandles: new Color[] { ColorUtils.GetRandColor(), ColorUtils.GetRandColor(), ColorUtils.GetRandColor() }
            );
            roomColorQueue.Enqueue(roomColors);
        }


        RoomColors initialRoomColors = new RoomColors
            (
                doors: new Color[] { Color.white, Color.white, Color.white },
                doorHandles: new Color[] { Color.grey, Color.grey, Color.grey }
            );

        //Set initial rooms color (subsequent loops handled by triggers)
        sceneRooms[(int)RoomNumber.MinusOne].ChangeRoomColor(initialRoomColors);
        sceneRooms[(int)RoomNumber.One].ChangeRoomColor(roomColorQueue.Dequeue());
        var colors = roomColorQueue.Dequeue();
        sceneRooms[(int)RoomNumber.Two].ChangeRoomColor(colors);
        lastRoom2Colors = colors;
    }
    
    // --- Signal receivers for other scripts to signal manager ---

    public void OnLoopTeleport() // When loop teleport takes user from room 3 to 1
    {
        currentRoom = RoomNumber.One;

        // //TODO add logic (door/room decor/color, etc)
    }

    public void OnDoorClicked(DoorPos pos)
    {
        sceneRooms[(int)currentRoom].SetLockDoors(true);

        sceneRooms[(int)currentRoom].OpenDoor(pos);

        //TODO add logic
    }

    private RoomColors lastRoom2Colors; //used to keep track of colors used for room 2 to be applied to room N1
    private RoomColors lastRoom3Colors; //used to keep track of colors used for room 2 to be applied to room 1
    public void OnRoomEntry(RoomNumber newRoomNum) //Player has entered a room, thus close the doors behind them and unlock next set of doors
    {
        if (currentRoom == newRoomNum) return; // Triggered again while in same room thus ignore

        RoomColors colors;

        switch(newRoomNum) {
            case RoomNumber.One:
                totalRoomsVisited++; // If just entered room 1 or 2 then increment rooms visited

                colors = roomColorQueue.Dequeue();
                lastRoom2Colors = colors;
                sceneRooms[(int)RoomNumber.Two].ChangeRoomColor(colors);
                break;
            case RoomNumber.Two:
                totalRoomsVisited++;
                sceneRooms[(int)newRoomNum - 1].CloseAllDoors(); // For either room two or three close doors behind them

                colors = roomColorQueue.Dequeue();
                lastRoom3Colors = colors;
                sceneRooms[(int)RoomNumber.Three].ChangeRoomColor(colors);
                sceneRooms[(int)RoomNumber.MinusOne].ChangeRoomColor(lastRoom2Colors);
                break;
            case RoomNumber.Three:
                sceneRooms[(int)newRoomNum - 1].CloseAllDoors();

                sceneRooms[(int)RoomNumber.One].ChangeRoomColor(lastRoom3Colors);
                break;
        }

        sceneRooms[(int)newRoomNum].SetLockDoors(false); // Unlock doors in new room

        currentRoom = newRoomNum; // Update current room

        // Punish player if they tried to steal any items
        punisher.Punish(_heldObjects.Count);

        //TODO add logic (modify room behind them according to next layout)


        RuleManager.Instance.DiscoverRuleByTitle(rule);
        rule++;

        //FIXME TESTING ONLY
        //punisher.Punish(PunishmentManager.PunishmentType.Saturation, 1);
        //punisher.Punish(PunishmentManager.PunishmentType.HueShift, 1);
        //punisher.Punish(PunishmentManager.PunishmentType.ColorTint, 1);
        //punisher.Punish(PunishmentManager.PunishmentType.Blur, 1);
        //punisher.Punish(PunishmentManager.PunishmentType.LensDistortion, 1);
        //punisher.Punish(PunishmentManager.PunishmentType.FilmGrain, 1);
        //punisher.Punish(PunishmentManager.PunishmentType.Vignette, 1);
        //punisher.Punish(PunishmentManager.PunishmentType.ChromaticAberration, 1);
        //punisher.Punish(1);
    }

    // Funny logic for punishing messing with the mugs
    private HashSet<GrabNotifier> _heldObjects = new HashSet<GrabNotifier>();

    public void OnObjectGrabbed(GrabNotifier obj)
    {
        _heldObjects.Add(obj);
    }

    public void OnObjectReleased(GrabNotifier obj)
    {
        _heldObjects.Remove(obj);
    }
    public GameRule.RuleName rule = GameRule.RuleName.GeGa;
}