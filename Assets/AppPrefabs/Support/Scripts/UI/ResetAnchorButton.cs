using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using HoloToolkit.Unity.InputModule;
using HoloToolkit.Unity;
using System;
using HoloToolkit.Examples.SharingWithUNET;

public class ResetAnchorButton : MonoBehaviour, IInputClickHandler {

    int resetFrame = 0;
    public void OnInputClicked(InputClickedEventData eventData)
    {
        if (UnityEngine.XR.WSA.HolographicSettings.IsDisplayOpaque == false && NetworkDiscoveryWithAnchors.Instance.isServer)
        {
            UNetAnchorManager.Instance.MakeNewAnchor();
            eventData.Use();
        }
        else
        {
            Debug.Log("Only the server on hololens for now");
        }
    }

    // Use this for initialization
    void Start ()
    {

    }
	
	// Update is called once per frame
	void Update ()
    {
		
	}
}
