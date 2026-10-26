using DG.Tweening;
using Match3.Board;
using System.Collections;
using System.Collections.Generic;
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
        [SerializeField] private int _startingMoves = 20;
        [SerializeField] private BoardView _boardView;

        public GameState State { get; private set; }

        private BoardModel _board;
        private readonly List<BoardCell> _matchBuffer = new List<BoardCell>(32);
        private readonly List<BoardCell> _changedBuffer = new List<BoardCell>(64);

        private bool _isBusy; // Блокировка ввода во время анимации

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
            // Если игра окончена или идёт анимация — игнорируем
            if (State.IsGameOver || _isBusy) return;
            StartCoroutine(SwipeRoutine(fromX, fromY, toX, toY));
        }

        private IEnumerator SwipeRoutine(int fromX, int fromY, int toX, int toY)
        {
            _isBusy = true;

            // 1. Анимируем обмен (модель пока НЕ трогаем)
            var swapTween = _boardView.AnimateSwap(fromX, fromY, toX, toY);
            yield return swapTween.WaitForCompletion();

            // 2. Меняем модель и меняем ссылки в массиве _views
            _board.Swap(fromX, fromY, toX, toY);
            _boardView.SwapViews(fromX, fromY, toX, toY);

            // 3. Проверяем совпадения
            MatchFinder.FindMatches(_board, _matchBuffer);

            if (_matchBuffer.Count == 0)
            {
                // Нет совпадений — возвращаем модель и ссылки обратно
                _board.Swap(fromX, fromY, toX, toY);
                _boardView.SwapViews(fromX, fromY, toX, toY); // вернуть ссылки на место

                // Анимируем визуальный возврат
                var revertTween = _boardView.AnimateSwap(toX, toY, fromX, fromY);
                yield return revertTween.WaitForCompletion();

                // Синхронизируем позиции и спрайты (на всякий случай)
                _boardView.ResetPosition(fromX, fromY);
                _boardView.ResetPosition(toX, toY);
                _boardView.RefreshCell(_board.Cells[fromX, fromY]);
                _boardView.RefreshCell(_board.Cells[toX, toY]);

                _isBusy = false;
                yield break;
            }

            // 4. Успех: очки и каскады
            State.AddScore(ScoreRules.CalculateForMatch(_matchBuffer.Count));
            yield return StartCoroutine(ResolveMatchesRoutine());
            State.ConsumeMove();

            _isBusy = false;
        }

        private IEnumerator ResolveMatchesRoutine()
        {
            while (true)
            {
                // 1. Анимация исчезновения
                var clearTween = _boardView.AnimateClear(_matchBuffer);
                yield return clearTween.WaitForCompletion();

                // 2. Мгновенно применяем гравитацию + рефилл в модели
                _board.ResolveStep(_matchBuffer, _changedBuffer);

                // ВАЖНО: сначала обновляем спрайты, потом сбрасываем масштаб!
                // Иначе элементы появятся уменьшенными.
                _boardView.RefreshCells(_changedBuffer);
                for (int i = 0; i < _changedBuffer.Count; i++)
                    _boardView.ResetScale(_changedBuffer[i]);

                // 3. Проверяем новые совпадения
                MatchFinder.FindMatches(_board, _matchBuffer);
                if (_matchBuffer.Count == 0)
                    break;

                State.AddScore(ScoreRules.CalculateForMatch(_matchBuffer.Count));
            }
        }
    }
}

