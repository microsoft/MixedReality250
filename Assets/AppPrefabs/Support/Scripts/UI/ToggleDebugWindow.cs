// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using UnityEngine;
using HoloToolkit.Unity.InputModule;

public class ToggleDebugWindow : MonoBehaviour, IInputClickHandler
{
    bool debugEnabled = false;
    public GameObject DebugWindow;
	
    // Use this for initialization
	void Start ()
    {
        UpdateChildren();
	}

    private void Update()
    {
        if (Input.GetButtonUp("Fire3"))
        {
            OnInputClicked(null);
        }
    }

    public void OnInputClicked(InputClickedEventData eventData)
    {
        debugEnabled = !debugEnabled;
        UpdateChildren();
    }

    private void UpdateChildren()
    {
        DebugWindow.SetActive(debugEnabled);
    }
}
