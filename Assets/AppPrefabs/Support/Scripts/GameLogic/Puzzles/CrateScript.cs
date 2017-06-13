using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using HoloToolkit.Unity;
using HoloToolkit.Unity.InputModule;
using System;

public class CrateScript : MonoBehaviour, IInputClickHandler
{

    private Vector3 goalLocalPosY;
    public Vector3 goalLocalPos;

    private Vector3 startLocalPos;
    LevelControl levelState;
    public CratePuzzle cratePuzzle;
    float AnimationStartTime;
    public float animationTime = 1.5f;
    bool atGoal = false;

    public void OnInputClicked(InputClickedEventData eventData)
    {
        if (levelState.Immersed)
        {
            if (cratePuzzle.CrateClicked(this.gameObject))
            {
                atGoal = true;
                AnimationStartTime = Time.realtimeSinceStartup;
                UAudioManager.Instance.PlayEvent("Box_Tap", this.gameObject);
            }
        }
    }

    // Use this for initialization
    void Start()
    {
        startLocalPos = transform.localPosition;
        levelState = LevelControl.Instance;
        goalLocalPosY = startLocalPos;
        goalLocalPosY.y = goalLocalPos.y;
    }

    private void Update()
    {
        float timeLeftRatio = TimeLeftRatio();
        Vector3 startPos = StartPos(timeLeftRatio);
        Vector3 endPos = EndPos(timeLeftRatio);
        
        if(timeLeftRatio > 0.5f)
        {
            timeLeftRatio = (timeLeftRatio - 0.5f) * 2;
        }
        else
        {
            timeLeftRatio *= 2;
        }

        if (timeLeftRatio < 1)
        {
            transform.localPosition = Vector3.Lerp(startPos, endPos, timeLeftRatio);
        }
        else
        {
            transform.localPosition = endPos;
        }
    }

    Vector3 StartPos(float timeLeftRatio)
    {

        if (timeLeftRatio > 0.5f)
        {
            return goalLocalPosY;
        }

        return atGoal ? startLocalPos : goalLocalPosY;

    }

    Vector3 EndPos(float timeLeftRatio)
    {
        if (timeLeftRatio > 0.5f)
        {
            return atGoal ? goalLocalPos : startLocalPos;
        }

        return goalLocalPosY;
    }

    public void Reset()
    {
        atGoal = false;
        AnimationStartTime = Time.realtimeSinceStartup;
    }

    public void SetSolved()
    {
        if (!atGoal)
        {
            atGoal = true;
            AnimationStartTime = Time.realtimeSinceStartup;
        }
    }
    float TimeLeftRatio()
    {
        float timeLeft = animationTime - (Time.realtimeSinceStartup - AnimationStartTime);
        if (timeLeft >= 0)
        {
            return 1.0f-(timeLeft / animationTime);
        }
        else
        {
            return 1.0f;
        }
    }
}
