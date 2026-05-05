using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;

public class QuizUI : MonoBehaviour
{
    // Removed the global timeLimit variable from here!

    [Header("UI Visibility")]
    [Tooltip("Drag the GameObject holding your quiz UI here so it can be hidden/shown.")]
    public GameObject quizUIContainer;

    [Header("Result Screens")]
    public QuizQuestion outofTimeQuestion;
    public QuizQuestion correctAnswer;
    public QuizQuestion wrongAnswer;

    [Header("Audio Settings")]
    [Tooltip("The speaker that plays the quick sound effects.")]
    public AudioSource audioSource;
    [Tooltip("The speaker that plays the looping background music.")]
    public AudioSource musicSource;

    public AudioClip correctSound;
    public AudioClip wrongSound;
    public AudioClip outOfTimeSound;
    [Tooltip("The music that loops while waiting for an answer.")]
    public AudioClip waitingMusic;

    [Header("Question Data List")]
    [Tooltip("Drag all your QuizQuestion ScriptableObjects in here!")]
    public List<QuizQuestion> questionList;

    private int currentQuestionIndex = 0;

    [Header("UI Elements")]
    [Tooltip("The new image panel that sits behind the text.")]
    public RectTransform textBackgroundPanel;
    public TextMeshProUGUI questionText;
    public Image questionImage;

    [Header("Answer Buttons")]
    public Button buttonA;
    public Button buttonB;
    public Button buttonC;
    public Button buttonD;

    [Header("Answer Button Labels")]
    public TextMeshProUGUI buttonAText;
    public TextMeshProUGUI buttonBText;
    public TextMeshProUGUI buttonCText;
    public TextMeshProUGUI buttonDText;

    public int CorrectAnswerIndex { get; private set; }

    private Coroutine questionTimerCoroutine;

    // -------------------------------------------------------

    private void Start()
    {
        if (quizUIContainer != null)
        {
            quizUIContainer.SetActive(false);
        }

        if (buttonA != null) buttonA.onClick.AddListener(() => CheckAnswer(0));
        if (buttonB != null) buttonB.onClick.AddListener(() => CheckAnswer(1));
        if (buttonC != null) buttonC.onClick.AddListener(() => CheckAnswer(2));
        if (buttonD != null) buttonD.onClick.AddListener(() => CheckAnswer(3));
    }

    // -------------------------------------------------------

    public void CheckAnswer(int selectedIndex)
    {
        if (CorrectAnswerIndex == -1) return;

        if (musicSource != null && musicSource.isPlaying)
        {
            musicSource.Stop();
        }

        if (selectedIndex == CorrectAnswerIndex)
        {
            if (audioSource != null && correctSound != null)
                audioSource.PlayOneShot(correctSound);

            if (correctAnswer != null)
                LoadQuestion(correctAnswer);
        }
        else
        {
            if (audioSource != null && wrongSound != null)
                audioSource.PlayOneShot(wrongSound);

            if (wrongAnswer != null)
                LoadQuestion(wrongAnswer);
        }
    }

    // -------------------------------------------------------
    //FIX ME: Currently just restarts once we run out of quiz questions
    public void StartQuiz()
    {
        if (quizUIContainer != null) quizUIContainer.SetActive(true);

        // First, make sure there are actually questions in the list
        if (questionList != null && questionList.Count > 0)
        {
            // If the bookmark has reached the end of the list, loop back to 0!
            if (currentQuestionIndex >= questionList.Count)
            {
                currentQuestionIndex = 0;
            }

            // Load the question
            LoadQuestion(questionList[currentQuestionIndex]);
        }
        else
        {
            Debug.LogWarning("The Question List is totally empty! Please add some questions in the Inspector.");
        }
    }

    public void NextQuestion()
    {
        currentQuestionIndex++;
        StartQuiz();
    }

    public void LoadQuestion(QuizQuestion incoming)
    {
        if (incoming == null) return;

        if (questionTimerCoroutine != null)
        {
            StopCoroutine(questionTimerCoroutine);
            questionTimerCoroutine = null;
        }

        if (questionText != null)
        {
            questionText.text = incoming.questionText;

            if (textBackgroundPanel != null)
            {
                ApplyLayoutMath(textBackgroundPanel, incoming.textPosition);
            }
            else
            {
                ApplyLayoutMath(questionText.GetComponent<RectTransform>(), incoming.textPosition);
            }
        }

        if (questionImage != null)
        {
            if (incoming.artwork == null || incoming.imagePosition == QuizQuestion.LayoutPosition.Hidden)
            {
                questionImage.enabled = false;
            }
            else
            {
                questionImage.enabled = true;
                questionImage.sprite = incoming.artwork;
                ApplyLayoutMath(questionImage.GetComponent<RectTransform>(), incoming.imagePosition);
            }
        }

        if (buttonAText != null) buttonAText.text = incoming.answerA;
        if (buttonBText != null) buttonBText.text = incoming.answerB;
        if (buttonCText != null) buttonCText.text = incoming.answerC;
        if (buttonDText != null) buttonDText.text = incoming.answerD;

        CorrectAnswerIndex = incoming.correctAnswerIndex;

        if (incoming == correctAnswer || incoming == wrongAnswer)
        {
            CorrectAnswerIndex = -1;
            SetButtonsActive(false);
        }
        else
        {
            SetButtonsActive(true);

            if (incoming != outofTimeQuestion)
            {
                // We now pass the specific question's timeLimit into the timer Coroutine!
                questionTimerCoroutine = StartCoroutine(QuestionTimer(incoming.timeLimit));

                if (musicSource != null && waitingMusic != null)
                {
                    musicSource.clip = waitingMusic;
                    musicSource.loop = true;

                    if (!musicSource.isPlaying)
                    {
                        musicSource.Play();
                    }
                }
            }
        }
    }

    // -------------------------------------------------------

    private void SetButtonsActive(bool isActive)
    {
        if (buttonA != null) buttonA.gameObject.SetActive(isActive);
        if (buttonB != null) buttonB.gameObject.SetActive(isActive);
        if (buttonC != null) buttonC.gameObject.SetActive(isActive);
        if (buttonD != null) buttonD.gameObject.SetActive(isActive);
    }

    // -------------------------------------------------------

    // The timer now requires a float value to be passed into it when it starts
    private IEnumerator QuestionTimer(float currentQuestionTimeLimit)
    {
        // It waits for the amount of time dictated by the specific Question Object
        yield return new WaitForSeconds(currentQuestionTimeLimit);

        if (musicSource != null && outOfTimeSound != null)
        {
            musicSource.Stop();
            musicSource.clip = outOfTimeSound;
            musicSource.loop = true;
            musicSource.Play();
        }

        QuizQuestion currentQ = questionList[currentQuestionIndex];

        outofTimeQuestion.answerA = currentQ.answerA;
        outofTimeQuestion.answerB = currentQ.answerB;
        outofTimeQuestion.answerC = currentQ.answerC;
        outofTimeQuestion.answerD = currentQ.answerD;
        outofTimeQuestion.correctAnswerIndex = currentQ.correctAnswerIndex;

        LoadQuestion(outofTimeQuestion);
    }

    // -------------------------------------------------------

    private void ApplyLayoutMath(RectTransform rect, QuizQuestion.LayoutPosition position)
    {
        if (position == QuizQuestion.LayoutPosition.Hidden)
        {
            rect.gameObject.SetActive(false);
            return;
        }

        rect.gameObject.SetActive(true);

        switch (position)
        {
            case QuizQuestion.LayoutPosition.TopHalf:
                rect.anchorMin = new Vector2(0f, 0.5f);
                rect.anchorMax = new Vector2(1f, 1f); break;
            case QuizQuestion.LayoutPosition.BottomHalf:
                rect.anchorMin = new Vector2(0f, 0f);
                rect.anchorMax = new Vector2(1f, 0.5f); break;
            case QuizQuestion.LayoutPosition.TopQuarter:
                rect.anchorMin = new Vector2(0f, 0.75f);
                rect.anchorMax = new Vector2(1f, 1f); break;
            case QuizQuestion.LayoutPosition.BottomQuarter:
                rect.anchorMin = new Vector2(0f, 0f);
                rect.anchorMax = new Vector2(1f, 0.25f); break;
            case QuizQuestion.LayoutPosition.TopThreeQuarters:
                rect.anchorMin = new Vector2(0f, 0.25f);
                rect.anchorMax = new Vector2(1f, 1f); break;
            case QuizQuestion.LayoutPosition.BottomThreeQuarters:
                rect.anchorMin = new Vector2(0f, 0f);
                rect.anchorMax = new Vector2(1f, 0.75f); break;
            case QuizQuestion.LayoutPosition.MiddleHalf:
                rect.anchorMin = new Vector2(0f, 0.25f);
                rect.anchorMax = new Vector2(1f, 0.75f); break;
            case QuizQuestion.LayoutPosition.FullScreen:
                rect.anchorMin = new Vector2(0f, 0f);
                rect.anchorMax = new Vector2(1f, 1f); break;
        }

        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }
}