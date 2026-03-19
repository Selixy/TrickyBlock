using UnityEngine;
using DG.Tweening;

public class UI : MonoBehaviour
{
    [SerializeField] private float popDuration = 0.45f;
    [SerializeField] private Ease popEase = Ease.OutBack;

    private void Awake()
    {
        transform.localScale = Vector3.zero;
        transform
            .DOScale(Vector3.one, popDuration)
            .SetEase(popEase);
    }
}
