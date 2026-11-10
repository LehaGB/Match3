using Match3.Core;
using UnityEngine;

namespace Match3.Board
{
    [RequireComponent(typeof(SpriteRenderer))]
    public class BoardElementView : MonoBehaviour
    {
        private SpriteRenderer _renderer;
        private Vector3 _baseScale;
        private Quaternion _baseRotation;

        // Для вращения Радуги
        private bool _isRotating = false;
        private float _rotationSpeed = 90f; // Градусов в секунду

        public SpriteRenderer Renderer
        {
            get
            {
                if (_renderer == null) _renderer = GetComponent<SpriteRenderer>();
                return _renderer;
            }
        }

        private void Awake()
        {
            _baseScale = transform.localScale;
            _baseRotation = transform.rotation;
        }

        public void SetSprite(Sprite sprite) => Renderer.sprite = sprite;
        public void SetPosition(Vector3 position) => transform.position = position;
        public void ResetScale() => transform.localScale = _baseScale;

        public void SetSpecialVisual(SpecialType special)
        {
            // Сбрасываем к базовому состоянию
            transform.localScale = _baseScale;
            transform.rotation = _baseRotation;
            Renderer.color = Color.white;
            _isRotating = false;

            switch (special)
            {
                case SpecialType.None:
                    break;

                case SpecialType.Bomb:
                    transform.localScale = _baseScale * 1.7f;
                    transform.rotation = Quaternion.Euler(0, 0, 45f);
                    break;

                case SpecialType.Rainbow:
                    // Радуга: меньше, но вращается и светится
                    transform.localScale = _baseScale * 1.7f;
                    Renderer.color = new Color(1f, 1f, 0.5f); // Легкий золотистый оттенок (свечение)
                    _isRotating = true;
                    break;
            }
        }

        private void Update()
        {
            // Если это радуга — вращаем её для эффекта
            if (_isRotating)
            {
                transform.Rotate(Vector3.forward, _rotationSpeed * Time.deltaTime);
            }
        }
    }
}