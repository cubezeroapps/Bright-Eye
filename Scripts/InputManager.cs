using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class InputManager : MonoBehaviour
{
    public Button cameraButton;
    public RealTimeDetection realTimeDetection;
    public VoiceFeedbackManager voiceFeedbackManager;
    private bool inputEnabled = false;
    private bool isDragging = false;
    private Vector2 touchStart;

    void Start()
    {
        voiceFeedbackManager.PlayAudioClip(0.5f, 1);
        StartCoroutine(EnableInputAfterDelay(0.1f));
    }

    IEnumerator EnableInputAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        cameraButton.onClick.AddListener(realTimeDetection.StartCamera);
        inputEnabled = true;
    }

    void Update()
    {
        if (!realTimeDetection.IsDetecting && inputEnabled)
        {
            if (Input.touchCount > 0)
            {
                Touch touch = Input.GetTouch(0);

                switch (touch.phase)
                {
                    case TouchPhase.Began:
                        touchStart = touch.position;
                        isDragging = false;
                        break;
                    case TouchPhase.Moved:
                        if (!isDragging)
                        {
                            Vector2 currentTouchPosition = touch.position;
                            Vector2 direction = currentTouchPosition - touchStart;
                            float dragDistance = Vector2.Distance(touchStart, currentTouchPosition);

                            if (dragDistance > 450 && Mathf.Abs(direction.x) < Mathf.Abs(direction.y))
                            {
                                if (direction.y < 0)
                                {
                                    isDragging = true;
                                    realTimeDetection.StartCamera();
                                }
                                else if (direction.y > 0 && !voiceFeedbackManager.IsAudioPlaying)
                                {
                                    isDragging = true;
                                    voiceFeedbackManager.StartCoroutine(voiceFeedbackManager.PlayAudioClip(0.3f, 1));
                                }
                            }
                        }
                        break;
                }
            }
        }
    }
}