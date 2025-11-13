using UnityEngine;

public interface IGameState
{
    void EnterState();
    void ExitState();
    void UpdateState();
}

