using System.Collections;
using UnityEngine;

public class RightHandTwoBeatTutorialState : RightHandBeatBaseState
{
    [SerializeField] private int tutorialBeats = 2;

    private int currentBeatIndex;
    private bool waitingForHit;
    private bool hitReceived;
    private int totalHits;
    private int totalMisses;
    private Coroutine tutorialRoutine;

    public override void EnterState()
    {
        base.EnterState();

        if (!TryInitializeBeatSequence(tutorialBeats, out var beats))
        {
            CompleteState();
            return;
        }

        totalHits = 0;
        totalMisses = 0;
        currentBeatIndex = 0;

        bubbleManager.AddBubbles(beats);
        tutorialRoutine = StartCoroutine(CoTutorialSequence());
    }

    public override void ExitState()
    {
        if (tutorialRoutine != null)
        {
            StopCoroutine(tutorialRoutine);
            tutorialRoutine = null;
        }

        if (bubbleManager != null)
        {
            bubbleManager.ClearAll();
        }

        base.ExitState();
    }

    private IEnumerator CoTutorialSequence()
    {
        for (currentBeatIndex = 0; currentBeatIndex < tutorialBeats; currentBeatIndex++)
        {
            Debug.Log($"[RightHandTwoBeatTutorialState] Starting tutorial beat #{currentBeatIndex + 1}");

            bubbleManager.ProgressToNextBeat();
            waitingForHit = true;
            hitReceived = false;

            yield return StartCoroutine(CoWaitForHit());

            if (hitReceived)
            {
                totalHits++;
                Debug.Log($"[RightHandTwoBeatTutorialState] Beat #{currentBeatIndex + 1} resolved with a hit.");
            }
            else
            {
                totalMisses++;
                Debug.LogWarning($"[RightHandTwoBeatTutorialState] Beat #{currentBeatIndex + 1} ended without a hit.");
            }
        }

        Debug.Log($"[RightHandTwoBeatTutorialState] Tutorial complete! Hits: {totalHits}, Misses: {totalMisses}");
        CompleteState();
    }

    private IEnumerator CoWaitForHit()
    {
        if (bubbleManager == null)
        {
            waitingForHit = false;
            hitReceived = false;
            yield break;
        }

        Bubble targetBubble = bubbleManager.PeekFrontmostBubble();
        if (targetBubble == null)
        {
            Debug.LogWarning("[RightHandTwoBeatTutorialState] No bubble available to wait for.");
            waitingForHit = false;
            hitReceived = false;
            yield break;
        }

        bool resolved = false;

        void OnBubbleResolved(Bubble bubble, BubbleManager.BubbleResolution resolution)
        {
            if (bubble != targetBubble)
            {
                return;
            }

            resolved = true;
            waitingForHit = false;
            hitReceived = resolution == BubbleManager.BubbleResolution.Hit;

            if (hitReceived)
            {
                Debug.Log($"[RightHandTwoBeatTutorialState] Received hit for bubble #{bubble.SequenceIndex}");
            }
            else
            {
                Debug.LogWarning($"[RightHandTwoBeatTutorialState] Bubble #{bubble.SequenceIndex} missed.");
            }
        }

        bubbleManager.BubbleResolved += OnBubbleResolved;

        try
        {
            while (!resolved)
            {
                if (bubbleManager == null || targetBubble == null)
                {
                    Debug.LogWarning("[RightHandTwoBeatTutorialState] Target bubble disappeared before resolution.");
                    waitingForHit = false;
                    hitReceived = false;
                    break;
                }
                yield return null;
            }
        }
        finally
        {
            if (bubbleManager != null)
            {
                bubbleManager.BubbleResolved -= OnBubbleResolved;
            }
        }

        if (!resolved && (bubbleManager == null || targetBubble == null))
        {
            yield break;
        }

        yield return new WaitForSeconds(0.2f);
    }
}

