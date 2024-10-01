using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.IO;

public class VoiceFeedbackManager : MonoBehaviour
{
    public AudioSource audioSource;
    public AudioClip[] audioClips;

    public bool isAudioPlaying { get; private set; }

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

    public void PlayAudioFromPath(string path)
    {
        StartCoroutine(PlayAudioFromPrefabName(path));
    }

    private IEnumerator PlayAudioFromPrefabName(string path)
    {
        string audioPath = path + ".wav";
        string fullPath;

    #if UNITY_ANDROID
        fullPath = "jar:file://" + Application.dataPath + "!/assets/" + audioPath;
    #else
        fullPath = Path.Combine(Application.streamingAssetsPath, audioPath);
    #endif

        using (UnityWebRequest www = UnityWebRequestMultimedia.GetAudioClip(fullPath, AudioType.WAV))
        {
            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.Success)
            {
                AudioClip clip = DownloadHandlerAudioClip.GetContent(www);
                audioSource.clip = clip;
                audioSource.Play();
                isAudioPlaying = true;

                while (audioSource.isPlaying)
                {
                    yield return null;
                }
                isAudioPlaying = false;
            }
        }
    }
}