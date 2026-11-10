using DG.Tweening;
using TMPro;
using UnityEngine;

namespace Match3.UI
{
    public class FloatingScoreText : MonoBehaviour
    {
        [SerializeField] private TMP_Text _text;
        [SerializeField] private float _duration = 0.8f;
        [SerializeField] private float _riseDistance = 1.5f;

        private Tween _tween;

        public void Play(Vector3 worldPosition, int amount)
        {
            _text.text = $"+{amount}";
            transform.position = worldPosition;
            gameObject.SetActive(true);

            _text.alpha = 1f;
            transform.localScale = Vector3.one;

            _tween?.Kill();
            _tween = DOTween.Sequence()
                .Append(transform.DOMoveY(worldPosition.y + _riseDistance, _duration).SetEase(Ease.OutQuad))
                .Join(_text.DOFade(0f, _duration).SetEase(Ease.InQuad))
                .OnComplete(() => gameObject.SetActive(false));
        }

        private void OnDisable()
        {
            _tween?.Kill();
        }
    }
}