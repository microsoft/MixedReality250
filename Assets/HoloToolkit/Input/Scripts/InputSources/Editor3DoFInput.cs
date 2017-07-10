// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.WSA;
using UnityEngine.XR.WSA.Input;

namespace HoloToolkit.Unity.InputModule
{
    /// <summary>
    /// Input source for fake 3DoF controller information, which gives finer details about current controller state and position
    /// than the standard GestureRecognizer.
    /// </summary>
    /// <remarks>This input source only triggers SourceUp and SourceDown for the 3DoF controller. Everything else is handled by GesturesInput.</remarks>
    [RequireComponent(typeof(Manual3DoFWithOrientationControl))]
    public class Editor3DoFInput : BaseInputSource
    {
        /// <summary>
        /// Data for a 3DoF controller.
        /// </summary>
        private class Editor3DoFData
        {
            public Editor3DoFData(uint controllerId)
            {
                ControllerId = controllerId;
                ControllerDelta = Vector3.zero;

                IsSelectButtonDown = false;
                IsSelectButtonDownPending = false;
                SelectButtonStateChanged = false;
                SelectButtonStateUpdateTimer = -1;

                IsMenuButtonDown = false;
                IsMenuButtonDownPending = false;
                MenuButtonStateChanged = false;
                MenuButtonStateUpdateTimer = -1;

                IsGrasped = false;
                IsGraspedPending = false;
                GraspStateChanged = false;
                GraspStateUpdateTimer = -1;               
            }

            public readonly uint ControllerId;
            public Vector3 ControllerDelta;

            public bool IsSelectButtonDown;
            public bool IsSelectButtonDownPending;
            public bool SelectButtonStateChanged;
            public float SelectButtonStateUpdateTimer;
            public float SelectButtonDownStartTime;

            public bool IsMenuButtonDown;
            public bool IsMenuButtonDownPending;
            public bool MenuButtonStateChanged;
            public float MenuButtonStateUpdateTimer;
            // public float MenuButtonDownStartTime;

            public bool IsGrasped;
            public bool IsGraspedPending;
            public bool GraspStateChanged;
            public float GraspStateUpdateTimer;            
        }

        private Manual3DoFWithOrientationControl manual3DoFControl;

        /// <summary>
        /// Delay before a button pressed is considered.
        /// This mitigates fake button taps that can sometimes be detected while the controller is moving.
        /// </summary>
        private const float ButtonPressDelay = 0.07f;

        /// <summary>
        /// The maximum interval between button down and button up that will result in a clicked event.
        /// </summary>
        private const float MaxClickDuration = 0.5f;

        /// <summary>
        /// Number of fake controllers supported in the editor.
        /// </summary>
        private const int EditorControllerCount = 2;

        /// <summary>
        /// Array containing the controller data for the two fake controllers
        /// </summary>
        private readonly Editor3DoFData[] editorControllerData = new Editor3DoFData[EditorControllerCount];

        /// <summary>
        /// Dictionary linking each controller ID to its data.
        /// </summary>
        private readonly Dictionary<uint, Editor3DoFData> controllerIdToData = new Dictionary<uint, Editor3DoFData>(4);
        private readonly List<uint> pendingControllerIdDeletes = new List<uint>();

        // HashSets used to be able to quickly update the controller data when controllers become visible / not visible
        private readonly HashSet<uint> currentControllers = new HashSet<uint>();
        private readonly HashSet<uint> newControllers = new HashSet<uint>();

        public override SupportedInputInfo GetSupportedInputInfo(uint sourceId)
        {
            return SupportedInputInfo.None;
        }

        public override bool TryGetPosition(uint sourceId, out Vector3 position)
        {
            // Position not supported by 3DoF controllers
            position = Vector3.zero;
            return false;
        }

        public override bool TryGetOrientation(uint sourceId, out Quaternion orientation)
        {
            // I think we're not supposed to expose orientation
            orientation = Quaternion.identity;
            return false;
        }

        public override bool TryGetPointingRay(uint sourceId, out Ray pointingRay)
        {
            // Not currently supported by 3DoF controllers
            pointingRay = default(Ray);
            return false;
        }

        /// <summary>
        /// Gets the position delta of the specified controller.
        /// </summary>
        /// <param name="controllerId">ID of the controller to get.</param>
        /// <returns>The current movement vector of the specified controller.</returns>
        public Vector3 GetControllerDelta(uint controllerId)
        {
            if (controllerId >= editorControllerData.Length)
            {
                string message = string.Format("GetHandDelta called with invalid hand ID {0}.", controllerId);
                throw new ArgumentException(message, "handId");
            }

            return editorControllerData[controllerId].ControllerDelta;
        }

        private void Awake()
        {
#if !UNITY_EDITOR
            Destroy(this);
#endif
            manual3DoFControl = GetComponent<Manual3DoFWithOrientationControl>();
            for (uint i = 0; i < editorControllerData.Length; i++)
            {
                editorControllerData[i] = new Editor3DoFData(i);
            }
        }

#if UNITY_EDITOR
        private void Update()
        {
            newControllers.Clear();
            currentControllers.Clear();

            UpdateControllerData();
            SendControllerVisibilityEvents();
        }
#endif

        /// <summary>
        /// Update the controller data for the currently detected controllers.
        /// </summary>
        private void UpdateControllerData()
        {
            if (manual3DoFControl.ControllerInView)
            {
                GetOrAddControllerData(0);
                currentControllers.Add(0);

                UpdateControllerState(manual3DoFControl.ControllerSourceState, editorControllerData[0]);
            }
        }

        /// <summary>
        /// Gets the controller data for the specified controller source if it already exists, otherwise creates it.
        /// </summary>
        /// <param name="sourceId">Controller source for which controller's data should be retrieved.</param>
        /// <returns>The controller data requested.</returns>
        private Editor3DoFData GetOrAddControllerData(uint sourceId)
        {
            Editor3DoFData controllerData;
            if (!controllerIdToData.TryGetValue(sourceId, out controllerData))
            {
                controllerData = new Editor3DoFData(sourceId);
                controllerIdToData.Add(controllerData.ControllerId, controllerData);
                newControllers.Add(controllerData.ControllerId);

            }

            return controllerData;
        }

        /// <summary>
        /// Updates the controller positional information.
        /// </summary>
        /// <param name="controllerSource">Controller source to use to update the position.</param>
        /// <param name="editorControllerData">EditorControllerData structure to update.</param>
        private void UpdateControllerState(DebugInteractionSourceState controllerSource, Editor3DoFData editorControllerData)
        {
            // Check for button presses
            if (controllerSource.IsSelectPressed != editorControllerData.IsSelectButtonDownPending)
            {
                editorControllerData.IsSelectButtonDownPending = controllerSource.IsSelectPressed;
                editorControllerData.SelectButtonStateUpdateTimer = ButtonPressDelay;
            }

            if (controllerSource.IsMenuPressed != editorControllerData.IsMenuButtonDownPending)
            {
                editorControllerData.IsMenuButtonDownPending = controllerSource.IsMenuPressed;
                editorControllerData.MenuButtonStateUpdateTimer = ButtonPressDelay;
            }

            if (controllerSource.IsGrasped != editorControllerData.IsGraspedPending)
            {
                editorControllerData.IsGraspedPending = controllerSource.IsGrasped;
                editorControllerData.GraspStateUpdateTimer = ButtonPressDelay;
            }

            // Not sure if we really need to do this?
            // Button presses are delayed to mitigate issue with hand position shifting during air tap
            editorControllerData.SelectButtonStateChanged = false;
            if (editorControllerData.SelectButtonStateUpdateTimer > 0)
            {
                editorControllerData.SelectButtonStateUpdateTimer -= Time.deltaTime;
                if (editorControllerData.SelectButtonStateUpdateTimer <= 0)
                {
                    editorControllerData.IsSelectButtonDown = editorControllerData.IsSelectButtonDownPending;
                    editorControllerData.SelectButtonStateChanged = true;
                    if (editorControllerData.IsSelectButtonDown)
                    {
                        editorControllerData.SelectButtonDownStartTime = Time.time;
                    }
                }
            }

            // TODO: update other button states (menu, grasp)

            SendControllerStateEvents(editorControllerData);
        }

        /// <summary>
        /// Sends the events for controller state changes.
        /// </summary>
        /// <param name="editorControllerData">Controller data for which events should be sent.</param>
        private void SendControllerStateEvents(Editor3DoFData editorControllerData)
        {
            // Select button pressed/released events
            if (editorControllerData.SelectButtonStateChanged)
            {
                SourceButtonEventArgs buttonArgs = new SourceButtonEventArgs(this, editorControllerData.ControllerId, InteractionSourcePressType.Select);

                if (editorControllerData.IsSelectButtonDown)
                {
                    inputManager.RaiseSourceDown(buttonArgs.InputSource, buttonArgs.SourceId);
                }
                else
                {
                    inputManager.RaiseSourceUp(buttonArgs.InputSource, buttonArgs.SourceId);

                    // Also send click event when using this controller replacement input
                    if (Time.time - editorControllerData.SelectButtonDownStartTime < MaxClickDuration)
                    {
                        // We currently only support single taps in editor
                        SourceClickEventArgs args = new SourceClickEventArgs(this, editorControllerData.ControllerId, 1);
                        inputManager.RaiseInputClicked(args.InputSource, args.SourceId, args.TapCount);
                    }
                }
            }

            if (editorControllerData.MenuButtonStateChanged)
            {
                SourceButtonEventArgs buttonArgs = new SourceButtonEventArgs(this, editorControllerData.ControllerId, InteractionSourcePressType.Menu);

                if (editorControllerData.IsMenuButtonDown)
                {
                    inputManager.RaiseSourceDown(buttonArgs.InputSource, buttonArgs.SourceId);
                }
                else
                {
                    inputManager.RaiseSourceUp(buttonArgs.InputSource, buttonArgs.SourceId);
                }
            }

            if (editorControllerData.GraspStateChanged)
            {
                SourceButtonEventArgs buttonArgs = new SourceButtonEventArgs(this, editorControllerData.ControllerId, InteractionSourcePressType.Grasp);

                if (editorControllerData.IsGrasped)
                {
                    inputManager.RaiseSourceDown(buttonArgs.InputSource, buttonArgs.SourceId);
                }
                else
                {
                    inputManager.RaiseSourceUp(buttonArgs.InputSource, buttonArgs.SourceId);
                }
            }
        }

        /// <summary>
        /// Sends the events for controller visibility changes.
        /// </summary>
        private void SendControllerVisibilityEvents()
        {
            // Send event for new controllers that were added
            foreach (uint newController in newControllers)
            {
                InputSourceEventArgs args = new InputSourceEventArgs(this, newController);
                inputManager.RaiseSourceDetected(args.InputSource, args.SourceId);
            }

            // Send event for controllers that are no longer visible and remove them from our dictionary
            foreach (uint existingController in controllerIdToData.Keys)
            {
                if (!currentControllers.Contains(existingController))
                {
                    pendingControllerIdDeletes.Add(existingController);
                    InputSourceEventArgs args = new InputSourceEventArgs(this, existingController);
                    inputManager.RaiseSourceLost(args.InputSource, args.SourceId);
                }
            }

            // Remove pending controller IDs
            for (int i = 0; i < pendingControllerIdDeletes.Count; ++i)
            {
                controllerIdToData.Remove(pendingControllerIdDeletes[i]);
            }
            pendingControllerIdDeletes.Clear();
        }
    }
}