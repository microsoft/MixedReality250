using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using HoloToolkit.Unity.InputModule;
using System;

public class TeleportScript : MonoBehaviour
{
    public GameObject ActiveMarker;
    private MixedRealityTeleport warper;
    private Animator animationController;
    GazeManager gazeManager;
    bool warping = false;
    bool warpCancelled = false;
    int layerMask;
    FadeScript fadeControl;

    public void EnableMarker()
    {
        ActiveMarker.SetActive(true);
        animationController.StartPlayback();
        animationController.speed = 1;
    }

    public void DisableMarker()
    {
        animationController.StopPlayback();
        ActiveMarker.SetActive(false);
        animationController.speed = 0;
    }

    void Start ()
    {
        fadeControl = FadeScript.Instance;
        warper = MixedRealityTeleport.Instance;
        animationController = GetComponentInChildren<Animator>();
        animationController.StopPlayback();
        animationController.speed = 0;
        gazeManager = GazeManager.Instance;
        ActiveMarker.SetActive(false);
        layerMask = 1 << (LayerMask.NameToLayer("Ignore Raycast"));
        layerMask = ~layerMask;
        Debug.Log(LayerMask.NameToLayer("Ignore Raycast"));
    }

    // Update is called once per frame
    void Update()
    {
        if (warping)
        {
            if (Input.GetButtonUp("Jump"))
            {
                warping = false;
                if (warpCancelled == false)
                {

                    Vector3 hitPos = ActiveMarker.transform.position + Vector3.up * 2.6f;
                    Vector3 goal = hitPos;
                    fadeControl.DoFade(0.25f, 0.5f, () =>
                    {
                        warper.SetWorldPostion(goal);
                        Debug.DrawLine(hitPos, goal);
                    }, null);
                }

                warpCancelled = false;
                DisableMarker();

            }
            else
            {
                PositionMarker();
            }
        }
        else
        {
            if (fadeControl.Busy == false && Input.GetButtonDown("Jump"))
            {
                warping = true;
                EnableMarker();
                PositionMarker();
            }
        }
    }

    public void PositionMarker()
    {
        Vector3 hitNormal = HitNormal();
        if (Vector3.Dot(hitNormal, Vector3.up) > 0.90f)
        {
            warpCancelled = false;
            ActiveMarker.transform.position = gazeManager.HitPosition;
        }
        else
        {
            warpCancelled = true;
        }

        animationController.speed = warpCancelled ? 0 : 1;
    }

    public Vector3 HitNormal()
    {
        Vector3 retval = Vector3.zero;
        RaycastHit hitInfo;
        if (Physics.Raycast(Camera.main.transform.position, Camera.main.transform.forward, out hitInfo, 15.0f,layerMask))
        {
            retval = hitInfo.normal;
        }
        return retval;
    }
}
