using System.Collections.Generic;
using UnityEngine;

public abstract class RightHandBeatBaseState : GameStateBehaviour
{
    [Header("Beat Settings")]
    [SerializeField] protected List<Vector2> basePattern = new()
    {
        new Vector2(0.3f, 1.95f),
        new Vector2(0.3f, 1.55f)
    };

    [Header("References")]
    [SerializeField] protected BubbleManager bubbleManager;

    protected bool TryInitializeBeatSequence(int beatCount, out List<Vector2> beats)
    {
        beats = null;

        if (bubbleManager == null)
        {
            Debug.LogError($"[{GetType().Name}] BubbleManager reference is not assigned.");
            return false;
        }

        if (basePattern == null || basePattern.Count == 0)
        {
            Debug.LogError($"[{GetType().Name}] Base pattern is empty.");
            return false;
        }

        if (beatCount <= 0)
        {
            Debug.LogWarning($"[{GetType().Name}] Requested beat count is not positive.");
            return false;
        }

        bubbleManager.ClearAll();
        beats = BuildBeats(beatCount);
        return true;
    }

    protected List<Vector2> BuildBeats(int count)
    {
        var beats = new List<Vector2>(count);
        for (int i = 0; i < count; i++)
        {
            beats.Add(basePattern[i % basePattern.Count]);
        }
        return beats;
    }
}

