using UnityEngine;
using UnityEngine.XR.Hands;

[System.Serializable]
public struct HandJointData
{
    public XRHandJointID jointID;
    public Vector3 position;
    public Quaternion rotation;
    public float radius;
    public XRHandJointTrackingState trackingState;
    public bool isValid;

    public HandJointData(XRHandJoint joint)
    {
        jointID = joint.id;
        trackingState = joint.trackingState;
        isValid = false;
        position = Vector3.zero;
        rotation = Quaternion.identity;
        radius = 0f;

        if (joint.TryGetPose(out Pose pose))
        {
            position = pose.position;
            rotation = pose.rotation;
            isValid = true;
        }

        if (joint.TryGetRadius(out float r))
        {
            radius = r;
        }
    }
}
