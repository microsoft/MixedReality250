// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Assertions;

namespace HoloToolkit.Unity
{
    /// <summary>
    /// The different rules for how audio should be played back.
    /// </summary>
    public enum AudioContainerType
    {
        Random,
        Sequence,
        Simultaneous,
        ContinuousSequence,
        ContinuousRandom
    }

    /// <summary>
    /// The AudioContainer class is sound container for an AudioEvent. It also specifies the rules of how to
    /// play back the contained AudioClips.
    /// </summary>
    [Serializable]
    public class AudioContainer : UPlayable
    {
        public const float InfiniteLoop = -1;

        [Tooltip("The type of the audio container.")]
        public AudioContainerType containerType = AudioContainerType.Random;
        
        public float loopTime = 0;
        public float crossfadeTime = 0f;

        public UPlayable[] sounds = null;

#if UNITY_EDITOR
        // This field is used for displaying containers within the inspector only.
        [NonSerialized]
        public bool isExpanded = true;
#endif

        // Null means we haven't played a sound yet. This value should only be used for Sequence and Random containers.
        private int? currentClipIndex = null;
        
        public override bool IsLooping
        {
            get
            {
                bool isLooping;

                if (this.IsSimultaneous)
                {
                    isLooping = this.Loop || this.sounds.Any(playable => playable.IsLooping);
                }
                else
                {
                    isLooping = this.Loop || this.CurrentPlayable.IsLooping;
                }

                return isLooping;
            }
        }

        public override bool IsInfinite
        {
            get
            {
                if (base.IsInfinite)
                {
                    return true;
                }

                switch (containerType)
                {
                    case AudioContainerType.Random:
                    case AudioContainerType.Sequence:
                        return this.CurrentPlayable.IsInfinite;
                    case AudioContainerType.ContinuousSequence:
                    case AudioContainerType.ContinuousRandom:
                        return true;
                    case AudioContainerType.Simultaneous:
                        return this.sounds.Any(playable => playable.IsInfinite);
                    default:
                        throw new NotImplementedException("A type of container is not considered.");
                }
            }
        }

        /// <summary>
        /// Is this container a continuous container?
        /// </summary>
        /// <returns>True if this AudioEvent's container is one of the continuous types (random or sequential), otherwise false.</returns>
        public bool IsContinuous
        {
            get { return containerType == AudioContainerType.ContinuousRandom || containerType == AudioContainerType.ContinuousSequence; }
        }

        public bool IsSimultaneous
        {
            get { return containerType == AudioContainerType.Simultaneous; }
        }

        private UPlayable CurrentPlayable
        {
            get { return this.sounds[this.currentClipIndex.Value]; }
        }

        public override void PrepareForPlay()
        {
            base.PrepareForPlay();
            
            if (this.IsSimultaneous)
            {
                for (int i = 0; i < this.sounds.Length; i++)
                {
                    this.sounds[i].PrepareForPlay();
                }
            }
            else
            {
                this.MoveToNextPlayable();
                this.CurrentPlayable.PrepareForPlay();
            }
        }

        /// <summary>
        /// Determine which rules to follow for container playback, and begin the appropriate function.
        /// </summary>
        public override IEnumerator PlayAsync(ActiveEvent activeEvent)
        {
            BeginPlay();

            if (sounds.Length == 0)
            {
                Debug.LogErrorFormat(this, "Trying to play container \"{0}\" with no clips.", this);
                yield break;
            }

            // We store the playable before the delay, so that we have the correct one in the case where another instance (without delay) was started.
            UPlayable currentPlayable = null;

            if (this.currentClipIndex.HasValue)
            {
                currentPlayable = CurrentPlayable;
            }

            if (this.CurrentDelay > 0.0f)
            {
                yield return new WaitForSeconds(this.CurrentDelay);
            }

            if (!activeEvent.StopRequested)
            {
                switch (containerType)
                {
                    case AudioContainerType.Random:
                    case AudioContainerType.Simultaneous:
                    case AudioContainerType.Sequence:
                        yield return StartOneOffEventCoroutine(activeEvent, currentPlayable);
                        break;
                    case AudioContainerType.ContinuousSequence:
                    case AudioContainerType.ContinuousRandom:
                        yield return PlayContinuousContainerCoroutine(activeEvent);
                        break;
                    default:
                        Debug.LogErrorFormat(this, "Trying to play container \"{0}\" with an unknown AudioContainerType \"{1}\".", this, containerType);
                        yield break;
                }
            }
        }

        public override bool IsEmpty()
        {
            return this.sounds.All(playable => playable.IsEmpty());
        }

        public override float GetLength()
        {
            if (IsInfinite)
            {
                return InfiniteLoop;
            }

            float length;
            
            switch (containerType)
            {
                case AudioContainerType.Random:
                case AudioContainerType.Sequence:
                    length = this.CurrentPlayable.GetLength() + this.CurrentDelay;
                    break;
                case AudioContainerType.Simultaneous:
                    length = this.sounds.Max(playable => playable.GetLength()) + this.CurrentDelay;
                    break;
                default:
                    throw new InvalidOperationException("Shouldn't get to this point with a continuous container.");
            }

            return length;
        }

        /// <summary>
        /// Begin playing a non-continuous container, loop if applicable.
        /// </summary>
        private IEnumerator StartOneOffEventCoroutine(ActiveEvent activeEvent, UPlayable playable)
        {
            if (Loop)
            {
                yield return PlayLoopingOneOffContainerCoroutine(activeEvent);
            }
            else
            {
                yield return PlayOneOffContainerCoroutine(activeEvent, playable);
            }
        }
        
        /// <summary>
        /// Play a non-continuous container.
        /// </summary>
        private IEnumerator PlayOneOffContainerCoroutine(ActiveEvent activeEvent, UPlayable playable)
        {
            // Simultaneous sounds.
            if (this.IsSimultaneous)
            {
                foreach (UPlayable sound in sounds)
                {
                    // We start coroutines here. Since we want to all the sounds to start playing at the same time.
                    StartCoroutine(PlayClipCoroutine(sound, activeEvent));
                }
            }
            // Sequential and Random sounds.
            else
            {
                yield return PlayClipCoroutine(playable, activeEvent);
            }
        }

        /// <summary>
        /// Repeatedly trigger the one-off container based on the loop time.
        /// </summary>
        private IEnumerator PlayLoopingOneOffContainerCoroutine(ActiveEvent activeEvent)
        {
            int? currentIndex = null;

            while (!activeEvent.StopRequested)
            {
                UPlayable currentPlayable = null;

                if (!this.IsSimultaneous)
                {
                    currentIndex = this.GenerateNextClipIndex(currentIndex);
                    currentPlayable = this.sounds[currentIndex.Value];
                }

                // Start coroutine (don't yield return) so that the wait time is accurate, since length includes delay.
                StartCoroutine(PlayOneOffContainerCoroutine(activeEvent, currentPlayable));

                float eventLoopTime = loopTime;

                // Protect against containers looping every frame by defaulting to the length of the audio clip.
                if (eventLoopTime == 0)
                {
                    if (!this.IsSimultaneous)
                    {
                        eventLoopTime = currentPlayable.GetLength();
                    }
                    else
                    {
                        eventLoopTime = this.GetLength();
                    }
                }
                
                yield return new WaitForSeconds(eventLoopTime);
            }
        }

        private void MoveToNextPlayable()
        {
            this.currentClipIndex = GenerateNextClipIndex(this.currentClipIndex);
        }

        private int GenerateNextClipIndex(int? currentIndex)
        {
            int nextIndex = -1;

            switch (containerType)
            {
                case AudioContainerType.Random:
                case AudioContainerType.ContinuousRandom:
                    nextIndex = UnityEngine.Random.Range(0, sounds.Length);
                    break;
                case AudioContainerType.Sequence:
                case AudioContainerType.ContinuousSequence:
                    // The first time we call this, curren
                    if (currentIndex.HasValue)
                    {
                        nextIndex = (currentIndex.Value + 1) % sounds.Length;
                    }
                    else
                    {
                        // For the first play in a sequence container, we want the first clip.
                        nextIndex = 0;
                    }
                    break;
                case AudioContainerType.Simultaneous:
                    throw new InvalidOperationException("Can't get next clip index on a simultaneous container.");
            }

            return nextIndex;
        }

        /// <summary>
        /// Coroutine for "continuous" containers that alternates between two sources to crossfade clips for continuous playlist looping.
        /// </summary>
        /// <returns>The coroutine.</returns>
        private IEnumerator PlayContinuousContainerCoroutine(ActiveEvent activeEvent)
        {
            float waitTime = 0;
            int? currentIndex = null;

            while (!activeEvent.StopRequested)
            {
                currentIndex = this.GenerateNextClipIndex(currentIndex);
                UPlayable tempClip = this.sounds[currentIndex.Value];

                if (tempClip.IsEmpty())
                {
                    Debug.LogErrorFormat(this, "Sound clip in event \"{0}\" is null!", activeEvent.audioEvent.name);
                    waitTime = 0;
                }
                else
                {
                    if (!activeEvent.IsPlayingSecondary)
                    {
                        activeEvent.volDest = activeEvent.audioEvent.volumeCenter;
                        activeEvent.altVolDest = 0f;
                    }
                    else
                    {
                        activeEvent.volDest = 0f;
                        activeEvent.altVolDest = activeEvent.audioEvent.volumeCenter;
                    }
                    
                    activeEvent.CurrentAudioSource.volume = 0f;
                    activeEvent.currentFade = crossfadeTime;
                    yield return PlayClipCoroutine(tempClip, activeEvent);

                    waitTime = tempClip.GetLength() - crossfadeTime;
                }

                // Alternate playing on primary / secondary audio sources.
                activeEvent.IsPlayingSecondary = !activeEvent.IsPlayingSecondary;
                
                yield return new WaitForSeconds(waitTime);
            }
        }
        
        private IEnumerator PlayClipCoroutine(UPlayable playable, ActiveEvent activeEvent)
        {
            if (this.IsSimultaneous || this.PlaySimultaneous)
            {
                playable.PlaySimultaneous = true;
            }

            yield return playable.PlayAsync(activeEvent);
        }
    }
}