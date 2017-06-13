using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// This forces an object to keep pointing up regardless of its parent's rotation
/// </summary>
public class AlwaysPointingUp : MonoBehaviour {

    Quaternion lastRot;

    private void Start()
    {
        lastRot = transform.localRotation;
    }

    // Update is called once per frame
    void Update ()
    {
        // first we need to get a rotation from the parent's up to actual up
        Quaternion rot = Quaternion.FromToRotation(transform.parent.up, Vector3.up);
        // the rotate smoothly toward the parent roation with the 'not up' part cancelled out
        transform.rotation = Quaternion.Slerp(lastRot, rot * transform.parent.rotation, 0.05f);
        lastRot = transform.rotation;
    }
}
