public class QuizQuestion
{
    public enum QuestionType { VisualChallenge, Quiz, FinalQuiz }
    public enum SuccessEffect { None, PunishmentShield, RevealRule }
    public enum FailEffect { None, Punishment, DoublePunishment, TriplePunishment }

    public QuestionType questionType = QuestionType.Quiz;
    // 1 = easiest, 5 = hardest. Drives illusion subtlety when image generation is implemented.
    public int difficulty = 1;

    public string questionText;
    // Optional image shown alongside the question.
    public UnityEngine.Sprite artwork;

    public string answerA, answerB, answerC, answerD;
    // 0 = A, 1 = B, 2 = C, 3 = D
    public int correctAnswerIndex = 0;

    // Seconds to answer. -1 means no timer.
    public float timeLimit = -1f;

    public SuccessEffect successEffect = SuccessEffect.None;
    public FailEffect failEffect = FailEffect.None;

    // Set at runtime by QuizUI
    public bool wasCorrect;
    public bool wasAnswered; // false means timed out
}
