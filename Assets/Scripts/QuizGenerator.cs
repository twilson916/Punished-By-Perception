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

        public float? customTimer; //optional timer value
    }

    // Answers are kept plausible-but-wrong so the player actually has to know.
    private static readonly Template[] Pool =
    {
        new Template {
            question   = "How many punishments trigger an automatic run reset?",
            a = "5", b = "7", c = "3", d = "10",
            correct    = 1,  // B = 7
            onSuccess  = QuizQuestion.SuccessEffect.None,
            onFail     = QuizQuestion.FailEffect.TriplePunishment,
            customTimer = 30f,
        },
        new Template {
            question   = "How many rooms must you pass through to complete the game?",
            a = "15", b = "25", c = "10", d = "20",
            correct    = 3,  // D = 20
            onSuccess  = QuizQuestion.SuccessEffect.None,
            onFail     = QuizQuestion.FailEffect.TriplePunishment,
            customTimer = 30f,
        },
        new Template {
            question   = "Every Nth room contains a shop. What is N?",
            a = "5", b = "7", c = "9", d = "3",
            correct    = 2,  // C = 9
            onSuccess  = QuizQuestion.SuccessEffect.None,
            onFail     = QuizQuestion.FailEffect.TriplePunishment,
            customTimer = 30f,
        },
        new Template {
            question   = "Completing a challenge successfully grants you which benefit?",
            a = "An extra life", b = "A punishment shield", c = "A rulebook hint", d = "Double points",
            correct    = 1,  // B = punishment shield
            onSuccess  = QuizQuestion.SuccessEffect.None,
            onFail     = QuizQuestion.FailEffect.TriplePunishment,
            customTimer = 30f,
        },
        new Template {
            question   = "You reset your run with only 1 active punishment.\nWhat happens at the start of your NEXT run?",
            a = "Nothing — a clean slate awaits you",
            b = "You begin with 2 punishments",
            c = "Your entire save file is wiped",
            d = "You automatically win",
            correct    = 1,  // B — low-punishment restart penalty
            onSuccess  = QuizQuestion.SuccessEffect.None,
            onFail     = QuizQuestion.FailEffect.TriplePunishment,
            customTimer = 30f,
        },
        new Template {
            question   = "How often does a quiz appear in the room sequence?",
            a = "Every 3 rooms", b = "Every 7 rooms", c = "Every 9 rooms", d = "Every 5 rooms",
            correct    = 1,  // B = 7
            onSuccess  = QuizQuestion.SuccessEffect.None,
            onFail     = QuizQuestion.FailEffect.TriplePunishment,
            customTimer = 30f,
        },
        new Template {
            question   = "How often does a challenge appear in the room sequence?",
            a = "Every 3 rooms", b = "Every 5 rooms", c = "Every 7 rooms", d = "Every 9 rooms",
            correct    = 0,  // A = 3
            onSuccess  = QuizQuestion.SuccessEffect.None,
            onFail     = QuizQuestion.FailEffect.TriplePunishment,
            customTimer = 30f,
        },
        new Template {
            question   = "How many unique sound effects happen when you pick up a mug?",
            a = "12", b = "16", c = "14", d = "15",
            correct    = 3,  // D = 15
            onSuccess  = QuizQuestion.SuccessEffect.None,
            onFail     = QuizQuestion.FailEffect.TriplePunishment,
            customTimer = 30f,
        },
        new Template {
            question   = "How many unique sound effects happen when you drop a mug?",
            a = "8", b = "5", c = "6", d = "7",
            correct    = 2,  // C = 5
            onSuccess  = QuizQuestion.SuccessEffect.None,
            onFail     = QuizQuestion.FailEffect.TriplePunishment,
            customTimer = 20f,
        },
        new Template {
            question   = "How many unique sound effects happen when you attempt to open a locked door?",
            a = "7", b = "4", c = "8", d = "10",
            correct    = 0,  // A = 7
            onSuccess  = QuizQuestion.SuccessEffect.None,
            onFail     = QuizQuestion.FailEffect.TriplePunishment,
            customTimer = 30f,
        },
        new Template {
            question   = "What happens when you attempt to take a mug with you to the next room?",
            a = "A special rule is unlocked", b = "You win!", c = "A punishment is removed", d = "None of the above",
            correct    = 3,  // D = ^
            onSuccess  = QuizQuestion.SuccessEffect.None,
            onFail     = QuizQuestion.FailEffect.TriplePunishment,
            customTimer = 30f,
        },
        new Template {
            question   = "How many books were in the previous room (HINT think hard)?",
            a = "30", b = "37", c = "41", d = "36",
            correct    = 3,  // D = 36
            onSuccess  = QuizQuestion.SuccessEffect.None,
            onFail     = QuizQuestion.FailEffect.TriplePunishment,
            customTimer = 10f,
        },
        new Template {
            question   = "What is the color of the door handles behind you when starting a new round?",
            a = "grey", b = "gray", c = "black", d = "white",
            correct    = 1,  // B = gray
            onSuccess  = QuizQuestion.SuccessEffect.None,
            onFail     = QuizQuestion.FailEffect.TriplePunishment,
            customTimer = 10f,
        },
        new Template {
            question   = "What is the color of the trim in the room?",
            a = "milk", b = "off-white", c = "cream", d = "white",
            correct    = 2,  // C = cream
            onSuccess  = QuizQuestion.SuccessEffect.None,
            onFail     = QuizQuestion.FailEffect.TriplePunishment,
            customTimer = 10f,
        },
        new Template {
            question   = "What is the first letter of the alphabet?",
            a = "A) B", b = "B) A", c = "C) Answer A", d = "D) All of the above",
            correct    = 1,  // B = A
            onSuccess  = QuizQuestion.SuccessEffect.None,
            onFail     = QuizQuestion.FailEffect.TriplePunishment,
            customTimer = 15f,
        },
        new Template {
            question   = "If you were to guess randomly, what is the chance of getting this question right?",
            a = "25%", b = "50%", c = "50%", d = "All of the above",
            correct    = 3,  // D, idk
            onSuccess  = QuizQuestion.SuccessEffect.None,
            onFail     = QuizQuestion.FailEffect.TriplePunishment,
            customTimer = 10f,
        },
        new Template {
            question   = "The correct answer is option C.",
            a = "A) The answer", b = "B) No it isn't", c = "C) False", d = "D) C",
            correct    = 1,  // B = No it isn't, because answer C is false
            onSuccess  = QuizQuestion.SuccessEffect.PunishmentShield,
            onFail     = QuizQuestion.FailEffect.TriplePunishment,
            customTimer = 15f,
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
            timeLimit          = t.customTimer ?? -1f,
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
            timeLimit          = t.customTimer ?? -1f,
            successEffect      = QuizQuestion.SuccessEffect.RevealRule,
            failEffect         = QuizQuestion.FailEffect.DoublePunishment,
        };
    }
}
