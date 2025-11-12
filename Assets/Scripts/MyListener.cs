using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using UnityEngine;
using System.Threading;
using System.Collections.Generic;
using System.Globalization;

public class MyListener : MonoBehaviour
{
    public static MyListener Instance { get; private set; }

    Thread thread;
    public int connectionPort = 25001;
    TcpListener server;
    TcpClient client;
    bool running;

    // Thread-safe payload data accessible to other scripts
    private PosePayload latestPayload;
    private readonly object payloadLock = new object();
    private volatile bool payloadDirty;

    public static event Action<PosePayload> OnNewPosePayload;

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
    }

    public struct ArmSegmentData
    {
        public string Name;
        public Vector3 Direction;
    }

    public class PosePayload
    {
        public Vector3[] BodyWorld = System.Array.Empty<Vector3>();
        public Vector3[] BodyImage = System.Array.Empty<Vector3>();
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

    void Awake()
    {
        // Singleton pattern
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        Debug.Log($"[MyListener] Starting on port {connectionPort}");
        ThreadStart ts = new ThreadStart(GetData);
        thread = new Thread(ts);
        thread.Start();
    }

    void GetData()
    {
        try
        {
            server = new TcpListener(IPAddress.Any, connectionPort);
            server.Start();
            Debug.Log($"[MyListener] Waiting for client...");
            
            client = server.AcceptTcpClient();
            Debug.Log("[MyListener] Client connected!");

            running = true;
            while (running)
            {
                Connection();
                Thread.Sleep(10);
            }

            server.Stop();
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[MyListener] Error: {e.Message}");
        }
    }

    void Connection()
    {
        try
        {
            NetworkStream nwStream = client.GetStream();
            byte[] buffer = new byte[client.ReceiveBufferSize];
            int bytesRead = nwStream.Read(buffer, 0, client.ReceiveBufferSize);
            string dataReceived = Encoding.UTF8.GetString(buffer, 0, bytesRead);

            if (!string.IsNullOrEmpty(dataReceived))
            {
                PosePayload payload = ParsePayload(dataReceived);
                if (payload != null)
                {
                    MarkPayloadUpdated(payload);

                    if (payload.ArmSegments.Count > 0)
                    {
                        Debug.Log($"[MyListener] Received payload: body={payload.BodyWorld.Length} armSegments={payload.ArmSegments.Count}");
                    }
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[MyListener] Connection error: {e.Message}");
        }
    }

    void Update()
    {
        PosePayload payloadToDispatch = null;

        if (payloadDirty)
        {
            lock (payloadLock)
            {
                if (payloadDirty && latestPayload != null)
                {
                    payloadToDispatch = latestPayload.DeepCopy();
                    payloadDirty = false;
                }
            }
        }

        if (payloadToDispatch != null)
        {
            OnNewPosePayload?.Invoke(payloadToDispatch);
        }
    }

    public Vector3[] GetLatestPositions()
    {
        lock (payloadLock)
        {
            if (latestPayload?.BodyWorld != null && latestPayload.BodyWorld.Length > 0)
            {
                return (Vector3[])latestPayload.BodyWorld.Clone();
            }

            if (latestPayload?.BodyImage != null && latestPayload.BodyImage.Length > 0)
            {
                return (Vector3[])latestPayload.BodyImage.Clone();
            }

            return null;
        }
    }

    public bool HasPositions()
    {
        lock (payloadLock)
        {
            return (latestPayload?.BodyWorld != null && latestPayload.BodyWorld.Length > 0) ||
                   (latestPayload?.BodyImage != null && latestPayload.BodyImage.Length > 0);
        }
    }

    public bool TryGetArmSegments(out Dictionary<string, ArmSegmentData> armSegments)
    {
        lock (payloadLock)
        {
            if (latestPayload != null && latestPayload.ArmSegments.Count > 0)
            {
                armSegments = new Dictionary<string, ArmSegmentData>(latestPayload.ArmSegments);
                return true;
            }
        }

        armSegments = null;
        return false;
    }

    public bool TryGetHandStates(out Dictionary<string, HandStateData> handStates)
    {
        lock (payloadLock)
        {
            if (latestPayload != null && latestPayload.HandStates.Count > 0)
            {
                handStates = new Dictionary<string, HandStateData>(latestPayload.HandStates);
                return true;
            }
        }

        handStates = null;
        return false;
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

    private void MarkPayloadUpdated(PosePayload payload)
    {
        lock (payloadLock)
        {
            latestPayload = payload;
            payloadDirty = true;
        }
    }

    private static PosePayload ParsePayload(string dataString)
    {
        if (string.IsNullOrWhiteSpace(dataString))
        {
            return null;
        }

        PosePayload payload = new PosePayload();
        bool hasData = false;

        string[] tokens = dataString.Split('|');
        int index = 0;

        while (index < tokens.Length)
        {
            string token = tokens[index].Trim();
            if (string.IsNullOrEmpty(token))
            {
                index++;
                continue;
            }

            if (token.StartsWith("body_world:", System.StringComparison.OrdinalIgnoreCase))
            {
                payload.BodyWorld = ParseIndexedVector3List(token.Substring("body_world:".Length));
                hasData = true;
                index++;
                continue;
            }

            if (token.StartsWith("body_image:", System.StringComparison.OrdinalIgnoreCase))
            {
                payload.BodyImage = ParseIndexedVector3List(token.Substring("body_image:".Length));
                hasData = true;
                index++;
                continue;
            }

            if (token.StartsWith("metrics:", System.StringComparison.OrdinalIgnoreCase))
            {
                payload.Metrics = ParseMetrics(token.Substring("metrics:".Length));
                hasData = true;
                index++;
                continue;
            }

            if (token.StartsWith("gesture:", System.StringComparison.OrdinalIgnoreCase))
            {
                payload.Gesture = token.Substring("gesture:".Length);
                hasData = true;
                index++;
                continue;
            }

            if (token.StartsWith("hands:", System.StringComparison.OrdinalIgnoreCase))
            {
                List<string> handParts = new List<string>();
                string first = token.Substring("hands:".Length);
                if (!string.IsNullOrEmpty(first))
                {
                    handParts.Add(first);
                }

                index++;
                while (index < tokens.Length)
                {
                    string peek = tokens[index].Trim();
                    if (IsSectionHeader(peek))
                    {
                        break;
                    }

                    if (!string.IsNullOrEmpty(peek))
                    {
                        handParts.Add(peek);
                    }

                    index++;
                }

                ParseHands(handParts, payload.Hands);
                hasData = true;
                continue;
            }

            if (token.StartsWith("hand_states:", System.StringComparison.OrdinalIgnoreCase))
            {
                List<string> stateParts = new List<string>();
                string first = token.Substring("hand_states:".Length);
                if (!string.IsNullOrEmpty(first))
                {
                    stateParts.Add(first);
                }

                index++;
                while (index < tokens.Length)
                {
                    string peek = tokens[index].Trim();
                    if (IsSectionHeader(peek))
                    {
                        break;
                    }

                    if (!string.IsNullOrEmpty(peek))
                    {
                        stateParts.Add(peek);
                    }

                    index++;
                }

                ParseHandStates(stateParts, payload.HandStates);
                hasData = payload.HandStates.Count > 0 || hasData;
                continue;
            }

            if (token.StartsWith("arm_segments:", System.StringComparison.OrdinalIgnoreCase))
            {
                List<string> segmentParts = new List<string>();
                string first = token.Substring("arm_segments:".Length);
                if (!string.IsNullOrEmpty(first))
                {
                    segmentParts.Add(first);
                }

                index++;
                while (index < tokens.Length)
                {
                    string peek = tokens[index].Trim();
                    if (IsSectionHeader(peek))
                    {
                        break;
                    }

                    if (!string.IsNullOrEmpty(peek))
                    {
                        segmentParts.Add(peek);
                    }

                    index++;
                }

                ParseArmSegments(segmentParts, payload.ArmSegments);
                hasData = payload.ArmSegments.Count > 0 || hasData;
                continue;
            }

            index++;
        }

        return hasData ? payload : null;
    }

    private static bool IsSectionHeader(string token)
    {
        if (string.IsNullOrEmpty(token))
        {
            return false;
        }

        return token.StartsWith("body_world:", System.StringComparison.OrdinalIgnoreCase) ||
               token.StartsWith("body_image:", System.StringComparison.OrdinalIgnoreCase) ||
               token.StartsWith("hands:", System.StringComparison.OrdinalIgnoreCase) ||
               token.StartsWith("hand_states:", System.StringComparison.OrdinalIgnoreCase) ||
               token.StartsWith("metrics:", System.StringComparison.OrdinalIgnoreCase) ||
               token.StartsWith("gesture:", System.StringComparison.OrdinalIgnoreCase) ||
               token.StartsWith("arm_segments:", System.StringComparison.OrdinalIgnoreCase);
    }

    private static Vector3[] ParseIndexedVector3List(string data)
    {
        if (string.IsNullOrWhiteSpace(data))
        {
            return System.Array.Empty<Vector3>();
        }

        string[] entries = data.Split(';');
        Dictionary<int, Vector3> parsed = new Dictionary<int, Vector3>();
        int maxIndex = -1;

        foreach (string entry in entries)
        {
            string trimmed = entry.Trim();
            if (string.IsNullOrEmpty(trimmed))
            {
                continue;
            }

            string[] pair = trimmed.Split(':');
            if (pair.Length != 2)
            {
                continue;
            }

            if (!int.TryParse(pair[0], out int index))
            {
                continue;
            }

            Vector3? vector = ParseVector3(pair[1]);
            if (vector.HasValue)
            {
                parsed[index] = vector.Value;
                if (index > maxIndex)
                {
                    maxIndex = index;
                }
            }
        }

        if (maxIndex < 0)
        {
            return System.Array.Empty<Vector3>();
        }

        Vector3[] result = new Vector3[maxIndex + 1];
        for (int i = 0; i <= maxIndex; i++)
        {
            if (parsed.TryGetValue(i, out Vector3 value))
            {
                result[i] = value;
            }
        }

        return result;
    }

    private static void ParseHands(IEnumerable<string> parts, Dictionary<string, Vector3[]> destination)
    {
        foreach (string part in parts)
        {
            string trimmed = part.Trim();
            if (string.IsNullOrEmpty(trimmed))
            {
                continue;
            }

            string[] pair = trimmed.Split(':');
            if (pair.Length < 2)
            {
                continue;
            }

            string handKey = pair[0];
            string coords = trimmed.Substring(handKey.Length + 1);

            Vector3[] positions = ParseIndexedVector3List(coords);
            if (positions.Length > 0)
            {
                destination[handKey] = positions;
            }
        }
    }

    private static void ParseHandStates(IEnumerable<string> parts, Dictionary<string, HandStateData> destination)
    {
        foreach (string part in parts)
        {
            string trimmed = part.Trim();
            if (string.IsNullOrEmpty(trimmed))
            {
                continue;
            }

            int colonIndex = trimmed.IndexOf(':');
            if (colonIndex <= 0)
            {
                continue;
            }

            string handKey = trimmed.Substring(0, colonIndex);
            string payload = trimmed.Substring(colonIndex + 1);

            string[] properties = payload.Split(',');

            float x = 0f;
            float y = 0f;
            bool hasX = false;
            bool hasY = false;
            string direction = null;
            bool pointing = false;
            string gesture = null;

            foreach (string property in properties)
            {
                string[] kvp = property.Split('=');
                if (kvp.Length != 2)
                {
                    continue;
                }

                string key = kvp[0];
                string value = kvp[1];

                switch (key)
                {
                    case "x":
                        if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out float parsedX))
                        {
                            x = parsedX;
                            hasX = true;
                        }
                        break;
                    case "y":
                        if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out float parsedY))
                        {
                            y = parsedY;
                            hasY = true;
                        }
                        break;
                    case "dir":
                        direction = value;
                        break;
                    case "pointing":
                        pointing = value == "1" ||
                                   value.Equals("true", System.StringComparison.OrdinalIgnoreCase);
                        break;
                    case "gesture":
                        gesture = value;
                        break;
                }
            }

            HandStateData state = new HandStateData
            {
                Handedness = handKey,
                Position = new Vector2(hasX ? x : 0f, hasY ? y : 0f),
                Direction = string.IsNullOrEmpty(direction) ? "none" : direction,
                IsPointing = pointing,
                Gesture = string.IsNullOrEmpty(gesture) ? "none" : gesture,
            };

            destination[handKey] = state;
        }
    }

    private static void ParseArmSegments(IEnumerable<string> parts, Dictionary<string, ArmSegmentData> destination)
    {
        foreach (string part in parts)
        {
            if (TryParseArmSegment(part, out ArmSegmentData segment))
            {
                destination[segment.Name] = segment;
            }
        }
    }

    private static bool TryParseArmSegment(string token, out ArmSegmentData segment)
    {
        segment = default;
        string trimmed = token?.Trim();
        if (string.IsNullOrEmpty(trimmed))
        {
            return false;
        }

        int colonIndex = trimmed.IndexOf(':');
        if (colonIndex <= 0)
        {
            return false;
        }

        string name = trimmed.Substring(0, colonIndex);
        string payload = trimmed.Substring(colonIndex + 1);
        string[] components = payload.Split(';');

        Vector3? direction = null;

        foreach (string component in components)
        {
            string[] kvp = component.Split('=');
            if (kvp.Length != 2)
            {
                continue;
            }

            string key = kvp[0];
            string value = kvp[1];

            switch (key)
            {
                case "dir":
                    direction = ParseVector3(value);
                    break;
            }
        }

        if (direction.HasValue)
        {
            segment = new ArmSegmentData
            {
                Name = name,
                Direction = direction.Value,
            };
            return true;
        }

        return false;
    }

    private static Vector3? ParseVector3(string value)
    {
        string[] comps = value.Split(',');
        if (comps.Length < 3)
        {
            return null;
        }

        if (float.TryParse(comps[0], NumberStyles.Float, CultureInfo.InvariantCulture, out float x) &&
            float.TryParse(comps[1], NumberStyles.Float, CultureInfo.InvariantCulture, out float y) &&
            float.TryParse(comps[2], NumberStyles.Float, CultureInfo.InvariantCulture, out float z))
        {
            return new Vector3(x, y, z);
        }

        return null;
    }

    private static PoseMetrics ParseMetrics(string value)
    {
        PoseMetrics metrics = new PoseMetrics();
        string[] parts = value.Split(',');

        foreach (string part in parts)
        {
            string[] kvp = part.Split('=');
            if (kvp.Length != 2)
            {
                continue;
            }

            if (kvp[0] == "body" && int.TryParse(kvp[1], out int bodyCount))
            {
                metrics.BodyLandmarkCount = bodyCount;
            }
            else if (kvp[0] == "hands" && int.TryParse(kvp[1], out int handCount))
            {
                metrics.HandLandmarkCount = handCount;
            }
        }

        return metrics;
    }

    private void OnApplicationQuit()
    {
        running = false;
        if (thread != null && thread.IsAlive)
            thread.Abort();
    }
}
