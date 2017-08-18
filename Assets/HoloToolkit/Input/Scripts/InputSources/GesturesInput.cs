// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.WSA.Input;

namespace HoloToolkit.Unity.InputModule
{
    /// <summary>
    /// Input source for gestures information from the WSA APIs, which gives access to various system-supported gestures
    /// and positional information for the various inputs that Windows gestures supports.
    /// This is mostly a wrapper on top of GestureRecognizer and InputManager.
    /// </summary>
    public class GesturesInput : BaseInputSource
    {
        // This enumeration gives the manager two different ways to handle the recognizer. Both will
        // set up the recognizer. The first causes the recognizer to start
        // immediately. The second allows the recognizer to be manually started at a later time.
        public enum RecognizerStartBehavior { AutoStart, ManualStart };

        [Tooltip("Whether the recognizer should be activated on start.")]
        public RecognizerStartBehavior RecognizerStart;

        [Tooltip("Set to true to use the use rails (guides) for the navigation gesture, as opposed to full 3D navigation.")]
        public bool UseRailsNavigation = false;

        [Tooltip("Use unscaled time. This is useful for scenarios that have a pause mechanism or otherwise adjust the timescale.")]
        public bool UseUnscaledTime = true;

        protected GestureRecognizer gestureRecognizer;
        protected GestureRecognizer navigationGestureRecognizer;

        #region IInputSource Capabilities and SourceData

        private struct SourceCapability<TReading>
        {
            public bool IsSupported;
            public bool IsAvailable;
            public TReading CurrentReading;
        }

        /// <summary>
        /// Data for an interaction source.
        /// </summary>
        private class SourceData
        {
            public static readonly Vector3 DefaultPosition = Vector3.zero;
            public static readonly Quaternion DefaultOrientation = Quaternion.identity;
            public static readonly Ray DefaultRay = default(Ray);

            public SourceData(uint sourceId)
            {
                SourceId = sourceId;
                Position = new SourceCapability<Vector3> { CurrentReading = DefaultPosition };
                Orientation = new SourceCapability<Quaternion> { CurrentReading = DefaultOrientation };
                PointingRay = new SourceCapability<Ray> { CurrentReading = DefaultRay };
            }

            public readonly uint SourceId;
            public SourceCapability<Vector3> Position;
            public SourceCapability<Quaternion> Orientation;
            public SourceCapability<Ray> PointingRay;

            public bool IsSourceDown;
            public bool IsSourceDownPending;
            public bool SourceStateChanged;
            public float SourceStateUpdateTimer;
        }

        /// <summary>
        /// Dictionary linking each source ID to its data.
        /// </summary>
        private readonly Dictionary<uint, SourceData> sourceIdToData = new Dictionary<uint, SourceData>(4);
        private readonly List<uint> pendingSourceIdDeletes = new List<uint>();

        // HashSets used to be able to quickly update the sources data when they become visible / not visible
        private readonly HashSet<uint> currentSources = new HashSet<uint>();
        private readonly HashSet<uint> newSources = new HashSet<uint>();

        /// <summary>
        /// Delay before a source press is considered.
        /// This mitigates fake source taps that can sometimes be detected while the input source is moving.
        /// </summary>
        private const float SourcePressDelay = 0.07f;

        #endregion IInputSource Capabilities and SourceData

        protected override void Start()
        {
            base.Start();

            gestureRecognizer = new GestureRecognizer();
            gestureRecognizer.Tapped += OnTappedEvent;
            
            gestureRecognizer.HoldStarted += OnHoldStartedEvent;
            gestureRecognizer.HoldCompleted += OnHoldCompletedEvent;
            gestureRecognizer.HoldCanceled += OnHoldCanceledEvent;

            gestureRecognizer.ManipulationStarted += OnManipulationStartedEvent;
            gestureRecognizer.ManipulationUpdated += OnManipulationUpdatedEvent;
            gestureRecognizer.ManipulationCompleted += OnManipulationCompletedEvent;
            gestureRecognizer.ManipulationCanceled += OnManipulationCanceledEvent;

            //gestureRecognizer.SetRecognizableGestures(GestureSettings.Tap | 
            //                                          GestureSettings.ManipulationTranslate |
            //                                          GestureSettings.Hold);

            gestureRecognizer.SetRecognizableGestures(GestureSettings.Tap);

            // We need a separate gesture recognizer for navigation, since it isn't compatible with manipulation
            navigationGestureRecognizer = new GestureRecognizer();

            navigationGestureRecognizer.NavigationStarted += OnNavigationStartedEvent;
            navigationGestureRecognizer.NavigationUpdated += OnNavigationUpdatedEvent;
            navigationGestureRecognizer.NavigationCompleted += OnNavigationCompletedEvent;
            navigationGestureRecognizer.NavigationCanceled += OnNavigationCanceledEvent;

            if (UseRailsNavigation)
            {
                navigationGestureRecognizer.SetRecognizableGestures(GestureSettings.NavigationRailsX |
                                                                    GestureSettings.NavigationRailsY |
                                                                    GestureSettings.NavigationRailsZ);
            }
            else
            {
                navigationGestureRecognizer.SetRecognizableGestures(GestureSettings.NavigationX |
                                                                    GestureSettings.NavigationY |
                                                                    GestureSettings.NavigationZ);
            }

            if (RecognizerStart == RecognizerStartBehavior.AutoStart)
            {
                gestureRecognizer.StartCapturingGestures();
                navigationGestureRecognizer.StartCapturingGestures();
            }
        }

        protected virtual void OnDestroy()
        {
            StopGestureRecognizer();
            if (gestureRecognizer != null)
            {
                gestureRecognizer.Tapped -= OnTappedEvent;

                gestureRecognizer.HoldStarted -= OnHoldStartedEvent;
                gestureRecognizer.HoldCompleted -= OnHoldCompletedEvent;
                gestureRecognizer.HoldCanceled -= OnHoldCanceledEvent;

                gestureRecognizer.ManipulationStarted -= OnManipulationStartedEvent;
                gestureRecognizer.ManipulationUpdated -= OnManipulationUpdatedEvent;
                gestureRecognizer.ManipulationCompleted -= OnManipulationCompletedEvent;
                gestureRecognizer.ManipulationCanceled -= OnManipulationCanceledEvent;

                gestureRecognizer.Dispose();
            }
            if (navigationGestureRecognizer != null)
            {
                navigationGestureRecognizer.NavigationStarted -= OnNavigationStartedEvent;
                navigationGestureRecognizer.NavigationUpdated -= OnNavigationUpdatedEvent;
                navigationGestureRecognizer.NavigationCompleted -= OnNavigationCompletedEvent;
                navigationGestureRecognizer.NavigationCanceled -= OnNavigationCanceledEvent;

                navigationGestureRecognizer.Dispose();
            }            
        }

        protected virtual void OnDisable()
        {
            StopGestureRecognizer();
        }

        protected virtual void OnEnable()
        {
            if (RecognizerStart == RecognizerStartBehavior.AutoStart)
            {
                StartGestureRecognizer();
            }
        }

        /// <summary>
        /// Make sure the gesture recognizer is off, then start it.
        /// Otherwise, leave it alone because it's already in the desired state.
        /// </summary>
        public void StartGestureRecognizer()
        {
            if (gestureRecognizer != null && !gestureRecognizer.IsCapturingGestures())
            {
                gestureRecognizer.StartCapturingGestures();
            }
            if (navigationGestureRecognizer != null && !navigationGestureRecognizer.IsCapturingGestures())
            {
                navigationGestureRecognizer.StartCapturingGestures();
            }
        }

        /// <summary>
        /// Make sure the gesture recognizer is on, then stop it.
        /// Otherwise, leave it alone because it's already in the desired state.
        /// </summary>
        public void StopGestureRecognizer()
        {
            if (gestureRecognizer != null && gestureRecognizer.IsCapturingGestures())
            {
                gestureRecognizer.StopCapturingGestures();
            }
            if (navigationGestureRecognizer != null && navigationGestureRecognizer.IsCapturingGestures())
            {
                navigationGestureRecognizer.StopCapturingGestures();
            }
        }

        private void Update()
        {
            newSources.Clear();
            currentSources.Clear();

            UpdateSourceData();
            SendSourceVisibilityEvents();
        }

        /// <summary>
        /// Update the source data for the currently detected sources.
        /// </summary>
        private void UpdateSourceData()
        {
            // Poll for updated reading from hands
            InteractionSourceState[] sourceStates = InteractionManager.GetCurrentReading();
            if (sourceStates != null)
            {
                for (var i = 0; i < sourceStates.Length; ++i)
                {
                    InteractionSourceState handSource = sourceStates[i];
                    SourceData sourceData = GetOrAddSourceData(handSource.source);
                    currentSources.Add(handSource.source.id);

                    UpdateSourceState(handSource, sourceData);
                }
            }
        }

        /// <summary>
        /// Gets the source data for the specified interaction source if it already exists, otherwise creates it.
        /// </summary>
        /// <param name="interactionSource">Interaction source for which data should be retrieved.</param>
        /// <returns>The source data requested.</returns>
        private SourceData GetOrAddSourceData(InteractionSource interactionSource)
        {
            SourceData sourceData;
            if (!sourceIdToData.TryGetValue(interactionSource.id, out sourceData))
            {
                sourceData = new SourceData(interactionSource.id);
                sourceIdToData.Add(sourceData.SourceId, sourceData);
                newSources.Add(sourceData.SourceId);
            }

            return sourceData;
        }

        /// <summary>
        /// Updates the source positional information.
        /// </summary>
        /// <param name="interactionSource">Interaction source to use to update the position.</param>
        /// <param name="sourceData">SourceData structure to update.</param>
        private void UpdateSourceState(InteractionSourceState interactionSource, SourceData sourceData)
        {
            sourceData.Position.IsAvailable = interactionSource.sourcePose.TryGetPosition(out sourceData.Position.CurrentReading);
              
            // Using a heuristic for IsSupported, since the APIs don't yet support querying this capability directly.
            sourceData.Position.IsSupported |= sourceData.Position.IsAvailable;

            sourceData.Orientation.IsAvailable = interactionSource.sourcePose.TryGetRotation(out sourceData.Orientation.CurrentReading);
            // Using a heuristic for IsSupported, since the APIs don't yet support querying this capability directly.
            sourceData.Orientation.IsSupported |= sourceData.Orientation.IsAvailable;
            
            sourceData.PointingRay.IsSupported = interactionSource.source.supportsPointing;
            sourceData.PointingRay.IsAvailable = false;// interactionSource.sourcePose.TryGetRay(out sourceData.PointingRay.CurrentReading);

            // TODO: Update other information we want to keep cached (touchpad position? trigger reading? thumbstick position?)

            // Check for source presses
            if (interactionSource.anyPressed != sourceData.IsSourceDownPending)
            {
                sourceData.IsSourceDownPending = interactionSource.anyPressed;
                sourceData.SourceStateUpdateTimer = SourcePressDelay;
            }

            // Source presses are delayed to mitigate issue with hand position shifting during air tap
            sourceData.SourceStateChanged = false;
            if (sourceData.SourceStateUpdateTimer >= 0)
            {
                float deltaTime = UseUnscaledTime
                    ? Time.unscaledDeltaTime
                    : Time.deltaTime;

                sourceData.SourceStateUpdateTimer -= deltaTime;
                if (sourceData.SourceStateUpdateTimer < 0)
                {
                    sourceData.IsSourceDown = sourceData.IsSourceDownPending;
                    sourceData.SourceStateChanged = true;
                }
            }

            SendSourceStateEvents(sourceData);
        }

        /// <summary>
        /// Sends the events for source state changes.
        /// </summary>
        /// <param name="sourceData">Source data for which events should be sent.</param>
        private void SendSourceStateEvents(SourceData sourceData)
        {
            // Source pressed/released events
            if (sourceData.SourceStateChanged)
            {
                if (sourceData.IsSourceDown)
                {
                    inputManager.RaiseSourceDown(this, sourceData.SourceId);
                }
                else
                {
                    inputManager.RaiseSourceUp(this, sourceData.SourceId);
                }
            }
        }

        /// <summary>
        /// Sends the events for source visibility changes.
        /// </summary>
        private void SendSourceVisibilityEvents()
        {
            // Send event for new sources that were added
            foreach (uint newSource in newSources)
            {
                inputManager.RaiseSourceDetected(this, newSource);
            }

            // Send event for sources that are no longer visible and remove them from our dictionary
            foreach (uint existingSource in sourceIdToData.Keys)
            {
                if (!currentSources.Contains(existingSource))
                {
                    pendingSourceIdDeletes.Add(existingSource);
                    inputManager.RaiseSourceLost(this, existingSource);
                }
            }

            // Remove pending source IDs
            for (int i = 0; i < pendingSourceIdDeletes.Count; ++i)
            {
                sourceIdToData.Remove(pendingSourceIdDeletes[i]);
            }
            pendingSourceIdDeletes.Clear();
        }

        #region BaseInputSource implementations

        public override SupportedInputInfo GetSupportedInputInfo(uint sourceId)
        {
            SupportedInputInfo retVal = SupportedInputInfo.None;

            SourceData sourceData;
            if (sourceIdToData.TryGetValue(sourceId, out sourceData))
            {
                if (sourceData.Position.IsSupported)
                {
                    retVal |= SupportedInputInfo.Position;
                }

                if (sourceData.Orientation.IsSupported)
                {
                    retVal |= SupportedInputInfo.Orientation;
                }

                if (sourceData.PointingRay.IsSupported)
                {
                    retVal |= SupportedInputInfo.PointingRay;
                }
            }

            return retVal;
        }

        public override bool TryGetPosition(uint sourceId, out Vector3 position)
        {
            SourceData sourceData;
            if (sourceIdToData.TryGetValue(sourceId, out sourceData) && TryGetReading(sourceData.Position, out position))
            {
                return true;
            }
            else
            {
                position = SourceData.DefaultPosition;
                return false;
            }
        }

        public override bool TryGetOrientation(uint sourceId, out Quaternion orientation)
        {
            SourceData sourceData;
            if (sourceIdToData.TryGetValue(sourceId, out sourceData) && TryGetReading(sourceData.Orientation, out orientation))
            {
                return true;
            }
            else
            {
                orientation = SourceData.DefaultOrientation;
                return false;
            }
        }

        public override bool TryGetPointingRay(uint sourceId, out Ray pointingRay)
        {
            SourceData sourceData;
            if (sourceIdToData.TryGetValue(sourceId, out sourceData) && TryGetReading(sourceData.PointingRay, out pointingRay))
            {
                return true;
            }
            else
            {
                pointingRay = SourceData.DefaultRay;
                return false;
            }
        }

        private bool TryGetReading<TReading>(SourceCapability<TReading> capability, out TReading reading)
        {
            if (capability.IsAvailable)
            {
                Debug.Assert(capability.IsSupported);

                reading = capability.CurrentReading;
                return true;
            }
            else
            {
                reading = default(TReading);
                return false;
            }
        }

        #endregion BaseInputSource implementations

        #region Raise GestureRecognizer Events

        protected void OnTappedEvent(TappedEventArgs obj)
        {
            inputManager.RaiseInputClicked(this, (uint)obj.source.id, obj.tapCount);
        }

        protected void OnHoldStartedEvent(HoldStartedEventArgs obj)
        {
            inputManager.RaiseHoldStarted(this, (uint)obj.source.id);
        }

        protected void OnHoldCanceledEvent(HoldCanceledEventArgs obj)
        {
            inputManager.RaiseHoldCanceled(this, (uint)obj.source.id);
        }

        protected void OnHoldCompletedEvent(HoldCompletedEventArgs obj)
        {
            inputManager.RaiseHoldCompleted(this, (uint)obj.source.id);
        }

        protected void OnManipulationStartedEvent(ManipulationStartedEventArgs obj)
        {
            inputManager.RaiseManipulationStarted(this, (uint)obj.source.id, Vector3.zero);
        }

        protected void OnManipulationUpdatedEvent(ManipulationUpdatedEventArgs obj)
        {
            inputManager.RaiseManipulationUpdated(this, (uint)obj.source.id, obj.cumulativeDelta);
        }

        protected void OnManipulationCompletedEvent(ManipulationCompletedEventArgs obj)
        {
            inputManager.RaiseManipulationCompleted(this, (uint)obj.source.id, obj.cumulativeDelta);
        }

        protected void OnManipulationCanceledEvent(ManipulationCanceledEventArgs obj)
        {
            inputManager.RaiseManipulationCanceled(this, (uint)obj.source.id, Vector3.zero);
        }

        protected void OnNavigationStartedEvent(NavigationStartedEventArgs obj)
        {
            inputManager.RaiseNavigationStarted(this, (uint)obj.source.id, Vector3.zero);
        }

        protected void OnNavigationUpdatedEvent(NavigationUpdatedEventArgs obj)
        {
            inputManager.RaiseNavigationUpdated(this, (uint)obj.source.id, obj.normalizedOffset);
        }

        protected void OnNavigationCompletedEvent(NavigationCompletedEventArgs obj)
        {
            inputManager.RaiseNavigationCompleted(this, (uint)obj.source.id, obj.normalizedOffset);
        }

        protected void OnNavigationCanceledEvent(NavigationCanceledEventArgs obj)
        {
            inputManager.RaiseNavigationCanceled(this, (uint)obj.source.id, Vector3.zero);
        }

        #endregion Raise GestureRecognizer Events
    }
}
