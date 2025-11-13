using UnityEngine;

[RequireComponent(typeof(Collider))]
public class Bubble : MonoBehaviour
{
    [Header("Visuals")]
    [SerializeField] private Renderer bubbleRenderer;
    [SerializeField] private Color defaultColor = Color.white;
    [SerializeField] private Color hitColor = Color.green;
    [SerializeField] private Color missColor = Color.red;

    private BubbleManager owner;
    private Collider bubbleCollider;
    private Material runtimeMaterial;
    private bool isResolved;

    public int SequenceIndex { get; private set; }

    private void Awake()
    {
        if (bubbleRenderer == null)
        {
            bubbleRenderer = GetComponentInChildren<Renderer>();
        }

        bubbleCollider = GetComponent<Collider>();
    }

    public void Init(BubbleManager owner, int sequenceIndex)
    {
        this.owner = owner;
        SequenceIndex = sequenceIndex;
        EnsureRuntimeMaterial();
        ApplyColor(defaultColor);

        if (bubbleCollider != null)
        {
            bubbleCollider.enabled = true;
        }

        isResolved = false;
    }

    public void ApplyResolutionVisual(BubbleManager.BubbleResolution resolution)
    {
        if (isResolved) return;
        isResolved = true;

        if (bubbleCollider != null)
        {
            bubbleCollider.enabled = false;
        }

        EnsureRuntimeMaterial();

        Color targetColor = resolution == BubbleManager.BubbleResolution.Hit ? hitColor : missColor;
        ApplyColor(targetColor);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Hand")) return; // only hands trigger hits
        owner.ResolveHit(this);
    }

    private void EnsureRuntimeMaterial()
    {
        if (bubbleRenderer == null) return;
        if (runtimeMaterial == null)
        {
            var shared = bubbleRenderer.sharedMaterial;
            runtimeMaterial = shared != null ? new Material(shared) : new Material(Shader.Find("Standard"));
            bubbleRenderer.material = runtimeMaterial;
        }
    }

    private void ApplyColor(Color color)
    {
        if (runtimeMaterial == null) return;
        runtimeMaterial.color = color;
    }
}
