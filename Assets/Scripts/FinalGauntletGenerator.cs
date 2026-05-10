using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// Generates the 5-question final gauntlet shown when the player reaches room 20.
// All room config building and meta-rule application is delegated to RoomConfigurator.
// Each question independently rolls a 25% chance of targeting 0 safe doors (answer = None).
//
// Q1 — discovered rules only, no meta rule
// Q2 — all rules,             no meta rule
// Q3 — all rules,             random environmental meta rule
// Q4 — all rules,             random environmental meta rule
// Q5 — discovered rules only, no meta rule, rulebook hidden
public static class FinalGauntletGenerator
{
    public static List<QuizQuestion> Generate(
        List<GameRule> allRules,
        HashSet<GameRule.RuleName> discoveredRuleNames,
        RoomConfigurator configurator)
    {
        var discoveredPool = allRules.Where(r => discoveredRuleNames.Contains(r.ruleName)).ToList();
        if (discoveredPool.Count < 10) discoveredPool = allRules;

        var questions = new List<QuizQuestion>();

        questions.Add(Wrap(
            configurator.BuildGauntletRoom(discoveredPool, allRules, Random.value < 0.25f, false),
            "Which door leads to safety?",
            difficulty: 1, hideRulebook: false));

        questions.Add(Wrap(
            configurator.BuildGauntletRoom(allRules, allRules, Random.value < 0.25f, false),
            "Which door leads to safety?",
            difficulty: 2, hideRulebook: false));

        questions.Add(Wrap(
            configurator.BuildGauntletRoom(allRules, allRules, Random.value < 0.25f, true),
            "Which door leads to safety?",
            difficulty: 3, hideRulebook: false));

        questions.Add(Wrap(
            configurator.BuildGauntletRoom(allRules, allRules, Random.value < 0.25f, true),
            "Which door leads to safety?",
            difficulty: 4, hideRulebook: false));

        questions.Add(Wrap(
            configurator.BuildGauntletRoom(discoveredPool, allRules, Random.value < 0.25f, false),
            "Which door leads to safety?",
            difficulty: 5, hideRulebook: true));

        return questions;
    }

    private static QuizQuestion Wrap(RoomConfig cfg, string text, int difficulty, bool hideRulebook)
    {
        int safeIdx = FindSafe(cfg);
        return new QuizQuestion
        {
            questionType       = QuizQuestion.QuestionType.FinalQuiz,
            difficulty         = difficulty,
            questionText       = text,
            answerA            = "Left door",
            answerB            = "Middle door",
            answerC            = "Right door",
            answerD            = "None",
            correctAnswerIndex = safeIdx >= 0 ? safeIdx : 3,
            timeLimit          = -1f,
            gauntletRoomConfig = cfg,
            hideRulebook       = hideRulebook,
            successEffect      = QuizQuestion.SuccessEffect.None,
            failEffect         = QuizQuestion.FailEffect.None,
        };
    }

    private static int FindSafe(RoomConfig cfg)
    {
        for (int i = 0; i < 3; i++)
            if (cfg.doors[i] != null && cfg.doors[i].finalResult == RuleResultType.Safe) return i;
        return -1;
    }
}
