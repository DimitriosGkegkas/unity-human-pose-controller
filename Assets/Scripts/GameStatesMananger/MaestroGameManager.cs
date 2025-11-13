using System.Collections.Generic;
using UnityEngine;

public class MaestroGameManager : MonoBehaviour
{
    public static MaestroGameManager Instance { get; private set; }

    private readonly List<IGameState> states = new List<IGameState>();
    private IGameState currentState;
    private int currentIndex = -1;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("[MaestroGameManager] Duplicate instance detected. Destroying this component.");
            Destroy(gameObject);
            return;
        }

        Instance = this;
        BuildStateList();
    }

    private void Start()
    {
        if (states.Count == 0)
        {
            Debug.LogWarning("[MaestroGameManager] No game states configured.");
            return;
        }

        SetState(0);
    }

    private void Update()
    {
        currentState?.UpdateState();
    }

    public void SetState(int index)
    {
        if (states.Count == 0)
        {
            Debug.LogWarning("[MaestroGameManager] Cannot set state because no states are configured.");
            return;
        }

        int targetIndex = Mathf.Clamp(index, 0, states.Count - 1);

        if (targetIndex == currentIndex && currentState != null)
        {
            return;
        }

        currentState?.ExitState();

        currentIndex = targetIndex;
        currentState = states[currentIndex];

        Debug.Log($"[MaestroGameManager] Transitioning to state {currentState.GetType().Name}");
        currentState.EnterState();
    }

    public void GoToNextState()
    {
        if (states.Count == 0)
        {
            return;
        }

        if (currentIndex >= states.Count - 1)
        {
            currentState?.ExitState();
            currentState = null;
            currentIndex = states.Count;
            Debug.Log("[MaestroGameManager] Reached the final state. No further transitions.");
            return;
        }

        SetState(currentIndex + 1);
    }

    public void GoToPreviousState()
    {
        if (states.Count == 0)
        {
            return;
        }

        int targetIndex = Mathf.Clamp(currentIndex - 1, 0, states.Count - 1);
        SetState(targetIndex);
    }

    private void BuildStateList()
    {
        states.Clear();

        foreach (var behaviour in GetComponentsInChildren<MonoBehaviour>(true))
        {
            if (behaviour == null || behaviour == this || !behaviour.isActiveAndEnabled)
            {
                continue;
            }

            Debug.Log($"[MaestroGameManager] Building state list: {behaviour.name}");
            if (behaviour is IGameState state)
            {
                states.Add(state);
            }
        }
        
    }

    private void OnValidate()
    {
        BuildStateList();
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }
}

