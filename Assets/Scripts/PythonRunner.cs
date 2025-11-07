using System.IO;
using UnityEngine;
using Process = System.Diagnostics.Process;
using ProcessStartInfo = System.Diagnostics.ProcessStartInfo;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class PythonRunner : MonoBehaviour
{
    [SerializeField] private bool runOnStart = true;
    [SerializeField] private string pythonBinary = "/usr/bin/python3";
    [SerializeField, HideInInspector] private string scriptAssetPath = string.Empty;

#if UNITY_EDITOR
    [SerializeField] private DefaultAsset scriptAsset;
    private void OnValidate() => scriptAssetPath = scriptAsset ? AssetDatabase.GetAssetPath(scriptAsset) : string.Empty;
#endif

    private Process process;

    private void Start()
    {
        if (runOnStart)
        {
            RunPython();
        }
    }

    private void OnDestroy() => TryKillProcess();

    [ContextMenu("Run Python Script")]
    public void RunPython()
    {
        var scriptPath = ResolveScriptPath(scriptAssetPath);

        TryKillProcess();

        var startInfo = new ProcessStartInfo
        {
            FileName = pythonBinary,
            Arguments = scriptPath,
            WorkingDirectory = Path.GetDirectoryName(scriptPath) ?? Application.dataPath,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };

        try
        {
            process = Process.Start(startInfo);
            if (process == null)
            {
                Debug.LogError("Python process failed to start.");
                return;
            }

            process.EnableRaisingEvents = true;
            process.OutputDataReceived += (_, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                {
                    Debug.Log($"[Python stdout] {e.Data}");
                }
            };
            process.ErrorDataReceived += (_, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                {
                    Debug.LogWarning($"[Python stderr] {e.Data}");
                }
            };
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Failed to start python script: {ex.Message}");
        }
    }

    [ContextMenu("Stop Python Script")]
    public void StopPython() => TryKillProcess();

    private void TryKillProcess()
    {
        if (process == null)
        {
            return;
        }

        try
        {
            if (!process.HasExited)
            {
                process.Kill();
                process.WaitForExit();
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning($"Failed to terminate python process: {ex.Message}");
        }
        finally
        {
            process.Dispose();
            process = null;
        }
    }

    private static string ResolveScriptPath(string assetPath)
    {
        if (string.IsNullOrEmpty(assetPath))
        {
            Debug.LogError("No python script assigned to PythonRunner.");
            throw new System.ArgumentException("Script path cannot be empty.");
        }

        if (Path.IsPathRooted(assetPath))
        {
            return assetPath;
        }

        string projectRoot = Path.GetDirectoryName(Application.dataPath) ?? Application.dataPath;
        return Path.GetFullPath(Path.Combine(projectRoot, assetPath));
    }
}

