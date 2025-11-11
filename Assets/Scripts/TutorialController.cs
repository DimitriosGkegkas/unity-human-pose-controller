using System;
using InputControllers;
using UnityEngine;
using UnityEngine.UI;

public class TutorialController : MonoBehaviour
{
    private enum TutorialStep
    {
        EnterSpace,
        RaiseHand,
        Rhythm1,
        Rhythm2,
        Rhythm3,
        Completed
    }

    private readonly string completionInstruction = "Tutorial done. Begin performance.";

    [Header("UI References")]
    [SerializeField] private Text instructionText;
    [SerializeField] private Text feedbackText;

    private TutorialStep currentStep = TutorialStep.EnterSpace;
    private bool tutorialCompleted;
    private string lastInstructionText = string.Empty;
    private string lastFeedbackText = string.Empty;

    private void Awake()
    {
        EnsureUiReferences();
    }

    private void Start()
    {
        EnsureUiReferences();
        SetStep(TutorialStep.EnterSpace);
    }

    private void Update()
    {
        if (tutorialCompleted)
        {
            return;
        }

        if (currentStep == TutorialStep.EnterSpace)
        {
            if (!IsPresentInputController.IsPresent())
            {
                return;
            }

            DisplayFeedback("Great, lets start the performance.");
            SetStep(TutorialStep.RaiseHand);
        }
        else if (currentStep == TutorialStep.RaiseHand)
        {
            if (!RaiseHandInputController.HasRaisedHand())
            {
                return;
            }

            DisplayFeedback("Perfect.");
            SetStep(TutorialStep.Rhythm1);
        }
        else if (currentStep == TutorialStep.Rhythm1)
        {
            // HandleRhythmStep("2/4", "Conduct a 2/4 rhythm: down → up.", "Nice 2/4 control!", TutorialStep.Rhythm34);
            DisplayInstruction("Conduct a 2/4 rhythm: down → up.");
            ControlRhythmInputController.IsControllingRhythm();
            string detected = ControlRhythmInputController.CurrentRhythm;

            if (string.IsNullOrEmpty(detected))
            {
                var historySnapshot = ControlRhythmInputController.GetDirectionHistorySnapshot();
                string historyDescription = historySnapshot.Count == 0
                    ? "No gestures recorded yet."
                    : $"Recent gestures: {string.Join(" → ", historySnapshot)}";
                DisplayFeedback($"No rhythm detected. {historyDescription}");
                return;
            }

            if (string.Equals(detected, "2/4", StringComparison.OrdinalIgnoreCase))
            {
                DisplayFeedback("Nice 2/4 control!");
                // SetStep(nextStep);
                return;
            }
            DisplayFeedback($"Detected rhythm: {detected}");
        }
    }


    private void SetStep(TutorialStep step)
    {
        currentStep = step;

        switch (step)
        {
            case TutorialStep.EnterSpace:
                DisplayInstruction("Enter the performance space.");
                DisplayFeedback("Move into the tracking area to begin.");
                break;
            case TutorialStep.RaiseHand:
                DisplayInstruction("Raise your hand to let the orchestra know you are ready.");
                DisplayFeedback("Waiting for raised hand...");
                break;
            case TutorialStep.Rhythm1:
                DisplayInstruction("Conduct a 2/4 rhythm: down → up.");
                DisplayFeedback("Waiting for 2/4 pattern...");
                break;
            case TutorialStep.Rhythm2:
                DisplayInstruction("Now conduct a 3/4 rhythm: down → right → up.");
                DisplayFeedback("Waiting for 3/4 pattern...");
                break;
            case TutorialStep.Rhythm3:
                DisplayInstruction("Finish with a 4/4 rhythm: down → left → right → up.");
                DisplayFeedback("Waiting for 4/4 pattern...");
                break;
            case TutorialStep.Completed:
                tutorialCompleted = true;
                DisplayInstruction(completionInstruction);
                DisplayFeedback("You are ready to conduct!");
                break;
        }
    }

    private void DisplayInstruction(string message)
    {
        if (message == null)
        {
            message = string.Empty;
        }

        if (string.Equals(lastInstructionText, message, StringComparison.Ordinal))
        {
            return;
        }

        lastInstructionText = message;
        Debug.Log("Instruction: " + message);

        if (instructionText != null)
        {
            instructionText.text = message;
            instructionText.gameObject.SetActive(true);
        }
    }

    private void DisplayFeedback(string message)
    {
        if (message == null)
        {
            message = string.Empty;
        }

        if (string.Equals(lastFeedbackText, message, StringComparison.Ordinal))
        {
            return;
        }

        lastFeedbackText = message;
        Debug.Log("Feedback: " + message);

        if (feedbackText != null)
        {
            feedbackText.text = message;
            feedbackText.gameObject.SetActive(true);
        }
    }

    private void EnsureUiReferences()
    {
        if (instructionText != null && feedbackText != null)
        {
            return;
        }

        var canvasGO = GetOrCreateCanvas();
        var panelGO = GetOrCreatePanel(canvasGO.transform);

        if (instructionText == null)
        {
            instructionText = CreateTextElement("InstructionText", panelGO.transform, TextAnchor.UpperLeft, new Vector2(15f, -15f));
        }

        if (feedbackText == null)
        {
            feedbackText = CreateTextElement("FeedbackText", panelGO.transform, TextAnchor.LowerLeft, new Vector2(15f, 15f));
        }
    }

    private GameObject GetOrCreateCanvas()
    {
        var existingCanvas = GameObject.Find("TutorialCanvas");
        if (existingCanvas != null)
        {
            return existingCanvas;
        }

        var canvasGO = new GameObject("TutorialCanvas");
        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        var canvasScaler = canvasGO.AddComponent<CanvasScaler>();
        canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        canvasScaler.referenceResolution = new Vector2(1920, 1080);

        canvasGO.AddComponent<GraphicRaycaster>();
        return canvasGO;
    }

    private GameObject GetOrCreatePanel(Transform parent)
    {
        var panelGO = parent.Find("TutorialPanel")?.gameObject;
        if (panelGO != null)
        {
            return panelGO;
        }

        panelGO = new GameObject("TutorialPanel");
        panelGO.transform.SetParent(parent, false);

        var rectTransform = panelGO.AddComponent<RectTransform>();
        rectTransform.anchorMin = new Vector2(0.05f, 0.05f);
        rectTransform.anchorMax = new Vector2(0.45f, 0.25f);
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;

        var image = panelGO.AddComponent<Image>();
        image.color = new Color(0f, 0f, 0f, 0.6f);

        return panelGO;
    }

    private Text CreateTextElement(string name, Transform parent, TextAnchor alignment, Vector2 margin)
    {
        var textGO = new GameObject(name);
        textGO.transform.SetParent(parent, false);

        var rectTransform = textGO.AddComponent<RectTransform>();
        rectTransform.anchorMin = new Vector2(0f, 0f);
        rectTransform.anchorMax = new Vector2(1f, 1f);
        rectTransform.offsetMin = new Vector2(margin.x, margin.y);
        rectTransform.offsetMax = new Vector2(-margin.x, -margin.y);

        var text = textGO.AddComponent<Text>();
        var builtinFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (builtinFont == null)
        {
            Debug.LogWarning("TutorialController: Could not load LegacyRuntime.ttf built-in font.", this);
        }
        text.font = builtinFont;
        text.fontSize = 26;
        text.alignment = alignment;
        text.color = Color.white;
        text.text = string.Empty;

        return text;
    }
}
