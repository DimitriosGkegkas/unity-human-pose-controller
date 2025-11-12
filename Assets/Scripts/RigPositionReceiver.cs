using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class BoneMapping
{
    [Tooltip("The bone transform to rotate")]
    public Transform boneTransform;
    
    [Tooltip("Name of the arm segment from the payload (e.g., left_upper_arm)")]
    public string armSegmentName;
}

public class RigPositionReceiver : MonoBehaviour
{
    [Header("Bone Mapping")]
    [Tooltip("Map bone transforms to their corresponding arm segment names in the payload")]
    public List<BoneMapping> boneMappings = new List<BoneMapping>();
    
    [Header("Settings")]
    public bool enableSmoothing = true;
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

    void OnEnable()
    {
        MyListener.OnNewPosePayload += HandlePayload;
    }

    void OnDisable()
    {
        MyListener.OnNewPosePayload -= HandlePayload;
    }

    void HandlePayload(MyListener.PosePayload payload)
    {
        if (payload?.ArmSegments == null || payload.ArmSegments.Count == 0)
        {
            return;
        }

        ApplyRotationsToRig(payload.ArmSegments);
    }

    public void ApplyRotationsToRig(Dictionary<string, MyListener.ArmSegmentData> armSegments)
    {
        if (armSegments == null || armSegments.Count == 0)
        {
            return;
        }

        foreach (var mapping in boneMappings)
        {
            if (mapping.boneTransform == null)
            {
                continue;
            }

            if (string.IsNullOrWhiteSpace(mapping.armSegmentName))
            {
                continue;
            }

            if (!armSegments.TryGetValue(mapping.armSegmentName, out var segment))
            {
                continue;
            }


            Quaternion targetRotation = new Quaternion();
            Vector3 targetDirectionParentLocal = mapping.boneTransform.parent.InverseTransformVector(segment.Direction);
            targetRotation.SetFromToRotation(new Vector3(0, 1, 0), targetDirectionParentLocal);

            if (enableSmoothing && currentRotations.TryGetValue(mapping.boneTransform, out var current))
            {
                targetRotation = Quaternion.Slerp(current, targetRotation, smoothingFactor);
            }


            mapping.boneTransform.localRotation = targetRotation;
            currentRotations[mapping.boneTransform] = targetRotation;
        }
    }

    public void SetArmSegments(Dictionary<string, MyListener.ArmSegmentData> armSegments)
    {
        ApplyRotationsToRig(armSegments);
    }
}

