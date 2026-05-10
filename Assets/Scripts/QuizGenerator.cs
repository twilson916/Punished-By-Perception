using UnityEngine;

// Generates meta/trivia quiz questions about the game's own rules.
// Called every 7th room and from the Gold Rulebook shop item.
public static class QuizGenerator
{
    private struct Template
    {
        public string question;
        public string a, b, c, d;
        public int correct;
        public QuizQuestion.SuccessEffect onSuccess;
        public QuizQuestion.FailEffect   onFail;
    }

    // Answers are kept plausible-but-wrong so the player actually has to know.
    private static readonly Template[] Pool =
    {
        new Template {
            question   = "How many punishments trigger an automatic run reset?",
            a = "5", b = "7", c = "3", d = "10",
            correct    = 1,  // B = 7
            onSuccess  = QuizQuestion.SuccessEffect.PunishmentShield,
            onFail     = QuizQuestion.FailEffect.Punishment,
        },
        new Template {
            question   = "How many rooms must you pass through to complete the game?",
            a = "15", b = "25", c = "10", d = "20",
            correct    = 3,  // D = 20
            onSuccess  = QuizQuestion.SuccessEffect.RevealRule,
            onFail     = QuizQuestion.FailEffect.DoublePunishment,
        },
        new Template {
            question   = "Every Nth room contains a shop. What is N?",
            a = "5", b = "7", c = "9", d = "3",
            correct    = 2,  // C = 9
            onSuccess  = QuizQuestion.SuccessEffect.PunishmentShield,
            onFail     = QuizQuestion.FailEffect.Punishment,
        },
        new Template {
            question   = "Completing a challenge successfully grants you which benefit?",
            a = "An extra life", b = "A punishment shield", c = "A rulebook hint", d = "Double points",
            correct    = 1,  // B = punishment shield
            onSuccess  = QuizQuestion.SuccessEffect.RevealRule,
            onFail     = QuizQuestion.FailEffect.Punishment,
        },
        new Template {
            question   = "The right chair is slightly pulled out from the table.\nWhat rule does this trigger?",
            a = "All safe doors become punishments",
            b = "All random doors become safe",
            c = "Rules are inverted for this room",
            d = "Nothing — the last guy forgot to push it in",
            correct    = 3,  // D = nothing (the troll meta-rule)
            onSuccess  = QuizQuestion.SuccessEffect.PunishmentShield,
            onFail     = QuizQuestion.FailEffect.DoublePunishment,
        },
        new Template {
            question   = "Every 3rd question during any quiz or challenge session,\nyou MUST select a specific answer regardless of the actual correct answer.\nWhich answer must you always select on those questions?",
            a = "Right (C)", b = "Middle (B)", c = "Left (A)", d = "Last option (D)",
            correct    = 2,  // C = Left / index 0 = A
            onSuccess  = QuizQuestion.SuccessEffect.RevealRule,
            onFail     = QuizQuestion.FailEffect.DoublePunishment,
        },
        new Template {
            question   = "You reset your run with only 1 active punishment.\nWhat happens at the start of your NEXT run?",
            a = "Nothing — a clean slate awaits you",
            b = "You begin with 2 punishments",
            c = "Your entire save file is wiped",
            d = "You automatically win",
            correct    = 1,  // B — low-punishment restart penalty
            onSuccess  = QuizQuestion.SuccessEffect.PunishmentShield,
            onFail     = QuizQuestion.FailEffect.Punishment,
        },
        new Template {
            question   = "How often does a quiz appear in the room sequence?",
            a = "Every 3 rooms", b = "Every 7 rooms", c = "Every 9 rooms", d = "Every 5 rooms",
            correct    = 1,  // B = 7
            onSuccess  = QuizQuestion.SuccessEffect.RevealRule,
            onFail     = QuizQuestion.FailEffect.Punishment,
        },
        new Template {
            question   = "How often does a challenge appear in the room sequence?",
            a = "Every 3 rooms", b = "Every 5 rooms", c = "Every 7 rooms", d = "Every 9 rooms",
            correct    = 0,  // A = 3
            onSuccess  = QuizQuestion.SuccessEffect.PunishmentShield,
            onFail     = QuizQuestion.FailEffect.Punishment,
        },
        new Template {
            question   = "You have completed 20 rooms and nothing seems to happen.\nWhat do you do?",
            a = "Keep going — there must be more rooms",
            b = "The door behind you leads to the exit",
            c = "There is no exit — this is a loop forever",
            d = "Buy something from the shop",
            correct    = 1,  // B — MinusOne room has the exit
            onSuccess  = QuizQuestion.SuccessEffect.RevealRule,
            onFail     = QuizQuestion.FailEffect.DoublePunishment,
        },
    };

    // Returns a random quiz question from the pool.
    public static QuizQuestion Generate()
    {
        var t = Pool[Random.Range(0, Pool.Length)];

        return new QuizQuestion
        {
            questionType       = QuizQuestion.QuestionType.Quiz,
            difficulty         = 1,
            questionText       = t.question,
            answerA            = t.a,
            answerB            = t.b,
            answerC            = t.c,
            answerD            = t.d,
            correctAnswerIndex = t.correct,
            timeLimit          = -1f, // no timer on text quizzes
            successEffect      = t.onSuccess,
            failEffect         = t.onFail,
        };
    }

    // Special version for the Gold Rulebook shop item — always rewards RevealRule.
    public static QuizQuestion GenerateRevealRule()
    {
        var t = Pool[Random.Range(0, Pool.Length)];

        return new QuizQuestion
        {
            questionType       = QuizQuestion.QuestionType.Quiz,
            difficulty         = 1,
            questionText       = t.question,
            answerA            = t.a,
            answerB            = t.b,
            answerC            = t.c,
            answerD            = t.d,
            correctAnswerIndex = t.correct,
            timeLimit          = -1f,
            successEffect      = QuizQuestion.SuccessEffect.RevealRule,
            failEffect         = QuizQuestion.FailEffect.DoublePunishment,
        };
    }
}
