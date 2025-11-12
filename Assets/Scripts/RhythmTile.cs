using UnityEngine;

[RequireComponent(typeof(MeshRenderer), typeof(Collider))]
public class RhythmTile : MonoBehaviour
{
    public MeshRenderer mesh;

    private FollowTheRythm parent;

    void Awake()
    {
        mesh = GetComponent<MeshRenderer>();
        parent = GetComponentInParent<FollowTheRythm>();
    }

    private void OnTriggerEnter(Collider other)
    {
        parent.TileHit(this);
    }
}
