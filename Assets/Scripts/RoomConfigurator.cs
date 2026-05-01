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

    // Struct informing how the environment is to be setup or changed
    public EnvironmentState environment = new EnvironmentState();

    public class EnvironmentState
    {
        public bool plantsSwapped = false;
        public bool leftChairOut = false;
        public bool rightChairOut = false;
        public bool ceilingLightChanged = false;
    }
}

[System.Serializable]
public class ActiveMetaRule
{
    public string ruleId;
    public string description;
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
    bool isEnvironmental { get; }
    bool IsActive(MetaRuleContext context);
    void Apply(RoomConfig config, MetaRuleContext context);
}

// ─────────────────────────────────────────────
//  ENVIRONMENTAL META-RULES (only one fires per room)
// ─────────────────────────────────────────────

// If the plants on the table are swapped:
// Safe → Punishment, Challenge → Random
public class PlantsSwappedRule : IMetaRule
{
    public string Id => "plants_swapped";
    public string Description => "If the plants on the table are swapped all safe rooms are punishments and all challenges are random";
    public bool isEnvironmental => true;
    private const float _probEnvActive = 0.33f; //probability that meta rule is active

    public bool IsActive(MetaRuleContext ctx) => true; // always eligible, Apply decides randomly

    public void Apply(RoomConfig config, MetaRuleContext ctx)
    {
        bool swapped = UnityEngine.Random.value < _probEnvActive;
        config.environment.plantsSwapped = swapped;

        if (swapped)
        {
            foreach (var door in config.doors)
            {
                door.finalResult = door.finalResult switch
                {
                    RuleResultType.Safe => RuleResultType.Punishment,
                    RuleResultType.Challenge => RuleResultType.Random,
                    _ => door.finalResult
                };
            }
            config.activeMetaRules.Add(new ActiveMetaRule { ruleId = Id, description = Description });
        }
    }
}

// If the left chair is pulled out slightly:
// Safe → Punishment, Random → Safe
public class LeftChairOutRule : IMetaRule
{
    public string Id => "left_chair_out";
    public string Description => "If the left chair is pulled out slightly then all safe rooms are punishments and all random doors are safe";
    public bool isEnvironmental => true;
    private const float _probEnvActive = 0.33f; //probability that meta rule is active

    public bool IsActive(MetaRuleContext ctx) => true;

    public void Apply(RoomConfig config, MetaRuleContext ctx)
    {
        bool pulledOut = UnityEngine.Random.value < _probEnvActive;
        config.environment.leftChairOut = pulledOut;

        if (pulledOut)
        {
            foreach (var door in config.doors)
            {
                door.finalResult = door.finalResult switch
                {
                    RuleResultType.Safe => RuleResultType.Punishment,
                    RuleResultType.Random => RuleResultType.Safe,
                    _ => door.finalResult
                };
            }
            config.activeMetaRules.Add(new ActiveMetaRule { ruleId = Id, description = Description });
        }
    }
}

// If the ceiling light is a different color:
// Safe → Punishment, Random → Punishment, Challenge → Safe, Punishment → Safe
public class CeilingLightColorRule : IMetaRule
{
    public string Id => "ceiling_light_changed";
    public string Description => "If the ceiling light is a different color then all safe and random rooms are punishments and all other rooms are safe";
    public bool isEnvironmental => true;
    private const float _probEnvActive = 0.33f; //probability that meta rule is active

    public bool IsActive(MetaRuleContext ctx) => true;

    public void Apply(RoomConfig config, MetaRuleContext ctx)
    {
        bool changed = UnityEngine.Random.value < _probEnvActive;
        config.environment.ceilingLightChanged = changed;

        if (changed)
        {
            foreach (var door in config.doors)
            {
                door.finalResult = door.finalResult switch
                {
                    RuleResultType.Safe => RuleResultType.Punishment,
                    RuleResultType.Random => RuleResultType.Punishment,
                    RuleResultType.Challenge => RuleResultType.Safe,
                    RuleResultType.Punishment => RuleResultType.Safe,
                    _ => door.finalResult
                };
            }
            config.activeMetaRules.Add(new ActiveMetaRule { ruleId = Id, description = Description });
        }
    }
}

// If the right chair is pulled out slightly...
// the last guy must've forgotten to put it back.
// Does absolutely nothing. Pure troll.
public class RightChairOutRule : IMetaRule
{
    public string Id => "right_chair_out";
    public string Description => "If the right chair is pulled out slightly then the last guy must've forgotten to put it back";
    public bool isEnvironmental => true;
    private const float _probEnvActive = 0.33f; //probability that meta rule is active

    public bool IsActive(MetaRuleContext ctx) => true;

    public void Apply(RoomConfig config, MetaRuleContext ctx)
    {
        bool pulledOut = UnityEngine.Random.value < _probEnvActive;
        config.environment.rightChairOut = pulledOut;

        // Does nothing to door results. The player will spend ages
        // trying to figure out what this does. It does nothing.
        if (pulledOut)
        {
            config.activeMetaRules.Add(new ActiveMetaRule { ruleId = Id, description = Description });
        }
    }
}

// ─────────────────────────────────────────────
//  NON-ENVIRONMENTAL META-RULES (can all fire independently)
// ─────────────────────────────────────────────

// Every 6th room: Safe ↔ Punishment swap.
public class InvertEvery6thRoomRule : IMetaRule
{
    public string Id => "invert_every_6th";
    public string Description => "Every 6th room has inverted logic, ie all safe rooms are punishments and vice versa";
    public bool isEnvironmental => false;

    public bool IsActive(MetaRuleContext ctx) => ctx.totalRoomsVisited > 1 && ctx.totalRoomsVisited % 6 == 0;

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

    private int _metaRuleMinRoom = 5;

    private HashSet<GameRule.RuleName> _dupeProtectionDoors = new HashSet<GameRule.RuleName>();

    private readonly List<IMetaRule> _metaRules = new List<IMetaRule>();

    // Register a meta-rule. Call during setup before any rooms are built.
    public void RegisterMetaRule(IMetaRule rule) => _metaRules.Add(rule);

    public void ResetState()
    {
        _dupeProtectionDoors.Clear();
    }

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
        if (context.totalRoomsVisited >= _metaRuleMinRoom)
        {
            // First: non-environmental rules (can all fire)
            foreach (var metaRule in _metaRules.Where(r => !r.isEnvironmental))
            {
                if (metaRule.IsActive(context))
                    metaRule.Apply(config, context);
            }

            // Second: environmental rules (only one fires)
            var eligibleEnv = _metaRules
                .Where(r => r.isEnvironmental && r.IsActive(context))
                .ToList();

            if (eligibleEnv.Count > 0)
            {
                var chosen = eligibleEnv[UnityEngine.Random.Range(0, eligibleEnv.Count)];
                chosen.Apply(config, context);
            }
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

        // Doors 1 & 2 — from the available pool, weighted
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