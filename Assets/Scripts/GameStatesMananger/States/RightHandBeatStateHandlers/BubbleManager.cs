using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

public class BubbleManager : MonoBehaviour
{
    [Header("Setup")]
    [SerializeField] private Bubble bubblePrefab;
    [SerializeField] private Transform spawnRoot;

    [Header("Path Anchors")]
    [SerializeField] private Transform pathStart;
    [SerializeField] private Transform pathEnd;

    [Header("Positioning")]
    [FormerlySerializedAs("startZ")]
    [SerializeField] private float startDistance = 0.5f;
    [FormerlySerializedAs("spacingZ")]
    [SerializeField] private float spacingDistance = 0.5f;
    [FormerlySerializedAs("missZ")]
    [SerializeField] private float missDistance = 0f;

    [Header("Movement")]
    [SerializeField] private float moveSpeed = 1f;
    [FormerlySerializedAs("targetZ")]
    [SerializeField] private float targetDistance = 0.2f;
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
        if (front != null)
        {
            var frame = BuildPathFrame();
            if (frame.DistanceToEnd(front.transform.position) <= targetDistance)
            {
                ResolveBubble(front, BubbleResolution.Miss);
            }
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
        var frame = BuildPathFrame();

        int startIndex = bubbles.Count;
        Bubble lastBubble = GetBackmostBubble();
        float lastDistance = lastBubble != null
            ? frame.DistanceFromStart(lastBubble.transform.position)
            : startDistance - spacingDistance;

        for (int i = 0; i < xyList.Count; i++)
        {
            Vector2 xy = xyList[i];
            lastDistance += spacingDistance;

            var bubble = Instantiate(
                bubblePrefab,
                frame.GetPosition(lastDistance, xy),
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
            var frame = BuildPathFrame();
            bool reachedTarget = false;
            float frameDelta = Mathf.Min(Time.deltaTime, maxProgressStep);
            if (frameDelta <= Mathf.Epsilon)
            {
                yield return null;
                continue;
            }
            MoveBubbles(frameDelta, frame);

            // Check if the first bubble reached the targetZ plane
            var first = GetFrontmostBubble();
            if (first != null && frame.DistanceToEnd(first.transform.position) <= targetDistance)
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
            var frame = BuildPathFrame();
            float frameDelta = Mathf.Min(Time.deltaTime, maxProgressStep);
            if (frameDelta <= Mathf.Epsilon)
            {
                yield return null;
                continue;
            }
            MoveBubbles(frameDelta, frame);
            yield return null;
        }
        progressRoutine = null;
    }

    private void MoveBubbles(float deltaTime, PathFrame frame)
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
            pos += frame.Direction * dz;
            b.transform.position = pos;

            if (frame.DistanceToEnd(pos) <= missDistance)
            {
                Debug.Log($"[BubbleManager] Bubble {b.SequenceIndex} missed (distanceToEnd={frame.DistanceToEnd(pos):F2})");
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
        var frame = BuildPathFrame();
        Bubble front = null;
        float minDistance = float.MaxValue;

        foreach (var b in bubbles)
        {
            if (b == null) continue;
            float distance = frame.DistanceToEnd(b.transform.position);
            if (distance < minDistance)
            {
                minDistance = distance;
                front = b;
            }
        }
        return front;
    }

    private Bubble GetBackmostBubble()
    {
        var frame = BuildPathFrame();
        Bubble back = null;
        float maxDistance = float.MinValue;

        foreach (var b in bubbles)
        {
            if (b == null) continue;
            float distance = frame.DistanceFromStart(b.transform.position);
            if (distance > maxDistance)
            {
                maxDistance = distance;
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
        var frame = BuildPathFrame();
        float planeHalf = 0.25f;

        Vector3 start = frame.Origin;
        Vector3 end = frame.Origin + frame.Direction * Mathf.Max(frame.Length, 0.5f);

        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(start, end);

        Vector3 targetCenter = frame.Origin + frame.Direction * Mathf.Max(frame.Length - targetDistance, 0f);
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(targetCenter - frame.Right * planeHalf, targetCenter + frame.Right * planeHalf);
        Gizmos.DrawLine(targetCenter - frame.Up * planeHalf, targetCenter + frame.Up * planeHalf);

        Vector3 missCenter = frame.Origin + frame.Direction * Mathf.Max(frame.Length - missDistance, 0f);
        Gizmos.color = Color.red;
        Gizmos.DrawLine(missCenter - frame.Right * planeHalf, missCenter + frame.Right * planeHalf);
        Gizmos.DrawLine(missCenter - frame.Up * planeHalf, missCenter + frame.Up * planeHalf);
    }

    private PathFrame BuildPathFrame()
    {
        Vector3 origin = pathStart != null ? pathStart.position : transform.position;
        Vector3 end = pathEnd != null ? pathEnd.position : origin + transform.forward;

        Vector3 direction = end - origin;
        if (direction.sqrMagnitude <= Mathf.Epsilon)
        {
            direction = transform.forward;
            if (direction.sqrMagnitude <= Mathf.Epsilon)
            {
                direction = Vector3.forward;
            }
        }
        direction = direction.normalized;

        float length = Vector3.Dot(end - origin, direction);

        Vector3 referenceUp = pathStart != null ? pathStart.up : transform.up;
        if (referenceUp.sqrMagnitude <= Mathf.Epsilon)
        {
            referenceUp = Vector3.up;
        }

        Vector3 right = Vector3.Cross(referenceUp, direction);
        if (right.sqrMagnitude <= Mathf.Epsilon)
        {
            right = Vector3.Cross(Vector3.up, direction);
            if (right.sqrMagnitude <= Mathf.Epsilon)
            {
                right = Vector3.Cross(Vector3.right, direction);
            }
        }
        right = right.normalized;

        Vector3 up = Vector3.Cross(direction, right).normalized;

        return new PathFrame
        {
            Origin = origin,
            Direction = direction,
            Right = right,
            Up = up,
            Length = length
        };
    }

    private struct PathFrame
    {
        public Vector3 Origin;
        public Vector3 Direction;
        public Vector3 Right;
        public Vector3 Up;
        public float Length;

        public float DistanceFromStart(Vector3 position)
        {
            return Vector3.Dot(position - Origin, Direction);
        }

        public float DistanceToEnd(Vector3 position)
        {
            return Length - DistanceFromStart(position);
        }

        public Vector3 GetPosition(float distance, Vector2 offset)
        {
            return Origin + Direction * distance + Right * offset.x + Up * offset.y;
        }
    }
}
