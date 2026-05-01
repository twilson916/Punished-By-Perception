using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// ─────────────────────────────────────────────
//  DATA STRUCTURES
// ─────────────────────────────────────────────

[System.Serializable]
public class DoorConfig
{
    public Color doorColor;
    public Color handleColor;
    public GameRule.RuleName ruleName;
    public RuleResultType baseResult;   // result before meta-rules
    public RuleResultType finalResult;  // result after meta-rules

    // Call this at the moment the player commits to a door.
    // If the result is Random, it rolls 1/3 between Safe, Punishment, Challenge.
    // Otherwise returns finalResult as-is.
    public RuleResultType ResolveResult()
    {
        if (finalResult == RuleResultType.Random)
        {
            int roll = UnityEngine.Random.Range(0, 3);
            return roll switch
            {
                0 => RuleResultType.Safe,
                1 => RuleResultType.Punishment,
                _ => RuleResultType.Challenge
            };
        }
        return finalResult;
    }
}

[System.Serializable]
public class RoomConfig
{
    public DoorConfig[] doors = new DoorConfig[3]; // indexed by DoorPos (Left=0, Center=1, Right=2)

    // Meta-rules that were active when this room was built (for debugging/display).
    public List<ActiveMetaRule> activeMetaRules = new List<ActiveMetaRule>();

    // Key-value bag for environmental modifiers that RoomController reads.
    // Examples: "vase_side" → "right", "lights_off" → true
    // RoomController checks these and moves objects / changes lighting accordingly.
    public Dictionary<string, object> environmentModifiers = new Dictionary<string, object>();
}

[System.Serializable]
public class ActiveMetaRule
{
    public string ruleId;
    public string description; // human-readable for debug / future rulebook display
}

// ─────────────────────────────────────────────
//  META-RULE SYSTEM
// ─────────────────────────────────────────────

// Snapshot of game state passed to meta-rules so they can decide
// whether to activate and how to mutate the room.
public class MetaRuleContext
{
    public int totalRoomsVisited;
    public int consecutiveSafeChoices;
    public RuleResultType lastChosenResult;
    public List<GameRule.RuleName> recentChosenRules; // last N rules the player picked
}

// Implement this for each hardcoded meta-rule.
// IsActive checks whether the rule fires this room.
// Apply mutates the RoomConfig (door results, environment flags, etc.).
public interface IMetaRule
{
    string Id { get; }
    string Description { get; }
    bool IsActive(MetaRuleContext context);
    void Apply(RoomConfig config, MetaRuleContext context);
}

//FIXME change these and implement more
// ── Example meta-rules (register these in GameManager or RoomConfigurator) ──

// Every N-th room, Safe and Punishment swap on all doors.
public class EveryNthRoomInvertRule : IMetaRule
{
    private readonly int _n;
    public string Id => $"invert_every_{_n}";
    public string Description => $"Every {_n} rooms, Safe and Punishment are swapped.";

    public EveryNthRoomInvertRule(int n) { _n = n; }

    public bool IsActive(MetaRuleContext ctx) => ctx.totalRoomsVisited > 1 && ctx.totalRoomsVisited % _n == 0;

    public void Apply(RoomConfig config, MetaRuleContext ctx)
    {
        foreach (var door in config.doors)
        {
            door.finalResult = door.finalResult switch
            {
                RuleResultType.Safe => RuleResultType.Punishment,
                RuleResultType.Punishment => RuleResultType.Safe,
                _ => door.finalResult
            };
        }
        config.activeMetaRules.Add(new ActiveMetaRule { ruleId = Id, description = Description });
    }
}

// Randomly places a vase on the left or right side of the room.
// When it's on the right, all door results invert.
// The player has to learn: "vase right = everything flips."
public class VasePositionInvertRule : IMetaRule
{
    public string Id => "vase_invert";
    public string Description => "When the vase is on the right, door results are inverted.";

    public bool IsActive(MetaRuleContext ctx) => true; // always eligible; Apply decides randomly

    public void Apply(RoomConfig config, MetaRuleContext ctx)
    {
        bool vaseOnRight = UnityEngine.Random.value > 0.5f;
        config.environmentModifiers["vase_side"] = vaseOnRight ? "right" : "left";

        if (vaseOnRight)
        {
            foreach (var door in config.doors)
            {
                door.finalResult = door.finalResult switch
                {
                    RuleResultType.Safe => RuleResultType.Punishment,
                    RuleResultType.Punishment => RuleResultType.Safe,
                    _ => door.finalResult
                };
            }
            config.activeMetaRules.Add(new ActiveMetaRule { ruleId = Id, description = Description });
        }
    }
}

/// After N consecutive safe choices the player gets cocky — force the left door to Punishment.
public class HubrisRule : IMetaRule
{
    private readonly int _threshold;
    public string Id => "hubris";
    public string Description => "Pride comes before the fall.";

    public HubrisRule(int threshold = 3) { _threshold = threshold; }

    public bool IsActive(MetaRuleContext ctx) => ctx.consecutiveSafeChoices >= _threshold;

    public void Apply(RoomConfig config, MetaRuleContext ctx)
    {
        // Force the leftmost Safe door to become a Punishment
        for (int i = 0; i < config.doors.Length; i++)
        {
            if (config.doors[i].finalResult == RuleResultType.Safe)
            {
                config.doors[i].finalResult = RuleResultType.Punishment;
                config.activeMetaRules.Add(new ActiveMetaRule { ruleId = Id, description = Description });
                break; // only flip one
            }
        }
    }
}

//end FIXME add/edit meta rules

// ─────────────────────────────────────────────
//  ROOM CONFIGURATOR
// ─────────────────────────────────────────────

public class RoomConfigurator
{
    // Undiscovered root rules are 3x more likely to be picked than discovered rules
    private const float UNDISCOVERED_ROOT_WEIGHT = 3f;
    private const float DISCOVERED_WEIGHT = 1f;
    private const float DISCOVERED_SAFE_WEIGHT = 0.15f;
    private const int MAX_SAFE_GUARANTEE_ATTEMPTS = 30;

    private HashSet<GameRule.RuleName> _dupeProtectionDoors = new HashSet<GameRule.RuleName>();

    private readonly List<IMetaRule> _metaRules = new List<IMetaRule>();

    // Register a meta-rule. Call during setup before any rooms are built.
    public void RegisterMetaRule(IMetaRule rule) => _metaRules.Add(rule);

    // Build a complete room configuration.
    // allRules: Every rule from the CSV (via RuleManager).
    // discoveredRules: Set of rule names the player has discovered so far.
    // context: Current game state for meta-rule evaluation.
    public RoomConfig BuildRoom(
        List<GameRule> allRules,
        HashSet<GameRule.RuleName> discoveredRules,
        MetaRuleContext context)
    {
        // ── Build the "available pool" ──
        //    Available = discovered rules  +  undiscovered rules that have NO parent (root rules).
        //    Weighted: undiscovered roots get higher weight so the player encounters new categories.
        var availablePool = new List<GameRule>();
        var weights = new List<float>();

        foreach (var rule in allRules)
        {
            bool isDiscovered = discoveredRules.Contains(rule.ruleName);
            bool isUndiscoveredRoot = !isDiscovered && rule.parent == GameRule.RuleName.None;

            if (isDiscovered || isUndiscoveredRoot)
            {
                availablePool.Add(rule);

                float weight;
                if (isUndiscoveredRoot)
                    weight = UNDISCOVERED_ROOT_WEIGHT;
                else if (isDiscovered && rule.result == RuleResultType.Safe)
                    weight = DISCOVERED_SAFE_WEIGHT;
                else
                    weight = DISCOVERED_WEIGHT;

                weights.Add(weight);
            }
        }

        // ── Pick doors (with retry to guarantee ≥1 Safe or random before meta-rules) ──
        RoomConfig config = null;
        for (int attempt = 0; attempt < MAX_SAFE_GUARANTEE_ATTEMPTS; attempt++)
        {
            config = AssembleDoors(allRules, availablePool, weights);
            if (config.doors.Any(d => d.baseResult == RuleResultType.Safe || d.baseResult == RuleResultType.Random))
                break;
        }

        // Hard fallback: force a random door if RNG was unkind
        if (!config.doors.Any(d => d.baseResult == RuleResultType.Safe || d.baseResult == RuleResultType.Random))
        {
            GameRule randomRule =
                availablePool.FirstOrDefault(r => r.result == RuleResultType.Random)
                ?? allRules.FirstOrDefault(r => r.result == RuleResultType.Random);

            if (randomRule != null)
                config.doors[1] = MakeDoorConfig(randomRule); // replace a pool door, not the wildcard
        }

        // ── Copy base → final before meta-rules touch anything ──
        foreach (var door in config.doors)
            door.finalResult = door.baseResult;

        // ── Apply meta-rules ──
        foreach (var metaRule in _metaRules)
        {
            if (metaRule.IsActive(context))
                metaRule.Apply(config, context);
        }

        // ── Shuffle door positions so wildcard isn't predictable ──
        ShuffleDoors(config);

        _dupeProtectionDoors.Clear();
        foreach (var door in config.doors)
        {
            _dupeProtectionDoors.Add(door.ruleName);
        }

        return config;
    }

    // ── Internal helpers ──

    private RoomConfig AssembleDoors(
        List<GameRule> allRules,
        List<GameRule> availablePool,
        List<float> weights)
    {
        var config = new RoomConfig();
        var used = new HashSet<GameRule.RuleName>(_dupeProtectionDoors);

        // Door 0 — WILDCARD: any rule from the entire set (the "you couldn't know" door)
        var wildcard = allRules[UnityEngine.Random.Range(0, allRules.Count)];
        config.doors[0] = MakeDoorConfig(wildcard);
        used.Add(wildcard.ruleName);

        // Doors 1 & 2 — from the available pool, weighted toward undiscovered roots
        for (int i = 1; i <= 2; i++)
        {
            var pick = WeightedRandomPick(availablePool, weights, used);
            if (pick != null)
            {
                config.doors[i] = MakeDoorConfig(pick);
                used.Add(pick.ruleName);
            }
            else
            {
                // Pool exhausted — fall back to anything unused
                var fallback = allRules.FirstOrDefault(r => !used.Contains(r.ruleName)) ?? allRules[0];
                config.doors[i] = MakeDoorConfig(fallback);
                used.Add(fallback.ruleName);
            }
        }

        return config;
    }

    private DoorConfig MakeDoorConfig(GameRule rule)
    {
        return new DoorConfig
        {
            doorColor = rule.doorColor,
            handleColor = rule.handleColor,
            ruleName = rule.ruleName,
            baseResult = rule.result,
            finalResult = rule.result
        };
    }

    private GameRule WeightedRandomPick(
        List<GameRule> pool,
        List<float> weights,
        HashSet<GameRule.RuleName> exclude)
    {
        float totalWeight = 0f;
        for (int i = 0; i < pool.Count; i++)
        {
            if (!exclude.Contains(pool[i].ruleName))
                totalWeight += weights[i];
        }

        if (totalWeight <= 0f) return null;

        float roll = UnityEngine.Random.Range(0f, totalWeight);
        float cumulative = 0f;

        for (int i = 0; i < pool.Count; i++)
        {
            if (exclude.Contains(pool[i].ruleName)) continue;
            cumulative += weights[i];
            if (roll <= cumulative)
                return pool[i];
        }

        // Shouldn't happen, but safety net
        return pool.LastOrDefault(r => !exclude.Contains(r.ruleName));
    }

    private void ShuffleDoors(RoomConfig config)
    {
        for (int i = config.doors.Length - 1; i > 0; i--)
        {
            int j = UnityEngine.Random.Range(0, i + 1);
            (config.doors[i], config.doors[j]) = (config.doors[j], config.doors[i]);
        }
    }
}