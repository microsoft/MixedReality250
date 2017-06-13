using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WireupDebugPanel : MonoBehaviour {

    public bool ConnectedPosition = false;
	// Use this for initialization
	void Awake ()
    {
		if (ConnectedPosition)
        {
            PositionDebugButton.Instance.ConnectedPosition = this.gameObject;
        }
        else
        {
            PositionDebugButton.Instance.DisconnectedPosition = this.gameObject;
        }
	}
}
