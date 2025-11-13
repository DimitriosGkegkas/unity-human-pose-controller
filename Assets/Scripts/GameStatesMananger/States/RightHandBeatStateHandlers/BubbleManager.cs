using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BubbleManager : MonoBehaviour
{
    [Header("Setup")]
    [SerializeField] private Bubble bubblePrefab;
    [SerializeField] private Transform spawnRoot;

    [Header("Positioning")]
    [SerializeField] private float startZ = 0.5f;
    [SerializeField] private float spacingZ = 0.5f;
    [SerializeField] private float missZ = 0f;

    [Header("Movement")]
    [SerializeField] private float moveSpeed = 1f;
    [SerializeField] private float targetZ = 0.2f;
    [SerializeField, Range(0.001f, 0.1f)] private float maxProgressStep = 1f / 30f;

    [Header("Resolution Effects")]
    [SerializeField, Min(0f)] private float resolveDestroyDelay = 1.5f;

    public enum BubbleResolution
    {
        Hit,
        Miss
    }

    public event Action<Bubble, BubbleResolution> BubbleResolved;

    private readonly List<Bubble> bubbles = new();
    private Coroutine progressRoutine;

    public int ActiveCount => bubbles.Count;

    // --------------------------- MENU COMMANDS --------------------------- //

    [ContextMenu("Add Sample Bubbles")]
    public void AddSampleBubbles()
    {
        var positions = new List<Vector2>
        {
            new Vector2(0.3f, 0.9f),
            new Vector2(0.3f, 0.5f)
        };
        AddBubbles(positions);
    }

    [ContextMenu("Progress To Next Beat")]
    public void ProgressToNextBeat()
    {
        StopProgressIfRunning();
        var front = GetFrontmostBubble();
        if (front != null && front.transform.position.z <= targetZ)
        {
            ResolveBubble(front, BubbleResolution.Miss);
        }
        progressRoutine = StartCoroutine(CoProgressToNextBeat());
    }

    [ContextMenu("Progress All (Flush)")]
    public void ProgressAll()
    {
        StopProgressIfRunning();
        progressRoutine = StartCoroutine(CoProgressAll());
    }

    [ContextMenu("Remove First Bubble (Debug)")]
    public void RemoveFirstBubbleDebug()
    {
        if (bubbles.Count > 0)
        {
            ResolveBubble(bubbles[0], BubbleResolution.Miss);
        }
        else
        {
            Debug.Log("[BubbleManager] No bubbles to remove.");
        }
    }

    [ContextMenu("Clear All Bubbles")]
    public void ClearAll()
    {
        StopProgressIfRunning();
        foreach (var b in bubbles)
            if (b != null) Destroy(b.gameObject);
        bubbles.Clear();
    }

    // --------------------------- CORE LOGIC --------------------------- //

    public void AddBubbles(IList<Vector2> xyList)
    {
        int startIndex = bubbles.Count;
        Bubble lastBubble = GetBackmostBubble();
        float lastZ = lastBubble != null ? lastBubble.transform.position.z : startZ - spacingZ;

        for (int i = 0; i < xyList.Count; i++)
        {
            Vector2 xy = xyList[i];
            lastZ += spacingZ;
            float z = lastZ;

            var bubble = Instantiate(
                bubblePrefab,
                new Vector3(xy.x, xy.y, z),
                Quaternion.identity,
                spawnRoot
            );

            bubble.Init(this, startIndex + i);
            bubbles.Add(bubble);
        }
    }

    public void RemoveBubble(Bubble bubble)
    {
        ResolveBubble(bubble, BubbleResolution.Miss);
    }

    private void StopProgressIfRunning()
    {
        if (progressRoutine != null)
        {
            StopCoroutine(progressRoutine);
            progressRoutine = null;
        }
    }

    // --------------------------- PROGRESS LOGIC --------------------------- //

    private IEnumerator CoProgressToNextBeat()
    {
        if (bubbles.Count == 0) yield break;

        while (true)
        {
            bool reachedTarget = false;
            float frameDelta = Mathf.Min(Time.deltaTime, maxProgressStep);
            if (frameDelta <= Mathf.Epsilon)
            {
                yield return null;
                continue;
            }
            MoveBubbles(frameDelta);

            // Check if the first bubble reached the targetZ plane
            var first = GetFrontmostBubble();
            if (first != null && first.transform.position.z <= targetZ)
                reachedTarget = true;

            if (reachedTarget)
                break;

            yield return null;
        }
        progressRoutine = null;
    }

    private IEnumerator CoProgressAll()
    {
        while (bubbles.Count > 0)
        {
            float frameDelta = Mathf.Min(Time.deltaTime, maxProgressStep);
            if (frameDelta <= Mathf.Epsilon)
            {
                yield return null;
                continue;
            }
            MoveBubbles(frameDelta);
            yield return null;
        }
        progressRoutine = null;
    }

    private void MoveBubbles(float deltaTime)
    {
        float dz = moveSpeed * deltaTime;

        for (int i = bubbles.Count - 1; i >= 0; i--)
        {
            var b = bubbles[i];
            if (b == null)
            {
                bubbles.RemoveAt(i);
                continue;
            }

            Vector3 pos = b.transform.position;
            pos.z -= dz;
            b.transform.position = pos;

            if (pos.z <= missZ)
            {
                Debug.Log($"[BubbleManager] Bubble {b.SequenceIndex} missed (z={pos.z:F2})");
                ResolveBubble(b, BubbleResolution.Miss);
            }
        }
    }

    public Bubble PeekFrontmostBubble()
    {
        return GetFrontmostBubble();
    }

    private Bubble GetFrontmostBubble()
    {
        Bubble front = null;
        float minZ = float.MaxValue;

        foreach (var b in bubbles)
        {
            if (b == null) continue;
            float z = b.transform.position.z;
            if (z < minZ)
            {
                minZ = z;
                front = b;
            }
        }
        return front;
    }

    private Bubble GetBackmostBubble()
    {
        Bubble back = null;
        float maxZ = float.MinValue;

        foreach (var b in bubbles)
        {
            if (b == null) continue;
            float z = b.transform.position.z;
            if (z > maxZ)
            {
                maxZ = z;
                back = b;
            }
        }
        return back;
    }

    // --------------------------- EXTERNAL CALLS --------------------------- //

    public void ResolveHit(Bubble bubble)
    {
        ResolveBubble(bubble, BubbleResolution.Hit);
    }

    private void ResolveBubble(Bubble bubble, BubbleResolution resolution, bool notify = true)
    {
        if (bubble == null) return;
        if (!bubbles.Contains(bubble)) return;

        Debug.Log($"[BubbleManager] Resolving bubble #{bubble.SequenceIndex} ({resolution})");
        bubbles.Remove(bubble);

        if (notify)
        {
            BubbleResolved?.Invoke(bubble, resolution);
        }

        bubble.ApplyResolutionVisual(resolution);
        Destroy(bubble.gameObject, resolveDestroyDelay);
    }

    // --------------------------- GIZMOS --------------------------- //

    private void OnDrawGizmosSelected()
    {
        Vector3 p = transform.position;
        Gizmos.color = Color.red;
        Gizmos.DrawLine(p + Vector3.forward * missZ + Vector3.left, p + Vector3.forward * missZ + Vector3.right);

        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(p + Vector3.forward * targetZ + Vector3.left, p + Vector3.forward * targetZ + Vector3.right);
    }
}
