using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class ImageOutputManager : MonoBehaviour
{
    public RawImage DetectionImage;
    public GameObject DetectionCanvas;

    public bool isAnimating { get; set; }

    private WebCamTexture webCamTexture;

    private void Start()
    {
        isAnimating = false;
        DetectionCanvas.SetActive(false);
        AdjustCanvasScaler();
        DOTween.Init();
    }

    private void AdjustCanvasScaler()
    {
        CanvasScaler canvasScaler = DetectionCanvas.GetComponent<CanvasScaler>();

        if (canvasScaler != null)
        {
            canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasScaler.referenceResolution = new Vector2(Screen.width, Screen.height);
            canvasScaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            canvasScaler.matchWidthOrHeight = 1;
        }
    }

    public void StartCamera()
    {
        if (webCamTexture == null)
        {
            webCamTexture = new WebCamTexture();
        }

        if (!webCamTexture.isPlaying)
        {
            webCamTexture.Play();

            DetectionImage.texture = webCamTexture;
            DetectionImage.material.mainTexture = webCamTexture;
            DetectionImage.rectTransform.localEulerAngles = new Vector3(0, 0, -90);

            AdjustCanvasScaler();
            AdjustImageSize();

            DetectionCanvas.SetActive(true);
            CanvasGroup canvasGroup = DetectionCanvas.GetComponent<CanvasGroup>();

            if (canvasGroup == null)
            {
                canvasGroup = DetectionCanvas.AddComponent<CanvasGroup>();
            }

            CanvasGroup imageCanvasGroup = DetectionImage.GetComponent<CanvasGroup>();

            if (imageCanvasGroup == null)
            {
                imageCanvasGroup = DetectionImage.gameObject.AddComponent<CanvasGroup>();
            }

            canvasGroup.alpha = 0;
            imageCanvasGroup.alpha = 0;

            canvasGroup.DOFade(1, 1f).OnComplete(() =>
            {
                imageCanvasGroup.DOFade(1, 1f).OnComplete(() =>
                {
                    isAnimating = false;
                });
            });
        }
    }

    public void StopCamera()
    {
        if (webCamTexture != null && webCamTexture.isPlaying)
        {
            Texture2D freezeFrame = new Texture2D(webCamTexture.width, webCamTexture.height);
            freezeFrame.SetPixels(webCamTexture.GetPixels());
            freezeFrame.Apply();

            DetectionImage.texture = freezeFrame;
        }

        CanvasGroup canvasGroup = DetectionCanvas.GetComponent<CanvasGroup>();
        CanvasGroup imageCanvasGroup = DetectionImage.GetComponent<CanvasGroup>();

        imageCanvasGroup.DOFade(0, 1f).OnComplete(() =>
        {
            if (webCamTexture != null && webCamTexture.isPlaying)
            {
                webCamTexture.Stop();
            }

            canvasGroup.DOFade(0, 1f).OnComplete(() =>
            {
                DetectionImage.texture = null;
                DetectionCanvas.SetActive(false);
                isAnimating = false;
            });
        });
    }

    private void AdjustImageSize()
    {
        RectTransform rectTransform = DetectionImage.GetComponent<RectTransform>();

        float aspectRatio = 4f / 3f;
        float screenHeight = Screen.height;
        float screenWidth = Screen.width;
        float fixedHeight = screenWidth;
        float adjustedWidth = fixedHeight * aspectRatio;

        rectTransform.sizeDelta = new Vector2(adjustedWidth, fixedHeight);
        rectTransform.anchoredPosition = new Vector2(rectTransform.anchoredPosition.x, -1050);
    }

    public Texture2D GetSnap()
    {
        if (webCamTexture == null)
        {
            return null;
        }

        Texture2D snap = new Texture2D(webCamTexture.width, webCamTexture.height, TextureFormat.RGBA32, false);

        snap.SetPixels(webCamTexture.GetPixels());
        snap.Apply();

        return snap;
    }
}