using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
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

    // ── Room configuration ──
    private RoomConfigurator configurator;
    private Dictionary<RoomNumber, RoomConfig> roomConfigs = new Dictionary<RoomNumber, RoomConfig>();

    // ── Meta-rule tracking ──
    private MetaRuleContext metaContext;
    private int consecutiveSafeChoices = 0;
    private RuleResultType lastChosenResult = RuleResultType.Safe;
    private List<GameRule.RuleName> recentChosenRules = new List<GameRule.RuleName>();
    private const int RECENT_RULES_HISTORY = 10;

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
        // FIXME: testing only — clear save state
        RuleManager.Instance.ClearAllRulesAndSave();

        currentState = GameState.Exploring;

        // ── Initialize the configurator and register meta-rules ──
        configurator = new RoomConfigurator();

        // Register your meta-rules here. Comment/uncomment as you develop them.
        // configurator.RegisterMetaRule(new EveryNthRoomInvertRule(5));
        // configurator.RegisterMetaRule(new VasePositionInvertRule());
        // configurator.RegisterMetaRule(new HubrisRule(3));

        // ── Build initial rooms ──
        // Room -1 (behind player): neutral/white, no gameplay
        var neutralConfig = MakeNeutralRoomConfig();
        roomConfigs[RoomNumber.MinusOne] = neutralConfig;
        ApplyRoomConfig(RoomNumber.MinusOne, neutralConfig);

        // Room 1 (current room): first real room
        var room1Config = BuildNewRoomConfig();
        roomConfigs[RoomNumber.One] = room1Config;
        ApplyRoomConfig(RoomNumber.One, room1Config);

        // Room 2 (ahead): pre-built so it's ready
        var room2Config = BuildNewRoomConfig();
        roomConfigs[RoomNumber.Two] = room2Config;
        ApplyRoomConfig(RoomNumber.Two, room2Config);

        // Unlock first room's doors
        sceneRooms[(int)currentRoom].SetLockDoors(false);
    }

    // ─────────────────────────────────────────────
    //  ROOM BUILDING
    // ─────────────────────────────────────────────

    // Ask the configurator for a new room based on current game state.
    private RoomConfig BuildNewRoomConfig()
    {
        var allRules = RuleManager.Instance.GetAllRules();
        var discovered = RuleManager.Instance.GetDiscoveredRuleNames();

        metaContext = new MetaRuleContext
        {
            totalRoomsVisited = totalRoomsVisited,
            consecutiveSafeChoices = consecutiveSafeChoices,
            lastChosenResult = lastChosenResult,
            recentChosenRules = new List<GameRule.RuleName>(recentChosenRules)
        };

        return configurator.BuildRoom(allRules, discovered, metaContext);
    }

    // Creates a neutral config for room -1 (behind the player). No gameplay rules.
    private RoomConfig MakeNeutralRoomConfig()
    {
        var config = new RoomConfig();
        for (int i = 0; i < 3; i++)
        {
            config.doors[i] = new DoorConfig
            {
                doorColor = Color.white,
                handleColor = Color.gray,
                ruleName = GameRule.RuleName.None,
                baseResult = RuleResultType.Safe,
                finalResult = RuleResultType.Safe
            };
        }
        return config;
    }

    // Push a RoomConfig's visual data to the actual RoomController in the scene.
    private void ApplyRoomConfig(RoomNumber roomNum, RoomConfig config)
    {
        var roomColors = new RoomColors(
            doors: new Color[] { config.doors[0].doorColor, config.doors[1].doorColor, config.doors[2].doorColor },
            doorHandles: new Color[] { config.doors[0].handleColor, config.doors[1].handleColor, config.doors[2].handleColor }
        );

        sceneRooms[(int)roomNum].ChangeRoomColor(roomColors);

        // Pass environment modifiers to the room controller for object placement, lighting, etc.
        sceneRooms[(int)roomNum].ApplyEnvironmentModifiers(config.environmentModifiers);
    }

    // ─────────────────────────────────────────────
    //  SIGNAL RECEIVERS
    // ─────────────────────────────────────────────

    public void OnLoopTeleport() // Room 3 → Room 1 teleport
    {
        currentRoom = RoomNumber.One;
    }

    public void OnDoorClicked(DoorPos pos)
    {
        sceneRooms[(int)currentRoom].SetLockDoors(true);
        sceneRooms[(int)currentRoom].OpenDoor(pos);

        // Resolve immediately — punishment/challenge hits when the door opens
        if (roomConfigs.ContainsKey(currentRoom))
        {
            RoomConfig config = roomConfigs[currentRoom];
            DoorConfig chosenDoor = config.doors[(int)pos];

            //play noise depending on rule action
            switch(chosenDoor.finalResult)
            {
                case RuleResultType.Safe:
                    break;
                case RuleResultType.Punishment:
                    AudioManager.Play(AudioManager.SoundCategory.Wrong);
                    break;
                case RuleResultType.Challenge:
                    AudioManager.Play(AudioManager.SoundCategory.Ding);
                    break;
                case RuleResultType.Random:
                    AudioManager.Play(AudioManager.SoundCategory.Random);
                    break;
            }

            RuleResultType resolvedResult = chosenDoor.ResolveResult();
            ProcessDoorResult(resolvedResult, chosenDoor, config);
        }
    }

    public void OnRoomEntry(RoomNumber newRoomNum)
    {
        if (currentRoom == newRoomNum) return;

        // Close doors behind the player
        if (newRoomNum == RoomNumber.Two || newRoomNum == RoomNumber.Three)
        {
            sceneRooms[(int)newRoomNum - 1].CloseAllDoors();
        }

        // Build and apply upcoming rooms
        switch (newRoomNum)
        {
            case RoomNumber.One:
                totalRoomsVisited++;

                var room2Config = BuildNewRoomConfig();
                roomConfigs[RoomNumber.Two] = room2Config;
                ApplyRoomConfig(RoomNumber.Two, room2Config);
                break;

            case RoomNumber.Two:
                totalRoomsVisited++;

                var room3Config = BuildNewRoomConfig();
                roomConfigs[RoomNumber.Three] = room3Config;
                ApplyRoomConfig(RoomNumber.Three, room3Config);

                if (roomConfigs.ContainsKey(RoomNumber.Two))
                    ApplyRoomConfig(RoomNumber.MinusOne, roomConfigs[RoomNumber.Two]);
                break;

            case RoomNumber.Three:
                if (roomConfigs.ContainsKey(RoomNumber.Three))
                {
                    roomConfigs[RoomNumber.One] = roomConfigs[RoomNumber.Three];
                    ApplyRoomConfig(RoomNumber.One, roomConfigs[RoomNumber.Three]);
                }
                break;
        }

        // Unlock new room's doors
        sceneRooms[(int)newRoomNum].SetLockDoors(false);
        currentRoom = newRoomNum;

        // Punish for held objects (mug logic)
        punisher.Punish(_heldObjects.Count);
    }

    // ─────────────────────────────────────────────
    //  DOOR RESULT PROCESSING
    // ─────────────────────────────────────────────

    private void ProcessDoorResult(RuleResultType result, DoorConfig door, RoomConfig roomConfig)
    {
        // ── Discover the rule ──
        // RuleManager handles parent-gating: if the parent isn't discovered,
        // the rule won't show in the rulebook yet (but the player still gets punished).
        RuleManager.Instance.DiscoverRuleByTitle(door.ruleName);

        // ── Track meta-rule context ──
        lastChosenResult = result;
        if (result == RuleResultType.Safe)
            consecutiveSafeChoices++;
        else
            consecutiveSafeChoices = 0;

        recentChosenRules.Add(door.ruleName);
        if (recentChosenRules.Count > RECENT_RULES_HISTORY)
            recentChosenRules.RemoveAt(0);

        // ── Act on the result ──
        switch (result)
        {
            case RuleResultType.Safe:
                Debug.Log($"[GameManager] SAFE — Rule: {door.ruleName}");
                // No action
                break;

            case RuleResultType.Punishment:
                Debug.Log($"[GameManager] PUNISHMENT — Rule: {door.ruleName}");
                punisher.Punish(1);
                break;

            case RuleResultType.Challenge:
                Debug.Log($"[GameManager] CHALLENGE — Rule: {door.ruleName}");
                // TODO: Trigger your challenge/quiz system here FIXME
                // e.g. ChallengeManager.Instance.StartChallenge();
                break;

            case RuleResultType.Random:
                // Shouldn't reach here since ResolveResult() already rolled,
                // but just in case:
                Debug.LogWarning("[GameManager] Random result was not resolved before processing.");
                break;
        }

        // ── Log meta-rule info for debugging ──
        if (roomConfig.activeMetaRules.Count > 0)
        {
            string metaInfo = string.Join(", ", roomConfig.activeMetaRules.Select(m => m.ruleId));
            Debug.Log($"[GameManager] Active meta-rules this room: {metaInfo}");
            Debug.Log($"[GameManager] Base result was {door.baseResult}, final was {door.finalResult}, resolved to {result}");
        }
    }

    // Funny logic for punishing messing with the mugs
    private HashSet<GrabNotifier> _heldObjects = new HashSet<GrabNotifier>();

    public void OnObjectGrabbed(GrabNotifier obj)
    {
        _heldObjects.Add(obj);
        AudioManager.Play(AudioManager.SoundCategory.ObjPickup);
    }

    public void OnObjectReleased(GrabNotifier obj)
    {
        _heldObjects.Remove(obj);
    }
}