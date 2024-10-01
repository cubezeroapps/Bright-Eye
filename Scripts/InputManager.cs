using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class InputManager : MonoBehaviour
{
    public RealTimeDetection realTimeDetection;
    public VoiceFeedbackManager voiceFeedbackManager;
    public ImageOutputManager imageOutputManager;
    public PageManager pageManager;
    public DBManager dbManager;

    private Vector2 touchStart;
    private Button cameraButton;
    private bool isShaking = false;
    private bool inputEnabled = false;
    private bool cameraRestartBlocked = false;
    private bool isHorizontalSwipe = false;
    private bool horizontalSwipeHandled = false;
    private bool verticalSwipeHandled = false;
    private bool isSingleFinger = true;

    private float accumulatedSwipeDistance = 0f;
    private float shakeDetectionThreshold = 50f;
    private float timeSinceLastShake = 0f;
    private float maxShakeInterval = 1f;
    private int shakeCount = 0;

    void Start()
    {
        voiceFeedbackManager.PlayAudioClip(0.5f, 1);
        StartCoroutine(EnableInputAfterDelay(1f));
    }

    public void AssignCameraButton(GameObject homePage)
    {
        cameraButton = homePage.GetComponentInChildren<Button>();

        if (cameraButton != null)
        {
            cameraButton.onClick.AddListener(HandleCameraStart);
        }
    }

    IEnumerator EnableInputAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        inputEnabled = true;
    }

    void Update()
    {
        if (inputEnabled && !cameraRestartBlocked && !pageManager.isAnimating && !imageOutputManager.isAnimating)
        {
            HandleTouchInput();

            if (!realTimeDetection.isDetecting)
            {
                DetectShake();
            }
        }
    }

    void HandleTouchInput()
    {
        int touchCount = Input.touchCount;

        if (touchCount == 2)
        {
            isSingleFinger = false;
        }

        if (touchCount == 1 && isSingleFinger)
        {
            HandleDragInput();
        }

        if (touchCount >= 2)
        {
            isSingleFinger = false;
        }

        if (touchCount == 0)
        {
            isSingleFinger = true;
        }
    }

    void HandleDragInput()
    {
        if (Input.touchCount == 1)
        {
            Touch touch = Input.GetTouch(0);
            switch (touch.phase)
            {
                case TouchPhase.Began:
                    touchStart = touch.position;
                    isHorizontalSwipe = false;
                    accumulatedSwipeDistance = 0f;
                    horizontalSwipeHandled = false;
                    verticalSwipeHandled = false;
                    break;

                case TouchPhase.Moved:
                    if (horizontalSwipeHandled || verticalSwipeHandled) return;

                    Vector2 currentTouchPosition = touch.position;
                    Vector2 direction = currentTouchPosition - touchStart;
                    float dragDistanceX = Mathf.Abs(direction.x);
                    float dragDistanceY = Mathf.Abs(direction.y);

                    if (!isHorizontalSwipe)
                    {
                        if (dragDistanceX > dragDistanceY && dragDistanceX > 200)
                        {
                            isHorizontalSwipe = true;
                        }
                        else if (dragDistanceY > dragDistanceX && dragDistanceY > 200)
                        {
                            isHorizontalSwipe = false;
                            HandleVerticalScroll(direction.y);
                            return;
                        }
                    }

                    if (isHorizontalSwipe && !realTimeDetection.isDetecting)
                    {
                        HandleHorizontalScroll(direction.x);
                    }
                    break;

                case TouchPhase.Ended:
                    horizontalSwipeHandled = false;
                    verticalSwipeHandled = false;
                    break;
            }
        }
    }

    void HandleHorizontalScroll(float directionX)
    {
        accumulatedSwipeDistance += directionX;

        if (Mathf.Abs(accumulatedSwipeDistance) >= 500)
        {
            if (accumulatedSwipeDistance > 0)
            {
                HandleRightSwipe();
            }
            else if (accumulatedSwipeDistance < 0)
            {
                HandleLeftSwipe();
            }

            accumulatedSwipeDistance = 0f;
            horizontalSwipeHandled = true;
        }
    }

    void HandleVerticalScroll(float directionY)
    {
        if (Mathf.Abs(directionY) > 700)
        {
            int currentPageIndex = pageManager.GetCurrentPageIndex();

            if (directionY > 0 && !realTimeDetection.isDetecting && !voiceFeedbackManager.isAudioPlaying)
            {
                Taptic.Success();

                if (currentPageIndex == 0)
                {
                    voiceFeedbackManager.StartCoroutine(voiceFeedbackManager.PlayAudioClip(0.3f, 1));
                }
                else
                {
                    string barcodeKey = "PageBarcode_" + currentPageIndex;
                    string barcode = PlayerPrefs.GetString(barcodeKey, "");

                    if (!string.IsNullOrEmpty(barcode))
                    {
                        dbManager.PlayManualAudio(barcode);
                    }
                }
            }
            else if (directionY < 0)
            {
                Taptic.Success();

                if (realTimeDetection.isDetecting)
                {
                    voiceFeedbackManager.StartCoroutine(voiceFeedbackManager.PlayAudioClip(0.3f, 7));
                    imageOutputManager.isAnimating = true;
                    realTimeDetection.isDetecting = false;
                    imageOutputManager.StopCamera();
                }
                else
                {
                    imageOutputManager.isAnimating = true;
                    realTimeDetection.isDetecting = true;
                    realTimeDetection.StartCamera();
                }
            }
            verticalSwipeHandled = true;
        }
    }

    void HandleRightSwipe()
    {
        int currentPageIndex = pageManager.GetCurrentPageIndex();

        if (currentPageIndex > 0)
        {
            currentPageIndex--;
            Taptic.Success();
            pageManager.MoveToPage(currentPageIndex);
        }
    }

    void HandleLeftSwipe()
    {
        int currentPageIndex = pageManager.GetCurrentPageIndex();
        int totalPages = pageManager.GetTotalPages();

        if (currentPageIndex < totalPages - 1)
        {
            currentPageIndex++;
            Taptic.Success();
            pageManager.MoveToPage(currentPageIndex);
        }
    }

    private void DetectShake()
    {
        if (Input.acceleration.sqrMagnitude > shakeDetectionThreshold)
        {
            if (!isShaking)
            {
                isShaking = true;
                shakeCount++;
                timeSinceLastShake = 0f;

                if (shakeCount >= 2 && pageManager.GetCurrentPageIndex() != 0)
                {
                    pageManager.RequestPageDelete();
                    shakeCount = 0;
                    timeSinceLastShake = 0f;
                }
            }
        }
        else
        {
            if (isShaking)
            {
                isShaking = false;
            }

            if (shakeCount > 0)
            {
                timeSinceLastShake += Time.deltaTime;
                if (timeSinceLastShake > maxShakeInterval)
                {
                    shakeCount = 0;
                    timeSinceLastShake = 0f;
                }
            }
        }
    }

    public void HandleCameraStart()
    {
        if (!cameraRestartBlocked && !realTimeDetection.isDetecting && !pageManager.isAnimating)
        {
            imageOutputManager.isAnimating = true;
            realTimeDetection.isDetecting = true;
            realTimeDetection.StartCamera();
        }
    }

    public IEnumerator BlockCameraStart(float duration)
    {
        cameraRestartBlocked = true;
        yield return new WaitForSeconds(duration);
        cameraRestartBlocked = false;
    }
}