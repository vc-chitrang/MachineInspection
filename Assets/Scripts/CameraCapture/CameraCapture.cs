using System;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Android;

public class CameraCapture:MonoBehaviour {
    [Header("Camera Settings")]
    [Tooltip("The UI RawImage to display camera feed")]
    public RawImage cameraDisplay;

    [Tooltip("Preferred camera to use (leave empty for back camera)")]
    public string preferredCamera = "";

    [Tooltip("Requested camera width (0 for default)")]
    public int requestedWidth = 1280;

    [Tooltip("Requested camera height (0 for default)")]
    public int requestedHeight = 720;

    [Tooltip("Frames per second (0 for default)")]
    public int requestedFPS = 30;

    [Header("Screenshot Settings")]
    [Tooltip("Prefix for screenshot filenames")]
    public string filenamePrefix = "CameraCapture_";

    [Tooltip("Show notification when screenshot is taken")]
    public bool showNotification = true;

    [Tooltip("UI element to display screenshot notification")]
    public GameObject captureNotification;

    [Tooltip("How long to show the notification in seconds")]
    public float notificationDuration = 2.0f;

    [Header("UI Elements")]
    [Tooltip("Button to flip between front/back cameras")]
    public Button flipCameraButton;

    // Private variables
    private WebCamTexture webCamTexture;
    private WebCamDevice[] devices;
    private int currentCameraIndex = -1;

    private void Start() {
        // Request camera permission on Android
        RequestCameraPermission();

        // Hide notification at start if it exists
        if (captureNotification != null) {
            captureNotification.SetActive(false);
        }

        // Add listener to flip camera button if assigned
        if (flipCameraButton != null) {
            flipCameraButton.onClick.AddListener(FlipCamera);
        }

        // Start the camera
        StartCoroutine(InitializeCamera());
    }

    private void RequestCameraPermission() {
        if (Application.platform == RuntimePlatform.Android) {
            if (!Permission.HasUserAuthorizedPermission(Permission.Camera)) {
                Permission.RequestUserPermission(Permission.Camera);
            }

            if (!Permission.HasUserAuthorizedPermission(Permission.ExternalStorageWrite)) {
                Permission.RequestUserPermission(Permission.ExternalStorageWrite);
            }
        }
    }

    private IEnumerator InitializeCamera() {
        // Wait until permissions are granted
        while (Application.platform == RuntimePlatform.Android &&
              (!Permission.HasUserAuthorizedPermission(Permission.Camera) ||
               !Permission.HasUserAuthorizedPermission(Permission.ExternalStorageWrite))) {
            yield return new WaitForSeconds(0.5f);
        }

        // Get all available webcam devices
        devices = WebCamTexture.devices;

        if (devices.Length == 0) {
            Debug.LogError("No camera detected on this device");
            yield break;
        }

        // Find preferred camera or use the first one
        if (!string.IsNullOrEmpty(preferredCamera)) {
            for (int i = 0;i < devices.Length;i++) {
                if (devices[i].name.Contains(preferredCamera)) {
                    currentCameraIndex = i;
                    break;
                }
            }
        }

        // If preferred camera not found, use the back camera by default
        if (currentCameraIndex < 0) {
            for (int i = 0;i < devices.Length;i++) {
                if (!devices[i].isFrontFacing) {
                    currentCameraIndex = i;
                    break;
                }
            }
        }

        // If still not found, use the first camera
        if (currentCameraIndex < 0 && devices.Length > 0) {
            currentCameraIndex = 0;
        }

        StartCamera(currentCameraIndex);
    }

    private void StartCamera(int cameraIndex) {
        if (cameraIndex < 0 || cameraIndex >= devices.Length) {
            Debug.LogError("Invalid camera index");
            return;
        }

        // Stop any existing camera
        if (webCamTexture != null && webCamTexture.isPlaying) {
            webCamTexture.Stop();
        }

        // Create new WebCamTexture
        WebCamDevice device = devices[cameraIndex];
        webCamTexture = new WebCamTexture(
            device.name,
            requestedWidth == 0 ? 1280 : requestedWidth,
            requestedHeight == 0 ? 720 : requestedHeight,
            requestedFPS == 0 ? 30 : requestedFPS
        );

        // Apply texture to RawImage
        if (cameraDisplay != null) {
            cameraDisplay.texture = webCamTexture;
        }

        // Start camera
        webCamTexture.Play();

        // Wait a frame for camera to initialize
        StartCoroutine(AdjustAspectRatio());
    }

    private IEnumerator AdjustAspectRatio() {
        // Wait for camera to start
        yield return new WaitForEndOfFrame();

        if (webCamTexture.width < 100) // Not yet initialized
        {
            yield return new WaitForSeconds(0.5f);
        }

        // Handle rotation
        int rotAngle = -webCamTexture.videoRotationAngle;
        cameraDisplay.rectTransform.localEulerAngles = new Vector3(0,0,rotAngle);

        // Handle aspect ratio
        float videoRatio = (float)webCamTexture.width / (float)webCamTexture.height;

        // Adjust image scale based on rotation
        if (rotAngle == 90 || rotAngle == 270) {
            videoRatio = 1.0f / videoRatio;
        }

        // Set correct aspect ratio on display
        Vector2 sizeDelta = cameraDisplay.rectTransform.sizeDelta;
        sizeDelta.x = sizeDelta.y * videoRatio;
        cameraDisplay.rectTransform.sizeDelta = sizeDelta;

        // Handle mirroring for front camera
        if (devices[currentCameraIndex].isFrontFacing) {
            cameraDisplay.rectTransform.localScale = new Vector3(-1,1,1);
        } else {
            cameraDisplay.rectTransform.localScale = new Vector3(1,1,1);
        }
    }

    /// <summary>
    /// Toggle between front and back cameras
    /// </summary>
    public void FlipCamera() {
        if (devices.Length <= 1)
            return;

        // Find next suitable camera
        int nextCameraIndex = (currentCameraIndex + 1) % devices.Length;

        // Start the new camera
        StartCamera(nextCameraIndex);
        currentCameraIndex = nextCameraIndex;
    }

    /// <summary>
    /// Capture an image from the camera feed
    /// </summary>
    public void CaptureImage() {
        StartCoroutine(CaptureImageCoroutine());
    }

    private IEnumerator CaptureImageCoroutine() {
        if (webCamTexture == null || !webCamTexture.isPlaying) {
            Debug.LogError("Camera is not active");
            yield break;
        }

        // Wait for the end of the frame
        yield return new WaitForEndOfFrame();

        try {
            // Create a texture to hold the screenshot
            Texture2D screenshot = new Texture2D(webCamTexture.width,webCamTexture.height,TextureFormat.RGB24,false);

            // Read the pixels from WebCamTexture
            screenshot.SetPixels(webCamTexture.GetPixels());
            screenshot.Apply();

            // Handle rotation and mirroring
            screenshot = ProcessCapturedImage(screenshot);

            // Convert to PNG bytes
            byte[] bytes = screenshot.EncodeToPNG();

            // Create the filename with timestamp
            string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            string filename = filenamePrefix + timestamp + ".png";

            // Get the path for Android
            string path = GetAndroidSavePath(filename);

            // Save the file
            File.WriteAllBytes(path,bytes);

            // Add to Android gallery
            AddToGallery(path);

            // Show notification if enabled
            if (showNotification && captureNotification != null) {
                StartCoroutine(ShowNotification());
            }

            Debug.Log("Camera image saved to: " + path);
        } catch (Exception e) {
            Debug.LogError("Failed to capture camera image: " + e.Message);
        }
    }

    private Texture2D ProcessCapturedImage(Texture2D original) {
        // Handle rotation based on camera orientation
        int rotAngle = webCamTexture.videoRotationAngle;
        bool mirrored = devices[currentCameraIndex].isFrontFacing;

        // If rotation is needed
        if (rotAngle != 0 || mirrored) {
            Color32[] originalPixels = original.GetPixels32();
            Color32[] rotatedPixels;
            int newWidth, newHeight;

            // Determine new dimensions based on rotation
            if (rotAngle == 90 || rotAngle == 270) {
                newWidth = original.height;
                newHeight = original.width;
                rotatedPixels = new Color32[originalPixels.Length];
            } else {
                newWidth = original.width;
                newHeight = original.height;
                rotatedPixels = new Color32[originalPixels.Length];
            }

            Texture2D rotatedTexture = new Texture2D(newWidth,newHeight,TextureFormat.RGB24,false);

            // Apply rotation and mirroring
            for (int y = 0;y < original.height;y++) {
                for (int x = 0;x < original.width;x++) {
                    int originalIndex = y * original.width + x;
                    int newX = x;
                    int newY = y;

                    // Apply rotation
                    switch (rotAngle) {
                        case 90:
                        newX = original.height - 1 - y;
                        newY = x;
                        break;
                        case 180:
                        newX = original.width - 1 - x;
                        newY = original.height - 1 - y;
                        break;
                        case 270:
                        newX = y;
                        newY = original.width - 1 - x;
                        break;
                    }

                    // Apply mirroring for front camera
                    if (mirrored) {
                        if (rotAngle == 90 || rotAngle == 270) {
                            newY = rotatedTexture.height - 1 - newY;
                        } else {
                            newX = rotatedTexture.width - 1 - newX;
                        }

                    }

                    int rotatedIndex = newY * rotatedTexture.width + newX;
                    rotatedPixels[rotatedIndex] = originalPixels[originalIndex];
                }
            }

            rotatedTexture.SetPixels32(rotatedPixels);
            rotatedTexture.Apply();

            return rotatedTexture;
        }

        return original;
    }

    private string GetAndroidSavePath(string filename) {
        string path;

        if (Application.platform == RuntimePlatform.Android) {
            // Use DCIM directory for Android
            path = Path.Combine(Application.persistentDataPath,filename);
        } else {
            // Fallback for editor or other platforms
            path = Path.Combine(Application.persistentDataPath,filename);
        }

        return path;
    }

    private void AddToGallery(string path) {
        if (Application.platform == RuntimePlatform.Android) {
            // Make the image visible in the Android Gallery
            using (AndroidJavaClass jcUnityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
            using (AndroidJavaObject joActivity = jcUnityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
            using (AndroidJavaObject joContext = joActivity.Call<AndroidJavaObject>("getApplicationContext"))
            using (AndroidJavaClass jcMediaScannerConnection = new AndroidJavaClass("android.media.MediaScannerConnection")) {
                jcMediaScannerConnection.CallStatic("scanFile",joContext,new string[] { path },new string[] { "image/png" },null);
            }
        }
    }

    private IEnumerator ShowNotification() {
        captureNotification.SetActive(true);
        yield return new WaitForSeconds(notificationDuration);
        captureNotification.SetActive(false);
    }

    private void OnDestroy() {
        // Stop the camera when this object is destroyed
        if (webCamTexture != null && webCamTexture.isPlaying) {
            webCamTexture.Stop();
        }
    }
}