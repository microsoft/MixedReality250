using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using HoloToolkit.Unity.InputModule;
using System;

public class ScaleOnGaze : MonoBehaviour, IFocusable {

    Vector3 startScale;
	// Use this for initialization
	void Start ()
    {
        startScale = transform.localScale;
	}

    private void OnSelect()
    {
        transform.localScale = startScale;
    }

    public void OnFocusEnter()
    {
        if (GetComponent<AnimationWhenSelected>() == null)
        {
            transform.localScale = startScale * 1.25f;
        }
    }

    public void OnFocusExit()
    {
        if (GetComponent<AnimationWhenSelected>() == null)
        {
            transform.localScale = startScale;
        }
    }
}
