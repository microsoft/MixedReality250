using System;
using System.Collections;
using System.Collections.Generic;
using HoloToolkit.Unity.InputModule;
using UnityEngine;
using HoloToolkit.Examples.SharingWithUNET;

public class StartSessionButton : MonoBehaviour, IInputClickHandler {

    NetworkDiscoveryWithAnchors networkDiscovery;

    public void OnInputClicked(InputClickedEventData eventData)
    {
        if (networkDiscovery.running)
        {
            // Only let hololens host...
#if !UNITY_EDITOR
            if (!UnityEngine.VR.WSA.HolographicSettings.IsDisplayOpaque)
#endif
            {
                networkDiscovery.StartHosting("SuperRad");
            }
        }
    }

    // Use this for initialization
    void Start () {
        networkDiscovery = NetworkDiscoveryWithAnchors.Instance;
#if !UNITY_EDITOR
        if (UnityEngine.VR.WSA.HolographicSettings.IsDisplayOpaque)

        {
            Debug.Log("Only hololens can host for now");
            Destroy(this.gameObject);
        }
#endif
    }
}
