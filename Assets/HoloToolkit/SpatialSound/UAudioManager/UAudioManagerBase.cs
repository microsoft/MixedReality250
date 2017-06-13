// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace HoloToolkit.Unity
{
    /// <summary>
    /// UAudioManagerBase provides the base functionality for UAudioManager classes.
    /// </summary>
    /// <typeparam name="TEvent">The type of AudioEvent being managed.</typeparam>
    /// <remarks>The TEvent type specified must derive from AudioEvent.</remarks>
    public partial class UAudioManagerBase<TEvent> : MonoBehaviour where TEvent : AudioEvent, new()
    {
        private const string ActiveEventsGameObjectName = "ActiveEvents";

        [SerializeField]
        protected TEvent[] events = null;
        [SerializeField]
        protected RTPC[] rtpcs;

        protected List<ActiveEvent> activeEvents;

        public GameObject ActiveEventsGameObject { get; private set; }

        public RTPC[] rtpcList { get { return rtpcs; } }

#if UNITY_EDITOR
        public TEvent[] EditorEvents { get { return events; } set { events = value; } }
        public List<ActiveEvent> ProfilerEvents { get { return activeEvents; } }
        public RTPC[] editorRTPCs { get { return rtpcs; } set { rtpcs = value; } }
#endif

        protected void Awake()
        {
            activeEvents = new List<ActiveEvent>();
            ActiveEventsGameObject = new GameObject(ActiveEventsGameObjectName);
            ActiveEventsGameObject.transform.SetParent(transform);

            SetRTPCDefaults();
        }

        protected void OnDestroy()
        {
            StopAllEvents();
        }

        /// <summary>
        /// Stops all ActiveEvents
        /// </summary>
        public void StopAllEvents()
        {
            if (activeEvents == null)
            {
                return;
            }

            for (int i = activeEvents.Count - 1; i >= 0; i--)
            {
                StopEvent(activeEvents[i]);
            }
        }

        /// <summary>
        /// Fades out all of the events over fadeTime and stops once completely faded out.
        /// </summary>
        /// <param name="fadeTime">The amount of time, in seconds, to fade between current volume and 0.</param>
        public void StopAllEvents(float fadeTime)
        {
            for (int i = activeEvents.Count - 1; i >= 0; i--)
            {
                activeEvents[i].StopEvent(fadeTime);
            }
        }

        /// <summary>
        /// Stops all events on a single emitter.
        /// </summary>
        public void StopAllEvents(GameObject emitter)
        {
            for (int i = activeEvents.Count - 1; i >= 0; i--)
            {
                if (activeEvents[i].AudioEmitter == emitter)
                {
                    StopEvent(activeEvents[i]);
                }
            }
        }

        public void SetRTPC(string rtpcName, float newValue)
        {
            for (int i = 0; i < this.rtpcs.Length; i++)
            {
                RTPC currentRTPC = this.rtpcs[i];
                if (currentRTPC.RTPCName == rtpcName)
                {
                    currentRTPC.value = newValue;
                }
            }
        }

        private void SetRTPCDefaults()
        {
            for (int i = 0; i < this.rtpcs.Length; i++)
            {
                RTPC currentRTPC = this.rtpcs[i];
                currentRTPC.value = currentRTPC.defaultValue;
            }
        }
      
        /// <summary>
        /// Stop audio sources in an event, and clean up instance references.
        /// </summary>
        /// <param name="activeEvent">The persistent reference to the event as long as it is playing.</param>
        protected void StopEvent(ActiveEvent activeEvent)
        {
            activeEvent.StopEvent();
            RemoveEventInstance(activeEvent);
        }

        /// <summary>
        /// Remove event from the currently active events.
        /// </summary>
        /// <param name="activeEvent">The persistent reference to the event as long as it is playing.</param>
        public void RemoveEventInstance(ActiveEvent activeEvent)
        {
            activeEvents.Remove(activeEvent);
        }

        /// <summary>
        /// Return the number of instances matching the name eventName for instance limiting check.
        /// </summary>
        /// <param name="eventName">The name of the event to check.</param>
        /// <returns>The number of instances of that event currently active.</returns>
        protected int GetInstances(string eventName)
        {
            int tempInstances = 0;

            for (int i = 0; i < activeEvents.Count; i++)
            {
                var eventInstance = activeEvents[i];

                if (!eventInstance.isActiveTimeComplete && eventInstance.audioEvent.name == eventName)
                {
                    tempInstances++;
                }
            }

            return tempInstances;
        }
    }
}