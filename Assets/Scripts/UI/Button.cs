using UnityEngine;
using UnityEngine.EventSystems;
using DG.Tweening;
using UnityEngine.SceneManagement;

public class Buttons : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, ISelectHandler, IDeselectHandler, IPointerClickHandler
{
    private enum ButtonActionType
    {
        Play,
        Options,
        Quit,
        LoadGame,
        LoadLobby
    }

    [Header("Idle")]
    [SerializeField] private float idleAmplitude = 8f;
    [SerializeField] private float idleDuration = 1.6f;
    [SerializeField] private Ease idleEase = Ease.InOutSine;

    [Header("State Scale")]
    [SerializeField] private float normalScale = 1f;
    [SerializeField] private float hoverScale = 1.05f;
    [SerializeField] private float selectedScale = 1.1f;
    [SerializeField] private float stateDuration = 0.18f;

    [Header("Click")]
    [SerializeField] private float clickPunchScale = 0.08f;
    [SerializeField] private float clickDuration = 0.22f;
    [SerializeField] private float clickRotationPunch = 5f;

    [Header("Action")]
    [SerializeField] private ButtonActionType actionType = ButtonActionType.Play;

    [Header("Play Settings")]
    [SerializeField] private string sceneToLoad;

    [Header("Options Settings")]
    [SerializeField] private RectTransform optionsPanel;
    [SerializeField] private float optionsSlideLeftAmount = 300f;
    [SerializeField] private float optionsSlideDuration = 0.35f;
    [SerializeField] private Ease optionsSlideEase = Ease.OutCubic;

    private RectTransform rectTransform;
    private Tween idleTween;
    private Tween scaleTween;
    private Tween clickTween;
    private Tween clickRotationTween;
    private Tween optionsSlideTween;
    private Vector2 baseAnchoredPosition;
    private bool isHovered;
    private bool isSelected;

    private void Awake()
    {
        rectTransform = transform as RectTransform;
        baseAnchoredPosition = rectTransform != null ? rectTransform.anchoredPosition : Vector2.zero;
        transform.localScale = Vector3.one * normalScale;
        StartIdleTween();
    }

    private void OnDisable()
    {
        KillTweens();
    }

    private void OnDestroy()
    {
        KillTweens();
    }

    private void StartIdleTween()
    {
        idleTween?.Kill();

        if (rectTransform == null)
        {
            return;
        }

        rectTransform.anchoredPosition = baseAnchoredPosition;
        idleTween = rectTransform
            .DOAnchorPosY(baseAnchoredPosition.y + idleAmplitude, idleDuration)
            .SetEase(idleEase)
            .SetLoops(-1, LoopType.Yoyo);
    }

    private void AnimateToCurrentState()
    {
        float targetScale = normalScale;

        if (isSelected)
        {
            targetScale = selectedScale;
        }
        else if (isHovered)
        {
            targetScale = hoverScale;
        }

        scaleTween?.Kill();
        scaleTween = transform
            .DOScale(targetScale, stateDuration)
            .SetEase(Ease.OutQuad);
    }

    private void KillTweens()
    {
        idleTween?.Kill();
        scaleTween?.Kill();
        clickTween?.Kill();
        clickRotationTween?.Kill();
        optionsSlideTween?.Kill();
    }

    private void ExecuteAction()
    {
        switch (actionType)
        {
            case ButtonActionType.Play:
                if (string.IsNullOrWhiteSpace(sceneToLoad))
                {
                    Debug.LogWarning($"[{nameof(Buttons)}] No scene name set for Play action on {name}.");
                    return;
                }

                SceneManager.LoadScene(sceneToLoad);
                break;

            case ButtonActionType.Options:
                if (optionsPanel == null)
                {
                    Debug.LogWarning($"[{nameof(Buttons)}] No options panel assigned for Options action on {name}.");
                    return;
                }

                optionsSlideTween?.Kill();
                optionsSlideTween = optionsPanel
                    .DOAnchorPosX(optionsPanel.anchoredPosition.x - optionsSlideLeftAmount, optionsSlideDuration)
                    .SetEase(optionsSlideEase);
                break;

            case ButtonActionType.Quit:
#if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
#else
                Application.Quit();
#endif
                break;

            case ButtonActionType.LoadGame:
                if (SceneLoader.Instance != null)
                {
                    SceneLoader.Instance.LoadGameScene();
                }
                else
                {
                    Debug.LogError($"[{nameof(Buttons)}] SceneLoader instance not found!");
                }
                break;

            case ButtonActionType.LoadLobby:
                if (SceneLoader.Instance != null)
                {
                    SceneLoader.Instance.LoadLobbyScene();
                }
                else
                {
                    Debug.LogError($"[{nameof(Buttons)}] SceneLoader instance not found!");
                }
                break;
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        isHovered = true;
        AnimateToCurrentState();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isHovered = false;
        AnimateToCurrentState();
    }

    public void OnSelect(BaseEventData eventData)
    {
        isSelected = true;
        AnimateToCurrentState();
    }

    public void OnDeselect(BaseEventData eventData)
    {
        isSelected = false;
        AnimateToCurrentState();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        clickTween?.Kill();
        clickRotationTween?.Kill();

        clickTween = transform
            .DOPunchScale(Vector3.one * clickPunchScale, clickDuration, 8, 0.65f)
            .SetEase(Ease.OutQuad);

        clickRotationTween = transform
            .DOPunchRotation(new Vector3(0f, 0f, clickRotationPunch), clickDuration, 8, 0.65f)
            .SetEase(Ease.OutQuad)
            .OnComplete(() =>
            {
                AnimateToCurrentState();
                ExecuteAction();
            });
    }
}
