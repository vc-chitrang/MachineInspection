using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.IO;
using System.Collections.Generic;

public class AndroidCameraCapture:MonoBehaviour {
    private WebCamTexture backCamera;
    public RawImage cameraDisplay;
    public AspectRatioFitter aspectFitter;
    public Button captureButton;

    private bool isCameraAvailable;
    private Texture defaultBackground;
    private int captureWidth;
    private int captureHeight;

    void Start() {
        defaultBackground = cameraDisplay.texture;

        // Set capture dimensions based on screen orientation
        UpdateCaptureDimensions();

        StartCoroutine(InitializeCamera());
        captureButton.onClick.AddListener(CaptureScreenshot);

        // Handle screen orientation changes
        Screen.orientation = ScreenOrientation.AutoRotation;

        cameraDisplay.gameObject.SetActive(true);
    }

    void UpdateCaptureDimensions() {
        // Use portrait resolution
        if (Screen.height > Screen.width) {
            captureWidth = 1080;   // Standard portrait width
            captureHeight = 1920;  // Standard portrait height
        } else {
            captureWidth = 1920;   // Landscape
            captureHeight = 1080;
        }
        Debug.Log($"Capture dimensions set to: {captureWidth}x{captureHeight}");
    }

    IEnumerator InitializeCamera() {
#if !UNITY_EDITOR
    yield return Application.RequestUserAuthorization(UserAuthorization.WebCam);
    if (!Application.HasUserAuthorization(UserAuthorization.WebCam))
    {
        Debug.LogError("Camera permission not granted");
        yield break;
    }
#endif

        WebCamDevice[] devices = WebCamTexture.devices;
        if (devices.Length == 0) {
            Debug.Log("No camera devices found.");
            isCameraAvailable = false;
            yield break;
        }

        foreach (var device in devices) {
            if (!device.isFrontFacing) {
                backCamera = new WebCamTexture(device.name,captureWidth,captureHeight);
                break;
            }
        }

        if (backCamera == null) {
            Debug.Log("Using first available camera");
            backCamera = new WebCamTexture(devices[0].name,captureWidth,captureHeight);
        }

        backCamera.Play();
        cameraDisplay.texture = backCamera;
        isCameraAvailable = true;

        // Ensure correct aspect ratio
        float videoRatio = (float)backCamera.width / backCamera.height;
        aspectFitter.aspectRatio = videoRatio;

        // Adjust scaling
        cameraDisplay.rectTransform.sizeDelta = new Vector2(Screen.width,Screen.height);
        cameraDisplay.rectTransform.localScale = Vector3.one;
    }

#if UNITY_EDITOR
    void CreateTestTexture() {
        Texture2D testTexture = new Texture2D(captureWidth,captureHeight);
        for (int y = 0;y < testTexture.height;y++) {
            for (int x = 0;x < testTexture.width;x++) {
                Color color = ((x & y) != 0 ? Color.white : Color.gray);
                if (x % 100 < 50 && y % 100 < 50)
                    color = Color.red;
                testTexture.SetPixel(x,y,color);
            }
        }
        testTexture.Apply();
        cameraDisplay.texture = testTexture;
    }
#endif

    void Update() {
        if (!isCameraAvailable)
            return;

        if (backCamera != null && backCamera.isPlaying) {
            // Update the aspect ratio to match the camera feed
            float ratio = (float)backCamera.width / (float)backCamera.height;

            // Check the screen orientation and adjust accordingly
            if (Screen.orientation == ScreenOrientation.Portrait || Screen.orientation == ScreenOrientation.PortraitUpsideDown) {
                aspectFitter.aspectMode = AspectRatioFitter.AspectMode.HeightControlsWidth;
            } else {
                aspectFitter.aspectMode = AspectRatioFitter.AspectMode.WidthControlsHeight;
            }

            aspectFitter.aspectRatio = ratio;

            // Handle orientation
            int orient = -backCamera.videoRotationAngle;
            cameraDisplay.rectTransform.localEulerAngles = new Vector3(0,0,orient);

            // Stretch the image to fit the screen
            cameraDisplay.rectTransform.sizeDelta = new Vector2(Screen.width,Screen.height);

            // Check for screen size changes
            if (captureWidth != Screen.width || captureHeight != Screen.height) {
                UpdateCaptureDimensions();
                Debug.Log("UpdateCaptureDimensions");
                // You could add logic here to restart camera with new dimensions if needed
            }
        }
    }

    public void CaptureScreenshot() {
        StartCoroutine(TakeScreenshot());
    }
    [SerializeField] private List<GameObject> uiList;
    private void SetUI(bool isEnable) {
        uiList.ForEach(ui => ui.SetActive(isEnable));
    }
    IEnumerator TakeScreenshot() {
        SetUI(false);
        yield return new WaitForEndOfFrame();

        // Create texture with current screen dimensions
        Texture2D screenshot = new Texture2D(captureWidth,captureHeight,TextureFormat.RGB24,false);

        // Capture the entire screen
        screenshot.ReadPixels(new Rect(0,0,captureWidth,captureHeight),0,0);
        screenshot.Apply();

        // Save the screenshot
        string filename = $"Capture_{captureWidth}x{captureHeight}_{System.DateTime.Now:yyyyMMddHHmmss}.png";
        string path = Path.Combine(Application.persistentDataPath,filename);

        byte[] bytes = screenshot.EncodeToPNG();
        File.WriteAllBytes(path,bytes);

        Debug.Log($"Screenshot saved to: {path} ({captureWidth}x{captureHeight})");

        // Refresh gallery on Android devices
#if UNITY_ANDROID && !UNITY_EDITOR
        RefreshAndroidGallery(path);
#endif

        Destroy(screenshot);
        SetUI(true);
    }

#if UNITY_ANDROID && !UNITY_EDITOR
    void RefreshAndroidGallery(string path)
    {
        AndroidJavaClass mediaScanner = new AndroidJavaClass("android.media.MediaScannerConnection");
        AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
        AndroidJavaObject activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
        mediaScanner.CallStatic("scanFile", activity, new string[] { path }, new string[] { "image/png" }, null);
    }
#endif

    void OnDisable() {
        if (backCamera != null && backCamera.isPlaying) {
            backCamera.Stop();
        }
    }
}