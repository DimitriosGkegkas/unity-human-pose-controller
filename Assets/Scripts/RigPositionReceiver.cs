using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class BoneMapping
{
    [Tooltip("The bone transform to rotate")]
    public Transform boneTransform;
    
    
    [Tooltip("Position index of the parent joint (where bone starts)")]
    public int parentJointIndex;

    [Tooltip("Position index of the joint")]
    public int jointIndex;
    
    [Tooltip("Position index of the child joint (where bone ends)")]
    public int childJointIndex;
    
    [Tooltip("Reference direction in local space that the bone points in its default pose (usually Vector3.up or Vector3.forward)")]
    public Vector3 localReferenceDirection = Vector3.up;
}

public class RigPositionReceiver : MonoBehaviour
{
    [Header("Bone Mapping")]
    [Tooltip("Map bone transforms to their corresponding position indices in the received data")]
    public List<BoneMapping> boneMappings = new List<BoneMapping>();
    
    [Header("Coordinate System")]
    [Tooltip("Adjust if pose estimation uses a different coordinate system")]
    public Vector3 coordinateSystemAdjustment = Vector3.one;
    
    [Header("Settings")]
    public bool enableSmoothing = false;
    [Range(0.1f, 1f)]
    public float smoothingFactor = 0.5f;

    // Store initial rotations and smoothing data
    private Dictionary<Transform, Quaternion> initialRotations = new Dictionary<Transform, Quaternion>();
    private Dictionary<Transform, Quaternion> currentRotations = new Dictionary<Transform, Quaternion>();

    void Start()
    {
        Debug.Log($"[RigPositionReceiver] Starting with {boneMappings.Count} bones");
        
        // Store initial rotations for each bone
        foreach (var mapping in boneMappings)
        {
            if (mapping.boneTransform != null)
            {
                initialRotations[mapping.boneTransform] = mapping.boneTransform.localRotation;
                currentRotations[mapping.boneTransform] = mapping.boneTransform.localRotation;
            }
        }
    }

    void Update()
    {
        // Get positions from MyListener
        if (MyListener.Instance != null && MyListener.Instance.HasPositions())
        {
            Vector3[] positions = MyListener.Instance.GetLatestPositions();
            if (positions != null && positions.Length > 0)
            {
                ApplyRotationsToRig(positions);
            }
        }
    }

    public void ApplyRotationsToRig(Vector3[] positions)
    {
        foreach (var mapping in boneMappings)
        {
            if (mapping.boneTransform == null) continue;
            if (mapping.parentJointIndex < 0 || mapping.parentJointIndex >= positions.Length) continue;
            if (mapping.jointIndex < 0 || mapping.jointIndex >= positions.Length) continue;
            if (mapping.childJointIndex < 0 || mapping.childJointIndex >= positions.Length) continue;

            // Validate localReferenceDirection
            if (mapping.localReferenceDirection.magnitude < 0.001f)
            {
                Debug.LogError($"[RigPositionReceiver] localReferenceDirection is invalid for {mapping.boneTransform.name}! Set it to Vector3.up, Vector3.down, or Vector3.forward in Inspector.");
                continue;
            }

            // Get parent and child joint positions from pose estimation
            Vector3 parentJointPos = positions[mapping.parentJointIndex];
            Vector3 jointPos = positions[mapping.jointIndex];
            Vector3 childJointPos = positions[mapping.childJointIndex];

            // Apply coordinate system adjustment
            parentJointPos = new Vector3(
                parentJointPos.x * coordinateSystemAdjustment.x,
                parentJointPos.y * coordinateSystemAdjustment.y,
                parentJointPos.z * coordinateSystemAdjustment.z
            );
            jointPos = new Vector3(
                jointPos.x * coordinateSystemAdjustment.x,
                jointPos.y * coordinateSystemAdjustment.y,
                jointPos.z * coordinateSystemAdjustment.z
            );
            childJointPos = new Vector3(
                childJointPos.x * coordinateSystemAdjustment.x,
                childJointPos.y * coordinateSystemAdjustment.y,
                childJointPos.z * coordinateSystemAdjustment.z
            );

            // Calculate the direction from parent to child (bone direction in pose estimation space)
            Vector3 targetBoneDirection = (childJointPos - jointPos);
            targetBoneDirection.z = 0;
            targetBoneDirection.Normalize();
            
            Vector3 refBoneDirection = (jointPos - parentJointPos);
            refBoneDirection.z = 0;
            refBoneDirection.Normalize();

            Quaternion targetRotation = Quaternion.FromToRotation(refBoneDirection, targetBoneDirection);
            Debug.Log($"Target rotation: {targetRotation.eulerAngles}");

            targetRotation.x = targetRotation.z * Mathf.Sign(mapping.localReferenceDirection.x);
            targetRotation.z = 0;
            targetRotation.y = 0;
            mapping.boneTransform.localRotation = targetRotation;
        }
    }

    // Public method to manually apply positions
    public void SetPositions(Vector3[] positions)
    {
        ApplyRotationsToRig(positions);
    }
}

