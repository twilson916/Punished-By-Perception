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
            a = "Line A (Top)", b = "Line B (Middle)", c = "Line C (Bottom)", d = "None of the Above",
            minDiff = 1, visualGen = GenerateMullerLyer
        },
        new Template {
            question = "Which left line (A, B, C) connects straight through the rectangle to a right line?",
            a = "Line A (Top)", b = "Line B (Middle)", c = "Line C (Bottom)", d = "None of the Above",
            minDiff = 2, visualGen = GeneratePoggendorff
        },
        new Template {
            question = "Which of the red vertical lines is actually perfectly straight?",
            a = "Line A (Left)", b = "Line B (Center)", c = "Line C (Right)", d = "None of the Above",
            minDiff = 2, visualGen = GenerateHering
        },
        new Template {
            question = "Which orange center circle is actually the largest?",
            a = "Left circle", b = "Middle circle", c = "Right circle", d = "None of the Above",
            minDiff = 2, visualGen = GenerateEbbinghaus
        },
        new Template {
            question = "Look at the inner grey squares. Which inner square is actually lighter?",
            a = "Left inner square", b = "Right inner square", c = "Neither", d = "I'm blind",
            minDiff = 4, visualGen = GenerateBrightnessContrast
        },
        new Template {
            question = "Which red horizontal bar is actually the longest?",
            a = "Bar A (Top/Far)", b = "Bar B (Middle)", c = "Bar C (Bottom/Close)", d = "None of the Above",
            minDiff = 1, visualGen = GeneratePonzo
        },
        new Template {
            question = "Look closely at the grid intersections. How many dots do you see?",
            a = "0 dots", b = "1 dot", c = "2 dots", d = "3 dots",
            minDiff = 3, visualGen = GenerateHermannGrid
        },
        new Template {
            question = "Which of these curved shapes is the biggest?",
            a = "Shape A (Left)", b = "Shape B (Middle)", c = "Shape C (Right)", d = "None of the Above",
            minDiff = 2, visualGen = GenerateJastrow},
    };

    public static QuizQuestion Generate(int difficulty = 1)
    {
        difficulty = Mathf.Clamp(difficulty, 1, 5);

        var eligible = new List<int>();
        var weights = new List<float>();
        float totalWeight = 0f;

        for (int i = 0; i < Pool.Length; i++)
        {
            if (Pool[i].minDiff <= difficulty)
            {
                eligible.Add(i);

                // Calculate weight: each difficulty level is 1.25x more likely than the one below it
                float weight = Mathf.Pow(1.25f, Pool[i].minDiff - 1);
                weights.Add(weight);
                totalWeight += weight;
            }
        }

        // Fallback just in case, though minDiff goes up to 3 here
        if (eligible.Count == 0)
        {
            for (int i = 0; i < Pool.Length; i++)
            {
                eligible.Add(i);
                weights.Add(1f);
                totalWeight += 1f;
            }
        }

        float randomVal = Random.Range(0f, totalWeight);
        int pick = eligible[0]; // Fallback to the first item just in case

        for (int i = 0; i < eligible.Count; i++)
        {
            randomVal -= weights[i];
            if (randomVal <= 0f)
            {
                pick = eligible[i];
                break;
            }
        }

        var t = Pool[pick];

        float timer = Mathf.Lerp(20f, 10f, (difficulty - 1) / 4f);

        Sprite art = null;
        int actualCorrectIndex = 0;

        // Generate procedural visual illusion
        if (t.visualGen != null)
        {
            t.visualGen(difficulty, out art, out actualCorrectIndex);

            // Hermann Grid gets a shorter time limit
            if (t.visualGen == GenerateHermannGrid)
                timer = Mathf.Lerp(10f, 5f, (difficulty - 1) / 4f);
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

    private static void GenerateJastrow(int diff, out Sprite art, out int correct)
    {
        // -1 = None, 0 = A, 1 = B, 2 = C
        int answer = Random.Range(-1, 3);
        art = IllusionGenerator.Jastrow(diff, answer);
        correct = answer == -1 ? 3 : answer;
    }
}