using UnityEngine;

public class FollowTheRythm : MonoBehaviour
{
    public MeshRenderer[] meshes;

    public Color defaultColor = Color.white;
    public Color occupiedColor = Color.green;
    public Color missedColor = Color.red;

    public float hitTime = 1.5f;         // time allowed to hit
    public float hitShowDuration = 1f;   // show green time
    public float missShowDuration = 1f;  // show red time

    public float waitingAlpha = 0.2f;

    private int currentIndex = 0;
    private float timer = 0f;
    private bool awaitingHit = true;   // currently waiting for hit input
    private bool resolved = false;     // whether current tile finished hit/miss stage

    void Start()
    {
        // Make material instances unique
        for (int i = 0; i < meshes.Length; i++)
            meshes[i].materials = DuplicateMaterials(meshes[i].materials);

        // Set initial states
        for (int i = 0; i < meshes.Length; i++)
            SetWaiting(meshes[i]);

        SetNextUp(meshes[currentIndex]);
    }

    Material[] DuplicateMaterials(Material[] source)
    {
        Material[] result = new Material[source.Length];
        for (int i = 0; i < source.Length; i++)
            result[i] = new Material(source[i]);
        return result;
    }

    void Update()
    {
        timer += Time.deltaTime;

        if (awaitingHit && timer >= hitTime)
        {
            // Missed
            awaitingHit = false;
            resolved = true;
            SetMissed(meshes[currentIndex]);
            timer = 0f;
        }
        else if (!awaitingHit && resolved)
        {
            // After showing hit/miss long enough, move on
            if (timer >= (IsHit(meshes[currentIndex]) ? hitShowDuration : missShowDuration))
            {
                AdvanceToNext();
            }
        }
    }

    public void TileHit(RhythmTile tile)
    {
        if (!awaitingHit) return; // ignore if already resolved
        if (meshes[currentIndex].gameObject != tile.gameObject) return; // only valid on current

        awaitingHit = false;
        resolved = true;
        SetHit(meshes[currentIndex]);
        timer = 0f;
    }

    void AdvanceToNext()
    {
        // Move current to waiting state
        SetWaiting(meshes[currentIndex]);

        // Advance index
        currentIndex = (currentIndex + 1) % meshes.Length;

        // Set new nextUp
        SetNextUp(meshes[currentIndex]);

        awaitingHit = true;
        resolved = false;
        timer = 0f;
    }

    // Visual State Methods
    void SetNextUp(MeshRenderer mesh)
    {
        SetColor(mesh, defaultColor);
        SetAlpha(mesh, 1f);
    }

    void SetHit(MeshRenderer mesh)
    {
        SetColor(mesh, occupiedColor);
        SetAlpha(mesh, 1f);
    }

    void SetMissed(MeshRenderer mesh)
    {
        SetColor(mesh, missedColor);
        SetAlpha(mesh, 1f);
    }

    void SetWaiting(MeshRenderer mesh)
    {
        SetColor(mesh, defaultColor);
        SetAlpha(mesh, waitingAlpha);
    }

    bool IsHit(MeshRenderer mesh)
    {
        // check if material color matches occupiedColor
        return mesh.material.color == occupiedColor;
    }

    void SetColor(MeshRenderer mesh, Color c)
    {
        foreach (var m in mesh.materials)
            m.color = c;
    }

    void SetAlpha(MeshRenderer mesh, float alpha)
    {
        foreach (var mat in mesh.materials)
        {
            Color c = mat.color;
            c.a = alpha;
            mat.color = c;
        }
    }
}
