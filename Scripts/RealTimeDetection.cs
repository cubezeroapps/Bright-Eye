using UnityEngine;
using System.Collections;
using TMPro;

public class RealTimeDetection : MonoBehaviour
{
    public TextMeshProUGUI resultText;
    public TextMeshProUGUI debugText;
    public ImageOutputManager imageOutputManager;
    public VoiceFeedbackManager voiceFeedbackManager;
    public ImagePreprocessor imagePreprocessor;
    public InputManager inputManager;

    private bool isDetecting = false;
    
    public bool IsDetecting
    {
        get { return isDetecting; }
    }

    void Start()
    {
        BrainCheck.OcrBridgeAndroid.SetUnityGameObjectNameAndMethodName(gameObject.name, "OnOCRComplete");
        BrainCheck.OcrBridgeAndroid.setLanguageForOcr(4);
    }

    public void StartCamera()
    {
        imageOutputManager.StartCamera();
        isDetecting = true;
        debugText.text = "Camera started.";
        voiceFeedbackManager.StartCoroutine(voiceFeedbackManager.PlayAudioClip(0.3f, 2));
        StartCoroutine(DelayedRecognition(3.0f));
    }

    IEnumerator DelayedRecognition(float delay)
    {
        yield return new WaitForSeconds(delay);
        ProcessCurrentImage();
    }

    public void OnOCRComplete(string recognizedText)
    {
        if (!string.IsNullOrEmpty(recognizedText))
        {
            resultText.text = recognizedText;
            debugText.text = "OCR detection complete.";
            imageOutputManager.StopCamera();
            inputManager.BlockCameraStart(2.0f);
            isDetecting = false;
        }
        else
        {
            StartCoroutine(DelayedRecognition(2.0f));
        }
    }

    void ProcessCurrentImage()
    {
        Texture2D snap = imageOutputManager.GetSnap();
        if (snap != null)
        {
            StartCoroutine(ProcessImageAsync(snap));
        }
    }

    IEnumerator ProcessImageAsync(Texture2D snap)
    {
        yield return null;
        Texture2D processedSnap = imagePreprocessor.ProcessImage(snap);
        string imagePath = BrainCheck.OcrBridgeAndroid.SaveTextutreToApplicationPathAndGetPath(snap, "currentImage.png");
        BrainCheck.OcrBridgeAndroid.sendImageToNativeForOCR(imagePath);
        Destroy(snap);
    }

    void OnDestroy()
    {
        CancelInvoke("ProcessCurrentImage");
    }
}