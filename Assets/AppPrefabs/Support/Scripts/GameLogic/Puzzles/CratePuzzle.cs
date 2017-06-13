using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using HoloToolkit.Unity;

public class CratePuzzle : MonoBehaviour, LevelControl.IAmAPuzzle {


    public string ToolTipText
    {
        get
        {
            return "Red Green Blue";
        }
    }

    public bool Solved
    {
        get;
        private set;
    }

    public GameObject[] CrateSequence;
    int GoalPos = 0;
    bool OpeningDoor = false;
    float OpenStartTime = 0;
    public GameObject doorObject;
    Vector3 doorStartPos;
    public Vector3 doorGoalLocalPosOffset;
    // Use this for initialization
    void Start () {
        doorStartPos = doorObject.transform.localPosition;
        Solved = false;
    }
	
	// Update is called once per frame
	void Update ()
    {
        if (OpeningDoor)
        {
            float TimeDelta = Time.realtimeSinceStartup - OpenStartTime;
            float TimeRat = TimeDelta / 5.0f;
            if (TimeRat < 1)
            {
                doorObject.transform.localPosition = Vector3.Lerp(doorStartPos, doorStartPos + doorGoalLocalPosOffset, TimeRat);
            }
            else
            {
                doorObject.transform.localPosition = doorStartPos + doorGoalLocalPosOffset;
                OpeningDoor = false;
            }
        }
    }

    public bool CrateClicked(GameObject crate)
    {
        if (crate == CrateSequence[GoalPos])
        {
            GoalPos++;
            if (GoalPos >= CrateSequence.Length)
            {
                Invoke("PuzzleWon", 2.0f);
            }
            return true;
        }

        Debug.Log("WRONG");
        return false;
    }

    void PuzzleWon()
    {
        Solved = true;
        GoalPos = 0;
        OpeningDoor = true;
        OpenStartTime = Time.realtimeSinceStartup;
        foreach (GameObject crate in CrateSequence)
        {
            crate.GetComponent<CrateScript>().SetSolved();
        }
        UAudioManager.Instance.PlayEvent("GarageDoor_Open", doorObject);
    }

    public void Reset()
    {
        Debug.Log("resetting crates");
        GoalPos = 0;
        OpeningDoor = false;
        Solved = false;
        doorObject.transform.localPosition = doorStartPos;
        foreach(GameObject crate in CrateSequence)
        {
            crate.GetComponent<CrateScript>().Reset();
        }
    }

    public void Complete()
    {
        PuzzleWon();
    }
}
