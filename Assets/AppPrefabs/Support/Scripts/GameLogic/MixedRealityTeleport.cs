using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MixedRealityTeleport : SingleInstance<MixedRealityTeleport> {

    /// <summary>
    /// How many degrees to turn when the left or right bumper are pressed
    /// </summary>
    public float BumperRotationSize = 30.0f;

    /// <summary>
    /// the fade control allows us to fade out and fade in the scene.
    /// This is done to improve comfort when using an immersive display
    /// </summary>
    FadeScript fadeControl;

    void Start()
    {
        fadeControl = FadeScript.Instance;
    }

    void Update()
    {
        // Check bumpers for coarse rotation
        float bumperRot = 0;

        if (Input.GetButtonUp("LeftBumper"))
        {
            bumperRot = -BumperRotationSize;
        }

        if (Input.GetButtonUp("RightBumper"))
        {
            bumperRot = BumperRotationSize;
        }

        if (bumperRot != 0)
        {
            fadeControl.DoFade(
                0.25f, // Fade out time
                0.25f, // Fade in time
                () => // Action after fade out
                {
                    transform.RotateAround(Camera.main.transform.position, Vector3.up, bumperRot);
                },
                null // Action after fade in
                );
        }
    }

    /// <summary>
    /// Places the player in the specified position of the world
    /// </summary>
    /// <param name="worldPosition"></param>
    public void SetWorldPostion(Vector3 worldPosition)
    {
        // There are two things moving the camera, the camera parent (that this script is attached to)
        // and the user's head (which the MR device is attached to. :)) when setting the world position,
        // we need to set it relative to the user's head in the scene so they are looking/standing where 
        // we expect.
        transform.position = worldPosition - Camera.main.transform.localPosition;
    }

    public void ResetRotation()
    {
        transform.localRotation = Quaternion.identity;
    }
}
