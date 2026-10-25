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
        private readonly List<BoardCell> _changedBuffer = new List<BoardCell>(64);

        private void Start()
        {
            _board = new BoardModel(_width, _height);
            _boardView.Initialize(_board);
            _boardView.SetSwipeListener(this);

            int noneCount = 0;
            for (int x = 0; x < _board.Width; x++)
                for (int y = 0; y < _board.Height; y++)
                    if (_board.Cells[x, y].Type == ElementType.None) noneCount++;
            Debug.Log($"None cells at start: {noneCount}");

            MatchFinder.FindMatches(_board, _matchBuffer);
            Debug.Log($"Matches at start: {_matchBuffer.Count}");
            _matchBuffer.Clear();
        }


        public void OnSwipe(int fromX, int fromY, int toX, int toY)
        {
            Debug.Log($"Swipe {fromX},{fromY} -> {toX},{toY}");

            _board.Swap(fromX, fromY, toX, toY);
            _boardView.RefreshCell(_board.Cells[fromX, fromY]);
            _boardView.RefreshCell(_board.Cells[toX, toY]);

            MatchFinder.FindMatches(_board, _matchBuffer);
            Debug.Log($"Matches found: {_matchBuffer.Count}");

            if (_matchBuffer.Count == 0)
            {
                // Откат: меняем обратно и обновляем визуал
                _board.Swap(fromX, fromY, toX, toY);
                _boardView.RefreshCell(_board.Cells[fromX, fromY]);
                _boardView.RefreshCell(_board.Cells[toX, toY]);
                Debug.Log("No match — revert");
                return;
            }

            ResolveMatches();
        }



        private void ResolveMatches()
        {
            // Пока есть совпадения — чистим, роняем, дозаполняем (каскады)
            while (true)
            {
                _board.ResolveStep(_matchBuffer, _changedBuffer);
                _boardView.RefreshCells(_changedBuffer);

                MatchFinder.FindMatches(_board, _matchBuffer);
                if (_matchBuffer.Count == 0)
                    break;
            }
        }
    }
}

