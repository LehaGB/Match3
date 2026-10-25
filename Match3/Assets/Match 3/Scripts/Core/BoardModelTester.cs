using Match3.Board;
using Match3.Core;
using UnityEngine;

namespace Match3.Core
{
    public class BoardModelTester : MonoBehaviour
    {
        [SerializeField] private int _width = 8;
        [SerializeField] private int _height = 8;

        [SerializeField] private BoardView _boardView;

        private void Start()
        {
            var board = new BoardModel(_width, _height, seed: 12345);
            _boardView.Initialize(board);
        }
    }
}
