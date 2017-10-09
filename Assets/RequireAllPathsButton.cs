using HoloToolkit.Examples.SharingWithUNET;
using HoloToolkit.Unity.InputModule;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RequireAllPathsButton : MonoBehaviour, IInputClickHandler
{
    public void OnInputClicked(InputClickedEventData eventData)
    {
        bool newState = !LevelControl.Instance.AllPathsRequired;
        PlayerController.Instance.SetRequireAllPaths(newState);
        GetComponent<TextMesh>().text = string.Format("Require All Paths: {0}", newState ? "ON" : "OFF");
    }
}
