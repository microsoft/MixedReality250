// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using UnityEngine;

namespace HoloToolkit.Unity.InputModule
{
    /// <summary>
    /// Class for manually controlling a 3DoF controller when not running on HoloLens (in editor).
    /// </summary>
    public class Manual3DoFWithOrientationControl : MonoBehaviour
    {
        public float HandReturnFactor = 0.25f;  /// [0.0,1.0] the closer this is to one the faster it brings the hand back to center
        public float HandTimeBeforeReturn = 0.5f;
        public float MinimumTrackedMovement = 0.001f;

        public AxisController PrimaryAxisControl;
        public AxisController SecondaryAxisControl;
        public ButtonController SelectButtonControl;
        public ButtonController MenuButtonControl;

        public DebugInteractionSourceState ControllerSourceState;

        public Color ActiveControllerColor;
        public Color DroppedControllerColor;

        /// <summary>
        /// Will place controller visualizations in the world space, only for debugging.
        /// Place the representative GameObjects in ControllerVisualizer.
        /// </summary>
        public bool VisualizeController = true;
        public GameObject ControllerVisualizer;

        public Texture ControllerTexture;

        public bool ControllerInView;

        private Renderer controllerVisualRenderer;
        private MaterialPropertyBlock controllerVisualPropertyBlock;
        private int mainTexID;
        private bool appHasFocus = true;

        private float timeBeforeReturn;

        private void Awake()
        {
            mainTexID = Shader.PropertyToID("_MainTex");

            ControllerSourceState.Pressed = false;
            ControllerSourceState.Properties.Location = new DebugInteractionSourceLocation();
            controllerVisualRenderer = ControllerVisualizer.GetComponent<Renderer>();
            controllerVisualPropertyBlock = new MaterialPropertyBlock();
            controllerVisualRenderer.SetPropertyBlock(controllerVisualPropertyBlock);

#if !UNITY_EDITOR
            VisualizeController = false;
            UpdateControllerVisualization();
            Destroy(this);
#endif
        }

        private void Update()
        {
            UpdateControllerVisualization();

            // float smoothingFactor = Time.deltaTime * 30.0f * HandReturnFactor;
            if (timeBeforeReturn > 0.0f)
            {
                timeBeforeReturn = Mathf.Clamp(timeBeforeReturn - Time.deltaTime, 0.0f, HandTimeBeforeReturn);
            }

            ControllerSourceState.IsSelectPressed = SelectButtonControl.Pressed();
            ControllerSourceState.IsMenuPressed = MenuButtonControl.Pressed();

            if (ControllerSourceState.IsSelectPressed)
            {
                timeBeforeReturn = HandTimeBeforeReturn;
            }

            if (appHasFocus)
            {
                // If there is a mouse translate with a modifier key and it is held down, do not reset the controller position.
                bool handTranslateActive =
                    (PrimaryAxisControl.axisType == AxisController.AxisType.Mouse && PrimaryAxisControl.buttonType != ButtonController.ButtonType.None && PrimaryAxisControl.ShouldControl()) ||
                    (SecondaryAxisControl.axisType == AxisController.AxisType.Mouse && SecondaryAxisControl.buttonType != ButtonController.ButtonType.None && SecondaryAxisControl.ShouldControl());

                if (handTranslateActive || ControllerSourceState.IsSelectPressed)
                {
                    timeBeforeReturn = HandTimeBeforeReturn;
                    ControllerInView = true;
                }

                ControllerVisualizer.transform.position = ControllerSourceState.Properties.Location.Position;
                ControllerVisualizer.transform.forward = Camera.main.transform.forward;

                controllerVisualPropertyBlock.SetTexture(mainTexID, ControllerTexture);
                controllerVisualRenderer.SetPropertyBlock(controllerVisualPropertyBlock);

                ControllerSourceState.Properties.Location.TryGetFunctionsReturnsTrue = ControllerInView;
            }
            else
            {
                ControllerSourceState.Properties.Location.TryGetFunctionsReturnsTrue = false;
            }
        }

        private void UpdateControllerVisualization()
        {
            controllerVisualRenderer.material.SetColor("_Color", ControllerInView ? ActiveControllerColor : DroppedControllerColor);

            if (ControllerVisualizer.activeSelf != VisualizeController)
            {
                ControllerVisualizer.SetActive(VisualizeController);
            }
        }
    }

}

