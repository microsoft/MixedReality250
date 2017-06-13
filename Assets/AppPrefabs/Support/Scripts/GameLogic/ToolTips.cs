using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using HoloToolkit.Unity;

/// <summary>
/// Renders a tooltip
/// </summary>
public class ToolTips : MonoBehaviour {

    /// <summary>
    /// The Animator that controls the tool tip animation
    /// </summary>
    public Animator tooltipAnim;

    /// <summary>
    /// The text panel that the tool tip text goes into.
    /// </summary>
    public Text textPanel;
    
    /// <summary>
    /// Start with empty text
    /// </summary>
    private void Start()
    {
        textPanel.text = "";

    }

    // When we are enabled, start the animation
    void OnEnable()
    {
        tooltipAnim.CrossFade("toolTipAppear", 0f, -1, 0f);
        UAudioManager.Instance.PlayEvent("Tooltip", this.gameObject);
    }

    /// <summary>
    /// Sets the text to display in the text panel
    /// </summary>
    /// <param name="tipText">The text to draw</param>
    public void SetTipText(string tipText)
    {
        textPanel.text = tipText;
    }
}
