using DG.Tweening;
using Match3.Board;
using Match3.UI;
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

        // Событие для UI (чтобы показать текст перемешивания)
        public event System.Action OnShuffleStarted;

        public GameState State { get; private set; }

        private BoardModel _board;

        private readonly List<MatchResult> _matchResults = new List<MatchResult>(16);
        private readonly List<BoardCell> _allMatchedCells = new List<BoardCell>(32);
        private readonly List<SpecialSpawnInfo> _specialSpawnList = new List<SpecialSpawnInfo>(8);
        private readonly List<BoardCell> _chainReactionSpecials = new List<BoardCell>(8);

        private readonly List<CellMove> _moveBuffer = new List<CellMove>(64);
        private readonly List<BoardCell> _newCellsBuffer = new List<BoardCell>(16);

        private bool _isBusy;

        private struct SpecialSpawnInfo
        {
            public int X;
            public int Y;
            public SpecialType Type;
        }

        private void Awake()
        {
            // Берем уровень из LevelManager (который пришел из сцены Menu)
            if (LevelManager.Instance != null)
            {
                _levelData = LevelManager.Instance.CurrentLevel;
            }

            if (_levelData == null)
            {
                Debug.LogError("LevelData не назначен! Запусти игру через меню.");
                State = new GameState();
                State.Initialize(15, 350);
                return;
            }

            State = new GameState();
            State.Initialize(_levelData.StartingsMoves, _levelData.TargetScore);

            State.GameWin += OnGameWinUnlockNextLevel;
            State.MovesChanged += OnMovesChanged;

            Debug.Log($"Загружен уровень {_levelData.LevelIndex}: Ходов = {_levelData.StartingsMoves}, Цель = {_levelData.TargetScore}");
        }

        private void Start()
        {
            int w = _levelData != null ? _levelData.Width : 8;
            int h = _levelData != null ? _levelData.Height : 8;

            _board = new BoardModel(w, h);
            _boardView.Initialize(_board);
            _boardView.SetSwipeListener(this);
        }

        private void OnMovesChanged(int moves)
        {
            //// Если ходов стало 5 (после того как было 0), значит, сработало перемешивание
            //if (moves == 5 && !State.IsGameOver && !State.IsGameWin)
            //{
            //    Debug.Log("Активировано перемешивание поля!");
            //    OnShuffleStarted?.Invoke(); // Сообщаем UI
            //    StartCoroutine(ShuffleRoutine());
            //}
        }

        private void OnGameWinUnlockNextLevel()
        {
            if (LevelManager.Instance != null)
            {
                int nextIndex = LevelManager.Instance.CurrentLevelIndex + 1;
                if (nextIndex < LevelManager.Instance.AllLevels.Length)
                {
                    LevelProgress.UnlockLevel(nextIndex);
                }
            }
        }

        public void OnSwipe(int fromX, int fromY, int toX, int toY)
        {
            if (State.IsGameOver || State.IsGameWin || _isBusy) return;
            StartCoroutine(SwipeRoutine(fromX, fromY, toX, toY));
        }

        private IEnumerator SwipeRoutine(int fromX, int fromY, int toX, int toY)
        {
            _isBusy = true;
            if (_audio != null) _audio.PlaySwipe();

            BoardCell cell1 = _board.Cells[fromX, fromY];
            BoardCell cell2 = _board.Cells[toX, toY];

            if (cell1.Special != SpecialType.None || cell2.Special != SpecialType.None)
            {
                var specialSwapTween = _boardView.AnimateSwap(fromX, fromY, toX, toY);
                yield return specialSwapTween.WaitForCompletion();

                yield return StartCoroutine(ActivateSpecialRoutine(fromX, fromY, toX, toY));

                _boardView.ResetPosition(fromX, fromY);
                _boardView.ResetPosition(toX, toY);
                _boardView.RefreshCell(_board.Cells[fromX, fromY]);
                _boardView.RefreshCell(_board.Cells[toX, toY]);

                _isBusy = false;
                yield break;
            }

            var swapTween = _boardView.AnimateSwap(fromX, fromY, toX, toY);
            yield return swapTween.WaitForCompletion();

            _board.Swap(fromX, fromY, toX, toY);
            _boardView.SwapViews(fromX, fromY, toX, toY);

            MatchFinder.FindMatches(_board, _matchResults, _allMatchedCells);

            if (_allMatchedCells.Count == 0)
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

            yield return StartCoroutine(ResolveMatchesRoutine());
            State.ConsumeMove();

            // Если ходы есть, но поле "мертвое" — перемешиваем
            if (!_board.HasPossibleMoves() && State.MovesLeft > 0 && !State.IsGameOver && !State.IsGameWin)
            {
                Debug.Log("Нет возможных ходов! Перемешиваем поле.");
                OnShuffleStarted?.Invoke(); // Показываем сообщение
                StartCoroutine(ShuffleRoutine());
            }

            _isBusy = false;
        }

        private IEnumerator ResolveMatchesRoutine()
        {
            int cascade = 0;

            while (true)
            {
                bool hasEmpty = false;
                for (int x = 0; x < _board.Width; x++)
                    for (int y = 0; y < _board.Height; y++)
                        if (_board.Cells[x, y].Type == ElementType.None) { hasEmpty = true; break; }

                if (hasEmpty)
                {
                    _board.ApplyGravityAndCollectMoves(_moveBuffer);
                    if (_moveBuffer.Count > 0)
                    {
                        var fallTween = _boardView.AnimateFall(_moveBuffer);
                        yield return fallTween.WaitForCompletion();
                        if (_audio != null) _audio.PlayFall();
                    }

                    _board.RefillAndCollectNew(_newCellsBuffer);
                    if (_newCellsBuffer.Count > 0)
                    {
                        var spawnTween = _boardView.AnimateSpawn(_newCellsBuffer, _board);
                        yield return spawnTween.WaitForCompletion();
                    }
                }

                MatchFinder.FindMatches(_board, _matchResults, _allMatchedCells);
                if (_allMatchedCells.Count == 0) break;

                bool hasSpecialInMatch = false;
                for (int i = 0; i < _allMatchedCells.Count; i++)
                {
                    if (_allMatchedCells[i].Special != SpecialType.None)
                    {
                        hasSpecialInMatch = true;
                        break;
                    }
                }

                if (hasSpecialInMatch)
                {
                    yield return StartCoroutine(ActivateSpecialsInMatchRoutine());
                    continue;
                }

                // 4. Обрабатываем создание НОВЫХ бонусов (как раньше)
                _specialSpawnList.Clear();
                for (int i = 0; i < _matchResults.Count; i++)
                {
                    var match = _matchResults[i];
                    if (match.Length >= 4)
                    {
                        BoardCell cell = match.Cells[match.Length / 2];
                        SpecialType specialToCreate = match.Length >= 5 ? SpecialType.Rainbow : SpecialType.Bomb;

                        _specialSpawnList.Add(new SpecialSpawnInfo { X = cell.X, Y = cell.Y, Type = specialToCreate });
                        _allMatchedCells.Remove(cell);

                        // НОВОЕ: Начисляем бонусные очки за создание
                        int bonusPoints = ScoreRules.GetBonusForSpecialCreation(specialToCreate);
                        if (bonusPoints > 0)
                        {
                            State.AddScore(bonusPoints);

                            // Показываем всплывающий текст (например, "+20")
                            if (_floatingScorePool != null)
                            {
                                Vector3 pos = _boardView.GetCellWorldPosition(cell);
                                _floatingScorePool.Play(pos, bonusPoints);
                            }
                        }
                    }
                }

                var clearTween = _boardView.AnimateClear(_allMatchedCells);
                yield return clearTween.WaitForCompletion();

                _board.ClearMatches(_allMatchedCells);

                for (int i = 0; i < _specialSpawnList.Count; i++)
                {
                    var info = _specialSpawnList[i];
                    BoardCell cell = _board.Cells[info.X, info.Y];

                    if (cell.Type != ElementType.None)
                    {
                        cell.Special = info.Type;
                        cell.Type = info.Type == SpecialType.Bomb ? _board.GetRandomType() : ElementType.Apple;
                        _boardView.RefreshCell(cell);
                        _boardView.ForceUpdateSpecial(cell.X, cell.Y, cell.Special);
                    }
                }

                ShowScoreAndAddPoints();
                if (_audio != null) _audio.PlayMatch();

                cascade++;
                if (cascade > 0 && _cameraShake != null) _cameraShake.Shake();
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

        private IEnumerator ActivateSpecialRoutine(int fromX, int fromY, int toX, int toY)
        {
            BoardCell cell1 = _board.Cells[fromX, fromY];
            BoardCell cell2 = _board.Cells[toX, toY];

            if (cell1.Special == SpecialType.Bomb || cell2.Special == SpecialType.Bomb)
            {
                BoardCell bomb = cell1.Special == SpecialType.Bomb ? cell1 : cell2;
                yield return StartCoroutine(ExplodeBombRoutine(bomb));
            }
            else if (cell1.Special == SpecialType.Rainbow || cell2.Special == SpecialType.Rainbow)
            {
                BoardCell rainbow = cell1.Special == SpecialType.Rainbow ? cell1 : cell2;
                BoardCell target = cell1.Special == SpecialType.Rainbow ? cell2 : cell1;
                ElementType targetType = target.Type;
                yield return StartCoroutine(ExplodeRainbowRoutine(rainbow, targetType));
            }

            yield return StartCoroutine(ResolveMatchesRoutine());
            State.ConsumeMove();
        }

        private IEnumerator ExplodeBombRoutine(BoardCell bomb)
        {
            _allMatchedCells.Clear();
            _chainReactionSpecials.Clear();

            for (int dx = -1; dx <= 1; dx++)
            {
                for (int dy = -1; dy <= 1; dy++)
                {
                    int nx = bomb.X + dx;
                    int ny = bomb.Y + dy;
                    if (nx >= 0 && nx < _board.Width && ny >= 0 && ny < _board.Height)
                    {
                        BoardCell target = _board.Cells[nx, ny];
                        if (target.Type != ElementType.None)
                        {
                            _allMatchedCells.Add(target);
                            if (target.Special != SpecialType.None && target != bomb)
                                _chainReactionSpecials.Add(target);
                        }
                    }
                }
            }

            var clearTween = _boardView.AnimateClear(_allMatchedCells);
            yield return clearTween.WaitForCompletion();

            _board.ClearMatches(_allMatchedCells);
            if (_audio != null) _audio.PlayMatch();

            for (int i = 0; i < _allMatchedCells.Count; i++)
                _allMatchedCells[i].Special = SpecialType.None;

            for (int i = 0; i < _chainReactionSpecials.Count; i++)
            {
                var special = _chainReactionSpecials[i];
                if (special.Special == SpecialType.Bomb)
                    yield return StartCoroutine(ExplodeBombRoutine(special));
                else if (special.Special == SpecialType.Rainbow)
                    yield return StartCoroutine(ExplodeRainbowRoutine(special, _board.GetRandomType()));
            }
        }

        private IEnumerator ExplodeRainbowRoutine(BoardCell rainbow, ElementType targetType)
        {
            _allMatchedCells.Clear();
            _chainReactionSpecials.Clear();

            for (int x = 0; x < _board.Width; x++)
            {
                for (int y = 0; y < _board.Height; y++)
                {
                    BoardCell target = _board.Cells[x, y];
                    if (target.Type == targetType || target == rainbow)
                    {
                        _allMatchedCells.Add(target);
                        if (target.Special != SpecialType.None && target != rainbow)
                            _chainReactionSpecials.Add(target);
                    }
                }
            }

            var clearTween = _boardView.AnimateClear(_allMatchedCells);
            yield return clearTween.WaitForCompletion();

            _board.ClearMatches(_allMatchedCells);
            if (_audio != null) _audio.PlayMatch();

            for (int i = 0; i < _allMatchedCells.Count; i++)
                _allMatchedCells[i].Special = SpecialType.None;

            for (int i = 0; i < _chainReactionSpecials.Count; i++)
            {
                var special = _chainReactionSpecials[i];
                if (special.Special == SpecialType.Bomb)
                    yield return StartCoroutine(ExplodeBombRoutine(special));
                else if (special.Special == SpecialType.Rainbow)
                    yield return StartCoroutine(ExplodeRainbowRoutine(special, _board.GetRandomType()));
            }
        }

        private IEnumerator ActivateSpecialsInMatchRoutine()
        {
            List<BoardCell> specialsToActivate = new List<BoardCell>();
            for (int i = 0; i < _allMatchedCells.Count; i++)
            {
                if (_allMatchedCells[i].Special != SpecialType.None)
                    specialsToActivate.Add(_allMatchedCells[i]);
            }

            for (int i = 0; i < specialsToActivate.Count; i++)
            {
                var special = specialsToActivate[i];
                if (special.Special == SpecialType.Bomb)
                    yield return StartCoroutine(ExplodeBombRoutine(special));
                else if (special.Special == SpecialType.Rainbow)
                    yield return StartCoroutine(ExplodeRainbowRoutine(special, _board.GetRandomType()));
            }
        }

        private IEnumerator ShuffleRoutine()
        {
            _isBusy = true;
            yield return new WaitForSeconds(0.5f);

            _board = new BoardModel(_board.Width, _board.Height);
            _boardView.RefreshAll(_board);

            yield return new WaitForSeconds(0.5f);
            _isBusy = false;
        }
    }
}