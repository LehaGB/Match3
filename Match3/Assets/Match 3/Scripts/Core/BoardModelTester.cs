using Match3.Board;
using Match3.Core;
using UnityEngine;

namespace Match3.Core
{
    public class BoardModelTester : MonoBehaviour, ISwipeListener
    {
        [SerializeField] private int _width = 8;
        [SerializeField] private int _height = 8;

        [SerializeField] private BoardView _boardView;

        private void Start()
        {
            var board = new BoardModel(_width, _height, seed: 12345);
            _boardView.Initialize(board);
            _boardView.SetSwipeListener(this);
        }

        public void OnSwipe(int fromX, int fromY, int toX, int toY)
        {
            Debug.Log($"Swipe: ({fromX},{fromY}) -> ({toX},{toY})");
        }
    }
}
