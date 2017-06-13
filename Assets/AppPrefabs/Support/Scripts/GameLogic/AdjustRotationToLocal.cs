using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Smoothly rotates a transform
/// In the project we attach this script to the 'head' of the avatars
/// </summary>
public class AdjustRotationToLocal : MonoBehaviour {

    /// <summary>
    /// Keeps track of the previous rotation
    /// </summary>
    Quaternion lastRot;

    private void Start()
    {
        lastRot = transform.localRotation;
    }

    void Update ()
    {
        Debug.DrawLine(transform.position, transform.position + transform.parent.forward * 2.0f);

        // this will slowly rotate the head of the user.  This hides network latency
        transform.rotation = Quaternion.Slerp(lastRot, transform.parent.rotation,0.1f);
        lastRot = transform.rotation;
	}
}
