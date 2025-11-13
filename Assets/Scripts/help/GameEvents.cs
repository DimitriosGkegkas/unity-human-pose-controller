using UnityEngine;

public class GameEvents : MonoBehaviour
{
    public UIEventHandler uiHandler;

    void Start()
    {
        uiHandler.ShowMessage("Welcome, Maestro!");
    }

    public void OnBeatSuccess()
    {
        uiHandler.ShowSuccess("Perfect Beat!");
    }

    public void OnNextStep()
    {
        uiHandler.ShowMessage("Now, raise your left hand...");
    }
}
