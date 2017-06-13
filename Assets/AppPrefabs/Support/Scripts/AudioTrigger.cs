using UnityEngine;
using System.Collections;
using HoloToolkit.Unity;

public class AudioTrigger : MonoBehaviour
{
#pragma warning disable 0649
    [SerializeField]
    private string startSound;
    [SerializeField]
    private float startDelay;
#pragma warning restore 0649

    public void PlaySound(string eventName)
    {
        UAudioManager.Instance.PlayEvent(eventName, this.gameObject);
    }

    private void Start()
    {
        if (!string.IsNullOrEmpty(this.startSound))
        {
            StartCoroutine(PlaySoundDelayedCoroutine(this.startSound, this.startDelay));
        }
    }

    private IEnumerator PlaySoundDelayedCoroutine(string eventName, float delayTime)
    {
        yield return new WaitForSeconds(delayTime);
        UAudioManager.Instance.PlayEvent(eventName, this.gameObject);
    }
}