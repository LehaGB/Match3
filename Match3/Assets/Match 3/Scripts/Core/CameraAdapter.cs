using UnityEngine;

namespace Match3.Core
{

    /// <summary>
    /// Класс CameraAdapter отвечает за адаптацию ортографической камеры под разные соотношения сторон экрана (например, при повороте телефона).
    /// Он автоматически вычисляет нужный размер камеры (Orthographic Size) так, чтобы игровое поле (сетка) всегда помещалось на экране целиком, 
    /// независимо от того, горизонтально или вертикально расположен экран устройства.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public class CameraAdapter : MonoBehaviour
    {
        [Header("Board Settings")]

        [Tooltip("Ширина игрового поля в юнитах (клетках)")]
        public float BoardWidth = 8f;

        [Tooltip("Высота игрового поля в юнитах (клетках)")]
        public float BoardHeight = 8f;

        [Tooltip("Отступ от краев экрана, чтобы поле не касалось границ")]
        public float Padding = 1f;

        private Camera _camera;
        private int _lastScreenWidth;
        private int _lastScreenHeight;

        private void Awake()
        {
            _camera = GetComponent<Camera>();
            AdaptCamera();
        }

        private void Update()
        {
            if (Screen.width != _lastScreenWidth || Screen.height != _lastScreenHeight)
            {
                _lastScreenWidth = Screen.width;
                _lastScreenHeight = Screen.height;

                // Если ориентация изменилась (поворот телефона) — адаптируем камеру.
                AdaptCamera();
            }
                
        }

        private void AdaptCamera()
        {
            // 1. Вычисляем, какой размер камеры нужен, чтобы поле поместилось по высоте.
            float targetHeight = (BoardHeight / 2f) + Padding;

            // 2. Вычисляем, какой размер камеры нужен, чтобы поле поместилось по ширине
            // (учитывая соотношение сторон экрана).
            float screenAspect = (float)Screen.width / Screen.height;
            float targetWidth = ((BoardWidth / 2f) + Padding) / screenAspect;

            // 3. Берем максимальное из двух значений, чтобы поле точно влезло.
            _camera.orthographicSize = Mathf.Max(targetHeight, targetWidth);
        }
    }
}

