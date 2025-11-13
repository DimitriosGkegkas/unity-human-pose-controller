using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;

public class MyListener : MonoBehaviour
{
    public static MyListener Instance { get; private set; }

    public static event Action<Dictionary<string, ArmSegmentData>> OnArmSegmentsUpdated;
    public static event Action<string> OnGestureUpdated;
    public static event Action<Dictionary<string, HandStateData>> OnHandStatesUpdated;

    public int connectionPort = 25001;

    private Thread listenerThread;
    private TcpListener server;
    private TcpClient client;
    private volatile bool running;

    private readonly object payloadLock = new object();
    private PosePayload latestPayload;
    private int payloadVersion;

    private readonly object queueLock = new object();
    private readonly Queue<PoseUpdate> pendingUpdates = new Queue<PoseUpdate>();

    private struct PoseUpdate
    {
        public Dictionary<string, ArmSegmentData> ArmSegments;
        public string Gesture;
        public Dictionary<string, HandStateData> HandStates;

        public bool HasData =>
            (ArmSegments != null && ArmSegments.Count > 0) ||
            !string.IsNullOrEmpty(Gesture) ||
            (HandStates != null && HandStates.Count > 0);
    }

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        Debug.Log($"[MyListener] Starting on port {connectionPort}");
        listenerThread = new Thread(GetData) { IsBackground = true };
        listenerThread.Start();
    }

    void Update()
    {
        DispatchQueuedUpdates();
    }

    void OnApplicationQuit()
    {
        Shutdown();
    }

    void OnDestroy()
    {
        Shutdown();
    }

    private void Shutdown()
    {
        running = false;

        try
        {
            client?.Close();
        }
        catch (Exception) { }

        try
        {
            server?.Stop();
        }
        catch (Exception) { }

        if (listenerThread != null && listenerThread.IsAlive)
        {
            listenerThread.Join(100);
        }
    }

    private void GetData()
    {
        try
        {
            server = new TcpListener(IPAddress.Any, connectionPort);
            server.Start();
            Debug.Log("[MyListener] Waiting for client...");

            client = server.AcceptTcpClient();
            Debug.Log("[MyListener] Client connected!");

            running = true;
            while (running)
            {
                Connection();
                Thread.Sleep(10);
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"[MyListener] Error: {e.Message}");
        }
    }

    private void Connection()
    {
        if (client == null)
        {
            return;
        }

        try
        {
            NetworkStream stream = client.GetStream();
            if (!stream.CanRead)
            {
                return;
            }

            byte[] buffer = new byte[client.ReceiveBufferSize];
            int bytesRead = stream.Read(buffer, 0, buffer.Length);

            if (bytesRead <= 0)
            {
                return;
            }

            string dataReceived = Encoding.UTF8.GetString(buffer, 0, bytesRead);
            HandleMessage(dataReceived);
        }
        catch (Exception e)
        {
            Debug.LogError($"[MyListener] Connection error: {e.Message}");
        }
    }

    private void HandleMessage(string data)
    {
        if (!PosePayloadParser.TryParse(data, out var payload))
        {
            return;
        }

        StoreLatestPayload(payload);
        QueueUpdates(payload);
    }

    private void StoreLatestPayload(PosePayload payload)
    {
        lock (payloadLock)
        {
            latestPayload = payload;
            payloadVersion++;
        }
    }

    private void QueueUpdates(PosePayload payload)
    {
        var update = new PoseUpdate();

        if (payload.ArmSegments != null && payload.ArmSegments.Count > 0)
        {
            update.ArmSegments = new Dictionary<string, ArmSegmentData>(payload.ArmSegments);
        }

        if (!string.IsNullOrEmpty(payload.Gesture))
        {
            update.Gesture = payload.Gesture;
        }

        if (payload.HandStates != null && payload.HandStates.Count > 0)
        {
            update.HandStates = new Dictionary<string, HandStateData>(payload.HandStates);
        }

        if (!update.HasData)
        {
            return;
        }

        lock (queueLock)
        {
            pendingUpdates.Enqueue(update);
        }
    }

    private bool TryDequeueUpdate(out PoseUpdate update)
    {
        lock (queueLock)
        {
            if (pendingUpdates.Count > 0)
            {
                update = pendingUpdates.Dequeue();
                return true;
            }
        }

        update = default;
        return false;
    }

    private void DispatchQueuedUpdates()
    {
        while (TryDequeueUpdate(out var update))
        {
            if (update.ArmSegments != null && update.ArmSegments.Count > 0)
            {
                OnArmSegmentsUpdated?.Invoke(update.ArmSegments);
            }

            if (!string.IsNullOrEmpty(update.Gesture))
            {
                OnGestureUpdated?.Invoke(update.Gesture);
            }

            if (update.HandStates != null && update.HandStates.Count > 0)
            {
                OnHandStatesUpdated?.Invoke(update.HandStates);
            }
        }
    }

    public bool TryGetLatestPayload(out PosePayload payload)
    {
        lock (payloadLock)
        {
            if (latestPayload == null)
            {
                payload = null;
                return false;
            }

            payload = latestPayload.DeepCopy();
            return true;
        }
    }

    public bool TryGetLatestPayload(out PosePayload payload, out int version)
    {
        lock (payloadLock)
        {
            version = payloadVersion;
            if (latestPayload == null)
            {
                payload = null;
                return false;
            }

            payload = latestPayload.DeepCopy();
            return true;
        }
    }

    public struct PoseMetrics
    {
        public int BodyLandmarkCount;
        public int HandLandmarkCount;
    }

    public struct HandStateData
    {
        public string Handedness;
        public Vector2 Position;
        public string Direction;
        public bool IsPointing;
        public string Gesture;
        public Vector3 PalmNormal;
        public bool HasPalmNormal;
    }

    public struct ArmSegmentData
    {
        public string Name;
        public Vector3 Direction;
    }

    public class PosePayload
    {
        public Vector3[] BodyWorld = Array.Empty<Vector3>();
        public Vector3[] BodyImage = Array.Empty<Vector3>();
        public Dictionary<string, Vector3[]> Hands = new Dictionary<string, Vector3[]>();
        public PoseMetrics Metrics;
        public string Gesture;
        public Dictionary<string, ArmSegmentData> ArmSegments = new Dictionary<string, ArmSegmentData>();
        public Dictionary<string, HandStateData> HandStates = new Dictionary<string, HandStateData>();

        public PosePayload DeepCopy()
        {
            var copy = new PosePayload
            {
                Gesture = Gesture,
                Metrics = Metrics,
                BodyWorld = BodyWorld != null ? (Vector3[])BodyWorld.Clone() : null,
                BodyImage = BodyImage != null ? (Vector3[])BodyImage.Clone() : null,
            };

            foreach (var kvp in Hands)
            {
                copy.Hands[kvp.Key] = kvp.Value != null ? (Vector3[])kvp.Value.Clone() : null;
            }

            foreach (var kvp in ArmSegments)
            {
                copy.ArmSegments[kvp.Key] = kvp.Value;
            }

            foreach (var kvp in HandStates)
            {
                copy.HandStates[kvp.Key] = kvp.Value;
            }

            return copy;
        }
    }
}

