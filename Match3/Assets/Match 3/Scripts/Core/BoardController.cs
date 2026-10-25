using System.Collections.Generic;
using Match3.Board;
using UnityEngine;

namespace Match3.Core
{
    public class BoardController : MonoBehaviour, ISwipeListener
    {
        [SerializeField] private int _width = 8;
        [SerializeField] private int _height = 8;
        [SerializeField] private BoardView _boardView;

        private BoardModel _board;

        private readonly List<BoardCell> _matchBuffer = new List<BoardCell>(32);

        private void Start()
        {
            _board = new BoardModel(_width, _height);
            _boardView.Initialize(_board);
            _boardView.SetSwipeListener(this);
        }


        public void OnSwipe(int fromX, int fromY, int toX, int toY)
        {
            _board.Swap(fromX, fromY, toX, toY);
            _boardView.RefreshCell(_board.Cells[fromX, fromY]);
            _boardView.RefreshCell(_board.Cells[toX, toY]);

            MatchFinder.FindMatches(_board, _matchBuffer);
            if (_matchBuffer.Count == 0)
            {
                // Совпадений нет — откатываем
                _board.Swap(fromX, fromY, toX, toY);
                _boardView.RefreshCell(_board.Cells[fromX, fromY]);
                _boardView.RefreshCell(_board.Cells[toX, toY]);
                Debug.Log("No match — revert");
            }
            else
            {
                Debug.Log($"Match! {_matchBuffer.Count} cells in matches");
            }
        }
    }
}

