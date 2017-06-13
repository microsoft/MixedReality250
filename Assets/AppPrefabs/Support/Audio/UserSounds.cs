using UnityEngine;
using HoloToolkit.Unity;

public class UserSounds : MonoBehaviour
{
    [SerializeField]
    private float stepDistance = 0.3f;
#pragma warning disable 0649
    [SerializeField]
    private string stepEvent;
#pragma warning restore 0649
    private Vector3 lastStep = Vector3.zero;
    private float currentStep = 0;

    private void Start()
    {
        this.lastStep = this.transform.position;
    }

    private void Update()
    {
        UpdateStepDistance();
    }

    private void UpdateStepDistance()
    {
        this.currentStep += Vector3.Distance(this.lastStep, this.transform.position);
        this.lastStep = this.transform.position;
        if (this.currentStep >= this.stepDistance)
        {
            this.currentStep = 0;
            UAudioManager.Instance.PlayEvent(this.stepEvent, this.gameObject);
        }
    }
}