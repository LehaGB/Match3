using Match3.Board;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;


namespace Match3.Board
{

    /// <summary>
    /// Распознавание свайпа: ручное отслеживание isPressed (обход бага Unity 6 с wasPressedThisFrame), 
    /// порог относительно экрана, определение направления, вызов NotifySwipe. 
    /// В редакторе принудительно включает Mouse через EnableDevice.
    /// </summary>
    public class BoardSwipeInput : MonoBehaviour
    {
        [SerializeField] private BoardView _boardView;
        [SerializeField, Range(0.03f, 0.2f)] private float _minSwipeFraction = 0.08f;

        private Vector2 _startScreenPos;
        private bool _tracking;
        private bool _wasPressedLastFrame;


        private void Awake()
        {
            // Включаем Enhanced Touch для надёжной работы тача на Android
            EnhancedTouchSupport.Enable();
        }


        private void Update()
        {
            var pointer = Pointer.current;
            if (pointer == null) return;

            bool isPressedNow = pointer.press.isPressed;

            // Момент нажатия
            if (isPressedNow && !_wasPressedLastFrame)
            {
                _startScreenPos = pointer.position.ReadValue();
                _tracking = true;
            }
            // Момент отпускания (проверяем по нашему флагу, а не по wasReleasedThisFrame)
            else if (!isPressedNow && _wasPressedLastFrame && _tracking)
            {
                _tracking = false;
                Vector2 endScreenPos = pointer.position.ReadValue();
                Vector2 delta = endScreenPos - _startScreenPos;

                float minDistance = Mathf.Min(Screen.width, Screen.height) * _minSwipeFraction;
                if (delta.magnitude < minDistance)
                {
                    // Слишком короткое движение — это тап, а не свайп
                    _wasPressedLastFrame = isPressedNow;
                    return;
                }

                bool horizontal = Mathf.Abs(delta.x) > Mathf.Abs(delta.y);
                Vector2Int dir = horizontal
                    ? new Vector2Int(delta.x > 0 ? 1 : -1, 0)
                    : new Vector2Int(0, delta.y > 0 ? 1 : -1);

                if (_boardView.ScreenToCell(_startScreenPos, out int fromX, out int fromY))
                {
                    int toX = fromX + dir.x;
                    int toY = fromY + dir.y;
                    if (_boardView.IsInside(toX, toY))
                        _boardView.NotifySwipe(fromX, fromY, toX, toY);
                }
            }

            _wasPressedLastFrame = isPressedNow;
        }
    }
}

