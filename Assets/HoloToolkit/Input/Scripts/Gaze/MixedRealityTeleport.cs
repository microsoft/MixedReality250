// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
using System;
using UnityEngine;
using UnityEngine.XR;
using UnityEngine.XR.WSA.Input;

namespace HoloToolkit.Unity.InputModule
{
    /// <summary>
    /// Script teleports the user to the location being gazed at when Y was pressed on a Gamepad.
    /// </summary>
    [RequireComponent(typeof(SetGlobalListener))]
    [RequireComponent(typeof(LineRenderer))]
    public class MixedRealityTeleport : Singleton<MixedRealityTeleport>, IControllerInputHandler
    {
        [Tooltip("Name of the thumbstick axis to check for teleport and strafe.")]
        public string LeftThumbstickX = "ControllerLeftStickX";

        [Tooltip("Name of the thumbstick axis to check for teleport and strafe.")]
        public string LeftThumbstickY = "ControllerLeftStickY";

        [Tooltip("Name of the thumbstick axis to check for rotation.")]
        public string RightThumbstickX = "ControllerRightStickX";

        [Tooltip("Name of the thumbstick axis to check for rotation.")]
        public string RightThumbstickY = "ControllerRightStickY";

        public bool EnableTeleport = true;
        public bool EnableRotation = true;
        public bool EnableStrafe = true;

        public float RotationSize = 45.0f;
        public float StrafeAmount = 0.5f;

        public GameObject TeleportMarker;
        private Animator animationController;

        private LineRenderer teleportLine;
        /// <summary>
        /// The fade control allows us to fade out and fade in the scene.
        /// This is done to improve comfort when using an immersive display.
        /// </summary>
        private FadeScript fadeControl;

        private GameObject teleportMarker;
        private bool isTeleportValid;
        private IPointingSource currentPointingSource;
        private uint currentSourceId;
        
        private void Start()
        {
            fadeControl = FadeScript.Instance;

            if (!XRDevice.isPresent || fadeControl == null)
            {
                if (fadeControl == null)
                {
                    Debug.LogError("The MixedRealityTeleport script on " + name + " requires a FadeScript object.");
                }

                Destroy(this);
                return;
            }

            teleportMarker = Instantiate(TeleportMarker);
            teleportMarker.SetActive(false);

            animationController = teleportMarker.GetComponentInChildren<Animator>();
            if (animationController != null)
            {
                animationController.StopPlayback();
            }

            teleportLine = GetComponent<LineRenderer>();
            teleportLine.enabled = false;
        }

        void Update()
        {
            // assume we will not render the teleport line, if we need to it will
            // be turned on in PositionMarker

            teleportLine.enabled = false;

            // If we don't have motion controllers we fall back to the gamepad.
            if (InteractionManager.numSourceStates == 0)
            {
                HandleGamepad();
            }

            if (currentPointingSource != null)
            {
                PositionMarker();
            }
        }

        private void HandleGamepad()
        {
            if (EnableTeleport && !fadeControl.Busy)
            {
                float leftX = Input.GetAxis(LeftThumbstickX);
                float leftY = Input.GetAxis(LeftThumbstickY);

                if (currentPointingSource == null && leftY > 0.8 && Math.Abs(leftX) < 0.3)
                {
                    if (FocusManager.Instance.TryGetSinglePointer(out currentPointingSource))
                    {
                        StartTeleport();
                    }
                }
                else if (currentPointingSource != null && new Vector2(leftX, leftY).magnitude < 0.2)
                {
                    FinishTeleport();
                }
            }

            if (EnableStrafe && currentPointingSource == null && !fadeControl.Busy)
            {
                float leftX = Input.GetAxis(LeftThumbstickX);
                float leftY = Input.GetAxis(LeftThumbstickY);

                if (leftX < -0.8 && Math.Abs(leftY) < 0.3)
                {
                    DoStrafe(Vector3.left * StrafeAmount);
                }
                else if (leftX > 0.8 && Math.Abs(leftY) < 0.3)
                {
                    DoStrafe(Vector3.right * StrafeAmount);
                }
                else if (leftY < -0.8 && Math.Abs(leftX) < 0.3)
                {
                    DoStrafe(Vector3.back * StrafeAmount);
                }
            }

            if (EnableRotation && currentPointingSource == null && !fadeControl.Busy)
            {
                float rightX = Input.GetAxis(RightThumbstickX);
                float rightY = Input.GetAxis(RightThumbstickY);

                if (rightX < -0.8 && Math.Abs(rightY) < 0.3)
                {
                    DoRotation(-RotationSize);
                }
                else if (rightX > 0.8 && Math.Abs(rightY) < 0.3)
                {
                    DoRotation(RotationSize);
                }
            }
        }

        void IControllerInputHandler.OnInputPositionChanged(InputPositionEventData eventData)
        {
            if (eventData.PressType == InteractionSourcePressType.Thumbstick)
            {
                if (EnableTeleport)
                {
                    if (currentPointingSource == null && eventData.Position.y > 0.8 && Math.Abs(eventData.Position.x) < 0.3)
                    {
                        if (FocusManager.Instance.TryGetPointingSource(eventData, out currentPointingSource))
                        {
                            currentSourceId = eventData.SourceId;
                            StartTeleport();
                        }
                    }
                    else if (currentPointingSource != null && currentSourceId == eventData.SourceId && eventData.Position.magnitude < 0.2)
                    {
                        FinishTeleport();
                    }
                }

                if (EnableStrafe && currentPointingSource == null)
                {
                    if (eventData.Position.y < -0.8 && Math.Abs(eventData.Position.x) < 0.3)
                    {
                        DoStrafe(Vector3.back * StrafeAmount);
                    }
                }

                if (EnableRotation && currentPointingSource == null)
                {
                    if (eventData.Position.x < -0.8 && Math.Abs(eventData.Position.y) < 0.3)
                    {
                        DoRotation(-RotationSize);
                    }
                    else if (eventData.Position.x > 0.8 && Math.Abs(eventData.Position.y) < 0.3)
                    {
                        DoRotation(RotationSize);
                    }
                }
            }
        }

        public void StartTeleport()
        {
            if (currentPointingSource != null && !fadeControl.Busy)
            {
                EnableMarker();
                PositionMarker();
            }
        }

        private void FinishTeleport()
        {
            if (currentPointingSource != null)
            {
                currentPointingSource = null;

                if (isTeleportValid)
                {
                    isTeleportValid = false;
                    
                    Vector3 hitPos = teleportMarker.transform.position + Vector3.up * 2.6f;

                    fadeControl.DoFade(0.25f, 0.5f, () =>
                    {
                        SetWorldPosition(hitPos);
                    }, null);
                }

                DisableMarker();
            }
        }

        public void DoRotation(float rotationAmount)
        {
            if (rotationAmount != 0 && !fadeControl.Busy)
            {
                fadeControl.DoFade(
                    0.25f, // Fade out time
                    0.25f, // Fade in time
                    () => // Action after fade out
                    {
                        transform.RotateAround(Camera.main.transform.position, Vector3.up, rotationAmount);
                    }, null); // Action after fade in
            }
        }

        public void DoStrafe(Vector3 strafeAmount)
        {
            if (strafeAmount.magnitude != 0 && !fadeControl.Busy)
            {
                fadeControl.DoFade(
                    0.25f, // Fade out time
                    0.25f, // Fade in time
                    () => // Action after fade out
                    {
                        Transform transformToRotate = Camera.main.transform;
                        transformToRotate.rotation = Quaternion.Euler(0, transformToRotate.rotation.eulerAngles.y, 0);
                        transform.Translate(strafeAmount, Camera.main.transform);
                    }, null); // Action after fade in
            }
        }

        /// <summary>
        /// Places the player in the specified position of the world
        /// </summary>
        /// <param name="worldPosition"></param>
        public void SetWorldPosition(Vector3 worldPosition)
        {
            // There are two things moving the camera: the camera parent (that this script is attached to)
            // and the user's head (which the MR device is attached to. :)). When setting the world position,
            // we need to set it relative to the user's head in the scene so they are looking/standing where 
            // we expect.
            transform.position = worldPosition - (Camera.main.transform.position - transform.position);
        }

        private void EnableMarker()
        {
            teleportMarker.SetActive(true);
            if (animationController != null)
            {
                animationController.StartPlayback();
            }
        }

        private void DisableMarker()
        {
            if (animationController != null)
            {
                animationController.StopPlayback();
            }
            teleportMarker.SetActive(false);
        }

        private void PositionMarker()
        {
            teleportMarker.SetActive(true);
            FocusDetails focusDetails = FocusManager.Instance.GetFocusDetails(currentPointingSource);
            Vector3 telePos = Vector3.zero;
           
            // Check if what is pointed at could be warped to.
            if (focusDetails.Object != null && (Vector3.Dot(focusDetails.Normal, Vector3.up) > 0.90f))
            {
                isTeleportValid = true;
                telePos = focusDetails.Point;
            }
            else
            {
                // If we can't warp straight ahead, try drawing an arc like we do in the shell.
                isTeleportValid = TryReallyHardToFindATeleportPoint(out telePos);
            }

            teleportMarker.transform.position = telePos;
            animationController.speed = isTeleportValid ? 1 : 0;

            SetArcLines();
        }

        /// <summary>
        /// Tracks how steep our controller was when finding an arc based teleport.  To avoid
        /// recalcuating this when drawing the arc, we stash this aside when we calculate the 
        /// teleport point
        /// </summary>
        float angleFromForward;

        /// <summary>
        /// If appropriate, draws the arc from the input device to the teleportation target.
        /// </summary>
        private void SetArcLines()
        {
            teleportLine.enabled = isTeleportValid;
            
            if (isTeleportValid)
            {
                // First we need to make our arc
                Vector3 start = currentPointingSource.Ray.origin;
                Vector3 end = teleportMarker.transform.position;
                Vector3 middle = (start + end) * 0.5f;
                
                Ray startToUp = currentPointingSource.Ray;

                // lets calculate the upper midpoint by solving the length of the hypotenuse
                float hyp = (start - middle).magnitude; // starting with the length of our adjacent leg
                float sinAngle = Mathf.Abs(Mathf.Sin(angleFromForward)); // and getting the angle of the leg/hyp 
                if (sinAngle != 0)
                {
                    hyp = hyp / sinAngle; 
                }

                // the upper mid should be at the distance along the hypotenuse along the angle the pointer is pointing.
                Vector3 upperMid = start + startToUp.direction * hyp;
                if (angleFromForward < 0)
                {
                    upperMid = middle;
                }

                // prevent waves instead of arcs in some cases
                upperMid.y = Mathf.Max(upperMid.y, Mathf.Max(start.y * 1.05f, end.y * 1.05f));

                // now draw some lines.  we want them to arc, so we will change our Y position exponetially and our X position 
                // linearly 
                float deltayUp = upperMid.y - start.y;
                float deltayDown = upperMid.y-end.y;

                // we will go up from the pointer until half position count, and then go down to the teleport marker
                float halfPositionCount = teleportLine.positionCount / 2;
                Vector3 nextStart = start;

                // start the line later for gaze based pointing
                int startIndex = (InteractionManager.numSourceStates == 0 ? (teleportLine.positionCount / 8) : 0);
                for(int index= 0; index<teleportLine.positionCount; index++)
                {
                    // we need this 'real index' to fill the first points in the line list at the same point
                    // for the case where we are using a gaze based pointer and you don't want a line drawn
                    // straight from your face. It is super annoying.
                    int realIndex = Mathf.Max(startIndex, index);

                    // first we check how far along we are on the xz motion
                    float InnerRat = Mathf.Abs(halfPositionCount - realIndex) / halfPositionCount;
                    
                    // and we use xz2 for the y motion.  This provides the arc effect.
                    float InnerRatSq = InnerRat * InnerRat;

                    // and depending on if we are going up or going down in the arc
                    // calculate the next line point
                    if (realIndex < halfPositionCount)
                    {
                        nextStart = Vector3.Lerp(start, upperMid, 1-InnerRat);
                        nextStart.y = start.y + (deltayUp * (1- InnerRatSq));
                    }
                    else
                    {
                        nextStart = Vector3.Lerp( upperMid, end, InnerRat);
                        nextStart.y = upperMid.y - (deltayDown * InnerRatSq);
                    }

                    teleportLine.SetPosition(index, nextStart);
                }
            }
        }

        /// <summary>
        /// When a straigh ray from a pointer doesn't find a telelport point, it is time to arc
        /// </summary>
        /// <param name="position">If a point is found this is the point</param>
        /// <returns>If a point was found</returns>
        private bool TryReallyHardToFindATeleportPoint(out Vector3 position)
        {
            position = Vector3.zero;
            // First, get the forward vector of the controller aligned to gravity
            Vector3 controllerDirection = currentPointingSource.Ray.direction;
            Vector3 controllerPosition = currentPointingSource.Ray.origin;

            Vector3 rightDirection = Vector3.right;
            
            if (controllerDirection == Vector3.down || controllerDirection == Vector3.up)
            {
                rightDirection = Vector3.right;
            }
            else
            {
                rightDirection = Vector3.Cross(Vector3.up, controllerDirection);
            }

            Vector3 alignedDirection = Vector3.Cross(controllerDirection, Vector3.up);

            if (controllerDirection == Vector3.right || controllerDirection == Vector3.left)
            {
                alignedDirection = Vector3.forward;
            }
            else
            {
                alignedDirection = Vector3.Cross(rightDirection, Vector3.up);
            }
            
            alignedDirection.Normalize();

            // then get the angle of the controller from the horizon
            float angleOfRotation = Vector3.SignedAngle(controllerDirection, alignedDirection, Vector3.right);

            angleFromForward = angleOfRotation;
           
            // if the controller is pointing down, don't bother calculating the arc
            if (angleOfRotation < 0)
            {
                return false;
            }

            // otherwise we'll cast a longer ray based on how close the angle is to 45deg
            float CastDistance = 10+(30 * Mathf.Abs(angleOfRotation/45.0f));

            int MaxTries = 10;
            // and find the furthest valid point we can find.
            // TBH, there is probably a better way of doing this.
            while (CastDistance > 0.25f && MaxTries-- > 0)
            {
                // we start at the current 'cast distance' from the controller
                Vector3 startPoint = controllerPosition + controllerDirection * CastDistance;
                RaycastHit rchInner;
                // Then cast a ray down
                if (Physics.Raycast(startPoint, Vector3.down, out rchInner))
                {
                    // If we hit something, see if is pointing more or less up
                    if ((Vector3.Dot(rchInner.normal, Vector3.up) > 0.90f))
                    {
                        // if it is, we need to see if we could move forward to that position
                        // we cast a ray from the pointer to the calculated position
                        RaycastHit rch;
                        float castDist = (rchInner.point - controllerPosition).magnitude + 0.2f;
                        
                        // if something is in our way, then we can't use the point we found
                        if (Physics.Raycast(controllerPosition, alignedDirection, out rch, castDist, ~GazeManager.Instance.RaycastLayerMasks[0]))
                        {
                            return false;
                        }

                        // if nothign is in our way, then we can use this point
                        position = rchInner.point;
                        return true;
                    }
                }

                // if we didn't find a good spot, check again a little closer to the user
                CastDistance *= 0.75f;
            }
            
            // if we get here, we couldn't find a good spot
            return false;
        }
    }
}