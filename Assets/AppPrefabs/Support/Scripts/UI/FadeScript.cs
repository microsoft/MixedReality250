using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class FadeScript : SingleInstance<FadeScript> {

    Material fadeMaterial;
    Color fadeColor = Color.black;
    enum FadeState
    {
        idle=0,
        fadingOut,
        FadingIn
    }

    public bool Busy
    {
        get
        {
            return currentState != FadeState.idle;
        }
    }

    FadeState currentState;
    float startTime;
    float fadeOutTime;
    Action fadeOutAction;
    float fadeInTime;
    Action fadeInAction;

    // Use this for initialization
    void Start ()
    {
        if (UnityEngine.VR.WSA.HolographicSettings.IsDisplayOpaque == false)
        {
            GetComponentInChildren<MeshRenderer>().enabled = false;
            Debug.Log("removing unnecessary full screen effect from hololens");
            return;
        }

        currentState = FadeState.idle;
        fadeMaterial = GetComponentInChildren<MeshRenderer>().material;
	}
	
	// Update is called once per frame
	void Update ()
    {
		if (Busy)
        {
            CalculateFade();
        }

        if (Input.GetKeyUp(KeyCode.F))
        {
            DoFade(3, 3, () => { Debug.Log("Done fading out"); }, () => { Debug.Log("Done fading in"); });
        }

	}

    void CalculateFade()
    {
        float actionTime = currentState == FadeState.fadingOut ? fadeOutTime : fadeInTime;
        float timeBusy = Time.realtimeSinceStartup - startTime;
        float timePercentUsed = timeBusy / actionTime;
        if (timePercentUsed >= 1.0f)
        {
            Action callback = currentState == FadeState.fadingOut ? fadeOutAction : fadeInAction;
            if (callback != null)
            {
                callback();
            }
            
            fadeColor.a = currentState == FadeState.fadingOut ? 1 : 0;
            fadeMaterial.color = fadeColor;

            currentState = currentState == FadeState.fadingOut ? FadeState.FadingIn : FadeState.idle;
            startTime = Time.realtimeSinceStartup;
        }
        else
        {
            fadeColor.a = currentState == FadeState.fadingOut ? timePercentUsed : 1 - timePercentUsed;
            fadeMaterial.color = fadeColor;
        }

    }

    private void OnDestroy()
    {
        if (fadeMaterial != null)
        {
            Destroy(fadeMaterial);
        }
    }

    public bool DoFade(float fadeOutTime, float fadeInTime, Action FadedOutAction, Action FadedInAction)
    {
        if (Busy)
        {
            Debug.Log("already fading");
            return false;
        }

        this.fadeOutTime = fadeOutTime;
        this.fadeOutAction = FadedOutAction;
        this.fadeInTime = fadeInTime;
        this.fadeInAction = FadedInAction;

        startTime = Time.realtimeSinceStartup;
        currentState = FadeState.fadingOut;
        return true;
    }
}
