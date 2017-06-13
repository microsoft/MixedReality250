using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using HoloToolkit.Unity;

public class NumPadPuzzle : MonoBehaviour, LevelControl.IAmAPuzzle
{
    public string ToolTipText
    {
        get
        {
            return "I love PI";
        }
    }

    public bool Solved
    {
        get;
        private set;
    }

    public Text numPadText;
    public char[] GoalSequence = { '3', '.', '1', '4' };
    int GoalPos = 0;
    public GameObject RightDoorObject;
    public GameObject LeftDoorObject;
    Vector3 startRightDoorLocalPos;
    Vector3 startLeftDoorLocalPos;
    public Vector3 goalDoorLocalPosOffset;
    bool OpeningDoor = false;
    float OpenStartTime = 0;
	// Use this for initialization
	void Start () {
        numPadText.text = "";
        startRightDoorLocalPos = RightDoorObject.transform.localPosition;
        startLeftDoorLocalPos = LeftDoorObject.transform.localPosition;
        Solved = false;
        
    }
	
	// Update is called once per frame
	void Update () {
		if (OpeningDoor)
        {
            float TimeDelta = Time.realtimeSinceStartup - OpenStartTime;
            float TimeRat = TimeDelta / 5.0f;
            if (TimeRat < 1)
            {
                RightDoorObject.transform.localPosition = Vector3.Lerp(startRightDoorLocalPos, startRightDoorLocalPos + goalDoorLocalPosOffset, TimeRat);
                LeftDoorObject.transform.localPosition = Vector3.Lerp(startLeftDoorLocalPos, startLeftDoorLocalPos - goalDoorLocalPosOffset, TimeRat);
            }
            else
            {
                RightDoorObject.transform.localPosition = startRightDoorLocalPos + goalDoorLocalPosOffset;
                LeftDoorObject.transform.localPosition = startLeftDoorLocalPos - goalDoorLocalPosOffset;
                OpeningDoor = false;
            }
        }
	}

    public bool ButtonHit(char button)
    {
        if (button == GoalSequence[GoalPos])
        {
            numPadText.color = Color.green;
            if (GoalPos == 0)
            {
                numPadText.text = "";
            }
            numPadText.text += button+" ";
            GoalPos++;
            UAudioManager.Instance.PlayEvent("Keypad_Hit", this.gameObject);
            if (GoalPos >= GoalSequence.Length)
            {
                Invoke("PuzzleWon", 1.0f);
            }
            return true;
        }
        UAudioManager.Instance.PlayEvent("Keypad_Failure", this.gameObject);
        numPadText.color = Color.red;
        return false;
    }

    void PuzzleWon()
    {
        numPadText.text = "W I N";
        GoalPos = 0;
        OpeningDoor = true;
        Solved = true;
        OpenStartTime = Time.realtimeSinceStartup;
        UAudioManager.Instance.PlayEvent("Keypad_Success", this.gameObject);
        UAudioManager.Instance.PlayEvent("Door_Open", RightDoorObject);
    }

    public void Complete()
    {
        PuzzleWon();
    }

    public void Reset()
    {
        Debug.Log("resetting numpad");
        numPadText.text = "";
        GoalPos = 0;
        LeftDoorObject.transform.localPosition = startLeftDoorLocalPos;
        RightDoorObject.transform.localPosition = startRightDoorLocalPos;
        Solved = false;
    }
}
