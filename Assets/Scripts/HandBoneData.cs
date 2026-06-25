using System;
using UnityEngine.XR.Hands;

[System.Serializable]
public class HandBoneData
{
    public Handedness handedness;
    public HandJointData[] joints;  // Indexed by XRHandJointID order

    public HandBoneData(Handedness hand)
    {
        handedness = hand;
        // 26 joints typically (from BeginMarker to EndMarker)
        int jointCount = XRHandJointID.EndMarker.ToIndex() - XRHandJointID.BeginMarker.ToIndex();
        joints = new HandJointData[jointCount];
    }

    // Populate from XRHand
    public void UpdateFromXRHand(XRHand hand)
    {
        if (!hand.isTracked)
        { 
            return; 
        }

        int index = 0;
        for (int i = XRHandJointID.BeginMarker.ToIndex();
             i < XRHandJointID.EndMarker.ToIndex(); i++)
        {
            XRHandJoint xrJoint = hand.GetJoint(XRHandJointIDUtility.FromIndex(i));
            joints[index] = new HandJointData(xrJoint);
            index++;
        }
    }

    public HandBoneData TakeSnapshot()
    {
        var snapshot = new HandBoneData(this.handedness);

        Array.Copy(this.joints, snapshot.joints, this.joints.Length);

        return snapshot;
    }
}