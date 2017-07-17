// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using HoloToolkit.Unity.InputModule;
using System;

public class DismissTutorialHandler : MonoBehaviour, IInputClickHandler {

    public GameObject UIToShowWhenDisabled;

    public void Start()
    {
        if (UIToShowWhenDisabled != null)
        {
            UIToShowWhenDisabled.SetActive(false);
        }
    }

    public void OnInputClicked(InputClickedEventData eventData)
    {
        if (UIToShowWhenDisabled != null)
        {
            UIToShowWhenDisabled.SetActive(true);
        }
        transform.gameObject.SetActive(false);
    }
}
