using System;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.Android;

public class ScreenshotCapture:MonoBehaviour {
    [Tooltip("Prefix for screenshot filenames")]
    public string filenamePrefix = "Screenshot_";

    [Tooltip("Show notification when screenshot is taken")]
    public bool showNotification = true;

    [Tooltip("UI element to display screenshot notification")]
    public GameObject screenshotNotification;

    [Tooltip("How long to show the notification in seconds")]
    public float notificationDuration = 2.0f;

    private void Start() {
        // Hide notification at start if it exists
        if (screenshotNotification != null) {
            screenshotNotification.SetActive(false);
        }

        // Request storage permission on Android
        RequestStoragePermission();
    }

    private void RequestStoragePermission() {
        if (Application.platform == RuntimePlatform.Android) {
            if (!Permission.HasUserAuthorizedPermission(Permission.ExternalStorageWrite)) {
                Permission.RequestUserPermission(Permission.ExternalStorageWrite);
            }
        }
    }

    /// <summary>
    /// Call this method to capture a screenshot
    /// </summary>
    [ContextMenu("CaptureScreenshot")]
    public void CaptureScreenshot() {
        StartCoroutine(CaptureScreenshotCoroutine());
    }

    private IEnumerator CaptureScreenshotCoroutine() {
        // Wait for the end of the frame so we capture everything correctly
        yield return new WaitForEndOfFrame();

        try {
            // Create a texture to hold the screenshot
            Texture2D screenTexture = new Texture2D(Screen.width,Screen.height,TextureFormat.RGB24,false);

            // Read the screen pixels
            screenTexture.ReadPixels(new Rect(0,0,Screen.width,Screen.height),0,0);
            screenTexture.Apply();

            // Convert to PNG bytes
            byte[] bytes = screenTexture.EncodeToPNG();

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
            if (showNotification && screenshotNotification != null) {
                StartCoroutine(ShowNotification());
            }

            Debug.Log("Screenshot saved to: " + path);
        } catch (Exception e) {
            Debug.LogError("Failed to capture screenshot: " + e.Message);
        }
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
        screenshotNotification.SetActive(true);
        yield return new WaitForSeconds(notificationDuration);
        screenshotNotification.SetActive(false);
    }
}