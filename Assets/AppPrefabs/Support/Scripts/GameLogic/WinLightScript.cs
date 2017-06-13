using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Controls the 'goal' lights in the level
/// </summary>
public class WinLightScript : MonoBehaviour {

    /// <summary>
    /// The light to turn on/off depending on if a goal is reached
    /// </summary>
    Light goalLight;

    /// <summary>
    /// Local level state.
    /// </summary>
    LevelControl levelState;

    /// <summary>
    /// The range of the light in the immersive model
    /// </summary>
    float ImmersiveRange;

    /// <summary>
    /// The range of the light given the default scale
    /// </summary>
    public float DefaultRange = 5.5f;

	void Start ()
    {
        levelState = LevelControl.Instance;
        goalLight = GetComponent<Light>();
        ImmersiveRange = DefaultRange / LevelControl.ImmersiveScale;
        goalLight.intensity = 1.5f;
    }
	
	void Update ()
    {
        if (levelState.Immersed)
        {
            goalLight.range = DefaultRange;
        }
        else
        {
            goalLight.range = ImmersiveRange;
        }
	}
}
