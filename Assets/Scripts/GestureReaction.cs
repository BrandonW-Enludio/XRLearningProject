using UnityEngine;
using UnityEngine.XR.Hands;

public class GestureReaction : MonoBehaviour
{
    public void OnGesturePerformed(ScriptableObject handPoseOrShape)
    {
        Debug.Log($"Gesture {handPoseOrShape.name} Performed!");
    }

    public void OnGesturedEnded(ScriptableObject handPoseOrShape)
    {
        Debug.Log($"Gesture {handPoseOrShape.name} Ended!");
    }
}
