using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using ZXing;

public class RealTimeDetection : MonoBehaviour
{
    public RawImage cameraStreamImage;
    public RenderTexture cameraRenderTexture; 

    public CanvasGroup fadeGroup;
    public ImageOutputManager imageOutputManager;
    public VoiceFeedbackManager voiceFeedbackManager;
    public InputManager inputManager;
    public DBManager dbManager;
    public PageManager pageManager;

    public bool isDetecting { get; set; }

    void Start()
    {
        isDetecting = false;
        cameraStreamImage.texture = cameraRenderTexture;
    }

    public void StartCamera()
    {
        fadeGroup.DOFade(1, 0.5f).OnComplete(() =>
        {
            imageOutputManager.StartCamera();
            StartCoroutine(DelayedRecognition(3.0f));
            voiceFeedbackManager.StartCoroutine(voiceFeedbackManager.PlayAudioClip(0.3f, 2));
        });
    }

    IEnumerator DelayedRecognition(float delay)
    {
        yield return new WaitForSeconds(delay);
        ProcessCurrentImage();
    }

    public void RecognitionComplete(string recognizedText)
    {
        if (!string.IsNullOrEmpty(recognizedText))
        {
            imageOutputManager.StopCamera();
            StartCoroutine(inputManager.BlockCameraStart(2.0f));
            isDetecting = false;

            dbManager.LoadPrefabData(recognizedText, (string prefabName) =>
            {
                if (!string.IsNullOrEmpty(prefabName))
                {
                    int totalPages = PlayerPrefs.GetInt("TotalPages", 1);
                    bool isPageAlreadyLoaded = false;

                    for (int i = 1; i < totalPages; i++)
                    {
                        string savedPrefabName = PlayerPrefs.GetString("PagePrefab_" + i, "");

                        if (savedPrefabName == prefabName)
                        {
                            isPageAlreadyLoaded = true;
                            break;
                        }
                    }

                    if (isPageAlreadyLoaded)
                    {
                        voiceFeedbackManager.StartCoroutine(voiceFeedbackManager.PlayAudioClip(0.3f, 4));
                    }
                    else
                    {
                        voiceFeedbackManager.StartCoroutine(voiceFeedbackManager.PlayAudioClip(0.3f, 3));
                        dbManager.LoadPrefabFromBundle(prefabName, recognizedText);
                    }
                }
            });
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

        var barcodeReader = new BarcodeReader
        {
            AutoRotate = true,
            Options = new ZXing.Common.DecodingOptions
            {
                PossibleFormats = new List<ZXing.BarcodeFormat>
                {
                    ZXing.BarcodeFormat.EAN_13,
                    ZXing.BarcodeFormat.UPC_A
                },
                TryInverted = true,
            }
        };

        var result = barcodeReader.Decode(snap.GetPixels32(), snap.width, snap.height);

        if (result != null)
        {
            RecognitionComplete(result.Text);
        }
        else
        {
            RecognitionComplete("");
        }
        Destroy(snap);
    }
}