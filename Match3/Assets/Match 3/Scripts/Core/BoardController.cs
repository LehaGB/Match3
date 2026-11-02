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

        public GameState State { get; private set; }

        private BoardModel _board;

        // Буферы
        private readonly List<MatchResult> _matchResults = new List<MatchResult>(16);
        private readonly List<BoardCell> _allMatchedCells = new List<BoardCell>(32);
        private readonly List<SpecialSpawnInfo> _specialSpawnList = new List<SpecialSpawnInfo>(8);
        private readonly List<BoardCell> _chainReactionSpecials = new List<BoardCell>(8); // Для цепной реакции

        private readonly List<CellMove> _moveBuffer = new List<CellMove>(64);
        private readonly List<BoardCell> _newCellsBuffer = new List<BoardCell>(16);

        private bool _isBusy;

        // Структура для отложенного создания бонусов
        private struct SpecialSpawnInfo
        {
            public int X;
            public int Y;
            public SpecialType Type;
        }

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
            // ИСПРАВЛЕНО: StartingMoves (без 's' на конце)
            State.Initialize(_levelData.StartingsMoves, _levelData.TargetScore);
            State.GameOver += OnGameOverCheckShuffle;
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
            if (_audio != null) _audio.PlaySwipe();

            BoardCell cell1 = _board.Cells[fromX, fromY];
            BoardCell cell2 = _board.Cells[toX, toY];

            Debug.Log($"Свайп: ({fromX},{fromY}) Spec={cell1.Special}, ({toX},{toY}) Spec={cell2.Special}");

            // --- ПРОВЕРКА НА БОНУСЫ ---
            if (cell1.Special != SpecialType.None || cell2.Special != SpecialType.None)
            {
                yield return StartCoroutine(ActivateSpecialRoutine(fromX, fromY, toX, toY));
                _isBusy = false;
                yield break;
            }

            // --- ОБЫЧНЫЙ СВАЙП ---
            var swapTween = _boardView.AnimateSwap(fromX, fromY, toX, toY);
            yield return swapTween.WaitForCompletion();

            _board.Swap(fromX, fromY, toX, toY);
            _boardView.SwapViews(fromX, fromY, toX, toY);

            MatchFinder.FindMatches(_board, _matchResults, _allMatchedCells);

            if (_allMatchedCells.Count == 0)
            {
                // Откат
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
            _isBusy = false;
        }

        private IEnumerator ResolveMatchesRoutine()
        {
            int cascade = 0;

            while (true)
            {
                // 1. Падение и рефилл, если есть пустые
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

                // 2. Ищем совпадения
                MatchFinder.FindMatches(_board, _matchResults, _allMatchedCells);
                if (_allMatchedCells.Count == 0) break;

                // 3. Обрабатываем создание бонусов
                _specialSpawnList.Clear();
                for (int i = 0; i < _matchResults.Count; i++)
                {
                    var match = _matchResults[i];
                    if (match.Length >= 4)
                    {
                        BoardCell cell = match.Cells[match.Length / 2];
                        // ИСПРАВЛЕНО: Rainbow (а не RainBow)
                        SpecialType specialToCreate = match.Length >= 5 ? SpecialType.Rainbow : SpecialType.Bomb;
                        _specialSpawnList.Add(new SpecialSpawnInfo { X = cell.X, Y = cell.Y, Type = specialToCreate });
                        _allMatchedCells.Remove(cell);
                        Debug.Log($"Подготовлен бонус: {specialToCreate} в ({cell.X}, {cell.Y})");
                    }
                }

                // 4. Анимация исчезновения
                var clearTween = _boardView.AnimateClear(_allMatchedCells);
                yield return clearTween.WaitForCompletion();

                // 5. Очистка модели
                _board.ClearMatches(_allMatchedCells);

                // 6. Применяем бонусы к модели
                for (int i = 0; i < _specialSpawnList.Count; i++)
                {
                    var info = _specialSpawnList[i];
                    BoardCell cell = _board.Cells[info.X, info.Y];
                    cell.Special = info.Type;
                    cell.Type = info.Type == SpecialType.Bomb ? _board.GetRandomType() : ElementType.Banana;
                    _boardView.RefreshCell(cell);
                }

                // 7. Очки и звук
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
            Debug.Log($"Активация: cell1({fromX},{fromY})={cell1.Special}, cell2({toX},{toY})={cell2.Special}");
            if (cell1.Special == SpecialType.Bomb || cell2.Special == SpecialType.Bomb)
            {
                BoardCell bomb = cell1.Special == SpecialType.Bomb ? cell1 : cell2;
                Debug.Log($"Взрываем бомбу в ({bomb.X}, {bomb.Y})");
                yield return StartCoroutine(ExplodeBombRoutine(bomb));
            }
            // ИСПРАВЛЕНО: Rainbow
            else if (cell1.Special == SpecialType.Rainbow || cell2.Special == SpecialType.Rainbow)
            {
                BoardCell rainbow = cell1.Special == SpecialType.Rainbow ? cell1 : cell2;
                BoardCell target = cell1.Special == SpecialType.Rainbow ? cell2 : cell1;
                yield return StartCoroutine(ExplodeRainbowRoutine(rainbow, target.Type));
            }

            yield return StartCoroutine(ResolveMatchesRoutine());
            State.ConsumeMove();
        }

        private IEnumerator ExplodeBombRoutine(BoardCell bomb)
        {
            _allMatchedCells.Clear();
            _chainReactionSpecials.Clear();

            // Собираем клетки для взрыва (крест 3x3)
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

            // Цепная реакция
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
            _chainReactionSpecials.Clear(); // ИСПРАВЛЕНО: используем _chainReactionSpecials вместо удаленного _specialBuffer

            // Уничтожаем все элементы нужного цвета
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

            // Цепная реакция
            for (int i = 0; i < _chainReactionSpecials.Count; i++)
            {
                var special = _chainReactionSpecials[i];
                if (special.Special == SpecialType.Bomb)
                    yield return StartCoroutine(ExplodeBombRoutine(special));
                else if (special.Special == SpecialType.Rainbow)
                    yield return StartCoroutine(ExplodeRainbowRoutine(special, _board.GetRandomType()));
            }
        }

        private void OnGameOverCheckShuffle()
        {
            if (State.ShufflesLeft > 0)
            {
                State.UseShuffle();
                Debug.Log("Ходы кончились! Перемешиваем поле.");
                StartCoroutine(ShuffleRoutine());
            }
        }

        private IEnumerator ShuffleRoutine()
        {
            _isBusy = true;
            yield return new WaitForSeconds(0.5f);

            State.ResetGameOver();
            _board = new BoardModel(_board.Width, _board.Height);
            _boardView.RefreshAll(_board);

            yield return new WaitForSeconds(0.5f);
            _isBusy = false;
        }
    }
}