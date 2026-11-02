using DG.Tweening;
using Match3.Board;
using Match3.UI;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Match3.Core
{
    public class BoardController : MonoBehaviour, ISwipeListener
    {
        [Header("Level & Settings")]
        [SerializeField] private LevelData _levelData;

        [Header("References")]
        [SerializeField] private BoardView _boardView;
        [SerializeField] private FloatingScorePool _floatingScorePool;
        [SerializeField] private AudioManager _audio;
        [SerializeField] private CameraShake _cameraShake;

        public GameState State { get; private set; }

        private BoardModel _board;

        // Буферы для новой логики
        private readonly List<MatchResult> _matchResults = new List<MatchResult>(16);
        private readonly List<BoardCell> _allMatchedCells = new List<BoardCell>(32);
        private readonly List<BoardCell> _specialBuffer = new List<BoardCell>(8);

        private readonly List<CellMove> _moveBuffer = new List<CellMove>(64);
        private readonly List<BoardCell> _newCellsBuffer = new List<BoardCell>(16);

        private bool _isBusy;

        private void Awake()
        {
            if (_levelData == null)
            {
                Debug.LogError("LevelData не назначен в инспекторе!");
                State = new GameState();
                State.Initialize(20, 1000);
                return;
            }

            State = new GameState();
            State.Initialize(_levelData.StartingsMoves, _levelData.TargetScore);
            State.GameOver += OnGameOverCheckShuffle;
        }

        private void OnGameOverCheckShuffle()
        {
            if(State.ShufflesLeft > 0)
            {
                State.UseShuffle();
                Debug.Log("Ходы кончились! Перемешиваем поле.");
                StartCoroutine(ShuffleRoutine());
            }
        }

        private void Start()
        {
            int w = _levelData != null ? _levelData.Width : 8;
            int h = _levelData != null ? _levelData.Height : 8;

            _board = new BoardModel(w, h);
            _boardView.Initialize(_board);
            _boardView.SetSwipeListener(this);
        }

        public void OnSwipe(int fromX, int fromY, int toX, int toY)
        {
            if (State.IsGameOver || State.IsGameWin || _isBusy) return;
            StartCoroutine(SwipeRoutine(fromX, fromY, toX, toY));
        }

        private IEnumerator SwipeRoutine(int fromX, int fromY, int toX, int toY)
        {
            _isBusy = true;

            // Звук свайпа
            if (_audio != null) _audio.PlaySwipe();

            var swapTween = _boardView.AnimateSwap(fromX, fromY, toX, toY);
            yield return swapTween.WaitForCompletion();

            _board.Swap(fromX, fromY, toX, toY);
            _boardView.SwapViews(fromX, fromY, toX, toY);

            MatchFinder.FindMatches(_board, _matchResults, _allMatchedCells);

            if (_allMatchedCells.Count == 0)
            {
                // Откат (нет совпадений)
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

            // Есть совпадение: очки + звук
            ShowScoreAndAddPoints();
            if (_audio != null) _audio.PlayMatch();

            yield return StartCoroutine(ResolveMatchesRoutine());
            State.ConsumeMove();

            _isBusy = false;
        }

        private IEnumerator ResolveMatchesRoutine()
        {
            int cascade = 0;

            while (true)
            {
                // 1. Обрабатываем бонусы
                ProcessSpecialCreation();

                // 2. Анимация исчезновения
                var clearTween = _boardView.AnimateClear(_allMatchedCells);
                yield return clearTween.WaitForCompletion();

                // 3. Очистка и падение
                _board.ClearMatches(_allMatchedCells);
                RestoreSpecials(); // Возвращаем бонусы на поле, чтобы они упали вместе со всеми

                _board.ApplyGravityAndCollectMoves(_moveBuffer);
                if (_moveBuffer.Count > 0)
                {
                    var fallTween = _boardView.AnimateFall(_moveBuffer);
                    yield return fallTween.WaitForCompletion();

                    // Звук падения
                    if (_audio != null) _audio.PlayFall();
                }

                // 4. Появление новых
                _board.RefillAndCollectNew(_newCellsBuffer);
                if (_newCellsBuffer.Count > 0)
                {
                    var spawnTween = _boardView.AnimateSpawn(_newCellsBuffer, _board);
                    yield return spawnTween.WaitForCompletion();
                }

                // 5. Проверка на каскад
                MatchFinder.FindMatches(_board, _matchResults, _allMatchedCells);
                if (_allMatchedCells.Count == 0)
                    break;

                // Если есть каскад - очки, звук, тряска
                ShowScoreAndAddPoints();
                if (_audio != null) _audio.PlayMatch();

                cascade++;
                if (cascade > 0 && _cameraShake != null)
                    _cameraShake.Shake();
            }
        }

        private void ShowScoreAndAddPoints()
        {
            if (_allMatchedCells.Count == 0) return;

            int points = ScoreRules.CalculateForMatch(_allMatchedCells.Count);
            State.AddScore(points);

            if (_floatingScorePool != null)
            {
                Vector3 pos = _boardView.GetCellWorldPosition(_allMatchedCells[0]);
                _floatingScorePool.Play(pos, points);
            }
        }

        private void ProcessSpecialCreation()
        {
            _specialBuffer.Clear();

            for (int i = 0; i < _matchResults.Count; i++)
            {
                var match = _matchResults[i];
                if (match.Length >= 4)
                {
                    BoardCell cell = match.Cells[match.Length / 2];
                    SpecialType specialToCreate = match.Length >= 5 ? SpecialType.RaiBow : SpecialType.Bomb;

                    _board.UpgradeToSpecial(cell, specialToCreate);
                    _specialBuffer.Add(cell);
                }
            }
        }

        private void RestoreSpecials()
        {
            for (int i = 0; i < _specialBuffer.Count; i++)
            {
                var cell = _specialBuffer[i];
                cell.Type = ElementType.Apple;

                if (cell.Special == SpecialType.Bomb)
                    cell.Type = _board.GetRandomType();

                _boardView.RefreshCell(cell);
            }
        }


        private IEnumerator ShuffleRoutine()
        {
            _isBusy = true;

            // Ждем немного, чтобы игрок понял, что произошло
            yield return new WaitForSeconds(0.5f);

            // Сбрасываем GameOver, так как мы дали второй шанс
            // (Для этого в GameState нужен метод ResetGameOver)
            State.ResetGameOver();

            // Создаем новое поле (оно сгенерируется без совпадений)
            _board = new BoardModel(_board.Width, _board.Height);

            // Полностью перерисовываем визуал
            _boardView.RefreshAll(_board);

            // Ждем, пока элементы появятся (можно добавить анимацию)
            yield return new WaitForSeconds(0.5f);

            _isBusy = false;
        }
    }
}