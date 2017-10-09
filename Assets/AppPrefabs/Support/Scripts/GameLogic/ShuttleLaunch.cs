using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using HoloToolkit.Unity;

public class ShuttleLaunch : MonoBehaviour {

    public float atmosphereHeight = 12f;
    public float ceilingHeight = 20f;
    public float stageOneSpeed = .5f;
    public float stageTwoSpeed = 1f;
    public float startTime = 1f;
    public float waveTime = 1f;
    public float RocketShrinkTime = 2f;
    public GameObject Shuttle;
    public GameObject FuelTank;
    public GameObject FuelThruster1;
    public GameObject FuelThruster2;
    public GameObject FuelThruster1Flame;
    public GameObject FuelThruster2Flame;
    public GameObject[] ShuttleThrusters;
    public GameObject Smoke;
    public GameObject Atmosphere;
    public GameObject CeilingRift;
    public GameObject CeilingHole;
    public GameObject Water;
    public GameObject siloDoor1;
    public GameObject siloDoor2;
    public Texture waterRocketTex;

    private Vector3 target1Pos;
    private Vector3 target2Pos;
    private Vector3 target3Pos;
    private Vector3 startLocalScale;
    private bool startLaunch = false;
    public float curTime;
    private Renderer waterRend;
    private Texture waterWavesTex;
    private enum launchStage { waiting, start, toAtmosphere, toCeiling, toInfinity, Done };
    launchStage curLaunchStage;
    private float speed;
    LevelControl levelState;
    //values for reseting
    public Vector3 ShuttleStartPos;
    public Vector3 FuelTankStartPos;
    public Vector3 FuelThruster1StartPos;
    public Vector3 FuelThruster2StartPos;
    public Vector3 siloDoor1StartPos;
    public Vector3 siloDoor2StartPos;
    public Quaternion ShuttleStartRot;
    public Quaternion FuelTankStartRot;
    public Quaternion FuelThruster1StartRot;
    public Quaternion FuelThruster2StartRot;

    // Use this for initialization
    void Start () {
        levelState = LevelControl.Instance;
        startLocalScale = Shuttle.transform.localScale;
        // Vector3 atmosPos = new Vector3(Atmosphere.transform.position.x, Atmosphere.transform.position.y + atmosphereHeight, Atmosphere.transform.position.z);
        Atmosphere.transform.localPosition += Vector3.up * atmosphereHeight * 5;

        CeilingHole.SetActive(true);
        CeilingRift.SetActive(false);
        // Vector3 CeilingPos = new Vector3(CeilingRift.transform.position.x, CeilingRift.transform.position.y + ceilingHeight, CeilingRift.transform.position.z);
        CeilingRift.transform.localPosition = new Vector3(CeilingRift.transform.localPosition.x, ceilingHeight * 5, CeilingRift.transform.localPosition.z);

        curLaunchStage = launchStage.start;

        speed = stageOneSpeed;
        curTime = startTime;
        waterRend = Water.GetComponent<Renderer>();
        waterWavesTex = waterRend.material.GetTexture("_DispTex");

        FuelThruster1Flame.transform.localScale = new Vector3(0, 0, 0);
        FuelThruster1Flame.SetActive(false);
        FuelThruster2Flame.transform.localScale = new Vector3(0, 0, 0);
        FuelThruster2Flame.SetActive(false);

        for (int i = 0; i < ShuttleThrusters.Length; i++)
        {
            ShuttleThrusters[i].transform.localScale = new Vector3(0, 0, 0);
            ShuttleThrusters[i].SetActive(false);
        }

        //store values for reset
        ShuttleStartPos = Shuttle.transform.localPosition;
        FuelTankStartPos = FuelTank.transform.localPosition;
        FuelThruster1StartPos = FuelThruster1.transform.localPosition;
        FuelThruster2StartPos = FuelThruster2.transform.localPosition;
        siloDoor1StartPos = siloDoor1.transform.localPosition;
        siloDoor2StartPos = siloDoor2.transform.localPosition;
        ShuttleStartRot = Shuttle.transform.localRotation;
        FuelTankStartRot = FuelTank.transform.localRotation;
        FuelThruster1StartRot = FuelThruster1.transform.localRotation;
        FuelThruster2StartRot = FuelThruster2.transform.localRotation;

    }
	
	// Update is called once per frame
	void Update () {
        if (Input.GetKeyDown(KeyCode.L))
        {
            //Hook into start launch here
            startLaunching();
        }
        if (Input.GetKeyDown(KeyCode.R))
        {
            //Hook into reset here
            resetLaunch();
        }
        if (startLaunch)
        {
            switch(curLaunchStage)
            {
                case launchStage.start:
                    {
                        if (curTime < 0)
                        {
                            startOver();
                        }

                        curTime -= Time.deltaTime;
                        float timeRat = curTime / startTime;
                        FuelThruster1Flame.transform.localScale = Vector3.Lerp(Vector3.one, FuelThruster2Flame.transform.localScale, timeRat);
                        FuelThruster2Flame.transform.localScale = Vector3.Lerp(Vector3.one, FuelThruster2Flame.transform.localScale, timeRat);
                        for (int i = 0; i < ShuttleThrusters.Length; i++)
                        {
                            ShuttleThrusters[i].transform.localScale = Vector3.Lerp(Vector3.one, ShuttleThrusters[i].transform.localScale, timeRat);
                        }

                        float step = .15f * Time.deltaTime;
                        siloDoor1.transform.localPosition = Vector3.MoveTowards(siloDoor1.transform.localPosition, new Vector3(siloDoor1StartPos.x + -.2f, siloDoor1StartPos.y, siloDoor1StartPos.z), step);
                        siloDoor2.transform.localPosition = Vector3.MoveTowards(siloDoor2.transform.localPosition, new Vector3(siloDoor2StartPos.x + .2f, siloDoor2StartPos.y, siloDoor2StartPos.z), step);
                    }
                    break;
                case launchStage.toAtmosphere:
                    {
                        float step = speed * Time.deltaTime * (levelState.Immersed ? LevelControl.ImmersiveScale : 1);
                        Shuttle.transform.position = Vector3.MoveTowards(Shuttle.transform.position, target1Pos, step);
                        if(Shuttle.transform.position.y > levelState.LevelTopper.transform.position.y+ (levelState.Immersed ? LevelControl.ImmersiveScale : 1) * 0.33f)
                        {
                            levelState.LevelTopper.SetActive(false);
                        }
                        if (curTime > 0)
                        {
                            waterRend.material.SetFloat("_DispAmount", Mathf.Lerp(0, .07f, (curTime / waveTime)));
                            curTime -= Time.deltaTime;
                        }


                        if ((Shuttle.transform.position - target1Pos).magnitude < 0.01f)
                        {
                            reachedAtmosphere();
                        }
                    }
                    break;
                case launchStage.toCeiling:
                    {
                        float step = speed * Time.deltaTime * (levelState.Immersed ? LevelControl.ImmersiveScale : 1);
                        Shuttle.transform.position = Vector3.MoveTowards(Shuttle.transform.position, target2Pos, step);

                        if ((Shuttle.transform.position - target2Pos).magnitude < 0.01f)
                        {
                            reachedCeiling();
                        }
                        for (int i = 0; i < ShuttleThrusters.Length; i++)
                        {
                            ShuttleThrusters[i].transform.localScale = Vector3.Lerp(new Vector3(1, 2, 1), ShuttleThrusters[i].transform.localScale, (curTime / startTime));
                        }
                        curTime -= Time.deltaTime;
                    }
                    break;
                case launchStage.toInfinity:
                    {
                        float step = speed * Time.deltaTime;
                        Shuttle.transform.position = Vector3.MoveTowards(Shuttle.transform.position, target3Pos, step);

                        if (curTime < 0)
                        {
                            Done();
                        }

                        Shuttle.transform.localScale = Vector3.Lerp(new Vector3(0, 0, 0), new Vector3(1, 1, 1), (curTime / RocketShrinkTime));
                        curTime -= Time.deltaTime;
                    }
                    break;
                case launchStage.Done:
                    break;

            }
        }
	}

    public void startLaunching()
    {
        Atmosphere.SetActive(true);
        target1Pos = Shuttle.transform.position;
        target1Pos.y += atmosphereHeight * (levelState.Immersed ? LevelControl.ImmersiveScale : 1);
        target2Pos = Shuttle.transform.position;
        target2Pos.y += ceilingHeight * (levelState.Immersed ? LevelControl.ImmersiveScale : 1);
        target3Pos = Shuttle.transform.position;
        target3Pos.y += (ceilingHeight + 3) * (levelState.Immersed ? LevelControl.ImmersiveScale : 1);

        curTime = waveTime;
        startLaunch = true;
        Debug.Log("Launching!!");
        FuelThruster1Flame.SetActive(true);
        FuelThruster2Flame.SetActive(true);
        for (int i = 0; i < ShuttleThrusters.Length; i++)
        {
            ShuttleThrusters[i].SetActive(true);
        }
        Smoke.SetActive(true);
        Atmosphere.SetActive(true);

        waterRend.material.SetFloat("_DispAmount", 0.07f);
        waterRend.material.SetTexture("_DispTex", waterRocketTex);
        Water.GetComponent<ScrollUVWave>().scrollSpeed = -.2f;
        UAudioManager.Instance.PlayEvent("Shuttle_Start", Shuttle);
        UAudioManager.Instance.PlayEvent("Shuttle_Launch", Shuttle);
    }

    void startOver ()
    {
        curTime = waveTime;
        curLaunchStage = launchStage.toAtmosphere;
        
        Debug.Log("STartover");
    }

    void Done()
    {
        curLaunchStage = launchStage.Done;
        startLaunch = false;
    }

    void reachedAtmosphere()
    {
        Debug.Log("Atmosphere!!");
        curTime = startTime;
        curLaunchStage = launchStage.toCeiling;
        speed = stageTwoSpeed;
        FuelTank.transform.parent = null;
        Rigidbody FuelTankRB = FuelTank.AddComponent<Rigidbody>();
        FuelTankRB.useGravity = false;
        FuelTankRB.AddForce(0, .1f, -.15f, ForceMode.Impulse);
        FuelTankRB.AddTorque(0, 0, -.25f, ForceMode.Impulse);
        FuelThruster1.transform.parent = null;
        Rigidbody FuelThruster1RB = FuelThruster1.AddComponent<Rigidbody>();
        FuelThruster1RB.useGravity = false;
        FuelThruster1RB.AddForce(.15f, .1f, -.15f, ForceMode.Impulse);
        FuelThruster1RB.AddTorque(-.15f, 0, -.25f, ForceMode.Impulse);
        FuelThruster2.transform.parent = null;
        Rigidbody FuelThruster2RB = FuelThruster2.AddComponent<Rigidbody>();
        FuelThruster2RB.useGravity = false;
        FuelThruster2RB.AddForce(-.15f, .1f, -.15f, ForceMode.Impulse);
        FuelThruster2RB.AddTorque(-.15f, 0, -.25f, ForceMode.Impulse);
        FuelThruster1Flame.SetActive(false);
        FuelThruster2Flame.SetActive(false);
#pragma warning disable 0618
        Smoke.GetComponent<ParticleSystem>().emissionRate = 0;
#pragma warning restore 0618

        waterRend.material.SetFloat("_DispAmount", 0.06f);
        waterRend.material.SetTexture("_DispTex", waterWavesTex);
        Water.GetComponent<ScrollUVWave>().scrollSpeed = 0.085f;
        CeilingRift.SetActive(true);
        UAudioManager.Instance.PlayEvent("Shuttle_Atmosphere", this.gameObject);
    }

    void reachedCeiling()
    {
        Debug.Log("Ceiling!!");
        curTime = RocketShrinkTime;
        curLaunchStage = launchStage.toInfinity;
        Smoke.SetActive(false);
        CeilingHole.SetActive(false);
    }

    public void resetLaunch()
    {
        startLaunch = false;
        waterRend.material.SetFloat("_DispAmount", 0.06f);
        waterRend.material.SetTexture("_DispTex", waterWavesTex);
        Water.GetComponent<ScrollUVWave>().scrollSpeed = 0.085f;
#pragma warning disable 0618
        Smoke.GetComponent<ParticleSystem>().emissionRate = 10;
#pragma warning restore 0618
        Smoke.SetActive(false);
       // Atmosphere.transform.position = new Vector3(0,0,0);
        Atmosphere.SetActive(false);
        //  CeilingRift.transform.position = new Vector3(0, 0, 0);
        CeilingRift.transform.localPosition = new Vector3(CeilingRift.transform.localPosition.x, ceilingHeight * 5, CeilingRift.transform.localPosition.z);
        CeilingRift.SetActive(false);
        CeilingHole.SetActive(true);
        curLaunchStage = launchStage.waiting;
        Destroy(FuelThruster1.GetComponent<Rigidbody>());
        Destroy(FuelThruster2.GetComponent<Rigidbody>());
        Destroy(FuelTank.GetComponent<Rigidbody>());
        FuelThruster1.transform.parent = Shuttle.transform;
        
        FuelThruster2.transform.parent = Shuttle.transform;
        
        FuelTank.transform.parent = Shuttle.transform;
        siloDoor1.transform.localPosition = siloDoor1StartPos;
        siloDoor2.transform.localPosition = siloDoor2StartPos;

        Shuttle.transform.localPosition = ShuttleStartPos;
        Shuttle.transform.localRotation = ShuttleStartRot;
        Shuttle.transform.localScale = startLocalScale;

        FuelTank.transform.localPosition = FuelTankStartPos;
        FuelTank.transform.localRotation = FuelTankStartRot;
        FuelTank.transform.localScale = Vector3.one;

        FuelThruster1.transform.localPosition = FuelThruster1StartPos;
        FuelThruster1.transform.localRotation = FuelThruster1StartRot;
        FuelThruster1.transform.localScale = Vector3.one;

        FuelThruster2.transform.localPosition = FuelThruster2StartPos;
        FuelThruster2.transform.localRotation = FuelThruster2StartRot;
        FuelThruster2.transform.localScale = Vector3.one;

        Start();
    }
}
