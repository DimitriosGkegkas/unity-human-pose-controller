using System.Collections;
using UnityEngine;

public class RightHandTwoBeatPlayState : RightHandBeatBaseState
{
    [SerializeField] private int beatsToPlay = 18;

    private int totalHits;
    private int totalMisses;
    private Coroutine playRoutine;

    public override void EnterState()
    {
        base.EnterState();

        if (!TryInitializeBeatSequence(beatsToPlay, out var beats))
        {
            CompleteState();
            return;
        }

        totalHits = 0;
        totalMisses = 0;

        bubbleManager.AddBubbles(beats);
        playRoutine = StartCoroutine(CoPlaySequence());
    }

    public override void ExitState()
    {
        if (playRoutine != null)
        {
            StopCoroutine(playRoutine);
            playRoutine = null;
        }

        if (bubbleManager != null)
        {
            bubbleManager.ClearAll();
        }

        base.ExitState();
    }

    private IEnumerator CoPlaySequence()
    {
        void HandleBubbleResolved(Bubble bubble, BubbleManager.BubbleResolution resolution)
        {
            if (resolution == BubbleManager.BubbleResolution.Hit)
            {
                totalHits++;
            }
            else
            {
                totalMisses++;
            }
        }

        bubbleManager.BubbleResolved += HandleBubbleResolved;

        try
        {
            bubbleManager.ProgressAll();

            while (bubbleManager != null && bubbleManager.ActiveCount > 0)
            {
                yield return null;
            }
        }
        finally
        {
            if (bubbleManager != null)
            {
                bubbleManager.BubbleResolved -= HandleBubbleResolved;
            }
        }

        Debug.Log($"[RightHandTwoBeatPlayState] Rhythm complete! Hits: {totalHits}, Misses: {totalMisses}");
        CompleteState();
    }
}

