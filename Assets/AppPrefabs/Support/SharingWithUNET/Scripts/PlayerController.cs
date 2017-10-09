// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using UnityEngine;
using UnityEngine.Networking;
using HoloToolkit.Unity.InputModule;
using System.Collections.Generic;
using HoloToolkit.Unity;
using UnityEngine.XR.WSA.Input;

namespace HoloToolkit.Examples.SharingWithUNET
{
    /// <summary>
    /// Controls player behavior (local and remote).
    /// </summary>
    [NetworkSettings(sendInterval = 0.033f)]
    public class PlayerController : NetworkBehaviour
    {
        private static PlayerController _Instance = null;
        public static PlayerController Instance
        {
            get
            {
                return _Instance;
            }
        }

        public static List<PlayerController> allPlayers = new List<PlayerController>();

        /// <summary>
        /// The transform of the shared world anchor.
        /// </summary>
        private Transform sharedWorldAnchorTransform;

        private UNetAnchorManager anchorManager;

        /// <summary>
        /// The position relative to the shared world anchor.
        /// </summary>
        [SyncVar]
        private Vector3 localPosition;

        /// <summary>
        /// The rotation relative to the shared world anchor.
        /// </summary>
        [SyncVar]
        private Quaternion localRotation;

        /// <summary>
        /// Sets the localPosition and localRotation on clients.
        /// </summary>
        /// <param name="postion">the localPosition to set</param>
        /// <param name="rotation">the localRotation to set</param>
        [Command(channel = 1)]
        public void CmdTransform(Vector3 postion, Quaternion rotation)
        {
            localPosition = postion;
            localRotation = rotation;
        }

        [SyncVar(hook = "AnchorEstablishedChanged")]
        bool AnchorEstablished;

        [Command]
        private void CmdSendAnchorEstablished(bool Established)
        {
            AnchorEstablished = Established;
            if (Established && SharesSpatialAnchors && !isLocalPlayer)
            {
                Debug.Log("remote device likes the anchor");
                anchorManager.AnchorFoundRemotely();
            }
        }

        void AnchorEstablishedChanged(bool update)
        {
            Debug.LogFormat("AnchorEstablished for {0} was {1} is now {2}", PlayerName, AnchorEstablished, update);
            AnchorEstablished = update;
            GetComponentInChildren<MeshRenderer>().enabled = update;
            if (!isLocalPlayer)
            {
                InitializeRemoteAvatar();
            }
        }

        [Command]
        private void CmdSetAnchorOwnerIP(string UpdatedIP)
        {
            anchorManager.UpdateAnchorOwnerIP(UpdatedIP);
        }

        public void SetAnchorOwnerIP(string UpdatedIP)
        {
            CmdSetAnchorOwnerIP(UpdatedIP);
        }

        [SyncVar(hook = "PlayerNameChanged")]
        string PlayerName;

        [Command]
        private void CmdSetPlayerName(string playerName)
        {
            PlayerName = playerName;
        }

        void PlayerNameChanged(string update)
        {
            Debug.LogFormat("Player name changing from {0} to {1}", PlayerName, update);
            PlayerName = update;
            if (PlayerName.ToLower() == "spectatorviewpc")
            {
                gameObject.SetActive(false);
            }
            else if (!isLocalPlayer)
            {
                InitializeRemoteAvatar();
            }
        }

#pragma warning disable 0414
        [SyncVar(hook = "PlayerIpChanged")]
        public string PlayerIp;
#pragma warning restore 0414
        [Command]
        private void CmdSetPlayerIp(string playerIp)
        {
            PlayerIp = playerIp;
        }

        void PlayerIpChanged(string update)
        {
            PlayerIp = update;
        }

        [SyncVar(hook = "PathIndexChanged")]
        int PathIndex = -1;

        [Command]
        private void CmdSendPathIndex(int CurrentPathIndex)
        {
            PathIndex = CurrentPathIndex;
        }

        void PathIndexChanged(int update)
        {
            Debug.LogFormat("{0}: Path index updated {1} => {2}", PlayerName, PathIndex, update);
            PathIndex = update;

            if (isLocalPlayer)
            {
                levelState.SetPathIndex(PathIndex);
                if (PathIndex == -1 && UnityEngine.XR.WSA.HolographicSettings.IsDisplayOpaque)
                {
                    Debug.Log("Getting in line");
                    WaitingForFreePath = true;
                    if (fadeControl != null && fadeControl.Busy == false)
                    {
                        fadeControl.DoFade(0.5f, 0.5f,
                            () =>
                            {
                                MixedRealityTeleport warper = MixedRealityTeleport.Instance;
                                if (warper != null)
                                {
                                   // warper.ResetRotation();
                                    warper.SetWorldPosition(levelState.transform.position + levelState.transform.forward * -2.5f + Vector3.up * 0.25f + levelState.transform.transform.right * Random.Range(-2f, 2.0f));
                                }
                            }, null);
                    }
                }
                else
                {
                    WaitingForFreePath = false;
                }
            }
            else
            {
                levelState.OnPathMessage(PathIndex, PlayerName);
            }
        }

        [SyncVar]
        private Vector3 BoltTarget;

        [SyncVar]
        private Vector3 BoltPos;

        [SyncVar(hook = "BoltReadyToGo")]
        private bool BoltReady;

        float BoltLifetime = 1.0f;
        float BoltStartTime;

        void BoltReadyToGo(bool update)
        {
            if (update)
            {
                BoltStartTime = Time.realtimeSinceStartup;
            }
            BoltReady = update;
        }

        [Command]
        private void CmdSetupBolt(Vector3 target, Vector3 sourcePos)
        {
            BoltTarget = (target);
            BoltPos = (sourcePos);
            BoltReady = true;
        }

        LineRenderer lineRend;

        [SyncVar(hook = "SharesAnchorsChanged")]
        public bool SharesSpatialAnchors;

        [Command]
        private void CmdSetCanShareAnchors(bool canShareAnchors)
        {
            Debug.Log("CMDSetCanShare " + canShareAnchors);
            SharesSpatialAnchors = canShareAnchors;
        }

        void SharesAnchorsChanged(bool update)
        {
            SharesSpatialAnchors = update;
            if (SharesSpatialAnchors)
            {
                cloudMaterial.color = Color.red;
            }
            else
            {
                cloudMaterial.color = Color.blue;
            }

            Debug.LogFormat("{0} {1} share", PlayerName, SharesSpatialAnchors ? "does" : "does not");
        }

        public void SetRequireAllPaths(bool require)
        {
            CmdSetRequireAllPaths(require);
        }

        [Command]
        private void CmdSetRequireAllPaths(bool require)
        {
            levelState.SetRequireAllPaths(require);
        }

        [Command]
        private void CmdUpdateAnchorName(string UpdatedName)
        {
            anchorManager.AnchorName = UpdatedName;
        }

        public void UpdateAnchorName(string UpdatedName)
        {
            CmdUpdateAnchorName(UpdatedName);
        }

        public void SendPathIndex(int RequestedPathIndex)
        {
            CmdSendPathIndex(RequestedPathIndex);
        }

        private void InvokeRequestPathIndex()
        {
            CmdRequestPathIndex();
        }

        [Command]
        public void CmdRequestPathIndex()
        {
            PathIndex = levelState.FindOpenPath();
        }

        private bool WaitingForFreePath = false;

        LevelControl levelState;

        NetworkDiscoveryWithAnchors networkDiscovery;

        private Material cloudMaterial;

        private FadeScript fadeControl;

        public bool AllowManualImmersionControl = false;

        private void InitializeRemoteAvatar()
        {
            if (!string.IsNullOrEmpty(PlayerName) && AnchorEstablished)
            {
                levelState.RemoteAvatarReady(this.gameObject, PlayerName, AnchorEstablished);
                levelState.OnPathMessage(PathIndex, PlayerName);
            }
        }

       
        void Awake()
        {
            cloudMaterial = GetComponentInChildren<MeshRenderer>().material;
            fadeControl = FadeScript.Instance;
            networkDiscovery = NetworkDiscoveryWithAnchors.Instance;
            anchorManager = UNetAnchorManager.Instance;
            levelState = LevelControl.Instance;
            allPlayers.Add(this);
        }

        private void Start()
        {
            if (SharedCollection.Instance == null)
            {
                Debug.LogError("This script required a SharedCollection script attached to a gameobject in the scene");
                Destroy(this);
                return;
            }

            if (isLocalPlayer)
            {
                Debug.Log("Init from start");
                InitializeLocalPlayer();
            }
            else
            {
                Debug.Log("remote player, analyzing start state " + PlayerName);
                AnchorEstablishedChanged(AnchorEstablished);
                SharesAnchorsChanged(SharesSpatialAnchors);
            }

            sharedWorldAnchorTransform = SharedCollection.Instance.gameObject.transform;
            transform.SetParent(sharedWorldAnchorTransform);
        }

        private void InitializeLocalPlayer()
        {
            if (isLocalPlayer)
            {
                Debug.Log("Setting instance for local player ");
                _Instance = this;
                Debug.LogFormat("Set local player name {0} ip {1}", networkDiscovery.broadcastData, networkDiscovery.LocalIp);
                CmdSetPlayerName(networkDiscovery.broadcastData);
                CmdSetPlayerIp(networkDiscovery.LocalIp);
                bool opaqueDisplay = UnityEngine.XR.WSA.HolographicSettings.IsDisplayOpaque;
                Debug.LogFormat("local player {0} share anchors ", (opaqueDisplay ? "does not" : "does"));
                CmdSetCanShareAnchors(!opaqueDisplay);

                if (opaqueDisplay && levelState != null && levelState.isActiveAndEnabled)
                {
                    Debug.Log("Requesting immersive path");
                    WaitingForFreePath = true;
                    CmdRequestPathIndex();
                }
                else
                {
                    Debug.Log("Defaulting to bird's eye view");
                    CmdSendPathIndex(-1);
                    if (opaqueDisplay && fadeControl != null && fadeControl.Busy == false)
                    {
                        fadeControl.DoFade(0.5f, 0.5f,
                            () =>
                            {
                                MixedRealityTeleport warper = MixedRealityTeleport.Instance;
                                if (warper != null)
                                {
                                    //warper.ResetRotation();
                                    warper.SetWorldPosition(levelState.transform.position + levelState.transform.forward * -2.5f + Vector3.up * 0.25f + levelState.transform.transform.right * Random.Range(-2f, 2.0f));
                                }
                            }, null);
                    }
                }

                if (!opaqueDisplay && anchorManager.AnchorOwnerIP == "")
                {
                    Invoke("DeferredAnchorOwnerCheck", 2.0f);
                    
                }

                if (UnityEngine.XR.WSA.HolographicSettings.IsDisplayOpaque)
                {
                    InteractionManager.InteractionSourceUpdated += InteractionManager_InteractionSourceUpdated;
                }
            }
        }

        int boltFrame=0;
        private void InteractionManager_InteractionSourceUpdated(InteractionSourceUpdatedEventArgs obj)
        {
            if (obj.state.source.supportsGrasp && obj.state.grasped && (Time.frameCount - boltFrame > 20))
            {
                boltFrame = Time.frameCount;
                Vector3 boltStart;
                if (!obj.state.sourcePose.TryGetPosition(out boltStart, InteractionSourceNode.Pointer))
                {
                    return;
                }

                
                Vector3 boltDirection;
                if (!obj.state.sourcePose.TryGetForward(out boltDirection, InteractionSourceNode.Pointer))
                {
                    return;
                }

                Vector3 boltTarget = boltStart + boltDirection * 15.0f;
                Transform LevelTransform = levelState.transform;

                boltStart = MixedRealityTeleport.Instance.transform.TransformPoint(boltStart);
                boltTarget = MixedRealityTeleport.Instance.transform.TransformPoint(boltTarget);
                CmdSetupBolt(LevelTransform.InverseTransformPoint(boltTarget), LevelTransform.InverseTransformPoint(boltStart));
            }

            if (obj.state.source.supportsMenu && obj.state.menuPressed)
            {
                ResetPosition();
            }
        }

        private void DeferredAnchorOwnerCheck()
        {
            if (!UnityEngine.XR.WSA.HolographicSettings.IsDisplayOpaque && anchorManager.AnchorOwnerIP == "")
            {
                Debug.Log("Claiming anchor ownership " + networkDiscovery.LocalIp);
                CmdSetAnchorOwnerIP(networkDiscovery.LocalIp);
            }
        }

        public override void OnStartClient()
        {
            base.OnStartClient();
        }

        private void OnDestroy()
        {
            if (allPlayers.Contains(this))
            {
                allPlayers.Remove(this);
            }

            if (levelState != null)
            {
                levelState.RemoteAvatarReady(this.gameObject, PlayerName, false);
            }

            if (cloudMaterial != null)
            {
                Destroy(cloudMaterial);
            }

            if (!isLocalPlayer && PlayerController.Instance != null)
            {
                PlayerController.Instance.CheckLine();
            }

            // Anchor owner is disconnecting, find a new anchor.
            if (UNetAnchorManager.Instance.AnchorOwnerIP == PlayerIp)
            {
                Debug.Log("Hey, the anchor owner is going away");
                anchorManager.AnchorOwnerIP = "";
            }
        }

        public void CheckLine()
        {
            Debug.Log("Checking our place in line");
            if (WaitingForFreePath)
            {
                Debug.Log("We were waiting...");
                WaitingForFreePath = false;
                Invoke("InvokeRequestPathIndex", 5.0f);
            }
        }

        private void Update()
        {
            // If we aren't the local player, we just need to make sure that the position of this object is set properly
            // so that we properly render their avatar in our world.
            if (!isLocalPlayer && string.IsNullOrEmpty(PlayerName) == false)
            {
                transform.localPosition = Vector3.Lerp(transform.localPosition, localPosition, 0.3f);
                transform.localRotation = localRotation;
                BoltCheck();
                return;
            }

            if (!isLocalPlayer)
            {
                return;
            }

            if (AnchorEstablished != anchorManager.AnchorEstablished)
            {
                CmdSendAnchorEstablished(anchorManager.AnchorEstablished);
                AnchorEstablished = anchorManager.AnchorEstablished;
            }

            if (AnchorEstablished == false)
            {
                return;
            }

            // if we are the remote player then we need to update our worldPosition and then set our 
            // local (to the shared world anchor) position for other clients to update our position in their world.
            transform.position = Camera.main.transform.position;
            transform.rotation = Camera.main.transform.rotation;

            // For UNET we use a command to signal the host to update our local position
            // and rotation
            CmdTransform(transform.localPosition, transform.localRotation);

            if (AllowManualImmersionControl && fadeControl.Busy == false && levelState.isActiveAndEnabled)
            {
                if (Input.GetButtonUp("Fire3"))
                {
                    if (PathIndex >= 0)
                    {
                        CmdSendPathIndex(-1);
                    }
                    else
                    {
                        CmdRequestPathIndex();
                    }
                }
            }

            if (!SharesSpatialAnchors)
            {
                if (Input.GetButtonUp("XBOX_VIEW"))
                {
                    ResetPosition();
                }
            }

            BoltCheck();
        }

        public void ResetPosition()
        {
            if (PathIndex >= 0)
            {
                levelState.ResetPosition();
            }
        }

        private void BoltCheck()
        {
            if (isLocalPlayer)
            {
                UnityEngine.XR.WSA.Input.InteractionSourceState[] sources = UnityEngine.XR.WSA.Input.InteractionManager.GetCurrentReading();
                if (sources != null && sources.Length == 2)
                {
                     if (sources[0].source.kind == UnityEngine.XR.WSA.Input.InteractionSourceKind.Hand && sources[1].source.kind == UnityEngine.XR.WSA.Input.InteractionSourceKind.Hand)
                    {
                       
                        Vector3 p1 = Vector3.zero;
                        Vector3 p2 = Vector3.zero;

                        if (sources[0].sourcePose.TryGetPosition(out p1) && sources[1].sourcePose.TryGetPosition(out p2)
                            && p1 != Vector3.zero && p2 != Vector3.zero)
                        {
                            float distBetweenHands = (p1 - p2).magnitude;
                            float targetDist = 0.1f;
                            
                            if (distBetweenHands < targetDist)
                            {
                                Transform LevelTransform = levelState.transform;
                                if (LevelTransform != null)
                                {
                                    Vector3 boltStart = (p1 + p2) * 0.5f;
                                    Vector3 boltTarget = GazeManager.Instance.HitPosition;

                                    CmdSetupBolt(LevelTransform.InverseTransformPoint(boltTarget), LevelTransform.InverseTransformPoint(boltStart));
                                }
                            }
                        }
                    }
                }

                if (Input.GetKeyUp(KeyCode.K) || Input.GetButtonUp("Fire2"))
                {
                    Transform LevelTransform = levelState.transform;
                    if (LevelTransform != null)
                    {
                        Vector3 boltStart = transform.position + transform.forward * 1.0f + transform.up * 0.2f;

                        CmdSetupBolt(LevelTransform.InverseTransformPoint(GazeManager.Instance.HitPosition), LevelTransform.InverseTransformPoint(boltStart));
                    }
                }
            }

            if (BoltReady)
            {
                if (lineRend == null)
                {
                    lineRend = GetComponent<LineRenderer>();
                }
                if (lineRend == null)
                {
                    Debug.Log("No line rend on " + gameObject.name);
                    return;
                }

                lineRend.startWidth = levelState.Immersed ? .01f * LevelControl.ImmersiveScale : .01f;
                if (isLocalPlayer)
                {
                    lineRend.startWidth *= levelState.Immersed ? 0.1f : 1;
                }
                else
                {
                    lineRend.startWidth *= levelState.IsImmersed(PlayerName) ? 0.1f : 1;
                }

                lineRend.endWidth = lineRend.startWidth*0.5f;

                float timeSinceStart = Time.realtimeSinceStartup - BoltStartTime;
                if (timeSinceStart > BoltLifetime)
                {
                    BoltReady = false;
                    lineRend.positionCount = 1;
                }
                else
                {
                    // lightning effect here started from https://forum.unity3d.com/threads/the-best-way-to-create-a-lightning-effect.9058/
                    float timeRat = timeSinceStart / BoltLifetime;
                    float toDestRat = timeRat < 0.5f ? timeRat / 0.5f : 1.0f;
                    float fromSourceRat = timeRat < 0.5f ? 0f : (timeRat - 0.5f) / 0.5f;

                    Transform levelTransform = levelState.transform;
                    
                    if (levelTransform != null)
                    {
                        Vector3 source = levelTransform.TransformPoint(BoltPos);
                        Vector3 finalTarget = levelTransform.TransformPoint(BoltTarget);
                        
                        Vector3 currentTarget = source + (finalTarget - source) * toDestRat;
                        Vector3 currentSource = source + (finalTarget - source) * fromSourceRat;
                        Vector3 lastPoint = currentSource;
                        
                        int i = 1;
                        lineRend.SetPosition(0, lastPoint);//make the origin of the LR the same as the transform
                        while (Vector3.Distance(currentTarget, lastPoint) > .1f)
                        {//was the last arc not touching the target?
                            lineRend.positionCount = i + 1;//then we need a new vertex in our line renderer
                            Vector3 fwd = currentTarget - lastPoint;//gives the direction to our target from the end of the last arc
                            fwd.Normalize();//makes the direction to scale
                            fwd = Randomize(fwd, 0.1f);//we don't want a straight line to the target though
                            fwd *= Random.Range(.1f * .9f, .1f);//nature is never too uniform
                            fwd += lastPoint;//point + distance * direction = new point. this is where our new arc ends
                            lineRend.SetPosition(i, fwd);//this tells the line renderer where to draw to
                            i++;
                            lastPoint = fwd;//so we know where we are starting from for the next arc
                        }
                        lineRend.positionCount = i + 1;
                        lineRend.SetPosition(i, currentTarget);
                    }
                }
            }
            else if (lineRend != null)
            {
                BoltReady = false;
                lineRend.positionCount = 1;
            }
        }

        private Vector3 Randomize(Vector3 newVector, float devation)
        {
            newVector += new Vector3(Random.Range(-6.0f, 6.0f), Random.Range(-6.0f, 6.0f), Random.Range(-6.0f, 6.0f)) * devation;
            newVector.Normalize();
            return newVector;
        }

        public void SceneReset()
        {
            Debug.Log("Resetting " + (WaitingForFreePath ? 10.0f : 20.0f).ToString());
            CmdSendPathIndex(-1);
            Invoke("InvokeRequestPathIndex", WaitingForFreePath ? 10.0f : 20.0f);
            WaitingForFreePath = false;
        }

        /// <summary>
        /// Called when the local player starts.  In general the side effect should not be noticed
        /// as the players' avatar is always rendered on top of their head.
        /// </summary>
        public override void OnStartLocalPlayer()
        {
            GetComponentInChildren<MeshRenderer>().enabled = false;
        }

        [Command]
        private void CmdSendSharedTransform(GameObject target, Vector3 pos, Quaternion rot)
        {
            UNetSharedHologram ush = target.GetComponent<UNetSharedHologram>();
            ush.CmdTransform(pos, rot);
        }

        /// <summary>
        /// For sending transforms for holograms which do not frequently change.
        /// </summary>
        /// <param name="target">The shared hologram (must have a </param>
        /// <param name="pos"></param>
        /// <param name="rot"></param>
        public void SendSharedTransform(GameObject target, Vector3 pos, Quaternion rot)
        {
            if (isLocalPlayer)
            {
                CmdSendSharedTransform(target, pos, rot);
            }
        }

        [Command]
        private void CmdSendAtGoal(int GoalIndex)
        {
            levelState.SetGoalIndex(GoalIndex);
        }

        public void SendAtGoal(int GoalIndex)
        {
            if (isLocalPlayer)
            {
                Debug.Log("sending at goal " + GoalIndex);
                CmdSendAtGoal(GoalIndex);
            }
        }

        [Command(channel = 1)]
        private void CmdSendImmersedPosition(string PlayerName, Vector3 levelPosition, Quaternion levelRotation)
        {
            levelState.SetRemoteAvatarLevelPosition(PlayerName, levelPosition, levelRotation);
        }

        public void SendImmersedPosition(Vector3 levelPosition, Quaternion levelRotation)
        {
            if (isLocalPlayer)
            {
                CmdSendImmersedPosition(this.PlayerName, levelPosition, levelRotation);
            }
        }

        [Command]
        public void CmdSendPuzzleSolved(int PuzzleIndex)
        {
            Debug.Log("CmdPuzzle solved");
            levelState.RpcPuzzleSolved(PuzzleIndex);
        }

        public void SendPuzzleSolved(int PuzzleIndex)
        {
            if (isLocalPlayer)
            {
                Debug.Log("Puzzle solved");
                CmdSendPuzzleSolved(PuzzleIndex);
            }
        }
    }
}
