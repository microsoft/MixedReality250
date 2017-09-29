using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using HoloToolkit.Unity.SpatialMapping;
using HoloToolkit.Unity.Playspace;
using HoloToolkit.Unity;
using HoloToolkit.Unity.Boundary;

public class VRRoomControl : SingleInstance<VRRoomControl>
{

    public GameObject tableParts;
    BoundaryManager playspace;

    void Start () {
        
        if (UnityEngine.XR.WSA.HolographicSettings.IsDisplayOpaque == false)
        {
            Debug.Log("Not an immersive display so we'll use spatial mapping instead of this fake world");
            Destroy(this.gameObject);
            return;
        }

        playspace = BoundaryManager.Instance;
	}

    public void EnableControls()
    {
        if (playspace != null)
        {
            playspace.RenderBoundary = true;
            playspace.RenderFloor = true;
        }
        tableParts.SetActive(true);
    }

    public void DisableControls()
    {
        if (playspace != null)
        {
            playspace.RenderBoundary = false;
            playspace.RenderFloor = false;
        }
        tableParts.SetActive(false);
    }
}
