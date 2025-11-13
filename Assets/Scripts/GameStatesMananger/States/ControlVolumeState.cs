using UnityEngine;

public class ControlVolumeState : GameStateBehaviour
{
    [Tooltip("Simulated target volume level for debugging.")]
    [Range(0f, 1f)]
    [SerializeField]
    private float targetVolume = 0.5f;

    public override void EnterState()
    {
        base.EnterState();
        Debug.Log($"[ControlVolumeState] Prompting player to match volume: {targetVolume:0.00}");
    }

    public override void ExitState()
    {
        Debug.Log("[ControlVolumeState] Exiting volume control state.");
        base.ExitState();
    }

    public override void UpdateState()
    {
        base.UpdateState();
        // Placeholder for volume control logic.
    }

    [ContextMenu("Simulate Volume Matched")]
    public void SimulateVolumeMatched()
    {
        Debug.Log("[ControlVolumeState] Simulated target volume reached.");
        CompleteState();
    }
}

