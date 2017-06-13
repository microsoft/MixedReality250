using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using HoloToolkit.Unity.InputModule;
using System;
using HoloToolkit.Examples.SharingWithUNET;

public class JoinSelectedSessionButton : MonoBehaviour, IInputClickHandler
{
    TextMesh textMesh;
    Material textMaterial;
    int textColorId;
    ScrollingSessionListUIController scrollingUIControl;
    NetworkDiscoveryWithAnchors networkDiscovery;

    private void Start()
    {
        scrollingUIControl = ScrollingSessionListUIController.Instance;
        textMesh = transform.parent.GetComponentInChildren<TextMesh>();
        textMaterial = textMesh.GetComponent<MeshRenderer>().material;
        textColorId = Shader.PropertyToID("_Color");
        textMaterial.SetColor(textColorId, Color.grey);
        networkDiscovery = NetworkDiscoveryWithAnchors.Instance;
    }

    private void Update()
    {
        if (networkDiscovery.running && networkDiscovery.isClient)
        {
            if (scrollingUIControl.SelectedSession != null)
            {
                textMaterial.SetColor(textColorId, Color.blue);
            }
            else
            {
                textMaterial.SetColor(textColorId, Color.grey);
            }
        }
        else
        {
            textMaterial.SetColor(textColorId, Color.grey);
        }
    }

    public void OnInputClicked(InputClickedEventData eventData)
    {
        ScrollingSessionListUIController.Instance.JoinSelectedSession();
    }
}
