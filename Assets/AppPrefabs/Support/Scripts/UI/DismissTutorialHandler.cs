// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using HoloToolkit.Unity.InputModule;
using System;
using HoloToolkit.Unity;

public class DismissTutorialHandler : SingleInstance<DismissTutorialHandler> {

    public GameObject UIToShowWhenDisabled;
    Camera mainCamera;
    private float targetDistance = 2.0f;
    private Vector3 nextTargetPos;
    bool moving = false;
    Vector3 prevCamPos = Vector3.zero;
    public void Start()
    {
        if (UIToShowWhenDisabled != null)
        {
            UIToShowWhenDisabled.SetActive(false);
        }

        if (UnityEngine.XR.WSA.HolographicSettings.IsDisplayOpaque)
        {
            targetDistance = 1.0f;
        }

        mainCamera = Camera.main;
        prevCamPos = mainCamera.transform.position;
        nextTargetPos = mainCamera.transform.position + mainCamera.transform.forward * targetDistance;
        moving = true;
    }

    public void DismissTutorial()
    {
        if (UIToShowWhenDisabled != null)
        {
            UIToShowWhenDisabled.SetActive(true);
            UIToShowWhenDisabled.transform.position = mainCamera.transform.position + mainCamera.transform.forward * targetDistance;
        }
        transform.gameObject.SetActive(false);
    }

    private void Update()
    {
        if ((mainCamera.transform.position - prevCamPos).magnitude > 0.5f)
        {
            Debug.Log("Detected big camera move, just snapping");
            transform.position = mainCamera.transform.position + mainCamera.transform.forward * targetDistance;
        }

        prevCamPos = mainCamera.transform.position;

        Vector3 targetPos = mainCamera.transform.position + mainCamera.transform.forward * targetDistance;
        if ((targetPos - transform.position).sqrMagnitude > 1.5f)
        {
            nextTargetPos = targetPos;
            moving = true;
        }
        else if (moving)
        {
            if ((targetPos - transform.position).sqrMagnitude < (nextTargetPos-transform.position).sqrMagnitude)
            {
                nextTargetPos = targetPos;
            }
        }

        if (moving)
        {
            transform.position = Vector3.Lerp(transform.position, nextTargetPos, Time.deltaTime * .25f);
        }

        if ((nextTargetPos - transform.position).sqrMagnitude < 0.1f)
        {
            moving = false;
        }

        transform.LookAt(mainCamera.transform);
        transform.Rotate(Vector3.up * 180);
    }
}
