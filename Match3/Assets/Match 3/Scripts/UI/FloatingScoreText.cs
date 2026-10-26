using UnityEngine;
using TMPro;
using DG.Tweening;
using Unity.VisualScripting;


namespace Match3.UI
{
    /// <summary>
    /// Отвечает за один всплывающий текст очков.
    ///  Не уничтожает себя (Destroy), а возвращается в пул через событие.
    /// </summary>
    public class FloatingScoreText : MonoBehaviour
    {
        [SerializeField] private TMP_Text _text;
        [SerializeField] private float _duration = 0.8f;
        [SerializeField] private float _riseDistance = 1.5f;

        private Tween _tween;

        public void Play(Vector3 worldPosition, int amout)
        {
            _text.text = $"+{amout}";
            transform.position = worldPosition;
            gameObject.SetActive(true);


            // Сбрасываем прозрачность и позицию.
            _text.alpha = 1f;
            transform.localScale = Vector3.one;

            _tween?.Kill();

            _tween = DOTween.Sequence()
                .Join(transform.DOMoveY(worldPosition.y + _riseDistance, _duration).SetEase(Ease.OutQuad))
                .Join(transform.DOScale(1.2f, _duration * 0.3f).SetEase(Ease.OutBack).SetLoops(2, LoopType.Yoyo))
                .Join(_text.DOFade(0f, _duration).SetEase(Ease.InQuad))
                .OnComplete(() => gameObject.SetActive(false));
        }

        private void OnDisable()
        {
            _tween?.Kill();
        }
    }
}

