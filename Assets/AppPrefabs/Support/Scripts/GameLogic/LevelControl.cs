//#define LINE_REND 
// LINE_REND is for debugging the gaze ray.

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using HoloToolkit.Examples.SharingWithUNET;
using HoloToolkit.Unity.InputModule;
using System;
using HoloToolkit.Unity;

/// <summary>
/// This script has the primary game state logic for the Shared Mixed Reality 250 app.
/// </summary>
public class LevelControl : NetworkBehaviour
{
    // Can't easily make network behaviors singletons with the template
    private static LevelControl _Instance;
    public static LevelControl Instance
    {
        get
        {
            LevelControl[] objects = FindObjectsOfType<LevelControl>();
            if (objects.Length != 1)
            {
                Debug.LogFormat("Expected exactly 1 {0} but found {1}", typeof(LevelControl).ToString(), objects.Length);
            }
            else
            {
                _Instance = objects[0];
            }
            return _Instance;
        }
    }

    /// <summary>
    /// All puzzles will implement this interface so we don't need to 
    /// know how the puzzle works at this layer.
    /// </summary>
    public interface IAmAPuzzle
    {
        void Reset();
        string ToolTipText { get; }
        void Complete();
        bool Solved { get; }
    }

    /// <summary>
    /// Keeps track of information about other players in the scene
    /// </summary>
    public class LevelPlayerStateData
    {
        /// <summary>
        /// Indicates which path the user is on, or -1 if the user is not immersed
        /// </summary>
        private int _PathIndex = -1;
        public int PathIndex
        {
            get
            {
                return _PathIndex;
            }
            set
            {
                _PathIndex = value;
            }
        }

        /// <summary>
        /// Returns true if the user is immersed
        /// </summary>
        public bool Immersed
        {
            get
            {
                return _PathIndex >= 0;
            }
        }

        /// <summary>
        /// A gameobject with the script that has the playercontroller logic for the player
        /// </summary>
        public GameObject FullAvatar { get; set; }
        /// <summary>
        /// The avatar for the player while immersed or being viewed by an immersed player.
        /// </summary>
        public GameObject ImmersedAvatar { get; set; }
        /// <summary>
        /// An object to track the remote gaze cursor while immersed.
        /// </summary>
        public GameObject GazeIndicator { get; set; }
    }

    /// <summary>
    /// Wires up entry points for paths with a specific avatar
    /// puzzle, goal position, and other parameters that will be common
    /// </summary>
    [Serializable]
    public class ImmersedAvatarPathInfo
    {
        /// <summary>
        /// The start position for the path
        /// </summary>
        public GameObject EntryPoint;
        /// <summary>
        /// The Avatar to draw for the path
        /// </summary>
        public GameObject Avatar;
        /// <summary>
        /// The puzzle along the path.  Must have a script
        /// which implements IAmAPuzzle.
        /// </summary>
        public GameObject Puzzle;
        /// <summary>
        /// Where to draw the tooltip for the puzzle
        /// </summary>
        public GameObject PuzzleTipPos;
        /// <summary>
        /// The victory position of the path.
        /// </summary>
        public GameObject GoalPoint;
        /// <summary>
        /// A light to turn on when victory is achieved
        /// </summary>
        public GameObject GoalLight;
    }

    public bool EnableCollaboration = false;

    /// <summary>
    /// Describes how the paths work.  Configurable through the editor
    /// </summary>
    public ImmersedAvatarPathInfo[] AvatarStuff;

    /// <summary>
    /// How much larger the world should be when immersed.
    /// </summary>
    public const float ImmersiveScale = 128.0f;

    /// <summary>
    /// A control for displaying a clue for the puzzle
    /// </summary>
    public ToolTips toolTipControl;

    /// <summary>
    /// Tracks if we are immersed or not.
    /// </summary>
    public bool Immersed { get; private set; }

    /// <summary>
    /// Keeps track of the starting scale of the model.
    /// </summary>
    Vector3 startScale;

    /// <summary>
    /// Tracks if we have sent the completion notification for the current
    /// puzzle
    /// </summary>
    private bool sentPuzzleComplete;

    /// <summary>
    /// Tracks if we have sent the completion notification for the current
    /// path
    /// </summary>
    private bool sentGoalComplete;

    /// <summary>
    /// Needed to track our parent so we can calculate the proper local postion
    /// </summary>
    public GameObject ParentObject;

    /// <summary>
    /// Object to instantiate for remote users' gaze cursors while immersed
    /// </summary>
    public GameObject GazeIndicatorPrefab;

    /// <summary>
    /// Object to spawn to represent remote users while immersed
    /// </summary>
    public GameObject GiantAvatar;

    /// <summary>
    /// Object to remote once a puzzle is solved to show the inside of the level.
    /// </summary>
    public GameObject LevelTopper;

    /// <summary>
    /// The object with the shuttle launch behavior
    /// </summary>
    public ShuttleLaunch shuttleObject;

    /// <summary>
    /// Script which allows us to move the camera position independently from the HMD
    /// </summary>
    MixedRealityTeleport warper;

    /// <summary>
    /// When immersed we have some invisible colliders to prevent users from walking off the edges.
    /// </summary>
    public GameObject SafetyColliders;

    /// <summary>
    /// The local player controller object
    /// </summary>
    HoloToolkit.Examples.SharingWithUNET.PlayerController _PlayerController;
    HoloToolkit.Examples.SharingWithUNET.PlayerController playerController
    {
        get
        {
            if (_PlayerController == null)
            {
                _PlayerController = HoloToolkit.Examples.SharingWithUNET.PlayerController.Instance;
            }

            return _PlayerController;
        }
    }

    /// <summary>
    /// Tracks which path we are currently on
    /// </summary>
    int onPathIndex = -1;

    /// <summary>
    /// Tracks all goals.
    /// </summary>
    SyncListBool AtGoal = new SyncListBool();

    /// <summary>
    /// finds the start tile for the current path
    /// </summary>
    GameObject currentStartTile
    {
        get
        {
            if (onPathIndex >= 0 && onPathIndex < AvatarStuff.Length)
            {
                return AvatarStuff[onPathIndex].EntryPoint;
            }
            return null;
        }
    }

    [SyncVar]
    bool RequireAllPaths = false;

    /// <summary>
    /// For demos we want to be able to get through the experience quickly, so we will allow 
    /// fewer paths to be completed.
    /// </summary>
    /// <param name="require"></param>
    public void SetRequireAllPaths(bool require)
    {
        RequireAllPaths = require;
    }

    public bool AllPathsRequired
    {
        get
        {
            return RequireAllPaths;
        }
    }

    FadeScript fadeScript;
    /// <summary>
    /// Keeps a mapping from user name to their game objects.
    /// </summary>
    Dictionary<string, LevelPlayerStateData> systemIdToPlayerState = new Dictionary<string, LevelPlayerStateData>();

    public Transform GetCurrentTransform(string PlayerName)
    {
        if (string.IsNullOrEmpty(PlayerName))
        {
            return null;
        }

        Transform retval = null;
        LevelPlayerStateData mad = null;
        if (systemIdToPlayerState.TryGetValue(PlayerName, out mad))
        {
            if (mad.Immersed)
            {
                retval = mad.ImmersedAvatar.transform;
            }
            else
            {
                retval = mad.FullAvatar.transform;
            }

        }

        return retval;
    }

    public bool IsImmersed(string PlayerName)
    {
        if (string.IsNullOrEmpty(PlayerName))
        {
            return false;
        }

        LevelPlayerStateData mad = null;
        if (systemIdToPlayerState.TryGetValue(PlayerName, out mad))
        {
            return (mad.Immersed);
        }

        return false;
    }

    /// <summary>
    /// The gaze beam was particularly tricky to implement correctly.  
    /// Enabling the LINE_REND enables drawing a line from the remote user's calculated position
    /// to the remote user's calculated gaze position. 
    /// </summary>
#if LINE_REND
    LineRenderer lineRend;
#endif

    /// <summary>
    /// When the server starts this sets up the list of bools that track if we've met the goals
    /// </summary>
    public override void OnStartServer()
    {
        base.OnStartServer();
        for (int index = 0; index < AvatarStuff.Length; index++)
        {
            AtGoal.Add(false);
        }
    }

    public void LevelLocalTransformChanging(Vector3 old, Vector3 updated)
    {
        if (warper != null)
        {
            warper.SetWorldPosition(warper.transform.position + (updated - old) + Camera.main.transform.localPosition);
        }
    }

    void Start()
    {
#if LINE_REND
        // This is for debugging the gaze ray.
        lineRend = gameObject.AddComponent<LineRenderer>();
        lineRend.positionCount = 2;
        Vector3[] points = new Vector3[] { Vector3.zero, Vector3.one };
        lineRend.SetPositions(points);
#endif

        fadeScript = FadeScript.Instance;
        warper = MixedRealityTeleport.Instance;
        startScale = transform.localScale;
        SetGoalLights();
        SafetyColliders.SetActive(false);
        Debug.LogFormat("{0} {1}", gameObject.name, this.netId);
    }

    private void SetGoalLights()
    {
        if (AtGoal != null && AtGoal.Count == AvatarStuff.Length)
        {
            for (int index = 0; index < AvatarStuff.Length; index++)
            {
                AvatarStuff[index].GoalLight.SetActive(AtGoal[index]);
            }
        }
    }

    /// <summary>
    /// For the most part this script deals with how the player behaves while 
    /// immersed.
    /// </summary>
    void Update()
    {
        if (Input.GetKeyUp(KeyCode.C))
        {
            Cheat();
        }
        if (Immersed)
        {
            // Calculate the remote gaze vectors for other players
            DrawRemoteGaze();

            if (EnableCollaboration)
            {
                // See if we have solved our puzzle
                CheckPuzzle();
                // See if we have reached our goal
                CheckGoal();
            }

            // calculate the rotation from our model rotation to our player rotation
            Quaternion rotToSend = Quaternion.Inverse(ParentObject.transform.rotation);
            rotToSend *= playerController.transform.localRotation;

            Vector3 modelLocalPosition = ParentObject.transform.InverseTransformPoint(playerController.transform.position);

            // Send our position relative to the model to other players
            playerController.SendImmersedPosition(modelLocalPosition, rotToSend);
        }
        else if (EnableCollaboration)
        {
            // If we aren't immersed we should draw a tooltip with a clue to the puzzle
            DrawToolTip();
        }
    }

    void Cheat()
    {
        for (int index = 0; index < AtGoal.Count; index++)
        {
            AtGoal[index] = true;
        }

        CheckAllGoals();
    }

    /// <summary>
    /// Checks to see if the current puzzle is solved.
    /// </summary>
    void CheckPuzzle()
    {
        // only do this once, we don't want to spam success
        if (sentPuzzleComplete == false)
        {
            // Get the puzzle object
            GameObject puzzle = AvatarStuff[onPathIndex].Puzzle;
            // And the puzzle interface (we could cache this...)
            IAmAPuzzle puzzleInterface = puzzle.GetComponent<IAmAPuzzle>();

            // If we have solved the puzzle
            if (puzzleInterface.Solved)
            {
                // Let everyone know
                playerController.SendPuzzleSolved(onPathIndex);
                // And don't send the completion again.
                sentPuzzleComplete = true;
            }
        }
    }

    /// <summary>
    /// Checks to see if we have reached the end goal of the path
    /// </summary>
    void CheckGoal()
    {
        // If we've already reached the goal, don't process again
        if (sentGoalComplete || AtGoal[onPathIndex] == true)
        {
            return;
        }

        // Get the goal object
        GameObject goal = AvatarStuff[onPathIndex].GoalPoint;

        // Calculate how far we are from the goal
        Vector3 hereToGoal = Camera.main.transform.position - goal.transform.position;
        if (hereToGoal.magnitude < 10.0f)
        {
            // if we are within 10 meters start checking the 'angle' to the goal
            float deltaFromUp = Vector3.Dot(Vector3.up, hereToGoal.normalized);
            // if the angle is small enough (closer to 1.0f the closer we are to exactly above)
            if (deltaFromUp > 0.95f)
            {
                // Say that we've made it
                Debug.Log("Arrived at goal " + onPathIndex);
                SendAtGoalMessage();
                Invoke("AllowForFewerImmersedPlayers", 1.0f);
            }
        }
    }

    void AllowForFewerImmersedPlayers()
    {
        if (RequireAllPaths && FindOpenPath() != -1)
        {
            playerController.CmdRequestPathIndex();
        }
    }

    /// <summary>
    /// Tells the server that we have arrived at our goal so the server can tell everyone else
    /// </summary>
    void SendAtGoalMessage()
    {
        if (onPathIndex >= 0 && onPathIndex < AtGoal.Count && playerController != null)
        {
            sentGoalComplete = true;
            playerController.SendAtGoal(onPathIndex);
        }
        else
        {
            Debug.LogFormat("Index {0} is invalid or player controller is null ({1})", onPathIndex, playerController == null ? "yes" : "no");
        }
    }

    /// <summary>
    /// When a remote player reaches a goal they will send their state of arriving at the goal.
    /// This will be called as a side effect to syncronize the AtGoal list and check the win state
    /// </summary>
    /// <param name="index">The index to set to true</param>
    public void SetGoalIndex(int index)
    {
        if (index >= 0 && index < AtGoal.Count)
        {
            Debug.Log("setting at goal " + index);
            AtGoal[index] = true;

            RpcCheckAllGoals();
        }
        else
        {
            Debug.LogFormat("Index {0} is invalid", index);
        }
    }


    /// <summary>
    /// When an avatar is created for a user we need to do some bookkeeping for the path.
    /// When the user leaves we need to clean up.
    /// </summary>
    /// <param name="avatarObject">The full avatar object (with playercontrol script)</param>
    /// <param name="PlayerName">The player name according to the network</param>
    /// <param name="Created">indicates if the player was created or destroyed</param>
    public void RemoteAvatarReady(GameObject avatarObject, string PlayerName, bool Created)
    {
        if (string.IsNullOrEmpty(PlayerName))
        {
            return;
        }

        LevelPlayerStateData playerStateData;
        // First try to get the created avatar info the system is referencing
        if (systemIdToPlayerState.TryGetValue(PlayerName, out playerStateData) == false && Created)
        {
            // If we didn't find the avatar and we are being told that the user is being created 
            // we need to create the data.
            playerStateData = new LevelPlayerStateData();
            systemIdToPlayerState.Add(PlayerName, playerStateData);
            playerStateData.GazeIndicator = Instantiate(GazeIndicatorPrefab);
            Debug.Log("Created avatar for " + PlayerName);
            playerStateData.FullAvatar = avatarObject;
            playerStateData.GazeIndicator.SetActive(false);
            ConfigureAvatarsForPathState();
        }

        // if we found the avatar data and we are being told that the player is being removed
        // we need to clean up the avatar
        if (!Created && playerStateData != null)
        {
            Debug.Log("Cleaning up player avatar for " + PlayerName);
            if (playerStateData.ImmersedAvatar != null)
            {
                Destroy(playerStateData.ImmersedAvatar);
            }

            if (playerStateData.GazeIndicator != null)
            {
                Destroy(playerStateData.GazeIndicator);
            }

            playerStateData = null;
            systemIdToPlayerState.Remove(PlayerName);
        }
    }

    /// <summary>
    /// Positions a player in the immersed world
    /// </summary>
    /// <param name="PlayerName"></param>
    /// <param name="pos"></param>
    /// <param name="rot"></param>
    public void SetRemoteAvatarLevelPosition(string PlayerName, Vector3 pos, Quaternion rot)
    {
        RpcSetRemoteAvatarLevelPosition(PlayerName, pos, rot);
    }

    /// <summary>
    /// Sent to all clients to sync a user's position while immersed
    /// </summary>
    /// <param name="PlayerName"></param>
    /// <param name="pos"></param>
    /// <param name="rot"></param>
    [ClientRpc]
    void RpcSetRemoteAvatarLevelPosition(string PlayerName, Vector3 pos, Quaternion rot)
    {
        LevelPlayerStateData lpsd;
        if (systemIdToPlayerState.TryGetValue(PlayerName, out lpsd) == false)
        {
            return;
        }

        if (lpsd.Immersed)
        {
            lpsd.ImmersedAvatar.transform.localPosition = pos;
            lpsd.ImmersedAvatar.transform.localRotation = rot;
        }
    }

    /// <summary>
    /// finds the tooltip for the closest unsolved puzzle while immersed
    /// Only users not immersed should see the tooltip
    /// </summary>
    void DrawToolTip()
    {
        IAmAPuzzle puzzleNearPlayer = null;
        foreach (KeyValuePair<string, LevelPlayerStateData> players in systemIdToPlayerState)
        {
            if (players.Value.Immersed == true)
            {
                GameObject puzzle = AvatarStuff[players.Value.PathIndex].Puzzle;
                IAmAPuzzle puzzleInterface = puzzle.GetComponent<IAmAPuzzle>();
                if (puzzleInterface.Solved == false && (players.Value.ImmersedAvatar.transform.position - puzzle.transform.position).magnitude < 0.1f)
                {
                    puzzleNearPlayer = puzzleInterface;
                    toolTipControl.transform.localPosition = AvatarStuff[players.Value.PathIndex].PuzzleTipPos.transform.localPosition;
                }
            }
        }

        SetToolTip(puzzleNearPlayer);
    }

    /// <summary>
    /// Draws the specified tool tip.  If null is passed for closest puzzle 
    /// no tooltip will be drawn.
    /// </summary>
    /// <param name="closestPuzzle">Data to help draw the tool tip or null to disable the tip</param>
    void SetToolTip(IAmAPuzzle closestPuzzle)
    {
        if (closestPuzzle != null)
        {
            if (toolTipControl.isActiveAndEnabled == false)
            {
                toolTipControl.gameObject.SetActive(true);
                toolTipControl.enabled = true;
            }

            toolTipControl.SetTipText(closestPuzzle.ToolTipText);
        }
        else
        {
            toolTipControl.gameObject.SetActive(false);
            toolTipControl.enabled = false;
        }
    }

    /// <summary>
    /// Calculates where a remote users's gaze cursor should be when a user is immersed 
    /// </summary>
    void DrawRemoteGaze()
    {
        foreach (KeyValuePair<string, LevelPlayerStateData> players in systemIdToPlayerState)
        {
            if (players.Value.Immersed == false && players.Value.GazeIndicator != null)
            {
                int noShadowboxMask = (1 << 30) | (1 << 31);
                noShadowboxMask = ~noShadowboxMask;
                // this transform is in the original space, but the model has been scaled ImmersiveScale times.
                // need to cast a ray from this transform's pov in our scaled up space.
                // 
                Transform remotePlayerTransform = players.Value.FullAvatar.transform;
                GazeStabilizer gazeStab = players.Value.GazeIndicator.GetComponent<GazeStabilizer>();
                if (gazeStab == null)
                {
                    gazeStab = players.Value.GazeIndicator.AddComponent<GazeStabilizer>();
                }
                gazeStab.UpdateStability(remotePlayerTransform.position, remotePlayerTransform.rotation);

                Vector3 remotePlayerToModel = gazeStab.StablePosition - transform.position;
                Vector3 remoteGazeOrigin = remotePlayerToModel * ImmersiveScale;


                Vector3 gazeTarget = remoteGazeOrigin + remotePlayerTransform.forward * ImmersiveScale * 2;
                Vector3 gazeNormal = Vector3.up;
                RaycastHit hitInfo;
                if (Physics.Raycast(remoteGazeOrigin, remotePlayerTransform.forward, out hitInfo, 1000.0f, noShadowboxMask))
                {
                    gazeTarget = hitInfo.point;
                    gazeNormal = hitInfo.normal;
                }

                players.Value.GazeIndicator.transform.position = Vector3.Lerp(players.Value.GazeIndicator.transform.position, gazeTarget, 0.1f);
                players.Value.GazeIndicator.transform.localRotation = Quaternion.Slerp(players.Value.GazeIndicator.transform.localRotation, Quaternion.FromToRotation(Vector3.up, gazeNormal), 0.5f);

                if (players.Value.ImmersedAvatar != null)
                {
                    players.Value.ImmersedAvatar.transform.position = remoteGazeOrigin;
                    players.Value.ImmersedAvatar.transform.LookAt(gazeTarget);
                }
                else
                {
                    Debug.Log("No immersed avatar...");
                }
#if LINE_REND
                Vector3[] points = new Vector3[] { remoteGazeOrigin, gazeTarget };
                lineRend.SetPositions(points);
#endif
            }
        }
    }

    /// <summary>
    /// Called when a goal is reached to check if all goals are reached so we can
    ///  do the rocket launch.
    /// </summary>
    [ClientRpc]
    void RpcCheckAllGoals()
    {
        Debug.Log("Checking goals");
        CheckAllGoals();
    }

    /// <summary>
    /// Called when a goal is reached to check if all goals are reached so we can
    ///  do the rocket launch.
    /// </summary>
    void CheckAllGoals()
    {
        // First set the lights
        for (int index = 0; index < AtGoal.Count; index++)
        {
            AvatarStuff[index].GoalLight.SetActive(AtGoal[index]);
        }

        int GoalsCompleted = 0;

        // check all of the goals
        for (int index = 0; index < AtGoal.Count; index++)
        {
            if (AtGoal[index] == false)
            {
                // if a goal hasn't been reached, we can return
                if (RequireAllPaths)
                {
                    return;
                }
                continue;
            }

            GoalsCompleted++;
        }

        if (!RequireAllPaths)
        {
            int NumImmersed = 0;
            foreach(var player in HoloToolkit.Examples.SharingWithUNET.PlayerController.allPlayers)
            {
                if (player.SharesSpatialAnchors == false)
                {
                    NumImmersed++;
                }
            }

            // If not all of the immersed players have completed we can return.
            if (GoalsCompleted < NumImmersed)
            {
                return;
            }
        }

        // if we get here, all of the goals have been reached and we can start the launch.
        shuttleObject.startLaunching();

        // And reset the experience after 60 seconds so we can play again.
        Invoke("ResetLevel", 50.0f);
    }

    /// <summary>
    /// Resets the experience to the start.
    /// </summary>
    void ResetLevel()
    {
        Debug.Log("resetting level ilp " + isLocalPlayer);

        RequireAllPaths = false;

        for (int index = 0; index < AtGoal.Count; index++)
        {
            AtGoal[index] = false;
        }

        shuttleObject.resetLaunch();
        LevelTopper.SetActive(true);

        for (int index = 0; index < AvatarStuff.Length; index++)
        {
            AvatarStuff[index].Puzzle.GetComponent<IAmAPuzzle>().Reset();
            AvatarStuff[index].GoalLight.SetActive(false);
        }

        if (UnityEngine.XR.WSA.HolographicSettings.IsDisplayOpaque)
        {
            playerController.SceneReset();
        }
    }

    /// <summary>
    /// Sent when a user enters or exits a path
    /// </summary>
    /// <param name="PathIndex">Indicates which path (-1 is outside the model)</param>
    /// <param name="PlayerName">The name of the player who is going in or out</param>
    public void OnPathMessage(int PathIndex, string PlayerName)
    {
        Debug.Log("Path message");
        int remoteAvatarIndex = PathIndex;
        if (remoteAvatarIndex >= AvatarStuff.Length)
        {
            Debug.Log("Invalid avatar index!  Is someone cheating?");
            remoteAvatarIndex = -1;
        }

        if (remoteAvatarIndex >= 0)
        {
            Debug.Log("User is immersed");
        }
        else
        {
            Debug.Log("User is not immersed");
        }

        LevelPlayerStateData lpsd;
        if (systemIdToPlayerState.TryGetValue(PlayerName, out lpsd) == false)
        {
            Debug.Log("No avatar for" + PlayerName);
        }
        else
        {
            if (lpsd.ImmersedAvatar != null)
            {
                Destroy(lpsd.ImmersedAvatar);
                lpsd.ImmersedAvatar = null;
            }

            lpsd.PathIndex = remoteAvatarIndex;
            if (lpsd.Immersed)
            {
                lpsd.ImmersedAvatar = Instantiate(AvatarStuff[lpsd.PathIndex].Avatar);
                lpsd.ImmersedAvatar.transform.SetParent(ParentObject.transform, false);
                ConfigureAvatarsForPathState();
            }

            SetRenderersAndColliders(lpsd.FullAvatar, !lpsd.Immersed);
        }
    }

    /// <summary>
    /// Searches for the next immersive path to send a user.
    /// If a path has it's goal completed or we already have a user on the 
    /// path, then we won't put the user there. We put the user on the first 
    /// path that is neither occupied nor completed.
    /// </summary>
    /// <returns></returns>
    public int FindOpenPath()
    {
        // First, lets find the possibilities.
        // any path marked 'true' in options will be 
        // considered invalid.  
        // First initialize options to be the same as 'atgoal' which tracks 
        // which paths are complete.
        bool[] options = new bool[AtGoal.Count];
        

        for (int index = 0; index < AtGoal.Count; index++)
        {
            options[index] = AtGoal[index];
        }

        
        // Then we need to check all of the current players to see which paths 
        // people are on.  We will set the options for their path index to be true
        foreach (KeyValuePair<string, LevelPlayerStateData> lpsd in systemIdToPlayerState)
        {
            if (lpsd.Value.PathIndex >= 0)
            {
                options[lpsd.Value.PathIndex] = true;
            }
        }

        if (RequireAllPaths)
        {
            // Find the first entry in options that is set to false.
            for (int index = 0; index < options.Length; index++)
            {
                if (options[index] == false)
                {
                    return index;
                }
            }
        }
        else if(options.Length > 0)
        {
            List<int> validOptions = new List<int>();
            for (int index = 0; index < options.Length; index++)
            {
                if (options[index] == false)
                {
                    validOptions.Add(index);
                }
            }
            if (validOptions.Count > 0)
            {
                int pickedOption = UnityEngine.Random.Range(0, validOptions.Count);
                return validOptions[pickedOption];
            }
        }
        // and if there are no options left return -1 to indicate that the user
        // should remain outside the model.
        return -1;
    }

    /// <summary>
    /// sets the path index for the local player
    /// </summary>
    /// <param name="pathIndex">The path index to set</param>
    public void SetPathIndex(int pathIndex)
    {
        if (UnityEngine.XR.WSA.HolographicSettings.IsDisplayOpaque && warper != null && fadeScript != null && !fadeScript.Busy && pathIndex != onPathIndex)
        {
            fadeScript.DoFade(1, 1,
                () =>
                {
                    SetGoalLights();
                    sentPuzzleComplete = false;
                    sentGoalComplete = false;
                    Immersed = (pathIndex >= 0);
                    onPathIndex = pathIndex;

                    UAudioManager.Instance.PlayEvent("Teleport");

                  //  warper.AllowTeleport = Immersed;
                  //  warper.ResetRotation();
                    SafetyColliders.SetActive(Immersed);
                    // setup the scene state based on if we are immersed or not.
                    if (Immersed)
                    {
                        transform.localScale = startScale * ImmersiveScale;
                        warper.SetWorldPosition(currentStartTile.transform.position + Vector3.up * 0.8f * transform.localScale.y);
                        VRRoomControl.Instance.DisableControls();
                    }
                    else
                    {
                        transform.localScale = startScale;
                     //   warper.ResetRotation();
                        warper.SetWorldPosition(transform.position + transform.forward * -2.5f + Vector3.up * 0.25f);
                        VRRoomControl.Instance.EnableControls();
                    }

                    // and configure the remote players avatars as well.
                    ConfigureAvatarsForPathState();

                }, null);
        }
    }

    // If the user gets stuck in the world, this puts them back to the beginning of their path.
    public void ResetPosition()
    {
        if (fadeScript != null && !fadeScript.Busy && onPathIndex >= 0)
        {
            fadeScript.DoFade(1, 1,
                () =>
                {
                    SetGoalLights();

                    UAudioManager.Instance.PlayEvent("Teleport");
                    transform.localScale = startScale * ImmersiveScale;
                    warper.SetWorldPosition(currentStartTile.transform.position + Vector3.up * 0.8f * transform.localScale.y);
                    VRRoomControl.Instance.DisableControls();

                    // and configure the remote players avatars as well.
                    ConfigureAvatarsForPathState();

                }, null);
        }
    }

    /// <summary>
    /// Sets up the proper avatars for each remote player based on the local
    /// player's path index and the remote player's path index.
    /// </summary>
    void ConfigureAvatarsForPathState()
    {
        Debug.LogFormat("Configuring for path state we are {0} immersed", Immersed ? "in" : "not in");
        foreach (KeyValuePair<string, LevelPlayerStateData> kvp in systemIdToPlayerState)
        {
            Debug.LogFormat("Player {0} {1} immersed", kvp.Key, kvp.Value.Immersed ? "is" : "is not");
            // If the remote player is immersed we don't want to use their 'full' avatar, but want to use their
            // path dependent immersive avatar
            SetRenderersAndColliders(kvp.Value.FullAvatar, !Immersed);

            // we are immersed and the remote player is not
            if (Immersed && kvp.Value.Immersed == false)
            {
                // we might need to clean up their immersive avatar if they
                // have recently left the immersive state
                if (kvp.Value.ImmersedAvatar != null)
                {
                    Destroy(kvp.Value.ImmersedAvatar);
                    kvp.Value.ImmersedAvatar = null;
                }

                // and we need to setup the 'giant' avatar which represents their persona while
                // the local player is immersed
                kvp.Value.ImmersedAvatar = Instantiate(GiantAvatar);
            }

            // if we have an immersed avatar for the player we need to scale it based on if we are immersed.
            if (kvp.Value.ImmersedAvatar != null)
            {
                Vector3 defaultScale = kvp.Value.PathIndex >= 0 ? AvatarStuff[kvp.Value.PathIndex].Avatar.transform.localScale : GiantAvatar.transform.localScale;
                kvp.Value.ImmersedAvatar.transform.localScale = defaultScale * ((Immersed && kvp.Value.Immersed == false) ? ImmersiveScale * 0.25f : 1);
            }

            // Gaze should be enabled if we are immersed and the remote player is not.
            if (Immersed && kvp.Value.Immersed == false)
            {
                Debug.Log("Enabling gaze for " + kvp.Key);
                kvp.Value.GazeIndicator.SetActive(true);
            }
            else
            {
                Debug.Log("Disabling gaze for" + kvp.Key);
                kvp.Value.GazeIndicator.SetActive(false);
            }
        }
    }

    /// <summary>
    /// Usually it's best to disable game objects to stop them from interacting, but sometimes
    /// we need to keep the game object active so update gets called.  This script will turn off 
    /// rendering and colliders for these objects.
    /// </summary>
    /// <param name="target">The target to 'disable' or 'enable'</param>
    /// <param name="enable">Whether to enable or disable the renderers/colliders</param>
    void SetRenderersAndColliders(GameObject target, bool enable)
    {
        MeshRenderer[] renderers = target.GetComponentsInChildren<MeshRenderer>();
        foreach (MeshRenderer meshRenderer in renderers)
        {
            meshRenderer.enabled = enable;
        }

        MeshCollider[] colliders = target.GetComponentsInChildren<MeshCollider>();
        foreach (MeshCollider meshCollider in colliders)
        {
            meshCollider.enabled = enable;
        }
    }

    /// <summary>
    /// Tells the client to update a puzzle as being solved
    /// </summary>
    /// <param name="PuzzleIndex">The puzzle that is solved</param>
    [ClientRpc]
    public void RpcPuzzleSolved(int PuzzleIndex)
    {
        if (PuzzleIndex < 0 || PuzzleIndex >= AvatarStuff.Length)
        {
            Debug.Log("Invalid puzzle index " + PuzzleIndex);
        }

        AvatarStuff[PuzzleIndex].Puzzle.GetComponent<IAmAPuzzle>().Complete();

        // should be the person 'paired' with the VR guest, but we don't have that information yet...
        // only hololens should see the mountain top go away.
        if (Immersed == false)
        {
            LevelTopper.SetActive(false);
        }
    }

}
