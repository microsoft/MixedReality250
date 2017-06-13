using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using HoloToolkit.Unity;
using HoloToolkit.Unity.InputModule;
using System;

public class MiningTorchScript : MonoBehaviour, IInputClickHandler {

    public MineCartScript mineCart;
    LevelControl levelState;
    public GameObject flame;
    public void OnInputClicked(InputClickedEventData eventData)
    {
        if (levelState.Immersed)
        {
            mineCart.TorchClicked(this.gameObject);
            flame.SetActive(true);
            UAudioManager.Instance.PlayEvent("Torch_Light", this.gameObject);
        }
        }

    public void Reset()
    {
        flame.SetActive(false);
    }
    // Use this for initialization
    void Start () {
        levelState = LevelControl.Instance;
    }
	
	
}
