using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using MyGame.Resources;
using Meta.WitAi;

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
    public GameState currentState { get; private set; }

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
    private int questionCount = 0;
    private bool lowPunishmentRestart = false;
    private bool hasPunishmentShield = false;
    private bool awaitingQuizUnlock = false; // true when a room-entry quiz is blocking door unlock
    private bool gauntletFired = false;      // prevents the gauntlet from triggering more than once per run

    // Difficulty
    public enum DifficultyMode { Normal, Child, Baby, Nightmare }
    public DifficultyMode difficulty = DifficultyMode.Normal;

    // Constants
    private const int ROOMS_TO_WIN = 5;//20; //FIXME testing only
    private const int MAX_PUNISHMENTS = 7;
    private const int CHALLENGE_ROOM_INTERVAL = 3;
    private const int QUIZ_ROOM_INTERVAL = 7;
    private const int SHOP_ROOM_INTERVAL = 9;
    private const float FINAL_QUIZ_PASS_THRESHOLD = 0.79f;
    private const float FINAL_QUIZ_PASS_THRESHOLD_BABY = 0.49f;
    //private const int INVERT_ROOM_INTERVAL = 6; //hardcoded into meta rule

    [SerializeField] private AudioClip monologue;
    private AudioSource audioSource;
    private bool playedMonologue = false;
    private bool endingDoorsClosed = false;
    [SerializeField] private Transform playerTransform;

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

        audioSource = GetComponent<AudioSource>();
    }

    private void Update()
    {
        if (currentState == GameState.Ending)
        {
            if (playerTransform.position.y < -1 && !playedMonologue)
            {
                playedMonologue = true;
                audioSource.clip = monologue;
                audioSource.Play();
            }
            if (playerTransform.position.z > 1.5 && !endingDoorsClosed)
            {
                endingDoorsClosed = true;
                sceneRooms[(int)RoomNumber.MinusOne].SetLockDoors(false);
                sceneRooms[(int)RoomNumber.One].SetLockDoors(false);
                sceneRooms[(int)RoomNumber.MinusOne].CloseAllDoors();
            }
        }
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

        // The quiz window lives in Room 1's physical space — the player cannot reach it from Room 3
        // because the rooms are physically separated and the teleport fires as soon as they step in.
        // So in practice the Room 3 quiz never completes before the teleport; the Room 1 copy is
        // always the one the player answers. The IsQuizPending check is a belt-and-suspenders guard
        // for the edge case where that assumption breaks (e.g. a delayed teleport trigger):
        // if Room 3's quiz somehow completed already, clear Room 1's duplicate so it doesn't show twice.
        if (!sceneRooms[(int)RoomNumber.Three].IsQuizPending() && awaitingQuizUnlock)
        {
            sceneRooms[(int)RoomNumber.One].ResetQuiz();
            awaitingQuizUnlock = false;
        }

        sceneRooms[(int)RoomNumber.Three].ResetQuiz();

        // Unlock room 1 directly — unless a quiz is waiting there
        if (!awaitingQuizUnlock)
            sceneRooms[(int)RoomNumber.One].SetLockDoors(false);

        currentRoom = RoomNumber.One;
    }

    public void OnDoorClicked(DoorPos pos, RoomNumber fromRoom)
    {
        if (currentState == GameState.Ending) return;

        sceneRooms[(int)fromRoom].SetLockDoors(true);
        sceneRooms[(int)fromRoom].OpenDoor(pos);

        if (fromRoom == RoomNumber.MinusOne)
        {
            currentState = GameState.Ending;
            sceneRooms[(int)RoomNumber.One].SetLockDoors(false); //no longer allowed to continue
            punisher.ResetAllPunishments();
            return;
        }

        if (totalRoomsVisited == 1)
            sceneRooms[(int)RoomNumber.MinusOne].SetLockDoor(DoorPos.Left, false);

        if ((totalRoomsVisited + 1) % SHOP_ROOM_INTERVAL == 0)
        {
            sceneRooms[(int)fromRoom + 1].SetShopVisible(true); //make shop visible before entering room
        }

        // Resolve immediately — punishment/challenge hits when the door opens
        if (roomConfigs.ContainsKey(fromRoom))
        {
            RoomConfig config = roomConfigs[fromRoom];
            DoorConfig chosenDoor = config.doors[(int)pos];

            //play noise depending on rule action
            switch (chosenDoor.finalResult)
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
            ProcessDoorResult(resolvedResult, chosenDoor, config, fromRoom);
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



        // Check win condition — queue the final gauntlet and lock doors until it's complete
        if (!gauntletFired && totalRoomsVisited >= ROOMS_TO_WIN)
        {
            gauntletFired = true;
            currentRoom = newRoomNum; // must be set before the early return so OnQuizSessionComplete unlocks the right room
            Debug.Log("[GameManager] Room 20 reached — queueing final gauntlet.");
            var gauntletQs = FinalGauntletGenerator.Generate(
                RuleManager.Instance.GetAllRules(),
                RuleManager.Instance.GetDiscoveredRuleNames(),
                configurator);
            foreach (var gq in gauntletQs)
            {
                sceneRooms[(int)newRoomNum].EnqueueQuestion(gq);
                if (newRoomNum == RoomNumber.Three)
                    sceneRooms[(int)RoomNumber.One].EnqueueQuestion(gq); // mirror to Room 1 for after teleport
            }
            awaitingQuizUnlock = true;
            return;
        }

        // Room interval events.
        // Room 3 and Room 1 are the same physical space — the player teleports immediately on entry.
        // We queue to the current room (Room 3) so the quiz can appear there, and also mirror
        // it to Room 1 as a backup. OnLoopTeleport checks whether Room 3's quiz was already
        // answered and clears the Room 1 duplicate if so; otherwise Room 1 shows it post-teleport.
        if (totalRoomsVisited % CHALLENGE_ROOM_INTERVAL == 0 && newRoomNum != RoomNumber.One)
        {
            Debug.Log("[GameManager] Challenge room — queueing visual challenge.");
            int diff = Mathf.Clamp(totalRoomsVisited / 5 + 1, 1, 5);
            var cq = ChallengeGenerator.Generate(diff);
            sceneRooms[(int)newRoomNum].EnqueueQuestion(cq);
            if (newRoomNum == RoomNumber.Three)
                sceneRooms[(int)RoomNumber.One].EnqueueQuestion(cq); // mirror to Room 1 for after teleport
            awaitingQuizUnlock = true;
        }

        if (totalRoomsVisited % QUIZ_ROOM_INTERVAL == 0 && newRoomNum != RoomNumber.One)
        {
            Debug.Log("[GameManager] Quiz room — queueing meta quiz question.");
            var qq = QuizGenerator.Generate();
            sceneRooms[(int)newRoomNum].EnqueueQuestion(qq);
            if (newRoomNum == RoomNumber.Three)
                sceneRooms[(int)RoomNumber.One].EnqueueQuestion(qq); // mirror to Room 1 for after teleport
            awaitingQuizUnlock = true;
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
        ApplyPunishment(_heldObjects.Count);

        // Unlock new room's doors — held back if a quiz was just queued
        if (newRoomNum != RoomNumber.Three && !awaitingQuizUnlock)
        {
            sceneRooms[(int)newRoomNum].SetLockDoors(false);
        }
        currentRoom = newRoomNum;
    }

    private void ProcessDoorResult(RuleResultType result, DoorConfig door, RoomConfig roomConfig, RoomNumber fromRoom)
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
                ApplyPunishment(1);
                break;

            case RuleResultType.Challenge:
                Debug.Log($"[GameManager] CHALLENGE — Rule: {door.ruleName}");
                int cDiff = Mathf.Clamp(totalRoomsVisited / 5 + 1, 1, 5);
                var challengeQ = ChallengeGenerator.Generate(cDiff);
                // Route challenge to the next room so it blocks the player on entry.
                // Apply the same Room 3/1 duality as all other enqueue sites.
                RoomNumber nextRoom = fromRoom switch
                {
                    RoomNumber.One   => RoomNumber.Two,
                    RoomNumber.Two   => RoomNumber.Three,
                    RoomNumber.Three => RoomNumber.One,
                    _                => RoomNumber.Two
                };
                sceneRooms[(int)nextRoom].EnqueueQuestion(challengeQ);
                if (nextRoom == RoomNumber.Three)
                    sceneRooms[(int)RoomNumber.One].EnqueueQuestion(challengeQ); // mirror to Room 1 for after teleport
                awaitingQuizUnlock = true;
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

    // Called by QuizUI before resolving a VisualChallenge answer.
    // Increments questionCount and returns the effective correct index —
    // on every 3rd challenge, index 0 (left/A) is correct regardless of the question's actual answer.
    public int GetEffectiveCorrectIndex(QuizQuestion q)
    {
        questionCount++;
        if (questionCount % 3 == 0)
        {
            Debug.Log($"[GameManager] Every 3rd question ({questionCount}) — left answer override active.");
            return 0;
        }
        return q.correctAnswerIndex;
    }

    // Called by QuizUI once the entire session queue is drained.
    public void OnQuizSessionComplete()
    {
        RuleManager.Instance?.SetRulebookVisible(true); // restore if gauntlet Q5 hid it
        if (awaitingQuizUnlock)
        {
            awaitingQuizUnlock = false;
            sceneRooms[(int)currentRoom].SetLockDoors(false);
        }
    }

    // ── Gauntlet / rulebook helpers ─────────────────────────────────────────

    // Applies a gauntlet question's room config to the current room, changing door colors visually.
    public void ApplyGauntletRoomConfig(RoomConfig config) => ApplyRoomConfig(currentRoom, config);

    public void HideRulebook() => RuleManager.Instance?.SetRulebookVisible(false);
    public void ShowRulebook() => RuleManager.Instance?.SetRulebookVisible(true);

    // Called by QuizUI after each non-FinalQuiz question resolves.
    // wasCorrect and effect metadata live on the question object itself.
    public void OnQuestionAnswered(QuizQuestion q)
    {
        if (q.wasCorrect)
        {
            switch (q.successEffect)
            {
                case QuizQuestion.SuccessEffect.PunishmentShield:
                    hasPunishmentShield = true;
                    Debug.Log("[GameManager] Quiz correct — shield acquired!");
                    break;

                case QuizQuestion.SuccessEffect.RevealRule:
                    MetaRuleRegistry.Instance.DiscoverNextTrickyRule();
                    Debug.Log("[GameManager] Quiz correct — revealed next tricky rule.");
                    break;
            }
        }
        else
        {
            switch (q.failEffect)
            {
                case QuizQuestion.FailEffect.Punishment:
                    ApplyPunishment(1);
                    break;

                case QuizQuestion.FailEffect.DoublePunishment:
                    ApplyPunishment(2);
                    break;

                case QuizQuestion.FailEffect.TriplePunishment:
                    ApplyPunishment(3);
                    break;
            }
        }
    }

    // Called by QuizUI after all FinalQuiz questions in a session resolve.
    // Returns true if passed — QuizUI uses this for the results screen display.
    public bool OnFinalQuizComplete(int correct, int total)
    {
        float threshold = difficulty == DifficultyMode.Baby ? FINAL_QUIZ_PASS_THRESHOLD_BABY : FINAL_QUIZ_PASS_THRESHOLD;
        bool passed = (float)correct / total >= threshold;

        if (passed)
        {
            Debug.Log($"[GameManager] FinalQuiz passed ({correct}/{total}) — entering fake ending.");
            currentState = GameState.FakeEnding; // disables two-hand reset, player must complete the fake ending
        }
        else
        {
            Debug.Log($"[GameManager] FinalQuiz failed ({correct}/{total}) — double punishment.");
            AudioManager.Play(AudioManager.SoundCategory.QuizFail);
            ApplyPunishment(2);
        }

        return passed;
    }

    private void ApplyPunishment(int severity)
    {
        if (severity <= 0) return;

        // Child mode: all punishments capped at severity 1
        if (difficulty == DifficultyMode.Child)
            severity = 1;

        // Nightmare mode: severity multiplied by 1.5, rounded up
        if (difficulty == DifficultyMode.Nightmare)
            severity = Mathf.CeilToInt(severity * 1.5f);

        // Baby mode: severity 1, 50% chance of hitting — shield only consumed if it hits
        if (difficulty == DifficultyMode.Baby)
        {
            severity = 1;
            if (UnityEngine.Random.value >= 0.5f)
            {
                Debug.Log("[GameManager] Baby mode — punishment missed!");
                return;
            }
        }

        if (hasPunishmentShield)
        {
            hasPunishmentShield = false;
            Debug.Log("[GameManager] Shield absorbed punishment!");
            return;
        }

        punisher.Punish(severity);

        if (punisher.GetActivePunishmentCount() >= MAX_PUNISHMENTS)
        {
            Debug.Log("[GameManager] Max punishments reached — auto reset!");
            ResetRun();
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
                ApplyPunishment(1);

                // Undo a random punishment (nothing is gained)
                int removed = punisher.Unpunish(1);
                if (removed > 0)
                    Debug.Log("[GameManager] Mulligan used — removed a punishment!");
                else
                    Debug.Log("[GameManager] Mulligan used — but nothing to remove.");
                break;

            case ShopUIController.ShopItem.PunishmentShield:
                //Cost is 1 punish
                ApplyPunishment(1);

                //Then they get a shield (nothing is gained)
                hasPunishmentShield = true;
                Debug.Log("[GameManager] Punishment shield purchased!");
                break;

            case ShopUIController.ShopItem.RulebookSilver:
                //Cost is 1 punish
                ApplyPunishment(1);

                MetaRuleRegistry.Instance.DiscoverNextFactualRule();
                Debug.Log("[GameManager] Silver rulebook purchased!");
                break;

            case ShopUIController.ShopItem.RulebookGold:
                //Cost is 1 punish
                ApplyPunishment(1);

                // Enqueue a quiz — correct answer reveals the next tricky meta-rule.
                // No awaitingQuizUnlock: this is a voluntary bonus, doors stay as-is.
                var revealQ = QuizGenerator.GenerateRevealRule();
                sceneRooms[(int)currentRoom].EnqueueQuestion(revealQ);
                if (currentRoom == RoomNumber.Three)
                    sceneRooms[(int)RoomNumber.One].EnqueueQuestion(revealQ); // mirror to Room 1 for after teleport
                Debug.Log("[GameManager] Gold rulebook purchased — quiz incoming.");
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

        playedMonologue = false;
        endingDoorsClosed = false;

        // Reset game state tracking
        totalRoomsVisited = 1;
        currentRoom = RoomNumber.One;
        currentState = GameState.Exploring;

        // Reset meta-rule tracking
        consecutiveSafeChoices = 0;
        lastChosenResult = RuleResultType.Safe;
        recentChosenRules.Clear();
        hasPunishmentShield = false;

        questionCount = 0;
        gauntletFired = false;

        // Ensure rulebook is visible (gauntlet Q5 may have hidden it)
        RuleManager.Instance?.SetRulebookVisible(true);

        // Reset held objects
        _heldObjects.Clear();

        // Clear all active punishments (visual effects etc.)
        punisher.ResetAllPunishments();

        // Reset configurator
        configurator.ResetState();

        // Clear room configs
        roomConfigs.Clear();

        // Close all doors in all rooms
        awaitingQuizUnlock = false;

        foreach (var room in sceneRooms)
        {
            room.CloseAllDoors();
            room.SetLockDoors(true);
            room.SetShopVisible(false);
            room.ResetQuiz();
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