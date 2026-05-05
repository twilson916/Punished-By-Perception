using UnityEngine;

public class QuizTrigger : MonoBehaviour
{
    [Header("Reference to your Quiz System")]
    public QuizUI quizManager;

    [Header("Optional Settings")]
    public bool triggerOnlyOnce = true;

    // We use a boolean instead of a count to track if THIS specific cube was already used
    private bool hasTriggered = false;

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            // Stop if this specific cube was already hit
            if (triggerOnlyOnce && hasTriggered) return;

            if (quizManager != null)
            {
                // Check if the Quiz Canvas is currently turned on
                if (!quizManager.quizUIContainer.activeInHierarchy)
                {
                    // If it's off, it means the game just started. Start the quiz!
                    quizManager.StartQuiz();
                }
                else
                {
                    // If it's already on (showing a Correct/Wrong screen), advance!
                    quizManager.NextQuestion();
                }

                // Mark this specific cube as used so it doesn't fire again
                hasTriggered = true;
            }
            else
            {
                Debug.LogWarning("QuizTrigger hit, but QuizUI is not assigned in the Inspector!");
            }
        }
    }
}