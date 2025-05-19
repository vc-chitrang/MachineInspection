using System.Collections;
using System.Collections.Generic;
using System.IO;
using TextureSource;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(VirtualTextureSource))]
public class CameraCapture:MonoBehaviour {
    public RawImage cameraDisplay;
    public Button captureButton;

    private Texture defaultBackground;
    private int captureWidth;
    private int captureHeight;
    [SerializeField] ImagePredictionAPI imagePredictionAPI;

    void Start() {
        defaultBackground = cameraDisplay.texture;

        captureButton.onClick.AddListener(CaptureScreenshot);

        // Handle screen orientation changes
        Screen.orientation = ScreenOrientation.Portrait;

        cameraDisplay.gameObject.SetActive(true);

        // Listen to OnTexture event from VirtualTextureSource
        // Also able to bind in the inspector
        if (TryGetComponent(out VirtualTextureSource source)) {
            source.OnTexture.AddListener(OnTexture);
        }
        captureWidth = Screen.width;
        captureHeight = Screen.height;
    }

    private void OnDestroy() {
        if (TryGetComponent(out VirtualTextureSource source)) {
            source.OnTexture.RemoveListener(OnTexture);
        }
    }
    private Texture _texture;
    public void OnTexture(Texture texture) {
        // Do whatever 🥳
        // You don't need to think about webcam texture rotation.
        _texture = texture;
        cameraDisplay.texture = _texture;        
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
        string filename = $"Capture_{captureWidth}x{captureHeight}_{System.DateTime.Now:yyyyMMddHHmmss}.jpeg";
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

        imagePredictionAPI.StartUpload(path);
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
}
