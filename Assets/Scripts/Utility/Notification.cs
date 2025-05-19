using TMPro;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class Notification:MonoBehaviour {
    public static Notification Instance { get; private set; } = null;
    private void Awake() {
        if (Instance == null) {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        } else {
            Destroy(gameObject);
        }
    }
    private void Start() {
        Hide();
    }

    private readonly float _animationDuration = 0.5f;
    private Ease easeType = Ease.OutSine;

    [SerializeField] private Image _background;
    [SerializeField] private TextMeshProUGUI _messageText;

    [SerializeField] private Color[] colors = new Color[2];

    public void Show(bool isSuccess = false, string message = "Something went wrong!!!") {        
        _messageText.text = message;
        _background.color = isSuccess ? colors[1] : colors[0];
        _background.DOKill();
        _background.GetComponent<RectTransform>().DOAnchorPosY(0,_animationDuration).SetEase(easeType).OnComplete(() => { 
            Invoke(nameof(Hide),4f);        
        });

    }

    private void Hide() {
        _background.DOKill();
        RectTransform _rect = _background.GetComponent<RectTransform>();
        float _targetPos = Mathf.Abs(_rect.rect.height);
        _rect.DOAnchorPosY(_targetPos,_animationDuration).SetEase(easeType);
    }
}//Notification class end.
