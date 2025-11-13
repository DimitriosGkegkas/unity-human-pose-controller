using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class PoseDetector : MonoBehaviour
{
    [Header("MediaPipe Pose Landmark Indices")]
    [Tooltip("MediaPipe pose landmark indices")]
    public int leftShoulderIndex = 11;
    public int rightShoulderIndex = 12;
    public int leftElbowIndex = 13;
    public int rightElbowIndex = 14;
    public int leftWristIndex = 15;
    public int rightWristIndex = 16;
    public int leftHipIndex = 23;
    public int rightHipIndex = 24;
    public int noseIndex = 0;

    [Header("T-Pose Detection")]
    [Tooltip("How horizontal the arms need to be (0-1, closer to 1 = more horizontal)")]
    [Range(0f, 1f)]
    public float tPoseHorizontalThreshold = 0.8f;
    
    [Tooltip("How extended the arms need to be (0-1, closer to 1 = more extended)")]
    [Range(0f, 1f)]
    public float tPoseExtensionThreshold = 0.7f;

    [Header("Raised Hand Detection")]
    [Tooltip("How high above shoulder the wrist needs to be")]
    public float handRaisedHeightThreshold = 0.1f;
    
    [Tooltip("How low below shoulder the wrist needs to be")]
    public float handDownHeightThreshold = -0.1f;

    [Header("Detection Smoothing")]
    [Tooltip("How many consecutive frames a pose must be detected to trigger")]
    public int requiredConsecutiveFrames = 10;

    [Header("UI References")]
    public Text feedbackText;
    public Image feedbackPanel;
    public Color tPoseColor = Color.green;
    public Color leftHandRaisedColor = Color.blue;
    public Color neutralColor = new Color(1f, 1f, 1f, 0.3f);

    // Detected pose states
    public enum DetectedPose
    {
        None,
        TPose,
        LeftHandRaised
    }

    private DetectedPose currentPose = DetectedPose.None;
    private Dictionary<DetectedPose, int> poseFrameCounter = new Dictionary<DetectedPose, int>();
    private DetectedPose confirmedPose = DetectedPose.None;
    private int lastPayloadVersion = -1;

    void Start()
    {
        Debug.Log("[PoseDetector] Starting pose detection");
        
        // Initialize counters
        poseFrameCounter[DetectedPose.None] = 0;
        poseFrameCounter[DetectedPose.TPose] = 0;
        poseFrameCounter[DetectedPose.LeftHandRaised] = 0;

        // Create UI if not assigned
        if (feedbackText == null || feedbackPanel == null)
        {
            CreateUI();
        }

        UpdateUI(DetectedPose.None);
    }

    private Vector3[] latestPositions;

    void Update()
    {
        if (MyListener.Instance == null)
        {
            return;
        }

        if (!MyListener.Instance.TryGetLatestPayload(out var payload, out int version))
        {
            return;
        }

        if (payload == null || version == lastPayloadVersion)
        {
            return;
        }

        lastPayloadVersion = version;
        ProcessPayload(payload);
    }

    private void ProcessPayload(MyListener.PosePayload payload)
    {
        Vector3[] positions = payload.BodyWorld != null && payload.BodyWorld.Length > 0
            ? payload.BodyWorld
            : payload.BodyImage;

        if (positions == null || positions.Length <= rightWristIndex)
        {
            return;
        }

        latestPositions = (Vector3[])positions.Clone();
        DetectPoses(latestPositions);
    }

    void DetectPoses(Vector3[] positions)
    {
        // Check all poses
        bool isTpose = CheckTPose(positions);
        bool isLeftHandRaised = CheckLeftHandRaised(positions);

        // Determine current pose (priority: specific poses over none)
        DetectedPose detectedThisFrame = DetectedPose.None;
        
        if (isTpose)
        {
            detectedThisFrame = DetectedPose.TPose;
        }
        else if (isLeftHandRaised)
        {
            detectedThisFrame = DetectedPose.LeftHandRaised;
        }

        // Update frame counter for smoothing
        if (detectedThisFrame == currentPose)
        {
            poseFrameCounter[detectedThisFrame]++;
        }
        else
        {
            // Reset all counters when pose changes
            foreach (var key in new List<DetectedPose>(poseFrameCounter.Keys))
            {
                poseFrameCounter[key] = 0;
            }
            currentPose = detectedThisFrame;
            poseFrameCounter[currentPose] = 1;
        }

        // Confirm pose if it's been detected for enough consecutive frames
        if (poseFrameCounter[currentPose] >= requiredConsecutiveFrames)
        {
            if (confirmedPose != currentPose)
            {
                confirmedPose = currentPose;
                OnPoseConfirmed(confirmedPose);
            }
        }
    }

    bool CheckTPose(Vector3[] positions)
    {
        // Get key positions
        Vector3 leftShoulder = positions[leftShoulderIndex];
        Vector3 rightShoulder = positions[rightShoulderIndex];
        Vector3 leftWrist = positions[leftWristIndex];
        Vector3 rightWrist = positions[rightWristIndex];
        Vector3 leftElbow = positions[leftElbowIndex];
        Vector3 rightElbow = positions[rightElbowIndex];
        Vector3 nose = positions[noseIndex];

        // Calculate shoulder width and center
        Vector3 shoulderCenter = (leftShoulder + rightShoulder) / 2f;
        float shoulderWidth = Vector3.Distance(leftShoulder, rightShoulder);

        // Check if arms are extended horizontally
        // Left arm
        float leftArmExtension = Vector3.Distance(leftShoulder, leftWrist) / shoulderWidth;
        float leftArmHorizontalness = Mathf.Abs(Vector3.Dot(
            (leftWrist - leftShoulder).normalized,
            Vector3.right
        ));

        // Right arm
        float rightArmExtension = Vector3.Distance(rightShoulder, rightWrist) / shoulderWidth;
        float rightArmHorizontalness = Mathf.Abs(Vector3.Dot(
            (rightWrist - rightShoulder).normalized,
            Vector3.left
        ));

        // Check if both arms meet T-pose criteria
        bool leftArmGood = leftArmExtension > tPoseExtensionThreshold && 
                          leftArmHorizontalness > tPoseHorizontalThreshold &&
                          leftWrist.y > shoulderCenter.y - 0.2f && // Not too low
                          leftWrist.y < shoulderCenter.y + 0.2f;   // Not too high

        bool rightArmGood = rightArmExtension > tPoseExtensionThreshold && 
                           rightArmHorizontalness > tPoseHorizontalThreshold &&
                           rightWrist.y > shoulderCenter.y - 0.2f &&
                           rightWrist.y < shoulderCenter.y + 0.2f;

        return leftArmGood && rightArmGood;
    }

    bool CheckLeftHandRaised(Vector3[] positions)
    {
        // Get key positions
        Vector3 leftShoulder = positions[leftShoulderIndex];
        Vector3 rightShoulder = positions[rightShoulderIndex];
        Vector3 leftWrist = positions[leftWristIndex];
        Vector3 rightWrist = positions[rightWristIndex];

        // Check if left wrist is significantly higher than left shoulder
        bool leftHandRaised = (leftWrist.y - leftShoulder.y) > handRaisedHeightThreshold;

        // Check if right wrist is at or below right shoulder
        bool rightHandDown = (rightWrist.y - rightShoulder.y) < handDownHeightThreshold;

        return leftHandRaised && rightHandDown;
    }

    void OnPoseConfirmed(DetectedPose pose)
    {
        Debug.Log($"[PoseDetector] Pose confirmed: {pose}");
        UpdateUI(pose);
    }

    void UpdateUI(DetectedPose pose)
    {
        if (feedbackText == null || feedbackPanel == null) return;

        switch (pose)
        {
            case DetectedPose.TPose:
                feedbackText.text = "T-POSE DETECTED! ✓";
                feedbackPanel.color = tPoseColor;
                break;
            
            case DetectedPose.LeftHandRaised:
                feedbackText.text = "LEFT HAND RAISED! ✓";
                feedbackPanel.color = leftHandRaisedColor;
                break;
            
            case DetectedPose.None:
            default:
                feedbackText.text = "No pose detected";
                feedbackPanel.color = neutralColor;
                break;
        }
    }

    void CreateUI()
    {
        Debug.Log("[PoseDetector] Creating UI elements");

        // Create Canvas if it doesn't exist
        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            GameObject canvasObj = new GameObject("PoseDetectorCanvas");
            canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObj.AddComponent<CanvasScaler>();
            canvasObj.AddComponent<GraphicRaycaster>();
        }

        // Create panel
        GameObject panelObj = new GameObject("FeedbackPanel");
        panelObj.transform.SetParent(canvas.transform, false);
        feedbackPanel = panelObj.AddComponent<Image>();
        feedbackPanel.color = neutralColor;

        RectTransform panelRect = panelObj.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 0.9f);
        panelRect.anchorMax = new Vector2(0.5f, 0.9f);
        panelRect.pivot = new Vector2(0.5f, 0.5f);
        panelRect.sizeDelta = new Vector2(400, 80);

        // Create text
        GameObject textObj = new GameObject("FeedbackText");
        textObj.transform.SetParent(panelObj.transform, false);
        feedbackText = textObj.AddComponent<Text>();
        feedbackText.text = "No pose detected";
        feedbackText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        feedbackText.fontSize = 24;
        feedbackText.alignment = TextAnchor.MiddleCenter;
        feedbackText.color = Color.white;

        RectTransform textRect = textObj.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(10, 10);
        textRect.offsetMax = new Vector2(-10, -10);

        Debug.Log("[PoseDetector] UI created successfully");
    }

    // Public methods for external access
    public DetectedPose GetCurrentPose()
    {
        return confirmedPose;
    }

    public bool IsPoseDetected(DetectedPose pose)
    {
        return confirmedPose == pose;
    }

    void OnGUI()
    {
        // Debug information
        if (latestPositions != null && latestPositions.Length > 24)
        {
            GUILayout.BeginArea(new Rect(10, 10, 300, 200));
            GUILayout.Label($"Current Pose: {currentPose}");
            GUILayout.Label($"Confirmed Pose: {confirmedPose}");
            GUILayout.Label($"Frame Count: {poseFrameCounter[currentPose]}/{requiredConsecutiveFrames}");
            GUILayout.Label($"Total Landmarks: {latestPositions.Length}");
            GUILayout.EndArea();
        }
    }
}

