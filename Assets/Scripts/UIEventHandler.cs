using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class UIEventHandler : MonoBehaviour
{
    [Header("Main Message UI")]
    [SerializeField] private CanvasGroup messageGroup;
    [SerializeField] private TMP_Text messageText;

    [Header("Success UI")]
    [SerializeField] private CanvasGroup successGroup;
    [SerializeField] private TMP_Text successText;

    [Header("Settings")]
    [SerializeField] private float fadeDuration = 0.3f;
    [SerializeField] private float autoHideDelay = 2.5f;

    private Coroutine fadeRoutine;

    private void Awake()
    {
        HideInstant(messageGroup);
        HideInstant(successGroup);
    }

    // --- PUBLIC API ---

    /// <summary>
    /// Shows a general message (like instructions or info).
    /// </summary>
    public void ShowMessage(string message)
    {
        if (messageText != null)
            messageText.text = message;

        ShowGroup(messageGroup);
    }

    /// <summary>
    /// Hides the main message UI.
    /// </summary>
    public void HideMessage()
    {
        HideGroup(messageGroup);
    }

    /// <summary>
    /// Shows a success message in a separate UI element.
    /// </summary>
    public void ShowSuccess(string message = "Success!")
    {
        if (successText != null)
            successText.text = message;

        ShowGroup(successGroup);
        Invoke(nameof(HideSuccess), autoHideDelay);
    }

    /// <summary>
    /// Hides the success UI.
    /// </summary>
    public void HideSuccess()
    {
        HideGroup(successGroup);
    }

    // --- PRIVATE HELPERS ---

    private void ShowGroup(CanvasGroup group)
    {
        if (fadeRoutine != null) StopCoroutine(fadeRoutine);
        fadeRoutine = StartCoroutine(FadeCanvasGroup(group, group.alpha, 1f));
        group.interactable = true;
        group.blocksRaycasts = true;
    }

    private void HideGroup(CanvasGroup group)
    {
        if (fadeRoutine != null) StopCoroutine(fadeRoutine);
        fadeRoutine = StartCoroutine(FadeCanvasGroup(group, group.alpha, 0f));
        group.interactable = false;
        group.blocksRaycasts = false;
    }

    private void HideInstant(CanvasGroup group)
    {
        group.alpha = 0f;
        group.interactable = false;
        group.blocksRaycasts = false;
    }

    private IEnumerator FadeCanvasGroup(CanvasGroup group, float start, float end)
    {
        float time = 0f;
        while (time < fadeDuration)
        {
            group.alpha = Mathf.Lerp(start, end, time / fadeDuration);
            time += Time.deltaTime;
            yield return null;
        }
        group.alpha = end;
    }
}
