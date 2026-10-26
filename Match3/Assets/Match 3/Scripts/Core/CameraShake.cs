using DG.Tweening;
using UnityEngine;

namespace Match3.Core
{
    /// <summary>
    /// при каскаде (втором и последующих совпадениях) камера слегка дрожит
    /// </summary>
    public class CameraShake : MonoBehaviour
    {
        [SerializeField] private float _duration = 0.2f;
        [SerializeField] private float _strength = 0.15f;

        private Camera _camera;
        private Tween _shakeTween;

        private void Awake()
        {
            _camera = GetComponent<Camera>();
        }

        public void Shake()
        {
            if (_camera == null) return;

            _shakeTween?.Kill();

            // Сохраняем базовую позицию и трясём вокруг неё.
            Vector3 basePos = _camera.transform.position;
            _shakeTween = _camera.transform.DOShakePosition(_duration, _strength, 10, 90, false, true).
                OnComplete(() => _camera.transform.position = basePos);

        }
    }
}

