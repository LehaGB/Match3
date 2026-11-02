using DG.Tweening;
using Match3.Board;
using Match3.UI;
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
        [SerializeField] private LevelData _levelData;
        [SerializeField] private BoardView _boardView;
        [SerializeField] private AudioManager _audio;
        [SerializeField] private CameraShake _cameraShake;
        [SerializeField] private FloatingScorePool _floatingScorePool;

        public GameState State { get; private set; }

        private BoardModel _board;
        private readonly List<BoardCell> _matchBuffer = new List<BoardCell>(32);
        private readonly List<BoardCell> _changedBuffer = new List<BoardCell>(64);

        private readonly List<CellMove> _moveBuffer = new List<CellMove>(64);
        private readonly List<BoardCell> _newCellsBuffer = new List<BoardCell>(16);

        private bool _isBusy;

        private void Awake()
        {
            State = new GameState();
            State.Initialize(_levelData.StartingsMoves, _levelData.TargetScore);
        }

        private void Start()
        {
            _board = new BoardModel(_levelData.Width, _levelData.Height);
            _boardView.Initialize(_board);
            _boardView.SetSwipeListener(this);
        }

        public void OnSwipe(int fromX, int fromY, int toX, int toY)
        {
            if (State.IsGameOver || _isBusy) return;
            StartCoroutine(SwipeRoutine(fromX, fromY, toX, toY));
        }

        private IEnumerator SwipeRoutine(int fromX, int fromY, int toX, int toY)
        {
            _audio.PlaySwipe();
            _isBusy = true;

            var swapTween = _boardView.AnimateSwap(fromX, fromY, toX, toY);
            yield return swapTween.WaitForCompletion();

            _board.Swap(fromX, fromY, toX, toY);
            _boardView.SwapViews(fromX, fromY, toX, toY);

            MatchFinder.FindMatches(_board, _matchBuffer);

            if (_matchBuffer.Count == 0)
            {
                _board.Swap(fromX, fromY, toX, toY);
                _boardView.SwapViews(fromX, fromY, toX, toY);

                var revertTween = _boardView.AnimateSwap(toX, toY, fromX, fromY);
                yield return revertTween.WaitForCompletion();

                _boardView.ResetPosition(fromX, fromY);
                _boardView.ResetPosition(toX, toY);
                _boardView.RefreshCell(_board.Cells[fromX, fromY]);
                _boardView.RefreshCell(_board.Cells[toX, toY]);

                _isBusy = false;
                yield break;
            }

            // Первое совпадение
            ShowScoreAndAddPoints();

            yield return StartCoroutine(ResolveMatchesRoutine());
            State.ConsumeMove();

            _isBusy = false;
        }

        private IEnumerator ResolveMatchesRoutine()
        {
            while (true)
            {
                // 1. Исчезновение
                var clearTween = _boardView.AnimateClear(_matchBuffer);
                yield return clearTween.WaitForCompletion();

                _audio.PlayMatch();

                // 2. Очистка и падение
                _board.ClearMatches(_matchBuffer);
                _board.ApplyGravityAndCollectMoves(_moveBuffer);

                if (_moveBuffer.Count > 0)
                {
                    var fallTween = _boardView.AnimateFall(_moveBuffer);
                    yield return fallTween.WaitForCompletion();

                    // ВОТ ЗДЕСЬ звук падения. Элементы только что приземлились.
                    _audio.PlayFall();
                }

                // 3. Появление новых
                _board.RefillAndCollectNew(_newCellsBuffer);
                if (_newCellsBuffer.Count > 0)
                {
                    var spawnTween = _boardView.AnimateSpawn(_newCellsBuffer, _board);
                    yield return spawnTween.WaitForCompletion();
                }

                // 4. Проверка на каскад
                MatchFinder.FindMatches(_board, _matchBuffer);
                if (_matchBuffer.Count == 0)
                    break;

                // Если есть каскад - показываем очки и начисляем
                ShowScoreAndAddPoints();

                if (_cameraShake != null) _cameraShake.Shake();
            }
        }

        // --- ВСПОМОГАТЕЛЬНЫЙ МЕТОД ДЛЯ ОЧКОВ И ЦИФР ---
        private void ShowScoreAndAddPoints()
        {
            if (_matchBuffer.Count == 0) return;

            int points = ScoreRules.CalculateForMatch(_matchBuffer.Count);
            State.AddScore(points);

            // Защита от NullReference, если пул не назначен
            if (_floatingScorePool != null)
            {
                Vector3 pos = _boardView.GetCellWorldPosition(_matchBuffer[0]);
                _floatingScorePool.Play(pos, points);
            }
        }
    }
}

