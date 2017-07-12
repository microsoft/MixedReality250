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
            networkDiscovery.StartHosting("SuperRad");
        }
    }

    // Use this for initialization
    void Start () {
        networkDiscovery = NetworkDiscoveryWithAnchors.Instance;
    }
}
