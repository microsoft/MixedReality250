// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Collections;
using UnityEngine;

namespace HoloToolkit.Unity
{
    /// <summary>
    /// Encapsulate a single Unity AudioClip with playback settings.
    /// </summary>
    public class UAudioClip : UPlayable
    {
        public AudioClip sound = null;

        private AudioSource audioSource;

        public override IEnumerator PlayAsync(ActiveEvent activeEvent)
        {
            BeginPlay();

            if (sound == null)
            {
                Debug.LogErrorFormat(this, "Trying to play empty sound for event \"{0}\".", activeEvent.audioEvent.name);

                yield break;
            }
            
            this.audioSource = activeEvent.CurrentAudioSource;

            if (this.CurrentDelay > 0.0f)
            {
                yield return new WaitForSeconds(this.CurrentDelay);    
            }

            if (!activeEvent.StopRequested)
            {
                if (PlaySimultaneous)
                {
                    audioSource.PlayOneShot(sound);
                }
                else
                {
                    audioSource.PlayClip(sound, Loop);
                }
            }
        }

        public override float GetLength()
        {
            if (sound == null)
            {
                return 0f;
            }
            
            float pitchAdjustedClipLength = sound.length;
            if (audioSource != null && audioSource.pitch != 0)
            {
                pitchAdjustedClipLength /= audioSource.pitch;
            }

            // Restrict non-looping ActiveTime values to be non-negative.
            return pitchAdjustedClipLength + CurrentDelay;
        }

        public override bool IsEmpty()
        {
            return sound == null;
        }
    }
}