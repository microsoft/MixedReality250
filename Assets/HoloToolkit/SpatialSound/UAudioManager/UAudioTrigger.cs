using UnityEngine;
using HoloToolkit.Unity;

public class UAudioTrigger : MonoBehaviour
{
#pragma warning disable 0649
    [SerializeField]
    private AudioTrigger[] audioEvents;
#pragma warning restore 0649

    [System.Serializable]
    public class AudioTrigger
    {
        public string audioEvent;
        public GameObject emitter;
        public bool playOnStart = false;
        public KeyCode playOnKeyHit;
        public KeyCode stopOnKeyHit;
    }

    private void Start ()
    {
        for (int i = 0; i < this.audioEvents.Length; i++)
        {
            AudioTrigger currentEvent = this.audioEvents[i];
            if (currentEvent.playOnStart)
            {
                PlayAudioTrigger(currentEvent);
            }
        }
    }
    
    private void Update ()
    {
        for (int i = 0; i < this.audioEvents.Length; i++)
        {
            AudioTrigger currentEvent = this.audioEvents[i];
            if (Input.GetKeyDown(currentEvent.playOnKeyHit))
            {
                PlayAudioTrigger(currentEvent);
            }
            if (Input.GetKeyDown(currentEvent.stopOnKeyHit))
            {
                StopAudioTrigger(currentEvent);
            }
        }
    }

    private void PlayAudioTrigger(AudioTrigger audioTrigger)
    {
        if (audioTrigger.emitter == null)
        {
            UAudioManager.Instance.PlayEvent(audioTrigger.audioEvent);
        }
        else
        {
            UAudioManager.Instance.PlayEvent(audioTrigger.audioEvent, audioTrigger.emitter);
        }
    }

    private void StopAudioTrigger(AudioTrigger audioTrigger)
    {
        if (audioTrigger.emitter == null)
        {
            UAudioManager.Instance.StopEvent(audioTrigger.audioEvent);
        }
        else
        {
            UAudioManager.Instance.StopEvent(audioTrigger.audioEvent, audioTrigger.emitter);
        }
    }
}