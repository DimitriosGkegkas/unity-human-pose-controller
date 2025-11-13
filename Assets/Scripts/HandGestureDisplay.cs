using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

public class HandGestureDisplay : MonoBehaviour
{
    [Header("UI References")]
    public Text titleText;
    public Text detailsText;
    public Image backgroundPanel;

    [Header("Visual Settings")]
    public Color pointingColor = new Color(1f, 0.85f, 0.4f, 0.8f);
    public Color neutralColor = new Color(1f, 1f, 1f, 0.3f);

    private readonly StringBuilder builder = new StringBuilder(256);
    private Dictionary<string, MyListener.HandStateData> latestHandStates;
    private string latestGesture = string.Empty;

    void Start()
    {
        if (titleText == null || detailsText == null || backgroundPanel == null)
        {
            CreateUI();
        }

        UpdateDisplayWaiting();
    }

    void OnEnable()
    {
        MyListener.OnHandStatesUpdated += HandleHandStates;
        MyListener.OnGestureUpdated += HandleGesture;
    }

    void OnDisable()
    {
        MyListener.OnHandStatesUpdated -= HandleHandStates;
        MyListener.OnGestureUpdated -= HandleGesture;
    }

    private void HandleHandStates(Dictionary<string, MyListener.HandStateData> handStates)
    {
        latestHandStates = handStates != null
            ? new Dictionary<string, MyListener.HandStateData>(handStates)
            : null;

        RenderDisplay();
    }

    private void HandleGesture(string gesture)
    {
        latestGesture = gesture ?? string.Empty;
        RenderDisplay();
    }

    private void RenderDisplay()
    {
        var handStates = latestHandStates;
        bool hasHandStates = handStates != null && handStates.Count > 0;

        if (!hasHandStates)
        {
            UpdateDisplayWaiting("Waiting for hand state data...");
            return;
        }

        builder.Clear();
        bool anyPointing = false;

        foreach (var entry in handStates)
        {
            MyListener.HandStateData state = entry.Value;
            string handLabel = NormalizeHandLabel(state.Handedness, entry.Key);

            builder.AppendLine(handLabel);
            builder.AppendLine($"  Gesture: {FormatGesture(state.Gesture)}");
            builder.AppendLine($"  Pointing: {(state.IsPointing ? "yes" : "no")}");
            builder.AppendLine($"  Direction: {state.Direction}");
            builder.AppendLine($"  Position: ({state.Position.x:F1}, {state.Position.y:F1})");
            builder.AppendLine();

            if (state.IsPointing)
            {
                anyPointing = true;
            }
        }

        if (detailsText != null)
        {
            detailsText.text = builder.ToString();
        }

        if (titleText != null)
        {
            titleText.text = string.IsNullOrEmpty(latestGesture) ? "Hand Gestures" : latestGesture;
        }

        if (backgroundPanel != null)
        {
            backgroundPanel.color = anyPointing ? pointingColor : neutralColor;
        }
    }

    private void UpdateDisplayWaiting(string message = "Waiting for gesture data...")
    {
        if (titleText != null)
        {
            titleText.text = string.IsNullOrEmpty(latestGesture) ? "Hand Gestures" : latestGesture;
        }

        if (detailsText != null)
        {
            detailsText.text = message;
        }

        if (backgroundPanel != null)
        {
            backgroundPanel.color = neutralColor;
        }
    }

    private static string NormalizeHandLabel(string handedness, string fallback)
    {
        string label = string.IsNullOrEmpty(handedness) ? fallback : handedness;
        if (string.IsNullOrEmpty(label))
        {
            return "Hand";
        }

        return char.ToUpper(label[0]) + label.Substring(1).ToLower();
    }

    private static string FormatGesture(string gesture)
    {
        if (string.IsNullOrEmpty(gesture) || gesture == "none")
        {
            return "None";
        }

        return gesture.Replace('_', ' ');
    }

    private void CreateUI()
    {
        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            GameObject canvasObject = new GameObject("HandGestureCanvas");
            canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObject.AddComponent<CanvasScaler>();
            canvasObject.AddComponent<GraphicRaycaster>();
        }

        GameObject panelObject = new GameObject("HandGesturePanel");
        panelObject.transform.SetParent(canvas.transform, false);
        backgroundPanel = panelObject.AddComponent<Image>();
        backgroundPanel.color = neutralColor;

        RectTransform panelRect = panelObject.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.02f, 0.55f);
        panelRect.anchorMax = new Vector2(0.3f, 0.9f);
        panelRect.pivot = new Vector2(0f, 1f);
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;

        GameObject titleObject = new GameObject("HandGestureTitle");
        titleObject.transform.SetParent(panelObject.transform, false);
        titleText = titleObject.AddComponent<Text>();
        titleText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        titleText.fontSize = 24;
        titleText.alignment = TextAnchor.UpperLeft;
        titleText.color = Color.white;
        titleText.text = "Hand Gestures";

        RectTransform titleRect = titleObject.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0f, 1f);
        titleRect.anchorMax = new Vector2(1f, 1f);
        titleRect.pivot = new Vector2(0f, 1f);
        titleRect.offsetMin = new Vector2(10f, -40f);
        titleRect.offsetMax = new Vector2(-10f, 0f);

        GameObject detailsObject = new GameObject("HandGestureDetails");
        detailsObject.transform.SetParent(panelObject.transform, false);
        detailsText = detailsObject.AddComponent<Text>();
        detailsText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        detailsText.fontSize = 18;
        detailsText.alignment = TextAnchor.UpperLeft;
        detailsText.color = Color.white;
        detailsText.horizontalOverflow = HorizontalWrapMode.Wrap;
        detailsText.verticalOverflow = VerticalWrapMode.Truncate;
        detailsText.text = string.Empty;

        RectTransform detailsRect = detailsObject.GetComponent<RectTransform>();
        detailsRect.anchorMin = new Vector2(0f, 0f);
        detailsRect.anchorMax = new Vector2(1f, 1f);
        detailsRect.offsetMin = new Vector2(10f, 10f);
        detailsRect.offsetMax = new Vector2(-10f, -50f);
    }
}



