using System.Collections;
using UnityEngine;

public class VoiceFeedbackManager : MonoBehaviour
{
    public AudioSource audioSource;
    public AudioClip[] audioClips;

    public bool isAudioPlaying { get; private set; }

    void Awake()
    {
        if (transform.parent == null)
        {
            DontDestroyOnLoad(gameObject);
        }
    }

    void Start()
    {
        isAudioPlaying = false;
        StartCoroutine(PlayAudioClip(0.1f, 0));
    }

    public IEnumerator PlayAudioClip(float delay, int clipIndex)
    {
        yield return new WaitForSeconds(delay);
        audioSource.clip = audioClips[clipIndex];
        audioSource.Play();
        isAudioPlaying = true;

        while (audioSource.isPlaying)
        {
            yield return null;
        }

        isAudioPlaying = false;
    }
}