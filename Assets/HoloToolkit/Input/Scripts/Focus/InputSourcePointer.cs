// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace HoloToolkit.Unity.InputModule
{
    // TODO: robertes: comment for HoloToolkit release.
    public class InputSourcePointer :
        IPointingSource
    {
        public IInputSource InputSource { get; set; }

        public uint InputSourceId { get; set; }

        public BaseRayStabilizer RayStabilizer { get; set; }

        public bool OwnAllInput { get; set; }

        public Ray Ray
        {
            get
            {
                return (RayStabilizer == null)
                    ? rawRay
                    : RayStabilizer.StableRay;
            }
        }

        public float? ExtentOverride { get; set; }

        public IList<LayerMask> PrioritizedLayerMasksOverride { get; set; }

        private Ray rawRay = default(Ray);

        public void UpdatePointer()
        {
            if (InputSource == null)
            {
                rawRay = default(Ray);
            }
            else
            {
                Debug.Assert(InputSource.SupportsInputInfo(InputSourceId, SupportedInputInfo.PointingRay));

                InputSource.TryGetPointingRay(InputSourceId, out rawRay);
            }

            if (RayStabilizer != null)
            {
                RayStabilizer.UpdateStability(rawRay.origin, rawRay.direction);
            }
        }

        public bool OwnsInput(BaseEventData eventData)
        {
            // TODO: robertes: consider dealing with voice specially.

            return (OwnAllInput || InputIsFromSource(eventData));
        }

        public bool InputIsFromSource(BaseEventData eventData)
        {
            // TODO: robertes: deal with the fact that Gestures come from a different source than InteractionManager input. Make
            //       sure that gestures come with a sourceId that can be correlated with the rawInput IDs.

            var inputData = (eventData as BaseInputEventData);

            return (inputData != null)
                && (inputData.InputSource == InputSource)
                && (inputData.SourceId == InputSourceId)
                ;
        }
    }
}
