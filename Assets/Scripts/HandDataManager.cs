using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Hands;
using UnityEngine.XR.Management;

public class HandDataManager : MonoBehaviour
{
    [Header("Hands")]
    public HandBoneData leftHandData;
    public HandBoneData rightHandData;

    static readonly List<XRHandSubsystem> k_SubsystemsReuse = new List<XRHandSubsystem>();
    private XRHandSubsystem m_HandSubsystem;

    private Dictionary<Handedness, HandBoneData> handDataDict = new();

    void OnEnable()
    {
        leftHandData = new HandBoneData(Handedness.Left);
        rightHandData = new HandBoneData(Handedness.Right);

        handDataDict[Handedness.Left] = leftHandData;
        handDataDict[Handedness.Right] = rightHandData;
    }

    // Lazy Initialize because hand subsystem usually isn't available on start or awake
    void Update()
    {
        if (m_HandSubsystem != null && m_HandSubsystem.running)
            return;

        SubsystemManager.GetSubsystems(k_SubsystemsReuse);
        for (int i = 0; i < k_SubsystemsReuse.Count; ++i)
        {
            XRHandSubsystem handSubsystem = k_SubsystemsReuse[i];
            if (handSubsystem.running)
            {
                SetSubsystem(handSubsystem);
                break;
            }
        }
    }

    void SetSubsystem(XRHandSubsystem handSubsystem)
    {
        UnsubscribeSubsystem();

        m_HandSubsystem = handSubsystem;
        m_HandSubsystem.updatedHands += OnHandsUpdated;
        m_HandSubsystem.trackingAcquired += OnTrackingAcquired;
        m_HandSubsystem.trackingLost += OnTrackingLost;
    }

    private void OnTrackingAcquired(XRHand hand)
    {
        Debug.Log($"Hand tracking ACQUIRED for -> {hand.handedness}");
    }

    private void OnTrackingLost(XRHand hand)
    {
        Debug.Log($"Hand tracking LOST for -> {hand.handedness}");

    }

    private void OnHandsUpdated(XRHandSubsystem subsystem, XRHandSubsystem.UpdateSuccessFlags updateSuccessFlags, XRHandSubsystem.UpdateType updateType)
    {
        // Left Hand
        bool leftJointsUpdated = (updateSuccessFlags & XRHandSubsystem.UpdateSuccessFlags.LeftHandJoints) != 0;
        bool leftRootUpdated = (updateSuccessFlags & XRHandSubsystem.UpdateSuccessFlags.LeftHandRootPose) != 0;

        if (leftJointsUpdated || leftRootUpdated)
        {
            leftHandData.UpdateFromXRHand(subsystem.leftHand);
        }

        // Right Hand
        bool rightJointsUpdated = (updateSuccessFlags & XRHandSubsystem.UpdateSuccessFlags.RightHandJoints) != 0;
        bool rightRootUpdated = (updateSuccessFlags & XRHandSubsystem.UpdateSuccessFlags.RightHandRootPose) != 0;

        if (rightJointsUpdated || rightRootUpdated)
        {
            rightHandData.UpdateFromXRHand(subsystem.rightHand);
        }
    }

    // === Bone Access API ===

    public HandJointData GetJoint(Handedness handedness, XRHandJointID jointID)
    {
        if (handDataDict.TryGetValue(handedness, out HandBoneData data))
        {
            int index = jointID.ToIndex() - XRHandJointID.BeginMarker.ToIndex();
            if (index >= 0 && index < data.joints.Length)
                return data.joints[index];
        }
        return default;
    }

    public Vector3 GetFingertipPosition(Handedness handedness, XRHandJointID fingerTipID = XRHandJointID.IndexTip)
    {
        HandJointData joint = GetJoint(handedness, fingerTipID);
        return joint.isValid ? joint.position : Vector3.zero;
    }

    public float GetDistanceBetweenJoints(Handedness handedness, XRHandJointID jointA, XRHandJointID jointB)
    {
        Vector3 posA = GetJoint(handedness, jointA).position;
        Vector3 posB = GetJoint(handedness, jointB).position;
        return Vector3.Distance(posA, posB);
    }

    // Simple finger curl example (0 = straight, 1 = fully curled)
    // Uses proximal -> intermediate -> distal angles or approximation
    public float GetFingerCurl(Handedness handedness, XRHandJointID proximalJoint)
    {
        // Example for index: Proximal -> Intermediate -> Distal
        XRHandJointID intermediate = GetNextJoint(proximalJoint);
        XRHandJointID distal = GetNextJoint(intermediate);

        Vector3 dir1 = (GetJoint(handedness, intermediate).position - GetJoint(handedness, proximalJoint).position).normalized;
        Vector3 dir2 = (GetJoint(handedness, distal).position - GetJoint(handedness, intermediate).position).normalized;

        float angle = Vector3.Angle(dir1, dir2);
        return Mathf.Clamp01((angle - 0f) / 90f); // Adjust thresholds as needed
    }

    private XRHandJointID GetNextJoint(XRHandJointID current)
    {
        // Simple mapping - expand for full finger logic
        switch (current)
        {
            case XRHandJointID.IndexProximal: return XRHandJointID.IndexIntermediate;
            case XRHandJointID.IndexIntermediate: return XRHandJointID.IndexDistal;
            // Add cases for other fingers...
            default: return current;
        }
    }

    // Validation helper
    public bool IsHandValid(Handedness handedness)
    {
        return handDataDict.TryGetValue(handedness, out var data) &&
               m_HandSubsystem != null &&
               (handedness == Handedness.Left ? m_HandSubsystem.leftHand.isTracked : m_HandSubsystem.rightHand.isTracked);
    }
    public HandBoneData GetSnapshot(Handedness hand)
    {
        if (!handDataDict.TryGetValue(hand, out var data))
            return null;

        // Deep copy so we don't send a live updating object
        var snapshot = new HandBoneData(hand);
        Array.Copy(data.joints, snapshot.joints, data.joints.Length);
        return snapshot;
    }

    void UnsubscribeSubsystem()
    {
        if (m_HandSubsystem != null)
        {
            m_HandSubsystem.updatedHands -= OnHandsUpdated;
            m_HandSubsystem.trackingAcquired -= OnTrackingAcquired;
            m_HandSubsystem.trackingLost -= OnTrackingLost;
        }
    }

    void OnDestroy()
    {
        UnsubscribeSubsystem();
    }
}