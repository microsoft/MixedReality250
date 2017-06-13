// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using UnityEngine;

namespace HoloToolkit.Unity
{
    public enum RTPCProperty
    {
        Volume,
        Pitch
    }

    [Serializable]
    public class RTPC : MonoBehaviour
    {
        public string RTPCName;
        public RTPCProperty audioProperty;
        public AnimationCurve propertyCurve;
        public float defaultValue;
        public float smoothingTime;

        [NonSerialized]
        public float value;
        
        public void ApplyValues(ActiveEvent activeEvent)
        {
            switch (audioProperty)
            {
                case RTPCProperty.Volume:
                    activeEvent.volDest = Evaluate();
                    activeEvent.currentFade = smoothingTime;
                    break;
                case RTPCProperty.Pitch:
                    activeEvent.SetPitch(Evaluate());
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private float Evaluate()
        {
            return propertyCurve.Evaluate(value);
        }
    }
}
