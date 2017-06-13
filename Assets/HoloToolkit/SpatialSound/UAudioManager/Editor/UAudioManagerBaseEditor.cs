// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace HoloToolkit.Unity
{
    public class UAudioManagerBaseEditor<TEvent> : Editor where TEvent : AudioEvent, new()
    {
        public static readonly string[] posTypes = { "2D", "3D", "Spatial Sound" };
        private const string PlayablesGameObjectName = "Playables";
        private const string RTPCGameObjectName = "RTPC";

        protected UAudioManagerBase<TEvent> myTarget;
        private string[] eventNames;
        private bool drawEvents = true;
        private string[] rtpcNames;
        private int selectedEventIndex = 0;
        private int selectedRTPCIndex = 0;
        private int selectedRTPCInEventIndex = 0;
        private bool showRTPCs = true;
        private bool showEventRTPCs = true;
        private bool allowLooping;
        private GUIStyle soundElementArrowsStyle;

        private Rect editorCurveSize = new Rect(0f, 0f, 1f, 1f);
        
        private GameObject playablesGameObject;
        private GameObject rtpcGameObject;

        protected void SetUpEditor()
        {
            // Having a null array of events causes too many errors and should only happen on first adding anyway.
            if (this.myTarget.EditorEvents == null)
            {
                this.myTarget.EditorEvents = new TEvent[0];
            }
            if (this.myTarget.editorRTPCs == null)
            {
                this.myTarget.editorRTPCs = new RTPC[0];
            }

            if (this.playablesGameObject == null)
            {
                this.playablesGameObject = FindOrCreateChildGameObject(PlayablesGameObjectName);
            }

            if (this.rtpcGameObject == null)
            {
                this.rtpcGameObject = FindOrCreateChildGameObject(RTPCGameObjectName);
            }

            soundElementArrowsStyle = new GUIStyle();
            soundElementArrowsStyle.fixedWidth = 20;
            
            this.eventNames = new string[this.myTarget.EditorEvents.Length];
            UpdateEventNames(this.myTarget.EditorEvents);
            UpdateRTPCNames();
        }

        protected void DrawInspectorGUI(bool showEmitters)
        {
            this.serializedObject.Update();
            EditorGUI.BeginChangeCheck();
            DrawEventHeader();

            if (this.drawEvents && this.myTarget.EditorEvents != null && this.myTarget.EditorEvents.Length > 0)
            {
                // Display current event in dropdown.
                EditorGUI.indentLevel++;
                this.selectedEventIndex = EditorGUILayout.Popup(this.selectedEventIndex, this.eventNames);

                if (this.selectedEventIndex < this.myTarget.EditorEvents.Length)
                {
                    TEvent selectedEvent;

                    selectedEvent = this.myTarget.EditorEvents[this.selectedEventIndex];                    
                    SerializedProperty selectedEventProperty = this.serializedObject.FindProperty("events").GetArrayElementAtIndex(this.selectedEventIndex);
                    EditorGUILayout.Space();

                    if (selectedEventProperty != null)
                    {
                        DrawEventInspector(selectedEventProperty, selectedEvent, this.myTarget.EditorEvents, showEmitters);

                        bool wasRemoved;
                        this.allowLooping = true;
                        DrawContainerInspector(selectedEvent.container, false, out wasRemoved);
                    }

                    EditorGUI.indentLevel--;
                }
            }

            DrawRTPCHeader();
            DrawRTPCInspector();

            EditorGUI.EndChangeCheck();
            this.serializedObject.ApplyModifiedProperties();

            if (GUI.changed)
            {
                EditorUtility.SetDirty(this.myTarget);
            }
        }

        private void DrawEventHeader()
        {
            // Add or remove current event.
            EditorGUILayout.Space();
            EditorGUILayout.BeginHorizontal();

            this.drawEvents = EditorGUILayout.Foldout(this.drawEvents, "Events");

            if (this.drawEvents)
            {
                using (new EditorGUI.DisabledScope((this.myTarget.EditorEvents != null) && (this.myTarget.EditorEvents.Length < 1)))
                {
                    if (GUILayout.Button("Remove Event"))
                    {
                        this.myTarget.EditorEvents = RemoveAudioEvent(this.myTarget.EditorEvents, this.selectedEventIndex);
                    }
                }

                if (GUILayout.Button("Add Event"))
                {
                    this.myTarget.EditorEvents = AddAudioEvent(this.myTarget.EditorEvents);
                }
            }

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space();
        }

        private void DrawEventInspector(SerializedProperty selectedEventProperty, TEvent selectedEvent, TEvent[] EditorEvents, bool showEmitters)
        {
            // Get current event's properties.
            EditorGUILayout.PropertyField(selectedEventProperty.FindPropertyRelative("name"));

            if (selectedEvent.container == null)
            {
                selectedEvent.container = playablesGameObject.AddComponent<AudioContainer>();
                this.serializedObject.ApplyModifiedProperties();
            }

            if (selectedEvent.name != this.eventNames[this.selectedEventIndex])
            {
                UpdateEventNames(EditorEvents);
            }

            if (showEmitters)
            {
                EditorGUILayout.PropertyField(selectedEventProperty.FindPropertyRelative("primarySource"));
                if (selectedEvent.container.IsContinuous)
                {
                    EditorGUILayout.PropertyField(selectedEventProperty.FindPropertyRelative("secondarySource"));
                }
            }

            // Positioning
            selectedEvent.spatialization = (SpatialPositioningType)EditorGUILayout.Popup("Positioning", (int)selectedEvent.spatialization, posTypes);

            if (selectedEvent.spatialization == SpatialPositioningType.SpatialSound)
            {
                EditorGUILayout.PropertyField(selectedEventProperty.FindPropertyRelative("roomSize"));
                EditorGUILayout.PropertyField(selectedEventProperty.FindPropertyRelative("minGain"));
                EditorGUILayout.PropertyField(selectedEventProperty.FindPropertyRelative("maxGain"));
                EditorGUILayout.PropertyField(selectedEventProperty.FindPropertyRelative("unityGainDistance"));
                EditorGUILayout.Space();
            }
            else if (selectedEvent.spatialization == SpatialPositioningType.ThreeD)
            {
                //Quick this : needs an update or the serialized object is not saving the threeD value
                this.serializedObject.Update();

                float curveHeight = 30f;
                float curveWidth = 300f;

                //Simple 3D Sounds properties
                EditorGUILayout.PropertyField(selectedEventProperty.FindPropertyRelative("maxDistanceAttenuation3D"));

                //volume attenuation
                selectedEventProperty.FindPropertyRelative("attenuationCurve").animationCurveValue = EditorGUILayout.CurveField("Attenuation", selectedEventProperty.FindPropertyRelative("attenuationCurve").animationCurveValue, Color.red, editorCurveSize, GUILayout.Height(curveHeight), GUILayout.Width(curveWidth), GUILayout.ExpandHeight(false), GUILayout.ExpandWidth(true));
                //Spatial green
                selectedEventProperty.FindPropertyRelative("spatialCurve").animationCurveValue = EditorGUILayout.CurveField("Spatial", selectedEventProperty.FindPropertyRelative("spatialCurve").animationCurveValue, Color.green, editorCurveSize, GUILayout.Height(curveHeight), GUILayout.Width(curveWidth), GUILayout.ExpandHeight(false), GUILayout.ExpandWidth(true));
                //spread lightblue
                selectedEventProperty.FindPropertyRelative("spreadCurve").animationCurveValue = EditorGUILayout.CurveField("Spread", selectedEventProperty.FindPropertyRelative("spreadCurve").animationCurveValue, Color.blue, editorCurveSize, GUILayout.Height(curveHeight), GUILayout.Width(curveWidth), GUILayout.ExpandHeight(false), GUILayout.ExpandWidth(true));
                //lowpass purple
                selectedEventProperty.FindPropertyRelative("lowPassCurve").animationCurveValue = EditorGUILayout.CurveField("LowPass", selectedEventProperty.FindPropertyRelative("lowPassCurve").animationCurveValue, Color.magenta, editorCurveSize, GUILayout.Height(curveHeight), GUILayout.Width(curveWidth), GUILayout.ExpandHeight(false), GUILayout.ExpandWidth(true));
                //Yellow reverb
                selectedEventProperty.FindPropertyRelative("reverbCurve").animationCurveValue = EditorGUILayout.CurveField("Reverb", selectedEventProperty.FindPropertyRelative("reverbCurve").animationCurveValue, Color.yellow, editorCurveSize, GUILayout.Height(curveHeight), GUILayout.Width(curveWidth), GUILayout.ExpandHeight(false), GUILayout.ExpandWidth(true));

                EditorGUILayout.Space();
            } 

            // Bus
            EditorGUILayout.PropertyField(selectedEventProperty.FindPropertyRelative("bus"));

            // Fades
            if (!selectedEvent.container.IsContinuous)
            {
                EditorGUILayout.PropertyField(selectedEventProperty.FindPropertyRelative("fadeInTime"));
                EditorGUILayout.PropertyField(selectedEventProperty.FindPropertyRelative("fadeOutTime"));
            }

            // Pitch Settings
            EditorGUILayout.PropertyField(selectedEventProperty.FindPropertyRelative("pitchCenter"));
            EditorGUILayout.PropertyField(selectedEventProperty.FindPropertyRelative("pitchRandomization"));

            // Volume settings
            EditorGUILayout.PropertyField(selectedEventProperty.FindPropertyRelative("volumeCenter"));
            EditorGUILayout.PropertyField(selectedEventProperty.FindPropertyRelative("volumeRandomization"));

            // Pan Settings
            if (selectedEvent.spatialization == SpatialPositioningType.TwoD)
            {
                EditorGUILayout.PropertyField(selectedEventProperty.FindPropertyRelative("panCenter"));
                EditorGUILayout.PropertyField(selectedEventProperty.FindPropertyRelative("panRandomization"));
            }
            // Instancing
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PropertyField(selectedEventProperty.FindPropertyRelative("instanceLimit"));
            EditorGUILayout.PropertyField(selectedEventProperty.FindPropertyRelative("instanceTimeBuffer"));
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.PropertyField(selectedEventProperty.FindPropertyRelative("instanceBehavior"));

            // Event RTPC Settings
            if (selectedEvent.rtpcs == null)
            {
                selectedEvent.rtpcs = new RTPC[0];
            }

            this.showEventRTPCs = EditorGUILayout.Foldout(this.showEventRTPCs, "Event RTPCs");

            if (this.showEventRTPCs)
            {
                EditorGUILayout.BeginHorizontal();
                this.selectedRTPCInEventIndex = EditorGUILayout.Popup(this.selectedRTPCInEventIndex, this.rtpcNames);

                EditorGUI.BeginDisabledGroup(this.myTarget.editorRTPCs == null || this.myTarget.editorRTPCs.Length == 0);
                if (GUILayout.Button("Add RTPC"))
                {
                    selectedEvent.rtpcs = AddElement(selectedEvent.rtpcs, this.myTarget.editorRTPCs[this.selectedRTPCInEventIndex]);
                }
                EditorGUI.EndDisabledGroup();

                EditorGUILayout.EndHorizontal();

                for (int i = 0; i < selectedEvent.rtpcs.Length; i++)
                {
                    EditorGUILayout.BeginHorizontal();

                    string name = selectedEvent.rtpcs[i] != null ? selectedEvent.rtpcs[i].RTPCName : "Removed";
                    EditorGUILayout.LabelField(name);
                    if (GUILayout.Button("Remove RTPC"))
                    {
                        selectedEvent.rtpcs = RemoveElement(selectedEvent.rtpcs, i);
                        break;
                    }
                    EditorGUILayout.EndHorizontal();
                }
            }

            // Container
            EditorGUILayout.Space();
        }
        
        private void DrawContainerInspector(AudioContainer audioContainer, bool canRemove, out bool wasRemoved)
        {
            bool addedSound = false;

            SerializedObject audioContainedSerializedObject = new SerializedObject(audioContainer);
            audioContainedSerializedObject.Update();

            EditorGUILayout.BeginHorizontal();

            EditorGUILayout.PropertyField(audioContainedSerializedObject.FindProperty("containerType"));

            if (canRemove)
            {
                if (GUILayout.Button("Remove Container"))
                {
                    wasRemoved = true;
                    audioContainedSerializedObject.Dispose();

                    EditorGUILayout.EndHorizontal();

                    RemoveAllSounds(audioContainer);
                    return;
                }
            }

            EditorGUILayout.EndHorizontal();

            if (!audioContainer.IsContinuous)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.PropertyField(audioContainedSerializedObject.FindProperty("delayCenter"));
                EditorGUILayout.PropertyField(audioContainedSerializedObject.FindProperty("delayRandomization"));

                EditorGUILayout.EndHorizontal();

                if (allowLooping)
                {
                    EditorGUILayout.PropertyField(audioContainedSerializedObject.FindProperty("loop"));

                    if (audioContainer.Loop)
                    {
                        EditorGUILayout.PropertyField(audioContainedSerializedObject.FindProperty("loopTime"));
                    }
                }
            }
            else
            {
                EditorGUILayout.PropertyField(audioContainedSerializedObject.FindProperty("crossfadeTime"));
            }

            // Sounds

            if (audioContainer.isExpanded)
            {
                EditorGUILayout.Space();
            }

            EditorGUILayout.BeginHorizontal();
            audioContainer.isExpanded = EditorGUILayout.Foldout(audioContainer.isExpanded, "Sounds");

            if (audioContainer.isExpanded)
            {
                if (GUILayout.Button("Add Audio Clip"))
                {
                    AddAudioClip(audioContainer, audioContainedSerializedObject);

                    // Skip drawing sound inspector after adding a new sound.
                    addedSound = true;
                }
                else if (GUILayout.Button("Add Container"))
                {
                    AddAudioContainer(audioContainer, audioContainedSerializedObject);

                    // Skip drawing sound inspector after adding a new sound.
                    addedSound = true;
                }
            }

            EditorGUILayout.EndHorizontal();

            if (!addedSound && audioContainer.isExpanded)
            {
                EditorGUI.indentLevel++;
                DrawSoundClipInspector(audioContainer, audioContainedSerializedObject);
                EditorGUI.indentLevel--;
            }

            audioContainedSerializedObject.ApplyModifiedProperties();
            wasRemoved = false;
        }

        private void RemoveAllSounds(AudioContainer audioContainer)
        {
            if (audioContainer.sounds != null)
            {
                foreach (UPlayable playable in audioContainer.sounds)
                {
                    if (playable is AudioContainer)
                    {
                        RemoveAllSounds((AudioContainer)playable);
                    }

                    DestroyImmediate(playable);
                }
            }
        }

        private void DrawSoundClipInspector(AudioContainer audioContainer, SerializedObject audioContainerSerializedObject)
        {
            bool oldAllowLooping = allowLooping;
            allowLooping = allowLooping && !audioContainer.Loop && !audioContainer.IsContinuous;

            for (int i = 0; audioContainer.sounds != null && i < audioContainer.sounds.Length; i++)
            {
                if (audioContainer.sounds[i] == null)
                {
                    UAudioClip audioClip = this.playablesGameObject.AddComponent<UAudioClip>();
                    audioContainer.sounds[i] = audioClip;
                }

                UPlayable currentSound = audioContainer.sounds[i];
                
                EditorGUILayout.Space();
                EditorGUILayout.Space();

                bool wasRemoved = false;

                EditorGUILayout.BeginHorizontal();

                soundElementArrowsStyle.margin.top = allowLooping ? 10 : 0;

                EditorGUILayout.BeginVertical(soundElementArrowsStyle);

                EditorGUI.BeginDisabledGroup(i <= 0);
                if (GUILayout.Button("▲", GUILayout.Width(20), GUILayout.Height(15)))
                {
                    MoveSound(audioContainer, audioContainerSerializedObject, i, -1);
                }
                EditorGUI.EndDisabledGroup(); 

                EditorGUI.BeginDisabledGroup(i >= audioContainer.sounds.Length - 1);
                if (GUILayout.Button("▼", GUILayout.Width(20), GUILayout.Height(15)))
                {
                    MoveSound(audioContainer, audioContainerSerializedObject, i, 1);
                }
                EditorGUI.EndDisabledGroup();

                EditorGUILayout.EndVertical();

                EditorGUILayout.BeginVertical();

                if (currentSound is UAudioClip)
                {
                    DrawAudioClipInspector(audioContainer, (UAudioClip) currentSound, out wasRemoved);
                }
                else if (currentSound is AudioContainer)
                {
                    DrawContainerInspector((AudioContainer) currentSound, true, out wasRemoved);
                }
                
                EditorGUILayout.EndVertical();
                EditorGUILayout.EndHorizontal();

                if (wasRemoved)
                {
                    audioContainerSerializedObject.FindProperty("sounds").GetArrayElementAtIndex(i).DeleteCommand();
                    audioContainerSerializedObject.FindProperty("sounds").DeleteArrayElementAtIndex(i);

                    DestroyImmediate(currentSound);
                    RemoveAt(audioContainer.sounds, i);
                }
            }

            allowLooping = oldAllowLooping;
        }

        private void DrawAudioClipInspector(AudioContainer audioContainer, UAudioClip currentSound, out bool wasRemoved)
        {
            EditorGUILayout.BeginHorizontal();

            SerializedObject playableSO = new SerializedObject(currentSound);
            playableSO.Update();

            EditorGUILayout.PropertyField(playableSO.FindProperty("sound"));

            if (GUILayout.Button("Remove Audio Clip"))
            {
                playableSO.Dispose();

                EditorGUILayout.EndHorizontal();

                wasRemoved = true;
                return;
            }

            EditorGUILayout.EndHorizontal();

            if (!audioContainer.IsContinuous)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.PropertyField(playableSO.FindProperty("delayCenter"));
                EditorGUILayout.PropertyField(playableSO.FindProperty("delayRandomization"));

                EditorGUILayout.EndHorizontal();

                // Disable looping audio clips in a simultaneous container.
                // Looping containers within a simultaneous container is allowed.
                if (allowLooping && !audioContainer.IsSimultaneous)
                {
                    EditorGUILayout.PropertyField(playableSO.FindProperty("loop"));
                }
                else
                {
                    currentSound.Loop = false;
                }
            }

            playableSO.ApplyModifiedProperties();
            wasRemoved = false;
        }

        private void DrawRTPCHeader()
        {
            EditorGUILayout.Space();

            EditorGUILayout.BeginHorizontal();

            this.showRTPCs = EditorGUILayout.Foldout(this.showRTPCs, "RTPCs");

            if (this.showRTPCs)
            {
                using (new EditorGUI.DisabledScope((this.myTarget.editorRTPCs == null) || (this.myTarget.editorRTPCs.Length == 0)))
                {
                    if (GUILayout.Button("Remove RTPC"))
                    {
                        RTPC rtpcToRemove = this.myTarget.editorRTPCs[this.selectedRTPCIndex];

                        this.serializedObject.FindProperty("rtpcs").GetArrayElementAtIndex(this.selectedRTPCIndex).DeleteCommand();
                        this.serializedObject.FindProperty("rtpcs").DeleteArrayElementAtIndex(this.selectedRTPCIndex);
                        this.serializedObject.ApplyModifiedProperties();

                        DestroyImmediate(rtpcToRemove);
                        
                        UpdateRTPCNames();

                        // Remove references to this RTPC from all existing events.
                        for (int i = 0; i < this.myTarget.EditorEvents.Length; i++)
                        {
                            this.myTarget.EditorEvents[i].rtpcs = RemoveElement(this.myTarget.EditorEvents[i].rtpcs, rtpcToRemove);
                        }

                        this.serializedObject.Update();
                        
                        if (this.selectedRTPCIndex >= this.myTarget.editorRTPCs.Length)
                        {
                            this.selectedRTPCIndex = this.myTarget.editorRTPCs.Length - 1;
                        }
                    }
                }

                if (GUILayout.Button("Add RTPC"))
                {
                    RTPC rtpc = this.rtpcGameObject.AddComponent<RTPC>();

                    int length = this.myTarget.editorRTPCs.Length;
                    this.serializedObject.FindProperty("rtpcs").InsertArrayElementAtIndex(length);
                    this.serializedObject.FindProperty("rtpcs").GetArrayElementAtIndex(length).objectReferenceValue = rtpc;
                    this.serializedObject.ApplyModifiedProperties();
                    UpdateRTPCNames();

                    this.selectedRTPCIndex = length;
                }
            }

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();
        }

        private void DrawRTPCInspector()
        {
            if (this.showRTPCs && this.myTarget.editorRTPCs != null && this.myTarget.editorRTPCs.Length > 0)
            {
                EditorGUI.indentLevel++;

                this.selectedRTPCIndex = EditorGUILayout.Popup(this.selectedRTPCIndex, this.rtpcNames);

                if (this.selectedRTPCIndex >= 0 && this.myTarget.editorRTPCs.Length > 0)
                {
                    RTPC currentRTPC = this.myTarget.editorRTPCs[this.selectedRTPCIndex];
                    SerializedObject rtpcSerializedObject = new SerializedObject(currentRTPC);
                    DisplayAllProperties(rtpcSerializedObject);

                    // Update the name for the current RTPC so that it updates as the user modifies it.
                    // This allows us not to update every name every frame.
                    this.rtpcNames[this.selectedRTPCIndex] = currentRTPC.RTPCName;
                }

                EditorGUI.indentLevel--;
            }
        }

        private void UpdateEventNames(TEvent[] EditorEvents)
        {
            HashSet<string> previousEventNames = new HashSet<string>();

            for (int i = 0; i < EditorEvents.Length; i++)
            {
                if (string.IsNullOrEmpty(EditorEvents[i].name))
                {
                    EditorEvents[i].name = "_NewEvent" + i.ToString();
                }

                while (previousEventNames.Contains(EditorEvents[i].name))
                {
                    EditorEvents[i].name = "_" + EditorEvents[i].name;
                }

                this.eventNames[i] = EditorEvents[i].name;
                previousEventNames.Add(this.eventNames[i]);
            }
        }

        private void UpdateRTPCNames()
        {
            HashSet<string> previousRTPCNames = new HashSet<string>();
            this.rtpcNames = new string[this.myTarget.editorRTPCs.Length];

            for (int i = 0; i < this.myTarget.editorRTPCs.Length; i++)
            {
                RTPC rtpc = this.myTarget.editorRTPCs[i];

                if (string.IsNullOrEmpty(rtpc.RTPCName))
                {
                    rtpc.RTPCName = "_NewRTPC";
                }

                while (previousRTPCNames.Contains(rtpc.RTPCName))
                {
                    rtpc.RTPCName = "_" + rtpc.RTPCName;
                }

                this.rtpcNames[i] = rtpc.RTPCName;
                previousRTPCNames.Add(this.rtpcNames[i]);
            }
        }

        private void AddAudioClip(AudioContainer audioContainer, SerializedObject audioContainerSO)
        {
            UAudioClip audioClip = this.playablesGameObject.AddComponent<UAudioClip>();

            AddSound(audioContainer, audioContainerSO, audioClip);
        }

        private void AddAudioContainer(AudioContainer audioContainer, SerializedObject audioContainerSO)
        {
            AudioContainer newContainer = this.playablesGameObject.AddComponent<AudioContainer>();

            AddSound(audioContainer, audioContainerSO, newContainer);
        }

        private void AddSound(AudioContainer audioContainer, SerializedObject audioContainerSO, UPlayable newPlayable)
        {
            if (audioContainer.sounds == null)
            {
                audioContainer.sounds = new UPlayable[0];
            }

            audioContainerSO.FindProperty("sounds").InsertArrayElementAtIndex(audioContainer.sounds.Length);
            audioContainerSO.FindProperty("sounds").GetArrayElementAtIndex(audioContainer.sounds.Length).objectReferenceValue = newPlayable;
        }

        private void MoveSound(AudioContainer audioContainer, SerializedObject audioContainerSO, int soundIndex, int positionDiff)
        {
            int newIndex = soundIndex + positionDiff;
            if (soundIndex >= 0 && soundIndex < audioContainer.sounds.Length &&
                newIndex >= 0 && newIndex < audioContainer.sounds.Length)
            {
                audioContainerSO.FindProperty("sounds").MoveArrayElement(soundIndex, soundIndex + positionDiff);
            }
        }

        private TEvent[] AddAudioEvent(TEvent[] EditorEvents)
        {
            int arrayLength = EditorEvents != null ? EditorEvents.Length + 1 : 1;
            TEvent tempEvent = new TEvent();
            TEvent[] tempEventArray = new TEvent[arrayLength];
            tempEvent.container = this.playablesGameObject.AddComponent<AudioContainer>();
            EditorEvents.CopyTo(tempEventArray, 0);
            tempEventArray[EditorEvents.Length] = tempEvent;
            this.eventNames = new string[tempEventArray.Length];
            UpdateEventNames(tempEventArray);
            this.selectedEventIndex = this.eventNames.Length - 1;
            return tempEventArray;
        }

        private TEvent[] RemoveAudioEvent(TEvent[] editorEvents, int eventToRemove)
        {
            editorEvents = RemoveElement(editorEvents, eventToRemove);
            this.eventNames = new string[editorEvents.Length];
            UpdateEventNames(editorEvents);

            if (this.selectedEventIndex >= editorEvents.Length)
            {
                this.selectedEventIndex--;
            }

            return editorEvents;
        }

        private GameObject FindOrCreateChildGameObject(string name)
        {
            Transform transform = this.myTarget.gameObject.transform.Find(name);
            GameObject gameObject;
            if (transform == null)
            {
                gameObject = new GameObject(name);
                gameObject.transform.parent = this.myTarget.gameObject.transform;
            }
            else
            {
                gameObject = transform.gameObject;
            }

            return gameObject;
        }

        public static T[] AddElement<T>(T[] array, T newElement)
        {
            if (array == null)
            {
                T[] newArray = new T[1];
                newArray[0] = newElement;
                return newArray;
            }
            else
            {
                T[] newArray = new T[array.Length + 1];
                array.CopyTo(newArray, 0);
                newArray[array.Length] = newElement;
                return newArray;
            }
        }

        /// <summary>
        /// Returns a new array that has the item at the given index removed.
        /// </summary>
        /// <param name="array">The array.</param>
        /// <param name="index">Index to remove.</param>
        /// <returns>The new array.</returns>
        public static T[] RemoveElement<T>(T[] array, int index)
        {
            T[] newArray = new T[array.Length - 1];

            for (int i = 0; i < array.Length; i++)
            {
                // Send the clip to the previous item in the new array if we have passed the removed clip.
                if (i > index)
                {
                    newArray[i - 1] = array[i];
                }
                else if (i < index)
                {
                    newArray[i] = array[i];
                }
            }

            return newArray;
        }

        /// <summary>
        /// Returns a new array that has the given item removed.
        /// </summary>
        /// <param name="array">The array.</param>
        /// <param name="element">Element to remove.</param>
        /// <returns>The new array.</returns>
        public static T[] RemoveElement<T>(T[] array, T element) where T : class
        {
            int numObjectsToRemove = array.Count(current => current == element);

            T[] newArray = new T[array.Length - numObjectsToRemove];
            int newArrayIndex = 0;
            for (int i = 0; i < array.Length; i++)
            {
                if (array[i] != element)
                {
                    newArray[newArrayIndex] = array[i];
                    newArrayIndex++;
                }
            }

            return newArray;
        }

        /// <summary>
        /// Returns anew array that has the item at the given index removed.
        /// </summary>
        /// <typeparam name="T">Type of the array elements.</typeparam>
        /// <param name="array">The array.</param>
        /// <param name="index">Index to remove.</param>
        /// <returns>The new array.</returns>
        public static T[] RemoveAt<T>(T[] array, int index)
        {
            T[] newArray = new T[array.Length - 1];
            for (int i = 0; i < array.Length; i++)
            {
                //Send the clip to the previous item in the new array if we have passed the removed clip
                if (i > index)
                {
                    newArray[i - 1] = array[i];
                }
                else if (i < index)
                {
                    newArray[i] = array[i];
                }
            }

            return newArray;
        }

        private static void DisplayAllProperties(SerializedObject serializedObject)
        {
            serializedObject.Update();

            SerializedProperty rtpcIterator = serializedObject.GetIterator();

            // Skip script property.
            rtpcIterator.NextVisible(true);

            while (rtpcIterator.NextVisible(true))
            {
                EditorGUILayout.PropertyField(rtpcIterator);
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}