using System.Net;
using System.Net.Sockets;
using System.Text;
using UnityEngine;
using System.Threading;
using System.Collections.Generic;

public class MyListener : MonoBehaviour
{
    public static MyListener Instance { get; private set; }

    Thread thread;
    public int connectionPort = 25001;
    TcpListener server;
    TcpClient client;
    bool running;

    // Thread-safe position data - PUBLIC so other scripts can access
    private Vector3[] latestPositions;
    private readonly object positionLock = new object();

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
                Vector3[] positions = ParseData(dataReceived);
                if (positions.Length > 0)
                {
                    lock (positionLock)
                    {
                        latestPositions = positions;
                    }
                    Debug.Log($"[MyListener] Received {positions.Length} positions");
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[MyListener] Connection error: {e.Message}");
        }
    }

    public static Vector3[] ParseData(string dataString)
    {
        string[] stringArray = dataString.Split(';');
        List<Vector3> positions = new List<Vector3>();

        foreach (string entry in stringArray)
        {
            if (string.IsNullOrWhiteSpace(entry)) continue;

            string[] keyValue = entry.Split(':');
            if (keyValue.Length < 2) continue;

            string[] coords = keyValue[1].Split(',');
            if (coords.Length == 3 &&
                float.TryParse(coords[0], out float x) &&
                float.TryParse(coords[1], out float y) &&
                float.TryParse(coords[2], out float z))
            {
                positions.Add(new Vector3(x, y, z));
            }
        }

        return positions.ToArray();
    }

    // Public method for other scripts to get the latest positions
    public Vector3[] GetLatestPositions()
    {
        lock (positionLock)
        {
            return latestPositions;
        }
    }

    public bool HasPositions()
    {
        lock (positionLock)
        {
            return latestPositions != null && latestPositions.Length > 0;
        }
    }

    private void OnApplicationQuit()
    {
        running = false;
        if (thread != null && thread.IsAlive)
            thread.Abort();
    }
}
