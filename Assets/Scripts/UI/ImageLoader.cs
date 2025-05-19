using UnityEngine;
using UnityEngine.UI;
using System.IO;

public class ImageLoader:MonoBehaviour {
    // Reference to your UI Image component
    [SerializeField] private Image _capturedImage;

    // Call this method to load and display the image
    public void LoadAndSetImage(string imagePath) {
        if (File.Exists(imagePath)) {
            byte[] imageData = File.ReadAllBytes(imagePath);

            // Create a Texture2D from the image data
            Texture2D texture = new Texture2D(2,2);
            if (texture.LoadImage(imageData)) {
                // Create a Sprite from the Texture2D
                Sprite sprite = Sprite.Create(
                    texture,
                    new Rect(0,0,texture.width,texture.height),
                    new Vector2(0.5f,0.5f)
                );

                // Set the sprite to the UI Image
                SetCapturedImage(sprite);
            } else {
                Debug.LogError("Failed to load image data.");
            }
        } else {
            Debug.LogError($"File not found at path: {imagePath}");
        }
    }

    // Method to set the sprite
    public void SetCapturedImage(Sprite sprite) {
        if (_capturedImage != null) {
            _capturedImage.sprite = sprite;
            _capturedImage.preserveAspect = true;  // This maintains the aspect ratio
        } else {
            Debug.LogError("Image component not assigned.");
        }
    }
}
