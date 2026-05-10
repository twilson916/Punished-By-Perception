using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;

public class QuizUI : MonoBehaviour
{
    [Header("Container")]
    [Tooltip("CanvasGroup on the root quiz panel — used to show/hide without deactivating the GameObject.")]
    public CanvasGroup canvasGroup;

    [Header("VR Interaction")]
    [Tooltip("Drag the child object created by the Meta Interaction SDK (like the RayCanvasInteraction) here.")]
    public GameObject interactionSDKObject;

    [Header("Screens")]
    [Tooltip("Shown before the quiz starts — contains the START button.")]
    public GameObject startScreen;
    [Tooltip("Shown while a question is active.")]
    public GameObject questionScreen;
    [Tooltip("Shown between questions — displays the result meme.")]
    public GameObject resultScreen;
    [Tooltip("Shown after the last FinalQuiz question — displays pass/fail summary.")]
    public GameObject finalScreen;

    [Header("Start Screen")]
    public Button startButton;

    [Header("Question Screen")]
    [Tooltip("The large, centered text used when there is NO image.")]
    public TextMeshProUGUI textOnlyQuestionText;
    [Tooltip("The smaller, top-aligned text used when an image IS present.")]
    public TextMeshProUGUI imageQuestionText;
    public Image questionImage;
    public Button buttonA, buttonB, buttonC, buttonD;
    [Tooltip("Top-right countdown text. Hidden automatically when the question has no timer.")]
    public TextMeshProUGUI timerText;
    [Tooltip("Top-left score text. Only visible during FinalQuiz sessions. Format: correct / asked / total")]
    public TextMeshProUGUI gauntletScoreText;

    [Header("Result Screen")]
    public Image resultImage;
    public Sprite correctMeme;
    public Sprite wrongMeme;
    public Sprite timeoutMeme;
    [Tooltip("Seconds to display the result meme before advancing to the next question.")]
    public float resultDisplayDuration = 2f;

    [Header("Final Screen")]
    public TextMeshProUGUI finalResultsText;
    [Tooltip("Optional button to dismiss the final screen. If unassigned it auto-closes after 4 seconds.")]
    public Button finalDismissButton;
    [Tooltip("Text displayed on the final screen when the player passes the gauntlet.")]
    [TextArea(5, 15)] public string finalPassText = "TODO add final pass text";

    // ── Runtime state ──────────────────────────────────────────────────────────
    private Queue<QuizQuestion> questionQueue = new Queue<QuizQuestion>();
    private bool sessionActive = false;
    private bool answerReceived = false;
    private bool timedOut = false;
    private int pendingAnswerIndex = -1;
    private bool finalDismissed = false;

    // Gauntlet tracking (FinalQuiz questions only)
    private int gauntletCorrect = 0;
    private int gauntletAsked = 0;
    private int gauntletTotal = 0;
    private bool isFinalQuizSession = false;

    // ── Unity lifecycle ────────────────────────────────────────────────────────

    private void Start()
    {
        HideUI();

        startButton?.onClick.AddListener(StartSession);
        buttonA?.onClick.AddListener(() => ReceiveAnswer(0));
        buttonB?.onClick.AddListener(() => ReceiveAnswer(1));
        buttonC?.onClick.AddListener(() => ReceiveAnswer(2));
        buttonD?.onClick.AddListener(() => ReceiveAnswer(3));
        finalDismissButton?.onClick.AddListener(OnFinalDismissed);
    }

    // ── Public API ─────────────────────────────────────────────────────────────

    public void EnqueueQuestion(QuizQuestion q)
    {
        questionQueue.Enqueue(q);

        if (!sessionActive && !IsVisible())
        {
            ShowUI();
            SwitchScreen(startScreen);
        }
    }

    public void ResetForNewRun()
    {
        StopAllCoroutines();
        questionQueue.Clear();
        sessionActive = false;
        answerReceived = false;
        timedOut = false;
        HideUI();
    }

    // ── Session flow ───────────────────────────────────────────────────────────

    private void StartSession()
    {
        gauntletCorrect = 0;
        gauntletAsked = 0;
        gauntletTotal = 0;
        isFinalQuizSession = false;

        foreach (var q in questionQueue)
        {
            if (q.questionType == QuizQuestion.QuestionType.FinalQuiz)
            {
                gauntletTotal++;
                isFinalQuizSession = true;
            }
        }

        StartCoroutine(RunSession());
    }

    private IEnumerator RunSession()
    {
        sessionActive = true;

        while (questionQueue.Count > 0)
        {
            QuizQuestion current = questionQueue.Dequeue();
            yield return StartCoroutine(RunQuestion(current));
        }

        sessionActive = false;
        GameManager.Instance.ShowRulebook(); // restore if gauntlet Q5 hid it
        GameManager.Instance.OnQuizSessionComplete();

        if (isFinalQuizSession)
            yield return StartCoroutine(ShowFinalResults());
        else
            HideUI();
    }

    private IEnumerator RunQuestion(QuizQuestion q)
    {
        answerReceived = false;
        timedOut = false;
        pendingAnswerIndex = -1;

        // Apply gauntlet room config before the question appears so the player
        // sees the doors change color while still standing in the room.
        if (q.gauntletRoomConfig != null)
            GameManager.Instance.ApplyGauntletRoomConfig(q.gauntletRoomConfig);
        if (q.hideRulebook)
            GameManager.Instance.HideRulebook();

        SwitchScreen(questionScreen);

        AudioManager.Play(AudioManager.SoundCategory.QuizMusic);

        if (q.artwork != null)
        {
            // VISUAL CHALLENGE LAYOUT
            if (questionImage != null)
            {
                questionImage.enabled = true;
                questionImage.sprite = q.artwork;
            }

            if (imageQuestionText != null)
            {
                imageQuestionText.gameObject.SetActive(true);
                imageQuestionText.text = q.questionText;
            }

            if (textOnlyQuestionText != null)
            {
                textOnlyQuestionText.gameObject.SetActive(false);
            }
        }
        else
        {
            // TEXT-ONLY QUIZ LAYOUT
            if (questionImage != null)
            {
                questionImage.enabled = false;
            }

            if (textOnlyQuestionText != null)
            {
                textOnlyQuestionText.gameObject.SetActive(true);
                textOnlyQuestionText.text = q.questionText;
            }

            if (imageQuestionText != null)
            {
                imageQuestionText.gameObject.SetActive(false);
            }
        }

        buttonA.GetComponentInChildren<TextMeshProUGUI>().text = q.answerA;
        buttonB.GetComponentInChildren<TextMeshProUGUI>().text = q.answerB;
        buttonC.GetComponentInChildren<TextMeshProUGUI>().text = q.answerC;
        buttonD.GetComponentInChildren<TextMeshProUGUI>().text = q.answerD;

        // Compute effective answer now so the log is accurate and the result can be cached.
        string[] answers = { q.answerA, q.answerB, q.answerC, q.answerD };
        int effectiveCorrectIndex = GameManager.Instance.GetEffectiveCorrectIndex(q);
        Debug.Log($"[QuizUI] ({q.questionType}) Base: [{q.correctAnswerIndex}] {answers[Mathf.Clamp(q.correctAnswerIndex, 0, 3)]} | Effective: [{effectiveCorrectIndex}] {answers[Mathf.Clamp(effectiveCorrectIndex, 0, 3)]}");

        SetButtonsActive(true);

        if (gauntletScoreText != null)
        {
            gauntletScoreText.gameObject.SetActive(isFinalQuizSession);
            UpdateGauntletText();
        }

        Coroutine timerRoutine = null;
        if (GameManager.Instance.difficulty == GameManager.DifficultyMode.Baby) q.timeLimit = -1f;
        if (q.timeLimit > 0f && timerText != null)
        {
            timerText.gameObject.SetActive(true);
            timerRoutine = StartCoroutine(RunTimer(q.timeLimit));
        }
        else if (timerText != null)
        {
            timerText.gameObject.SetActive(false);
        }

        yield return new WaitUntil(() => answerReceived || timedOut);

        AudioManager.StopCategory(AudioManager.SoundCategory.QuizMusic);

        if (timerRoutine != null) StopCoroutine(timerRoutine);
        if (timerText != null) timerText.gameObject.SetActive(false);
        SetButtonsActive(false);

        // effectiveCorrectIndex was pre-computed at display time above
        q.wasAnswered = !timedOut;
        q.wasCorrect = !timedOut && pendingAnswerIndex == effectiveCorrectIndex;

        if (q.questionType == QuizQuestion.QuestionType.FinalQuiz)
        {
            gauntletAsked++;
            if (q.wasCorrect) gauntletCorrect++;
            UpdateGauntletText();
        }

        // Show result meme for the configured delay
        SwitchScreen(resultScreen);
        if (resultImage != null)
            resultImage.sprite = timedOut ? timeoutMeme : (q.wasCorrect ? correctMeme : wrongMeme);

        //Play meme sounds
        if(timedOut)
        {
            AudioManager.Play(AudioManager.SoundCategory.QuizTimeout);
        }
        else if(q.wasCorrect)
        {
            AudioManager.Play(AudioManager.SoundCategory.QuizCorrect);
        }
        else
        {
            AudioManager.Play(AudioManager.SoundCategory.QuizFail);
        }

        // Notify GameManager immediately for non-gauntlet questions
        if (q.questionType != QuizQuestion.QuestionType.FinalQuiz)
            GameManager.Instance.OnQuestionAnswered(q);

        yield return new WaitForSeconds(resultDisplayDuration);

        //Stop meme sounds if still playing
        AudioManager.StopCategory(AudioManager.SoundCategory.QuizTimeout);
        AudioManager.StopCategory(AudioManager.SoundCategory.QuizCorrect);
        AudioManager.StopCategory(AudioManager.SoundCategory.QuizFail);

        // --- MEMORY CLEANUP: Destroy procedural textures ---
        if (q.artwork != null)
        {
            if (q.artwork.texture != null)
            {
                Destroy(q.artwork.texture); // Frees the GPU texture memory
            }
            Destroy(q.artwork); // Frees the Sprite wrapper
            q.artwork = null;   // Null the reference just in case
        }
    }

    private IEnumerator RunTimer(float duration)
    {
        float remaining = duration;
        while (remaining > 0f && !answerReceived)
        {
            if (timerText != null) timerText.text = Mathf.CeilToInt(remaining).ToString();
            remaining -= Time.deltaTime;
            yield return null;
        }

        if (!answerReceived)
        {
            if (timerText != null) timerText.text = "0";
            timedOut = true;
        }
    }

    private IEnumerator ShowFinalResults()
    {
        bool passed = GameManager.Instance.OnFinalQuizComplete(gauntletCorrect, gauntletTotal);

        SwitchScreen(finalScreen);

        if (finalResultsText != null)
        {
            finalResultsText.text = passed
                ? finalPassText
                : $"{gauntletCorrect} / {gauntletTotal} correct\nFAIL";
        }

        if (finalDismissButton != null)
        {
            finalDismissed = false;
            yield return new WaitUntil(() => finalDismissed);
            if(passed)
            {
                RuleManager.Instance.ClearAllRulesAndSave();
                MetaRuleRegistry.Instance.ClearAllMetaRules();
                GameManager.Instance.ResetRun();
            }
            else
            {
                GameManager.Instance.ResetRun();
            }
        }
        else
        {
            yield return new WaitForSeconds(4f);
        }

        HideUI();
    }

    // ── Helpers ────────────────────────────────────────────────────────────────

    private void ReceiveAnswer(int index)
    {
        if (answerReceived || timedOut) return;
        pendingAnswerIndex = index;
        answerReceived = true;
    }

    private void OnFinalDismissed() => finalDismissed = true;

    private void UpdateGauntletText()
    {
        if (gauntletScoreText != null)
            gauntletScoreText.text = $"{gauntletCorrect} / {gauntletAsked} / {gauntletTotal}";
    }

    private void SetButtonsActive(bool active)
    {
        buttonA?.gameObject.SetActive(active);
        buttonB?.gameObject.SetActive(active);
        buttonC?.gameObject.SetActive(active);
        buttonD?.gameObject.SetActive(active);
    }

    private void SwitchScreen(GameObject target)
    {
        if (startScreen != null)    startScreen.SetActive(startScreen == target);
        if (questionScreen != null) questionScreen.SetActive(questionScreen == target);
        if (resultScreen != null)   resultScreen.SetActive(resultScreen == target);
        if (finalScreen != null)    finalScreen.SetActive(finalScreen == target);
    }

    private void ShowUI()
    {
        if (canvasGroup == null) return;
        canvasGroup.alpha = 1f;
        canvasGroup.interactable = true;
        canvasGroup.blocksRaycasts = true;

        // --- Enable the Meta VR Laser interaction ---
        if (interactionSDKObject != null) interactionSDKObject.SetActive(true);
    }

    private void HideUI()
    {
        if (canvasGroup == null) return;
        canvasGroup.alpha = 0f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
        sessionActive = false;

        // --- Disable the Meta VR Laser interaction ---
        if (interactionSDKObject != null) interactionSDKObject.SetActive(false);
    }

    private bool IsVisible() => canvasGroup != null && canvasGroup.alpha > 0f;

    public bool HasPendingSession() => questionQueue.Count > 0 || sessionActive;
}
