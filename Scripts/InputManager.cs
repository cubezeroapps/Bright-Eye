using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class InputManager : MonoBehaviour
{
    public Button cameraButton;
    public RealTimeDetection realTimeDetection;
    public VoiceFeedbackManager voiceFeedbackManager;
    private Vector2 touchStart;
    private Vector2 lastTouchPosition;
    private bool inputEnabled = false;
    private bool cameraRestartBlocked = false;

    void Start()
    {
        voiceFeedbackManager.PlayAudioClip(0.5f, 1);
        StartCoroutine(EnableInputAfterDelay(0.1f));
    }

    IEnumerator EnableInputAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        cameraButton.onClick.AddListener(HandleCameraStart);
        inputEnabled = true;
    }

    void Update()
    {
        if (!realTimeDetection.isDetecting && inputEnabled && !cameraRestartBlocked)
        {
            HandleDragInput();
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
                    break;

                case TouchPhase.Moved:
                    Vector2 currentTouchPosition = touch.position;
                    Vector2 direction = currentTouchPosition - touchStart;
                    float dragDistance = Vector2.Distance(touchStart, currentTouchPosition);
                    float angleWithUp = Vector2.Angle(direction, Vector2.up);
                    float angleWithDown = Vector2.Angle(direction, Vector2.down);

                    if (dragDistance > 700)
                    {
                        if (angleWithUp < 20)
                        {
                            if (direction.y > 0 && !voiceFeedbackManager.isAudioPlaying)
                            {
                                voiceFeedbackManager.StartCoroutine(voiceFeedbackManager.PlayAudioClip(0.3f, 1));
                            }
                        }
                        else if (angleWithDown < 20)
                        {
                            if (direction.y < 0)
                            {
                                realTimeDetection.StartCamera();
                            }
                        }
                    }
                    break;

                case TouchPhase.Ended:
                    break;
            }
        }
    }

    public void HandleCameraStart()
    {
        if (!cameraRestartBlocked && !realTimeDetection.isDetecting)
        {
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