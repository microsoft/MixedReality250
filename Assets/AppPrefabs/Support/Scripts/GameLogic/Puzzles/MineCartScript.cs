using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using HoloToolkit.Unity;

public class MineCartScript : MonoBehaviour, LevelControl.IAmAPuzzle
{

    public GameObject[] Torches;
    bool[] TorchesClicked;
    Animator animator;
    public bool Solved
    {
        get;
        private set;
    }
    public string ToolTipText
    {
        get
        {
            return "Safe lighting needed";
        }
    }

    // Use this for initialization
    void Start () {
        TorchesClicked = new bool[Torches.Length];
        animator = GetComponent<Animator>();
        animator.speed = 0;
        animator.enabled = true;
        Solved = false;
    }

   
    void CheckAllTorches()
    {
        for (int index = 0; index < TorchesClicked.Length; index++)
        {
            if (TorchesClicked[index] == false)
            {
                return;
            }
        }
        Solved = true;
        animator.speed = 1f;
    }

   public void TorchClicked(GameObject clickedTorch)
    {
        for(int index=0;index<Torches.Length;index++)
        {
            if (Torches[index] == clickedTorch)
            {
                TorchesClicked[index] = true;
                CheckAllTorches();
            }
        }
    }

    public void Complete()
    {
        for (int index = 0; index < Torches.Length; index++)
        {
            TorchesClicked[index] = true;

            MiningTorchScript mts = Torches[index].GetComponent<MiningTorchScript>();
            mts.flame.SetActive(true);
            Solved = true;
            UAudioManager.Instance.PlayEvent("Cart_Move01", this.gameObject);
        }

        animator.speed = 1f;
    }

    public void Reset()
    {
        Debug.Log("resetting mine cart");
        for (int index = 0; index < Torches.Length; index++)
        {
            TorchesClicked[index] = false;
            Torches[index].GetComponent<MiningTorchScript>().Reset();
        }
        Solved = false;
        animator.Play("MiningCart_Entry",0,0);
        animator.speed = 0;
    }
}
