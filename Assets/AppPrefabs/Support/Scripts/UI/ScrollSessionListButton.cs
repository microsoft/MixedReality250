using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using HoloToolkit.Unity.InputModule;
using System;

public class ScrollSessionListButton : MonoBehaviour, IInputClickHandler {

    public int Direction;

    public void OnInputClicked(InputClickedEventData eventData)
    {
        ScrollingSessionListUIController.Instance.ScrollSessions(Direction);
    }
}
