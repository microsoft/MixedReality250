// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Collections;
using UnityEngine;

namespace HoloToolkit.Unity
{
    /// <summary>
    /// Currently active AudioEvents along with their AudioSource components for instance limiting events
    /// </summary>
    public class ActiveEvent : MonoBehaviour
    {
        public static AnimationCurve SpatialRolloff;

        public event Action OnPlayCompleted;

        public AudioEvent audioEvent = null;
        public float volDest = 1;
        public float altVolDest = 1;
        public float pitchDest = 1;
        public float panDest = 1;
        public float currentFade = 0;
        public bool isActiveTimeComplete = false;

        private bool initialized = false;
        private bool started = false;

        private AudioSourcesReference audioSourcesReference = null;

        private AudioSource primarySource = null;
        public AudioSource PrimarySource
        {
            get
            {
                if (primarySource == null)
                {
                    primarySource = GetOrCreateAudioSource();
                }

                return primarySource;
            }

            private set
            {
                primarySource = value;

                if (primarySource != null)
                {
                    SetSourceProperties(primarySource);
                    primarySource.enabled = true;
                }
            }
        }

        private AudioSource secondarySource = null;
        public AudioSource SecondarySource
        {
            get
            {
                if (secondarySource == null)
                {
                    secondarySource = GetOrCreateAudioSource();
                }

                return secondarySource;
            }

            private set
            {
                secondarySource = value;

                if (secondarySource != null)
                {
                    SetSourceProperties(secondarySource);
                    secondarySource.enabled = true;
                }
            }
        }

        public AudioSource CurrentAudioSource
        {
            get
            {
                AudioSource audioSource = IsPlayingSecondary ? SecondarySource : PrimarySource;

                audioSource.enabled = true;
                return audioSource;
            }
        }
        
        public bool IsPlayingSecondary { get; set; }

        public bool IsPlaying
        {
            get
            {
                return
                    (primarySource != null && primarySource.isPlaying) ||
                    (secondarySource != null && secondarySource.isPlaying);
            }
        }

        public GameObject AudioEmitter { get; private set; }

        public bool StopRequested { get; private set; }


        private void Start()
        {
            if (!initialized)
            {
                throw new InvalidOperationException("Call Initialize on this component as soon as it's instantiated.");
            }
        }

        private void Update()
        {
            if (started)
            {
                SetRTPCs();

                UpdateEmitterVolumes();

                // If there is no time left in the fade, make sure we are set to the destination volume.
                if (currentFade > 0)
                {
                    currentFade -= Time.deltaTime;
                }
            }
        }
        
        public void Initialize(AudioEvent audioEvent, GameObject emitter, AudioSource primarySource = null, AudioSource secondarySource = null)
        {
            this.audioEvent = audioEvent;
            AudioEmitter = emitter;

            audioSourcesReference = AudioEmitter.GetComponent<AudioSourcesReference>();

            if (audioSourcesReference == null)
            {
                audioSourcesReference = AudioEmitter.AddComponent<AudioSourcesReference>();
            }

            SetEventProperties();

            this.PrimarySource = primarySource;
            this.SecondarySource = secondarySource;

            initialized = true;
        }

        /// <summary>
        /// Sets the pitch value for the primary source.
        /// </summary>
        /// <param name="newPitch">The value to set the pitch, between 0 (exclusive) and 3 (inclusive).</param>
        public void SetPitch(float newPitch)
        {
            if (newPitch <= 0 || newPitch > 3)
            {
                Debug.LogErrorFormat("Invalid pitch {0} set for event", newPitch);
                return;
            }

            if (this.primarySource != null)
            {
                this.primarySource.pitch = newPitch;
            }

            if (this.secondarySource != null)
            {
                this.secondarySource.pitch = newPitch;
            }
        }

        public void StartEvent()
        {
            if (started)
            {
                throw new InvalidOperationException("Can't start an active event twice.");
            }

            IsPlayingSecondary = false;
            started = true;

            StartCoroutine(audioEvent.container.PlayAsync(this));
            StartCoroutine(TrackEventLifetimeCoroutine());
        }

        public void StopEvent(float? fadeTime = null)
        {
            if (StopRequested)
            {
                return;
            }

            StopRequested = true;

            if (fadeTime.HasValue)
            {
                StartCoroutine(StopEventCoroutine(fadeTime));
            }
            else
            {
                DestroyEvent();
            }
        }

        private IEnumerator StopEventCoroutine(float? fadeTime)
        {
            if (fadeTime != null)
            {
                volDest = 0f;
                altVolDest = 0f;
                currentFade = fadeTime.Value;

                yield return new WaitForSeconds(fadeTime.Value);
            }

            DestroyEvent();
        }

        private void DestroyEvent()
        {
            StopAllCoroutines();

            IsPlayingSecondary = false;

            if (primarySource != null)
            {
                primarySource.Stop();
                primarySource.enabled = false;
                primarySource = null;
            }

            if (secondarySource != null)
            {
                secondarySource.Stop();
                secondarySource.enabled = false;
                secondarySource = null;
            }

            if (OnPlayCompleted != null)
            {
                OnPlayCompleted();
            }

            Destroy(this);
        }

        private void SetEventProperties()
        {
            if (audioEvent.pitchRandomization != 0)
            {
                pitchDest = UnityEngine.Random.Range(audioEvent.pitchCenter - audioEvent.pitchRandomization, audioEvent.pitchCenter + audioEvent.pitchRandomization);
            }
            else
            {
                pitchDest = audioEvent.pitchCenter;
            }

            float vol = 1f;
            if (audioEvent.fadeInTime > 0)
            {
                currentFade = audioEvent.fadeInTime;
                if (audioEvent.volumeRandomization != 0)
                {
                    vol = UnityEngine.Random.Range(audioEvent.volumeCenter - audioEvent.volumeRandomization, audioEvent.volumeCenter + audioEvent.volumeRandomization);
                }
                else
                {
                    vol = audioEvent.volumeCenter;
                }
                volDest = vol;
            }
            else
            {
                if (audioEvent.volumeRandomization != 0)
                {
                    vol = UnityEngine.Random.Range(audioEvent.volumeCenter - audioEvent.volumeRandomization, audioEvent.volumeCenter + audioEvent.volumeRandomization);
                }
                else
                {
                    vol = audioEvent.volumeCenter;
                }
                volDest = vol;
            }

            if (audioEvent.panRandomization != 0)
            {
                panDest = UnityEngine.Random.Range(audioEvent.panCenter - audioEvent.panRandomization, audioEvent.panCenter + audioEvent.panRandomization);
            }
            else
            {
                panDest = audioEvent.panCenter;
            }
        }

        private AudioSource GetOrCreateAudioSource()
        {
            AudioSource audioSource = audioSourcesReference.GetUnusedAudioSource();
            SetSourceProperties(audioSource);
            audioSource.enabled = true;

            return audioSource;
        }

        private void SetSourceProperties(AudioSource audioSource)
        {
            audioSource.playOnAwake = false;

            switch (audioEvent.spatialization)
            {
                case SpatialPositioningType.TwoD:
                    audioSource.spatialBlend = 0f;
                    audioSource.spatialize = false;
                    break;
                case SpatialPositioningType.ThreeD:
                    audioSource.spatialBlend = 1f;
                    audioSource.spatialize = false;
                    break;
                case SpatialPositioningType.SpatialSound:
                    audioSource.spatialBlend = 1f;
                    audioSource.spatialize = true;
                    break;
                default:
                    Debug.LogErrorFormat("Unexpected spatialization type: {0}", audioEvent.spatialization.ToString());
                    break;
            }

            if (audioEvent.spatialization == SpatialPositioningType.SpatialSound)
            {
                //audioSource.SetCustomCurve(AudioSourceCurveType.CustomRolloff, ActiveEvent.SpatialRolloff);
                SpatialSoundSettings.SetRoomSize(audioSource, audioEvent.roomSize);
                //SpatialSoundSettings.SetMinGain(audioSource, audioEvent.minGain);
                //SpatialSoundSettings.SetMaxGain(audioSource, audioEvent.maxGain);
                //SpatialSoundSettings.SetUnityGainDistance(audioSource, audioEvent.unityGainDistance);
                audioSource.rolloffMode = AudioRolloffMode.Logarithmic;
                audioSource.maxDistance = 10;// audioEvent.maxDistanceAttenuation3D;
                audioSource.SetCustomCurve(AudioSourceCurveType.CustomRolloff, audioEvent.attenuationCurve);
                audioSource.SetCustomCurve(AudioSourceCurveType.SpatialBlend, audioEvent.spatialCurve);
                audioSource.SetCustomCurve(AudioSourceCurveType.Spread, audioEvent.spreadCurve);
                audioSource.SetCustomCurve(AudioSourceCurveType.ReverbZoneMix, audioEvent.reverbCurve);
            }
            else if (audioEvent.spatialization == SpatialPositioningType.ThreeD)
            {
                audioSource.rolloffMode = AudioRolloffMode.Custom;
                audioSource.maxDistance = audioEvent.maxDistanceAttenuation3D;
                audioSource.SetCustomCurve(AudioSourceCurveType.CustomRolloff, audioEvent.attenuationCurve);
                audioSource.SetCustomCurve(AudioSourceCurveType.SpatialBlend, audioEvent.spatialCurve);
                audioSource.SetCustomCurve(AudioSourceCurveType.Spread, audioEvent.spreadCurve);
                audioSource.SetCustomCurve(AudioSourceCurveType.ReverbZoneMix, audioEvent.reverbCurve);
            }
            else
            {
                audioSource.rolloffMode = AudioRolloffMode.Logarithmic;
            }

            if (audioEvent.bus != null)
            {
                audioSource.outputAudioMixerGroup = audioEvent.bus;
            }

            audioSource.pitch = pitchDest;

            if (audioEvent.fadeInTime > 0)
            {
                audioSource.volume = 0f;
            }
            else
            {
                audioSource.volume = volDest;
            }

            audioSource.panStereo = panDest;
        }
       
        private void UpdateEmitterVolumes()
        {
            // If we have a secondary source (for crossfades) adjust the volume based on the current fade time for each active event.
            if (secondarySource != null && secondarySource.volume != altVolDest)
            {
                if (Mathf.Abs(altVolDest - secondarySource.volume) < Time.deltaTime / currentFade)
                {
                    secondarySource.volume = altVolDest;
                }
                else
                {
                    secondarySource.volume += (altVolDest - secondarySource.volume) * Time.deltaTime / currentFade;
                }
            }

            // Adjust the volume of the main source based on the current fade time for each active event.
            if (primarySource != null && primarySource.volume != volDest)
            {
                if (Mathf.Abs(volDest - primarySource.volume) < Time.deltaTime / currentFade)
                {
                    primarySource.volume = volDest;
                }
                else
                {
                    primarySource.volume += (volDest - primarySource.volume) * Time.deltaTime / currentFade;
                }
            }
        }

        public void SetRTPCs()
        {
            for (int i = 0; i < this.audioEvent.rtpcs.Length; i++)
            {
                this.audioEvent.rtpcs[i].ApplyValues(this);
            }
        }

        /// <summary>
        /// Keep an event in the "activeEvents" list for the amount of time we think it will be playing, plus the instance buffer.
        /// This is mostly done for instance limiting purposes.
        /// </summary>
        /// <returns>The coroutine.</returns>
        private IEnumerator TrackEventLifetimeCoroutine()
        {
            // Only return active time if sound is not looping/continuous.
            if (!audioEvent.container.IsInfinite)
            {
                float activeTime = audioEvent.container.GetLength();

                yield return new WaitForSeconds(activeTime);

                // Mark this event so it no longer counts against the instance limit.
                isActiveTimeComplete = true;

                // Since the activeTime estimate may not be enough time to complete the clip (due to pitch changes during playback, or a negative instanceBuffer value, for example)
                // wait here until it is finished, so that we don't cut off the end.
                while (IsPlaying && !StopRequested)
                {
                    yield return null;
                }

                StopEvent();
            }
        }

        /// <summary>
        /// Creates a flat animation curve to negate Unity's distance attenuation when using Spatial Sound
        /// </summary>
        public static void CreateFlatSpatialRolloffCurve()
        {
            if (SpatialRolloff != null)
            {
                return;
            }
            SpatialRolloff = new AnimationCurve();
            SpatialRolloff.AddKey(0, 1);
            SpatialRolloff.AddKey(1, 1);
        }
    }
}