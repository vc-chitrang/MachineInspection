using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIManager:MonoBehaviour {
    public ImageLoader imageLoader;

    [SerializeField] private RectTransform _resultScreen;
    [SerializeField] private TextMeshProUGUI _resultText;
    [SerializeField] private Button _captureAgainButton;

    [Header("ResultSection")]
    [SerializeField] private TextMeshProUGUI _resultTextHeader;
    [SerializeField] private Image _resultImage;
    [SerializeField] private List<Color> _resultColorList;
    [SerializeField] private Button _buttonManager;

    private void Start() {
        SetResultScreen(false);
        //_captureAgainButton.onClick.AddListener(OnCaptureAgainButtonClicked);
        _buttonManager.onClick.AddListener(OnCaptureAgainButtonClicked);
    }
    private void OnCaptureAgainButtonClicked() {
        SetResultScreen(false);
    }

    private void SetResultScreen(bool isEnable) {
        _resultScreen.gameObject.SetActive(isEnable);
    }

    public void ShowResult(Detection findings) {
        string label = findings.label;
        string message = findings.description;

        SetResultScreen(true);
        _resultTextHeader.text = label;
        _resultText.text = message;

        bool isSuccess = findings.isSuccess;
        if (label.Trim().ToLower().Contains("fuel lid close") || 
            label.Trim().ToLower().Contains("tyre cap close")) {
            isSuccess = true;
        }
        _resultImage.color = isSuccess ? _resultColorList[1] : _resultColorList[0];
    }
}//UIManager class end.
