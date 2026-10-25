using Match3.Board;
using UnityEngine;
using UnityEngine.InputSystem;


namespace Match3.Board
{
    public class BoardSwipeInput : MonoBehaviour
    {
        [SerializeField] private BoardView _boardView;
        [SerializeField, Range(0.03f, 0.2f)] private float _minSwipeFraction = 0.08f;

        private Vector2 _startScreenPos;
        private bool _tracking;
        private bool _wasPressedLastFrame;

        private void Update()
        {
            InputSystem.EnableDevice(Mouse.current);
#if UNITY_EDITOR
            if (Mouse.current != null && !Mouse.current.enabled)
                InputSystem.EnableDevice(Mouse.current);
#endif
            var pointer = UnityEngine.InputSystem.Pointer.current;
            if (pointer == null) return;

            bool isPressedNow = pointer.press.isPressed;

            // Момент нажатия: сейчас нажато, в прошлом кадре — нет
            if (isPressedNow && !_wasPressedLastFrame)
            {
                _startScreenPos = pointer.position.ReadValue();
                _tracking = true;
            }
            // Момент отпускания: сейчас не нажато, в прошлом кадре — нажато
            else if (!isPressedNow && _wasPressedLastFrame && _tracking)
            {
                _tracking = false;
                Vector2 endScreenPos = pointer.position.ReadValue();
                Vector2 delta = endScreenPos - _startScreenPos;

                float minDistance = Mathf.Min(Screen.width, Screen.height) * _minSwipeFraction;
                if (delta.magnitude >= minDistance)
                {
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
            }

            _wasPressedLastFrame = isPressedNow;
        }
    }
}

