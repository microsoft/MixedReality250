using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.WSA;
using HoloToolkit.Unity.InputModule;

namespace HoloToolkit.Unity.Playspace
{
    /// <summary>
    /// Uses the StageRoot component to ensure we the coordinate system grounded at 0,0,0 for occluded devices.
    /// Places a floor quad as a child of the stage root at 0,0,0.
    /// Will also draw the bounds of your placespace if you set it during the Mixed Reality Portal first run experience.
    /// </summary>
    public class PlayspaceManager : SingleInstance<PlayspaceManager>
    {
        [Tooltip("Quad prefab to display as the floor.")]
        public GameObject FloorQuad;
        private GameObject floorQuadInstance;

        [Tooltip("Material used to draw bounds for play space. Leave empty if you have not setup your play space or don't want to render bounds.")]
        public Material PlayspaceBoundsMaterial;


#pragma warning disable 0414
        bool updatePlayspaceBounds = true;
#pragma warning disable 0414

        public Vector3[] EditorLines;
        List<GameObject> boundingBoxLines = new List<GameObject>();

        private bool renderPlaySpace = true;
        public bool RenderPlaySpace
        {

            get
            {
                return renderPlaySpace;
            }
            set
            {
                if (renderPlaySpace != value)
                {
                    renderPlaySpace = value;
                    SetRendering();
                }
            }
        }

        private void SetRendering()
        {
            if (floorQuadInstance != null)
            {
                floorQuadInstance.SetActive(renderPlaySpace);
            }

            foreach(GameObject go in boundingBoxLines)
            {
                go.SetActive(renderPlaySpace);
            }
        }

        private void Start()
        {
            WorldManager.OnPositionalLocatorStateChanged += WorldManager_OnPositionalLocatorStateChanged;
            

            // Render the floor as a child of the StageRoot component.
            if (FloorQuad != null &&
                UnityEngine.XR.WSA.HolographicSettings.IsDisplayOpaque)
            {
                floorQuadInstance = GameObject.Instantiate(FloorQuad);
                floorQuadInstance.SetActive(true);
                
                // Parent this to the component that has the StageRoot attached.
                floorQuadInstance.transform.SetParent(this.gameObject.transform.parent);

                RenderPlaySpace = false;
#if UNITY_EDITOR
                // So the floor quad does not occlude in editor testing, draw it lower.
                floorQuadInstance.transform.localPosition = new Vector3(0, -3, 0);
                updatePlayspaceBounds = true;

                UpdatePlayspaceBounds();
#else
                // Draw the floor at 0,-1.5f,0 under stage root.
                floorQuadInstance.transform.localPosition = new Vector3(0, -1.5f, 0);
#endif
            }
        }

        private void WorldManager_OnPositionalLocatorStateChanged(PositionalLocatorState oldState, PositionalLocatorState newState)
        {
            Debug.Log("Stage root tracking changed " + oldState.ToString() + " "+newState.ToString());
            // Hide the floor if tracking is lost or if StageRoot can't be located.
            if (floorQuadInstance != null &&
                UnityEngine.XR.WSA.HolographicSettings.IsDisplayOpaque)
            {
                bool located = newState == PositionalLocatorState.Active;
                floorQuadInstance.SetActive((located && renderPlaySpace));
                if (located)
                {
                    floorQuadInstance.transform.localPosition = new Vector3(0, Mathf.Min(-1.5f, transform.position.y), 0);
                }
                updatePlayspaceBounds = located;
            }
        }

        private void Update()
        {
            // This is simply showing how to draw the bounds.
            // Applications don't *need* to draw bounds. 
            // Bounds are more useful for placing objects.
#if !UNITY_EDITOR
            if (updatePlayspaceBounds && HolographicSettings.IsDisplayOpaque)
            {
                UpdatePlayspaceBounds();
            }
#endif
        }

        private void UpdatePlayspaceBounds()
        {
            RemoveBoundingBox();

//#if UNITY_EDITOR
            Vector3[] bounds = EditorLines;
            bool tryGetBoundsSuccess =true;
//#else
//            Vector3[] bounds = null;
//            bool tryGetBoundsSuccess = stageRoot.TryGetBounds(out bounds);
//#endif
            
            if (tryGetBoundsSuccess && bounds != null && bounds.Length > 1)
            {
                if (PlayspaceBoundsMaterial != null)
                {
                    Vector3 start;
                    Vector3 end;
                    for (int i = 1; i < bounds.Length; i++)
                    {
                        start = bounds[i - 1];
                        end = bounds[i];
                        DrawLine(start, end);
                    }
                    DrawLine(bounds[0], bounds[bounds.Length - 1]);
                    updatePlayspaceBounds = false;
                }
            }
        }

        private void DrawLine(Vector3 start, Vector3 end)
        {
            GameObject boundingBox = new GameObject();
            boundingBoxLines.Add(boundingBox);
            boundingBox.transform.SetParent(this.transform.parent);
                        
            LineRenderer lr = boundingBox.AddComponent<LineRenderer>();
            lr.useWorldSpace = false;
            lr.sharedMaterial = PlayspaceBoundsMaterial;
            lr.startWidth = 0.05f;
            lr.endWidth = 0.05f;
            lr.SetPosition(0, start);
            lr.SetPosition(1, end);            
        }

        private void RemoveBoundingBox()
        {
            if (boundingBoxLines != null)
            {
                foreach (GameObject boundingBoxLine in boundingBoxLines)
                {
                    DestroyImmediate(boundingBoxLine);
                }
            }
        }

        
    }
}