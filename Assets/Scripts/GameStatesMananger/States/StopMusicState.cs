using System;
using System.Collections.Generic;
using UnityEngine;

public class StopMusicState : GameStateBehaviour
{
    private const string LeftHandUpGesture = "left_hand_up";
    private const string CloseHandGesture = "Closed_Fist";

    [SerializeField] private float requiredHoldDuration = 3f;
    [SerializeField] private float decayRate = 0.5f;

    private bool isSubscribed;
    private bool hasLeftHandUp;
    private bool hasLeftHandClosed;
    public override void EnterState()
    {
        base.EnterState();
        Debug.Log("[StopMusicState] Waiting for raised hands...");
        Subscribe();
        ResetProgress();
    }

    public override void ExitState()
    {
        Debug.Log("[StopMusicState] Cleaning up listeners.");
        Unsubscribe();
        HideProgress();
        UpdateMessage(null);
        base.ExitState();
    }

    public override void UpdateState()
    {
        if (!hasLeftHandUp)
        {
            DecayProgress(requiredHoldDuration, decayRate);
            UpdateMessage("Raise your left hand and hold the pose to begin.");
            return;
        }

        if (!hasLeftHandClosed)
        {
            UpdateMessage("Close your left hand to stop the music.");
            HideProgress();
            return;
        }

        holdTime = Mathf.Min(requiredHoldDuration, holdTime + Time.deltaTime);
        float progress = holdTime / requiredHoldDuration;
        UpdateProgress(progress);
        UpdateMessage("Hold steady... feel the silence before the music.");

        if (holdTime >= requiredHoldDuration)
        {
            UpdateMessage("Beautiful! The music is stopped.");
            HideProgress();
            CompleteState();
        }
    }

    private void HandleGesture(string gesture) =>
        hasLeftHandUp = string.Equals(gesture, LeftHandUpGesture, StringComparison.OrdinalIgnoreCase);

    private void HandleHandStates(Dictionary<string, MyListener.HandStateData> handStates)
    {
        if (handStates == null) { hasLeftHandClosed = false; return; }

        bool left = false;
        foreach (var s in handStates.Values)
        {
            if (!string.Equals(s.Gesture, CloseHandGesture, StringComparison.OrdinalIgnoreCase)) continue;
            if (string.Equals(s.Handedness, "left", StringComparison.OrdinalIgnoreCase))
            {
                left = true;
            }
        }
        hasLeftHandClosed = left;
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
