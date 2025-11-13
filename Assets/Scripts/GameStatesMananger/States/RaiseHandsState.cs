using System;
using System.Collections.Generic;
using UnityEngine;

public class RaiseHandsState : GameStateBehaviour
{
    private const string RaisedHandsGesture = "both_hands_up";
    private const string OpenHandGesture = "open_palm";

    [SerializeField] private float requiredHoldDuration = 3f;
    [SerializeField] private float decayRate = 0.5f;

    private bool isSubscribed;
    private bool hasRaisedHand;
    private bool hasBothHandsOpen;
    public override void EnterState()
    {
        base.EnterState();
        Debug.Log("[RaiseHandsState] Waiting for raised hands...");
        Subscribe();
        ResetProgress();
    }

    public override void ExitState()
    {
        Debug.Log("[RaiseHandsState] Cleaning up listeners.");
        Unsubscribe();
        HideProgress();
        UpdateMessage(null);
        base.ExitState();
    }

    public override void UpdateState()
    {
        if (!hasRaisedHand)
        {
            DecayProgress(requiredHoldDuration, decayRate);
            UpdateMessage("Raise your hands and hold the pose to begin.");
            return;
        }

        if (!hasBothHandsOpen)
        {
            UpdateMessage("Open your palms to face the orchestra.");
            HideProgress();
            return;
        }

        holdTime = Mathf.Min(requiredHoldDuration, holdTime + Time.deltaTime);
        float progress = holdTime / requiredHoldDuration;
        UpdateProgress(progress);
        UpdateMessage("Hold steady... feel the silence before the music.");

        if (holdTime >= requiredHoldDuration)
        {
            UpdateMessage("Beautiful! The orchestra is ready.");
            HideProgress();
            CompleteState();
        }
    }

    private void HandleGesture(string gesture) =>
        hasRaisedHand = string.Equals(gesture, RaisedHandsGesture, StringComparison.OrdinalIgnoreCase);

    private void HandleHandStates(Dictionary<string, MyListener.HandStateData> handStates)
    {
        if (handStates == null) { hasBothHandsOpen = false; return; }

        bool left = false, right = false;
        foreach (var s in handStates.Values)
        {
            if (!string.Equals(s.Gesture, OpenHandGesture, StringComparison.OrdinalIgnoreCase)) continue;
            if (string.Equals(s.Handedness, "left", StringComparison.OrdinalIgnoreCase)) left = true;
            if (string.Equals(s.Handedness, "right", StringComparison.OrdinalIgnoreCase)) right = true;
        }
        hasBothHandsOpen = left && right;
    }

    private void Subscribe()
    {
        if (isSubscribed) return;
        MyListener.OnGestureUpdated += HandleGesture;
        MyListener.OnHandStatesUpdated += HandleHandStates;
        isSubscribed = true;
    }

    private void Unsubscribe()
    {
        if (!isSubscribed) return;
        MyListener.OnGestureUpdated -= HandleGesture;
        MyListener.OnHandStatesUpdated -= HandleHandStates;
        isSubscribed = false;
    }

}
