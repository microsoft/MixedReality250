using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using HoloToolkit.Unity.SpatialMapping;
using HoloToolkit.Unity.Playspace;

public class VRRoomControl : SingleInstance<VRRoomControl>
{

    public GameObject tableParts;
    PlayspaceManager playspace;

    void Start () {
        
        if (UnityEngine.VR.WSA.HolographicSettings.IsDisplayOpaque == false)
        {
            Debug.Log("Not an immersive display so we'll use spatial mapping instead of this fake world");
            Destroy(this.gameObject);
            return;
        }

        playspace = PlayspaceManager.Instance;
	}

    public void EnableControls()
    {
        playspace.RenderPlaySpace = true;
        tableParts.SetActive(true);
    }

    public void DisableControls()
    {
        playspace.RenderPlaySpace = false;
        tableParts.SetActive(false);
    }
}
