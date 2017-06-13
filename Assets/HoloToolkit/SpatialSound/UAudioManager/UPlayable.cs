// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Collections;
using UnityEngine;
using Random = UnityEngine.Random;

namespace HoloToolkit.Unity
{
    /// <summary>
    /// The UPlayable class is the base class for any sounds that can be played within a container.
    /// </summary>
    public abstract class UPlayable : MonoBehaviour
    {
        [SerializeField]
        private float delayCenter = 0;

        [SerializeField]
        private float delayRandomization = 0;

        private bool preparedForPlay = false;

        [SerializeField]
        private bool loop;
        public bool Loop
        {
            get { return loop; }
            set { loop = value; }
        }

        public bool PlaySimultaneous { get; set; }

        public virtual bool IsLooping
        {
            get { return this.Loop; }
        }

        public virtual bool IsInfinite
        {
            get { return IsLooping; }
        }

        private float currentDelay = 0.0f;
        protected float CurrentDelay
        {
            get { return this.currentDelay; }
        }

        public bool HasDelay
        {
            get { return delayCenter != 0f || delayRandomization != 0f; }
        }

        public virtual bool IsPlaying
        {
            get { return false; }
        }
        
        /// <summary>
        /// We use this method to set up all the necessary properties before actually playing a Playable object. This allows us to compute the length of the playable accurately before beginning to play.
        /// </summary>
        public virtual void PrepareForPlay()
        {
            if (this.preparedForPlay)
            {
                throw new InvalidOperationException("This Playable has already been prepared for play.");
            }

            this.GenerateDelay();

            this.preparedForPlay = true;
        }

        /// <summary>
        /// This must get called during the Play method in any subclasses.
        /// </summary>
        protected void BeginPlay()
        {
            if (!this.preparedForPlay)
            {
                this.PrepareForPlay();
            }

            this.preparedForPlay = false;
        }

        public abstract IEnumerator PlayAsync(ActiveEvent activeEvent);

        public abstract float GetLength();

        public abstract bool IsEmpty();
        
        protected float GenerateDelay()
        {
            if (HasDelay)
            {
                // Ensure delay is non-negative.
                this.currentDelay = Mathf.Max(0.0f, Random.Range(delayCenter - delayRandomization, delayCenter + delayRandomization));
            }
            else
            {
                this.currentDelay = 0.0f;
            }
            return this.currentDelay;
        }
    }
}