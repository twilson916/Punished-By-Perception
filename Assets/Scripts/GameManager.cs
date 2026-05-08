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

    [Tooltip("Drag the startup spawn cube here")]
    public MetaTeleportNode spawnPoint;

    [Tooltip("Drag in the hand reset detector")]
    public HandResetDetector detector;

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

    // Meta-rule game state
    private int challengeFailCount = 0;
    private int challengeCount = 0;        // total challenges encountered (for "every 3rd" rule)
    private bool lowPunishmentRestart = false; // set during reset, checked on next run start
    private bool hasPunishmentShield = false;

    // Constants
    private const int ROOMS_TO_WIN = 20;
    private const int MAX_PUNISHMENTS = 7;
    private const int MAX_CHALLENGE_FAILS = 3;
    private const int CHALLENGE_ROOM_INTERVAL = 3;
    private const int QUIZ_ROOM_INTERVAL = 7;
    private const int SHOP_ROOM_INTERVAL = 9;
    //private const int INVERT_ROOM_INTERVAL = 6; //hardcoded into meta rule

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
        currentState = GameState.Exploring;

        // ── Initialize the configurator and register meta-rules ──
        configurator = new RoomConfigurator();

        // Register meta-rules — these are always active in the system,
        // the environmental ones randomly decide whether to fire each room
        configurator.RegisterMetaRule(new PlantsSwappedRule());
        configurator.RegisterMetaRule(new LeftChairOutRule());
        configurator.RegisterMetaRule(new CeilingLightColorRule());
        configurator.RegisterMetaRule(new RightChairOutRule());
        configurator.RegisterMetaRule(new InvertEvery6thRoomRule());

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
        sceneRooms[(int)roomNum].ApplyEnvironmentModifiers(config.environment);
    }

    public void OnLoopTeleport() // Room 3 → Room 1 teleport
    {
        //Turn off any shops that mightve been in room three
        sceneRooms[(int)RoomNumber.Three].SetShopVisible(false);

        currentRoom = RoomNumber.One;
    }

    public void OnDoorClicked(DoorPos pos)
    {
        sceneRooms[(int)currentRoom].SetLockDoors(true);
        sceneRooms[(int)currentRoom].OpenDoor(pos);

        if (totalRoomsVisited == 1)
            sceneRooms[(int)RoomNumber.MinusOne].OpenDoor(DoorPos.Left);

        if((totalRoomsVisited + 1) % SHOP_ROOM_INTERVAL == 0)
        {
            sceneRooms[(int)currentRoom + 1].SetShopVisible(true); //make shop visible before entering room
        }

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

        if(totalRoomsVisited == 1) sceneRooms[(int)RoomNumber.MinusOne].SetLockDoors(true);

        // Close doors behind the player
        if (newRoomNum == RoomNumber.Two || newRoomNum == RoomNumber.Three)
        {
            sceneRooms[(int)newRoomNum - 1].CloseAllDoors();
        }

        // Close any shop from the previous room
        sceneRooms[(int)newRoomNum - 1].SetShopVisible(false);

        // Build and apply upcoming rooms
        switch (newRoomNum)
        {
            case RoomNumber.One:
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
                totalRoomsVisited++;

                if (roomConfigs.ContainsKey(RoomNumber.Three))
                {
                    roomConfigs[RoomNumber.One] = roomConfigs[RoomNumber.Three];
                    ApplyRoomConfig(RoomNumber.One, roomConfigs[RoomNumber.Three]);
                }
                break;
        }



        // Check win condition
        if (totalRoomsVisited >= ROOMS_TO_WIN && newRoomNum != RoomNumber.One)
        {
            // TODO: trigger win state FIXME
            Debug.Log("[GameManager] YOU WIN!");
            return;
        }

        // Room interval events
        if (totalRoomsVisited % CHALLENGE_ROOM_INTERVAL == 0 && newRoomNum != RoomNumber.One)
        {
            // TODO: trigger additional challenge (teammate implementing) FIXME
            Debug.Log("[GameManager] 3rd room — additional challenge!");
        }

        if (totalRoomsVisited % QUIZ_ROOM_INTERVAL == 0 && newRoomNum != RoomNumber.One)
        {
            // TODO: trigger quiz (teammate implementing) FIXME
            Debug.Log("[GameManager] 7th room — quiz time!");
        }

        if (totalRoomsVisited % SHOP_ROOM_INTERVAL == 0 && newRoomNum != RoomNumber.One)
        {
            sceneRooms[(int)newRoomNum].SetShopVisible(true);
            if(newRoomNum == RoomNumber.Three)
            {
                sceneRooms[(int)RoomNumber.One].SetShopVisible(true);
            }
            Debug.Log("[GameManager] 9th room — shop!");
        }



        // Punish for held objects (mug logic)
        if (hasPunishmentShield && _heldObjects.Count > 0)
        {
            punisher.Punish(_heldObjects.Count - 1);
            hasPunishmentShield = false;
        }
        else
        {
            punisher.Punish(_heldObjects.Count);
        }

        // Unlock new room's doors
        if (newRoomNum != RoomNumber.Three)
        {
            sceneRooms[(int)newRoomNum].SetLockDoors(false); //FIXME FIXME FIXME only unlock if no challenge or quiz
        }
        currentRoom = newRoomNum;
    }

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
                if (hasPunishmentShield)
                {
                    hasPunishmentShield = false;
                    Debug.Log("[GameManager] Shield absorbed!");
                }
                else
                {
                    punisher.Punish(1);

                    // Check 5-punishment auto reset
                    int activePunishments = punisher.GetActivePunishmentCount();
                    if (activePunishments >= MAX_PUNISHMENTS)
                    {
                        Debug.Log("[GameManager] 5 punishments reached — auto reset!");
                        ResetRun();
                        return;
                    }
                }
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

    public void OnChallengeComplete(bool success) //FIXME wire up to challenges when added
    {
        challengeCount++;

        if (success)
        {
            hasPunishmentShield = true;
            Debug.Log("[GameManager] Challenge passed — shield acquired!");
        }
        else
        {
            challengeFailCount++;
            punisher.Punish(2); // double punishment for failure

            if (challengeFailCount >= MAX_CHALLENGE_FAILS)
            {
                Debug.Log("[GameManager] 3 challenge fails — auto reset!");
                ResetRun();
                return;
            }
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

    public void OnShopItemPurchased(ShopUIController.ShopItem item)
    {
        switch (item)
        {
            case ShopUIController.ShopItem.Mulligan:
                //Cost is 1 punish
                punisher.Punish(1);

                // Undo a random punishment (nothing is gained)
                int removed = punisher.Unpunish(1);
                if (removed > 0)
                    Debug.Log("[GameManager] Mulligan used — removed a punishment!");
                else
                    Debug.Log("[GameManager] Mulligan used — but nothing to remove.");
                break;

            case ShopUIController.ShopItem.PunishmentShield:
                //Cost is 1 punish
                punisher.Punish(1);

                //Then they get a shield (nothing is gained)
                hasPunishmentShield = true;
                Debug.Log("[GameManager] Punishment shield purchased!");
                break;

            case ShopUIController.ShopItem.RulebookSilver:
                //Cost is 1 punish
                punisher.Punish(1);

                MetaRuleRegistry.Instance.DiscoverNextFactualRule();
                Debug.Log("[GameManager] Silver rulebook purchased!");
                break;

            case ShopUIController.ShopItem.RulebookGold:
                //Cost is 1 punish
                punisher.Punish(1);

                //FIXME FIXME tricky rules require the user to answer a quiz
                //enqueue question then on challenge complete feedback give them the factual hint as follows...
                //MetaRuleRegistry.Instance.DiscoverNextFactualRule();
                Debug.Log("[GameManager] Gold rulebook purchased!");
                break;
        }
    }

    // Full run reset. Clears all game state except rulebook discoveries.
    // Call this from the hand-on-floor gesture, 5-punishment limit, or 3-challenge-fail limit.
    public void ResetRun()
    {
        Debug.Log("[GameManager] Resetting run...");

        // Check if this reset qualifies for low-punishment next-run penalty
        int currentPunishments = punisher.GetActivePunishmentCount();
        lowPunishmentRestart = currentPunishments < 3;

        // Reset game state tracking
        totalRoomsVisited = 1;
        currentRoom = RoomNumber.One;
        currentState = GameState.Exploring;

        // Reset meta-rule tracking
        consecutiveSafeChoices = 0;
        lastChosenResult = RuleResultType.Safe;
        recentChosenRules.Clear();
        hasPunishmentShield = false;

        challengeFailCount = 0;
        challengeCount = 0;

        // Reset held objects
        _heldObjects.Clear();

        // Clear all active punishments (visual effects etc.)
        punisher.ResetAllPunishments();

        // Reset configurator
        configurator.ResetState();

        // Clear room configs
        roomConfigs.Clear();

        // Close all doors in all rooms
        foreach (var room in sceneRooms)
        {
            room.CloseAllDoors();
            room.SetLockDoors(true);
            room.SetShopVisible(false);

            //FIXME FIXME FIXME reset all quiz windows
        }

        // Rebuild rooms fresh
        var neutralConfig = MakeNeutralRoomConfig();
        roomConfigs[RoomNumber.MinusOne] = neutralConfig;
        ApplyRoomConfig(RoomNumber.MinusOne, neutralConfig);

        var room1Config = BuildNewRoomConfig();
        roomConfigs[RoomNumber.One] = room1Config;
        ApplyRoomConfig(RoomNumber.One, room1Config);

        var room2Config = BuildNewRoomConfig();
        roomConfigs[RoomNumber.Two] = room2Config;
        ApplyRoomConfig(RoomNumber.Two, room2Config);

        // Unlock first room
        sceneRooms[(int)RoomNumber.One].SetLockDoors(false);

        // Teleport player back to spawn
        spawnPoint.TriggerInstantTeleport();

        // Play new round audio
        AudioManager.Play(AudioManager.SoundCategory.NewRound);

        // Apply low-punishment penalty from PREVIOUS run
        if (lowPunishmentRestart)
        {
            punisher.Punish(2); //double punishment but don't want to give them 3 as then they could just reset immediately
            lowPunishmentRestart = false;
            Debug.Log("[GameManager] Started with a punishment for resetting too early!");
        }

        Debug.Log("[GameManager] Run reset complete.");
    }
}