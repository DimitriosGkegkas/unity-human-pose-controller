using System.Collections.Generic;
using UnityEngine;

public class CueSectionState : GameStateBehaviour
{
    [SerializeField] private float forwardAngleMin = 10f;
    [SerializeField] private float forwardAngleMax = 170f;
    [SerializeField] private float verticalSlackDegrees = 30f;
    [SerializeField] private float requiredHoldDuration = 2.5f;
    [SerializeField] private float progressDecayRate = 1.5f;

    private bool isSubscribed;
    private string palmFacingMessage;
    private bool isFacingTargetGroup;
    private const string TargetGroupLabel = "Left palm facing Group 3";

    public override void EnterState()
    {
        base.EnterState();
        Debug.Log("[CueSectionState] Cueing next section.");
        palmFacingMessage = "Face your left palm toward the violins (Group 3) and hold steady.";
        UpdateMessage(palmFacingMessage);
        ResetProgress();
        isFacingTargetGroup = false;
        Subscribe();
    }

    public override void ExitState()
    {
        Debug.Log("[CueSectionState] Cue section state finished.");
        Unsubscribe();
        UpdateMessage(null);
        HideProgress();
        base.ExitState();
    }

    public override void UpdateState()
    {
        if (isFacingTargetGroup)
        {
            holdTime = Mathf.Min(requiredHoldDuration, holdTime + Time.deltaTime);
            UpdateProgress(holdTime / requiredHoldDuration);

            if (holdTime >= requiredHoldDuration)
            {
                palmFacingMessage = "Great! The cue is locked in.";
                UpdateMessage(palmFacingMessage);
                HideProgress();
                CompleteState();
                return;
            }

            palmFacingMessage = "Hold steady toward the violins...";
        }
        else
        {
            DecayProgress(requiredHoldDuration, progressDecayRate);
        }

        UpdateMessage(palmFacingMessage);
    }

    private void Subscribe()
    {
        if (isSubscribed)
        {
            return;
        }

        MyListener.OnHandStatesUpdated += HandleHandStates;
        isSubscribed = true;
    }

    private void Unsubscribe()
    {
        if (!isSubscribed)
        {
            return;
        }

        MyListener.OnHandStatesUpdated -= HandleHandStates;
        isSubscribed = false;
    }

    private void HandleHandStates(Dictionary<string, MyListener.HandStateData> handStates)
    {
        if (handStates == null || !handStates.TryGetValue("left", out var leftHand))
        {
            SetPalmStatus(false, "Left hand not detected.");
            return;
        }

        if (!leftHand.HasPalmNormal)
        {
            SetPalmStatus(false, "Left palm normal unavailable.");
            return;
        }

        Vector3 normal = leftHand.PalmNormal.normalized;
        if (normal == Vector3.zero)
        {
            SetPalmStatus(false, "Left palm normal invalid.");
            return;
        }

        float verticalAngle = Mathf.Rad2Deg * Mathf.Asin(Mathf.Clamp(normal.y, -1f, 1f));
        if (Mathf.Abs(verticalAngle) > verticalSlackDegrees)
        {
            SetPalmStatus(false, "Left palm tilted too far vertically.");
            return;
        }

        float forwardAngle = Mathf.Rad2Deg * Mathf.Acos(Mathf.Clamp(Vector3.Dot(normal, Vector3.right), -1f, 1f));

        if (forwardAngle < forwardAngleMin || forwardAngle > forwardAngleMax)
        {
            SetPalmStatus(false, "Left palm not facing forward.");
            return;
        }

        string group = DetermineFacingGroup(forwardAngle);
        bool isTarget = string.Equals(group, TargetGroupLabel);
        if (isTarget)
        {
            SetPalmStatus(true, $"Hold steady toward the violins... (≈{forwardAngle:F0}°)");
        }
        else
        {
            SetPalmStatus(false, $"Shift toward the violins. {group} (≈{forwardAngle:F0}°)");
        }
    }

    private string DetermineFacingGroup(float angle)
    {
        if (angle < 50f)
        {
            return "Left palm facing Group 1";
        }

        if (angle < 90f)
        {
            return "Left palm facing Group 2";
        }

        if (angle < 130f)
        {
            return "Left palm facing Group 3";
        }

        return "Left palm facing Group 4";
    }

    private void SetPalmStatus(bool facingTarget, string message)
    {
        isFacingTargetGroup = facingTarget;
        palmFacingMessage = message;
    }

    [ContextMenu("Simulate Cue Complete")]
    public void SimulateCueCompletion()
    {
        Debug.Log("[CueSectionState] Simulated cue completion.");
        CompleteState();
    }
}