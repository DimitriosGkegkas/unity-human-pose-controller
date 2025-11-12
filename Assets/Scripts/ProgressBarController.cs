using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class ProgressBarController : MonoBehaviour
{
    [Header("Progress Bar UI")]
    [SerializeField] private CanvasGroup progressGroup;
    [SerializeField] private Slider progressSlider;  // ή Image αν προτιμάς
    [SerializeField] private Image progressFill;     // optional alternative to Slider
    [SerializeField] private bool useImageFill = false;

    [Header("Settings")]
    [SerializeField] private float fadeDuration = 0.3f;
    [SerializeField] private float autoHideDelay = 0f; // 0 σημαίνει μην κάνει auto-hide

    private Coroutine fadeRoutine;

    private void Awake()
    {
        HideInstant(progressGroup);
        ResetProgress();
    }

    /// <summary>
    /// Εμφανίζει το progress bar.
    /// </summary>
    public void ShowProgressBar()
    {
        if (fadeRoutine != null) StopCoroutine(fadeRoutine);
        fadeRoutine = StartCoroutine(FadeCanvasGroup(progressGroup, progressGroup.alpha, 1f));
        progressGroup.interactable = true;
        progressGroup.blocksRaycasts = true;

        if (autoHideDelay > 0)
            Invoke(nameof(HideProgressBar), autoHideDelay);
    }

    /// <summary>
    /// Κρύβει το progress bar.
    /// </summary>
    public void HideProgressBar()
    {
        if (fadeRoutine != null) StopCoroutine(fadeRoutine);
        fadeRoutine = StartCoroutine(FadeCanvasGroup(progressGroup, progressGroup.alpha, 0f));
        progressGroup.interactable = false;
        progressGroup.blocksRaycasts = false;
    }

    /// <summary>
    /// Θέτει την τιμή του progress (0.0f - 1.0f).
    /// </summary>
    public void SetProgressBar(float value)
    {
        value = Mathf.Clamp01(value);

        if (useImageFill && progressFill != null)
        {
            progressFill.fillAmount = value;
        }
        else if (progressSlider != null)
        {
            progressSlider.value = value;
        }
    }

    /// <summary>
    /// Επαναφέρει το progress στο 0.
    /// </summary>
    public void ResetProgress()
    {
        SetProgressBar(0f);
    }

    // --- PRIVATE HELPERS ---

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
