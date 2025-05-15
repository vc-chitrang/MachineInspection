using UnityEngine;
using UnityEngine.UI;

public class CameraUISetup:MonoBehaviour {
    [Header("References")]
    public CameraCapture cameraCapture;

    [Header("UI Elements")]
    public Button captureButton;
    public Button flipCameraButton;

    void Start() {
        // Ensure we have a CameraCapture component
        if (cameraCapture == null) {
            cameraCapture = FindObjectOfType<CameraCapture>();

            if (cameraCapture == null) {
                Debug.LogError("CameraCapture component not found! Please add one to the scene.");
                return;
            }
        }

        // Set up capture button
        if (captureButton != null) {
            captureButton.onClick.AddListener(cameraCapture.CaptureImage);
        }

        // Set up flip camera button
        if (flipCameraButton != null && cameraCapture.flipCameraButton == null) {
            cameraCapture.flipCameraButton = flipCameraButton;
            flipCameraButton.onClick.AddListener(cameraCapture.FlipCamera);
        }
    }
}