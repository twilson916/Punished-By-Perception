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
    public TextMeshProUGUI questionText;
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

        SwitchScreen(questionScreen);

        questionText.text = q.questionText;

        if (questionImage != null)
        {
            questionImage.enabled = q.artwork != null;
            if (q.artwork != null) questionImage.sprite = q.artwork;
        }

        buttonA.GetComponentInChildren<TextMeshProUGUI>().text = q.answerA;
        buttonB.GetComponentInChildren<TextMeshProUGUI>().text = q.answerB;
        buttonC.GetComponentInChildren<TextMeshProUGUI>().text = q.answerC;
        buttonD.GetComponentInChildren<TextMeshProUGUI>().text = q.answerD;
        SetButtonsActive(true);

        if (gauntletScoreText != null)
        {
            gauntletScoreText.gameObject.SetActive(isFinalQuizSession);
            UpdateGauntletText();
        }

        Coroutine timerRoutine = null;
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

        if (timerRoutine != null) StopCoroutine(timerRoutine);
        if (timerText != null) timerText.gameObject.SetActive(false);
        SetButtonsActive(false);

        // Resolve outcome — GameManager handles the Every3rdAnswerLeft override internally
        int effectiveCorrectIndex = GameManager.Instance.GetEffectiveCorrectIndex(q);

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

        // Notify GameManager immediately for non-gauntlet questions
        if (q.questionType != QuizQuestion.QuestionType.FinalQuiz)
            GameManager.Instance.OnQuestionAnswered(q);

        yield return new WaitForSeconds(resultDisplayDuration);
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
    }

    private void HideUI()
    {
        if (canvasGroup == null) return;
        canvasGroup.alpha = 0f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
        sessionActive = false;
    }

    private bool IsVisible() => canvasGroup != null && canvasGroup.alpha > 0f;
}
