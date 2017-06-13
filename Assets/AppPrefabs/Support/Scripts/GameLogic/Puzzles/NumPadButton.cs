using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using HoloToolkit.Unity.InputModule;
using System;

public class NumPadButton : MonoBehaviour, IInputClickHandler {

    public char ButtonLabel;
    public NumPadPuzzle parentNumpad;
    LevelControl levelState;

    public void OnInputClicked(InputClickedEventData eventData)
    {
        if(levelState.Immersed)
        {
            parentNumpad.ButtonHit(ButtonLabel);
        }
    }

    // Use this for initialization
    void Start () {
        levelState = LevelControl.Instance;
    }
	
	
}
