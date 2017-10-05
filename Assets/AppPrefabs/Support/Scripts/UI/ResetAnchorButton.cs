using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using HoloToolkit.Unity.InputModule;
using HoloToolkit.Unity;
using System;
using HoloToolkit.Unity.SharingWithUNET;

public class ResetAnchorButton : MonoBehaviour, IInputClickHandler {

	public void OnInputClicked(InputClickedEventData eventData)
	{
		if (NetworkDiscoveryWithAnchors.Instance.isServer)
		{
			UNetAnchorManager.Instance.MakeNewAnchor();
		}
		else
		{
			Debug.Log("Only the server for now");
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
