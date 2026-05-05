using UnityEngine;

[CreateAssetMenu(fileName = "New Question", menuName = "Quiz System/Question File")]
public class QuizQuestion : ScriptableObject
{
    [Header("Text Content")]
    [TextArea(3, 5)]
    public string questionText = "Enter your question here...";
    public LayoutPosition textPosition = LayoutPosition.TopQuarter;

    [Header("Image Content")]
    public Sprite artwork;
    public LayoutPosition imagePosition = LayoutPosition.MiddleHalf;

    [Header("Answers")]
    public string answerA = "Option A";
    public string answerB = "Option B";
    public string answerC = "Option C";
    public string answerD = "Option D";

    [Tooltip("0 = A, 1 = B, 2 = C, 3 = D")]
    [Range(0, 3)]
    public int correctAnswerIndex = 0;

    [Tooltip("How long the player has to answer before the out of time question loads.")]
    public float timeLimit = 10f;

    public enum LayoutPosition
    {
        TopHalf, BottomHalf, TopQuarter, BottomQuarter,
        TopThreeQuarters, BottomThreeQuarters, MiddleHalf, FullScreen, Hidden
    }

    //Maybe use something like this for difficulty?
    private enum questionType
    {
        Hard,
        Medium,
        Easy
    }

}