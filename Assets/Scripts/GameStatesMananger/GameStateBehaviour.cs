using UnityEngine;

public abstract class GameStateBehaviour : MonoBehaviour, IGameState
{
    private bool hasCompleted;
    protected float holdTime;
    protected string lastMessage;
    protected float lastProgress;
    protected bool progressVisible;

    public virtual void EnterState()
    {
        hasCompleted = false;
        Debug.Log($"[GameState] Enter {GetType().Name}");
    }

    public virtual void ExitState()
    {
        Debug.Log($"[GameState] Exit {GetType().Name}");
    }

    public virtual void UpdateState()
    {
        // Optional: override in child states for per-frame logic
    }

    protected void CompleteState()
    {
        if (hasCompleted)
            return;

        hasCompleted = true;
        Debug.Log($"[GameState] Complete {GetType().Name}");
        MaestroGameManager.Instance?.GoToNextState();
    }

    // Dummy methods to show message in the UI
    // and to show progress bar in the UI

    public void ShowMessage(string message)
    {
        Debug.Log($"[GameState] Show UI Message: {message}");
    }

    public void HideMessage()
    {
        Debug.Log("[GameState] Hide UI Message");
    }

    public void ShowProgressBar(float progress)
    {
        Debug.Log($"[GameState] Show Progress Bar: {progress}");
    }

    public void HideProgressBar()
    {
        Debug.Log("[GameState] Hide Progress Bar");
    }

    protected void ResetProgress()
    {
        holdTime = 0f;
        lastProgress = 0f;
        progressVisible = false;
        UpdateProgress(0f);
    }

    protected void DecayProgress(float requiredHoldDuration, float decayRate)
    {
        if (holdTime <= 0f)
        {
            HideProgress();
            return;
        }

        holdTime = Mathf.Max(0f, holdTime - decayRate * Time.deltaTime);
        UpdateProgress(holdTime / requiredHoldDuration);
    }

    protected void UpdateProgress(float progress)
    {
        progress = Mathf.Clamp01(progress);
        if (progress <= 0f)
        {
            HideProgress();
            return;
        }

        if (!progressVisible || Mathf.Abs(progress - lastProgress) > 0.01f)
        {
            ShowProgressBar(progress);
            progressVisible = true;
            lastProgress = progress;
        }
    }

    protected void HideProgress()
    {
        if (!progressVisible)
            return;

        HideProgressBar();
        progressVisible = false;
        lastProgress = 0f;
    }

    protected void UpdateMessage(string message)
    {
        if (message == lastMessage)
            return;

        if (string.IsNullOrEmpty(message))
            HideMessage();
        else
            ShowMessage(message);

        lastMessage = message;
    }
}
