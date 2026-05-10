using System.Collections.Generic;
using UnityEngine;

// Generates timed visual-illusion challenge questions.
// Difficulty 1-5: higher = more subtle / shorter timer.
// Success gives a punishment shield; failure gives a double punishment.
public static class ChallengeGenerator
{
    private delegate void ArtworkGenerator(int diff, out Sprite artwork, out int correctIndex);

    private struct Template
    {
        public string question;
        public string a, b, c, d;
        public int minDiff;
        public ArtworkGenerator visualGen;
    }

    private static readonly Template[] Pool =
    {
        // ── Procedural Visual Illusions ONLY ─────────────────────────────────
        new Template {
            question = "Which horizontal line is actually the longest?",
            a = "Line A (Top)", b = "Line B (Middle)", c = "Line C (Bottom)", d = "None — all equal",
            minDiff = 1, visualGen = GenerateMullerLyer
        },
        new Template {
            question = "Which left line (A, B, C) connects straight through the rectangle to a right line?",
            a = "Line A (Top)", b = "Line B (Middle)", c = "Line C (Bottom)", d = "None — none connect",
            minDiff = 1, visualGen = GeneratePoggendorff
        },
        new Template {
            question = "Which of the red vertical lines is actually perfectly straight?",
            a = "Line A (Left)", b = "Line B (Center)", c = "Line C (Right)", d = "None — all curved",
            minDiff = 2, visualGen = GenerateHering
        },
        new Template {
            question = "Which orange center circle is actually the largest?",
            a = "Left circle", b = "Middle circle", c = "Right circle", d = "None — all equal",
            minDiff = 2, visualGen = GenerateEbbinghaus
        },
        new Template {
            question = "Look at the inner grey squares. Which inner square is actually lighter?",
            a = "Left inner square", b = "Right inner square", c = "Neither — identical", d = "I'm blind",
            minDiff = 1, visualGen = GenerateBrightnessContrast
        },
        new Template {
            question = "Which red horizontal bar is actually the longest?",
            a = "Bar A (Top/Far)", b = "Bar B (Middle)", c = "Bar C (Bottom/Close)", d = "None — all equal",
            minDiff = 3, visualGen = GeneratePonzo
        },
        new Template {
            question = "Look closely at the grid intersections. How many physical dots are actually drawn?",
            a = "0 dots", b = "1 dot", c = "2 dots", d = "3 dots",
            minDiff = 3, visualGen = GenerateHermannGrid
        }
    };

    public static QuizQuestion Generate(int difficulty = 1)
    {
        difficulty = Mathf.Clamp(difficulty, 1, 5);

        var eligible = new List<int>();
        for (int i = 0; i < Pool.Length; i++)
            if (Pool[i].minDiff <= difficulty) eligible.Add(i);

        // Fallback just in case, though minDiff goes up to 3 here
        if (eligible.Count == 0)
            for (int i = 0; i < Pool.Length; i++) eligible.Add(i);

        int pick = eligible[Random.Range(0, eligible.Count)];
        var t = Pool[pick];

        float timer = Mathf.Lerp(25f, 10f, (difficulty - 1) / 4f); //FIXME choose specific values for timer after playtesting

        Sprite art = null;
        int actualCorrectIndex = 0;

        // Generate procedural visual illusion
        if (t.visualGen != null)
        {
            t.visualGen(difficulty, out art, out actualCorrectIndex);

            // Hermann Grid gets a shorter time limit
            if (t.visualGen == GenerateHermannGrid)
                timer = Mathf.Lerp(15f, 7f, (difficulty - 1) / 4f);
        }

        return new QuizQuestion
        {
            questionType = QuizQuestion.QuestionType.VisualChallenge,
            difficulty = difficulty,
            questionText = t.question,
            artwork = art,
            answerA = t.a,
            answerB = t.b,
            answerC = t.c,
            answerD = t.d,
            correctAnswerIndex = actualCorrectIndex,
            timeLimit = timer,
            successEffect = QuizQuestion.SuccessEffect.PunishmentShield,
            failEffect = QuizQuestion.FailEffect.DoublePunishment,
        };
    }

    // ── Generator Wrappers ───────────────────────────────────────────────────

    private static void GenerateMullerLyer(int diff, out Sprite art, out int correct)
    {
        int answer = Random.Range(-1, 3); // -1 = D (all equal)
        art = IllusionGenerator.MullerLyer(diff, answer);
        correct = answer == -1 ? 3 : answer;
    }

    private static void GeneratePoggendorff(int diff, out Sprite art, out int correct)
    {
        int answer = Random.Range(-1, 3);
        art = IllusionGenerator.Poggendorff(diff, answer);
        correct = answer == -1 ? 3 : answer;
    }

    private static void GenerateHering(int diff, out Sprite art, out int correct)
    {
        int answer = Random.Range(-1, 3);
        art = IllusionGenerator.Hering(diff, answer);
        correct = answer == -1 ? 3 : answer;
    }

    private static void GenerateEbbinghaus(int diff, out Sprite art, out int correct)
    {
        int answer = Random.Range(-1, 3);
        art = IllusionGenerator.Ebbinghaus(diff, answer);
        correct = answer == -1 ? 3 : answer;
    }

    private static void GenerateBrightnessContrast(int diff, out Sprite art, out int correct)
    {
        int answer = Random.Range(-1, 2); // 0=A, 1=B, -1=C(Neither)
        art = IllusionGenerator.BrightnessContrast(diff, answer);
        correct = answer == -1 ? 2 : answer;
    }

    private static void GeneratePonzo(int diff, out Sprite art, out int correct)
    {
        int answer = Random.Range(-1, 3);
        art = IllusionGenerator.Ponzo(diff, answer);
        correct = answer == -1 ? 3 : answer;
    }

    private static void GenerateHermannGrid(int diff, out Sprite art, out int correct)
    {
        int answer = Random.Range(0, 4); // 0, 1, 2, or 3 dots
        art = IllusionGenerator.HermannGrid(diff, answer);
        correct = answer;
    }
}