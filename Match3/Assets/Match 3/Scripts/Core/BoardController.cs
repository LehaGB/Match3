using System.Collections.Generic;
using Match3.Board;
using UnityEngine;

namespace Match3.Core
{

    /// <summary>
    /// Мозг» игры: связывает модель, представление и ввод. 
    /// Реализует ISwipeListener, обрабатывает свайп (обмен → проверка → откат или ResolveMatches), 
    /// крутит каскады.
    /// </summary>
    public class BoardController : MonoBehaviour, ISwipeListener
    {
        [SerializeField] private int _width = 8;
        [SerializeField] private int _height = 8;
        [SerializeField] private int _startingMoves = 20;  // количесво ходов при старте.
        [SerializeField] private BoardView _boardView;


        // Публичное состояние — UI подпишется на его события.
        public GameState State { get; private set; }


        private BoardModel _board;
        private readonly List<BoardCell> _matchBuffer = new List<BoardCell>(32);
        private readonly List<BoardCell> _changedBuffer = new List<BoardCell>(64);


        private void Awake()
        {
            State = new GameState();
            State.Initialize(_startingMoves);
        }

        private void Start()
        {
            _board = new BoardModel(_width, _height);
            _boardView.Initialize(_board);
            _boardView.SetSwipeListener(this);
        }


        public void OnSwipe(int fromX, int fromY, int toX, int toY)
        {
            // Если игра окончена — игнорируем ввод.
            if (State.IsGameOver) return;

            _board.Swap(fromX, fromY, toX, toY);
            _boardView.RefreshCell(_board.Cells[fromX, fromY]);
            _boardView.RefreshCell(_board.Cells[toX, toY]);

            MatchFinder.FindMatches(_board, _matchBuffer);

            if (_matchBuffer.Count == 0)
            {
                // Откат: меняем обратно и обновляем визуал
                _board.Swap(fromX, fromY, toX, toY);
                _boardView.RefreshCell(_board.Cells[fromX, fromY]);
                _boardView.RefreshCell(_board.Cells[toX, toY]);
                return;
            }
            // Ход успешен: начисляем очки за первый набор совпадений.
            State.AddScore(ScoreRules.CalculateForMatch(_matchBuffer.Count));
            ResolveMatches();
            State.ConsumeMove();
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

                // Каскадные совпадения тоже дают очки.
                State.AddScore(ScoreRules.CalculateForMatch(_matchBuffer.Count));
            }
        }
    }
}

