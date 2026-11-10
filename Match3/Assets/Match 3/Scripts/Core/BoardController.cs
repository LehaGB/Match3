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
            if (_levelData == null && LevelManager.Instance != null)
                _levelData = LevelManager.Instance.CurrentLevel;

            if (_levelData == null)
            {
                Debug.LogError("LevelData не назначен!");
                State = new GameState();
                State.Initialize(20, 1000);
                return;
            }

            Debug.Log($"ЗАГРУЗКА: LevelIndex={_levelData.LevelIndex}, Moves={_levelData.StartingsMoves}, Target={_levelData.TargetScore}");

            State = new GameState();
            State.Initialize(_levelData.StartingsMoves, _levelData.TargetScore);
            State.GameWin += OnGameWinUnlockNextLevel;
            State.MovesChanged += OnMovesChanged;
        }

        private void Start()
        {
            int w = _levelData != null ? _levelData.Width : 8;
            int h = _levelData != null ? _levelData.Height : 8;

            _board = new BoardModel(w, h);
            _boardView.Initialize(_board);
            _boardView.SetSwipeListener(this);
        }

        private void OnMovesChanged(int moves) { /* Перемешивание отключено */ }

        private void OnGameWinUnlockNextLevel()
        {
            if (LevelManager.Instance != null)
            {
                int nextIndex = LevelManager.Instance.CurrentLevelIndex + 1;
                if (nextIndex < LevelManager.Instance.AllLevels.Length)
                    LevelProgress.UnlockLevel(nextIndex);
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

            bool isSpecialSwipe = cell1.Special != SpecialType.None || cell2.Special != SpecialType.None;

            // 1. Анимация свайпа (для всех случаев)
            var swapTween = _boardView.AnimateSwap(fromX, fromY, toX, toY);
            yield return swapTween.WaitForCompletion();

            // 2. Меняем модель
            _board.Swap(fromX, fromY, toX, toY);
            _boardView.SwapViews(fromX, fromY, toX, toY);

            if (isSpecialSwipe)
            {
                // 3. Если это свайп бонуса — активируем его
                yield return StartCoroutine(ActivateSpecialRoutine(fromX, fromY, toX, toY));
                State.ConsumeMove();
                _isBusy = false;
                yield break;
            }

            // 4. Обычный свайп
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
                // 1. Падение и рефилл
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

                // 3. Проверка на бонусы в линии
                bool hasSpecial = false;
                for (int i = 0; i < _allMatchedCells.Count; i++)
                    if (_allMatchedCells[i].Special != SpecialType.None) { hasSpecial = true; break; }

                if (hasSpecial)
                {
                    List<BoardCell> specials = new List<BoardCell>();
                    for (int i = 0; i < _allMatchedCells.Count; i++)
                        if (_allMatchedCells[i].Special != SpecialType.None) specials.Add(_allMatchedCells[i]);

                    for (int i = 0; i < specials.Count; i++) _allMatchedCells.Remove(specials[i]);

                    if (_allMatchedCells.Count > 0)
                    {
                        var clearTween = _boardView.AnimateClear(_allMatchedCells);
                        yield return clearTween.WaitForCompletion();
                        _board.ClearMatches(_allMatchedCells);
                    }

                    for (int i = 0; i < specials.Count; i++)
                    {
                        if (specials[i].Special == SpecialType.Bomb)
                            yield return StartCoroutine(ExplodeBombRoutine(specials[i]));
                        else if (specials[i].Special == SpecialType.Rainbow)
                            yield return StartCoroutine(ExplodeRainbowRoutine(specials[i], _board.GetRandomType()));
                    }
                    ShowScoreAndAddPoints();
                    cascade++;
                    if (cascade > 0 && _cameraShake != null) _cameraShake.Shake();
                    continue;
                }

                // 4. Создание новых бонусов (4+)
                // 4. Создание новых бонусов (4+)
                _specialSpawnList.Clear();
                for (int i = 0; i < _matchResults.Count; i++)
                {
                    var match = _matchResults[i];
                    if (match.Length >= 4)
                    {
                        BoardCell cell = match.Cells[match.Length / 2];
                        bool already = false;
                        for (int j = 0; j < _specialSpawnList.Count; j++)
                            if (_specialSpawnList[j].X == cell.X && _specialSpawnList[j].Y == cell.Y) { already = true; break; }
                        if (already) continue;

                        SpecialType type = match.Length >= 5 ? SpecialType.Rainbow : SpecialType.Bomb;
                        _specialSpawnList.Add(new SpecialSpawnInfo { X = cell.X, Y = cell.Y, Type = type });
                        _allMatchedCells.Remove(cell);

                        // Начисляем очки за создание бонуса
                        int bonus = ScoreRules.GetBonusForSpecialCreation(type);
                        if (bonus > 0)
                        {
                            State.AddScore(bonus);
                            _floatingScorePool?.Play(_boardView.GetCellWorldPosition(cell), bonus);

                            // ВОТ ЭТА ПАУЗА: даем игроку увидеть цифру, прежде чем фрукты исчезнут
                            yield return new WaitForSeconds(0.2f);
                        }
                    }
                }

                // 5. Удаляем оставшиеся фрукты
                var clearTween2 = _boardView.AnimateClear(_allMatchedCells);
                yield return clearTween2.WaitForCompletion();
                _board.ClearMatches(_allMatchedCells);

                // 6. Применяем новые бонусы
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

                // 7. Очки за обычные фрукты
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
                _floatingScorePool.Play(_boardView.GetCellWorldPosition(_allMatchedCells[0]), points);
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
                yield return StartCoroutine(ExplodeRainbowRoutine(rainbow, target.Type));
            }

            yield return StartCoroutine(ResolveMatchesRoutine());
        }

        private IEnumerator ExplodeBombRoutine(BoardCell bomb)
        {
            _allMatchedCells.Clear();
            _chainReactionSpecials.Clear();
            for (int dx = -1; dx <= 1; dx++)
                for (int dy = -1; dy <= 1; dy++)
                {
                    int nx = bomb.X + dx, ny = bomb.Y + dy;
                    if (nx >= 0 && nx < _board.Width && ny >= 0 && ny < _board.Height)
                    {
                        BoardCell target = _board.Cells[nx, ny];
                        if (target.Type != ElementType.None)
                        {
                            _allMatchedCells.Add(target);
                            if (target.Special != SpecialType.None && target != bomb) _chainReactionSpecials.Add(target);
                        }
                    }
                }
            var clearTween = _boardView.AnimateClear(_allMatchedCells);
            yield return clearTween.WaitForCompletion();
            _board.ClearMatches(_allMatchedCells);
            if (_audio != null) _audio.PlayMatch();
            for (int i = 0; i < _allMatchedCells.Count; i++) _allMatchedCells[i].Special = SpecialType.None;
            for (int i = 0; i < _chainReactionSpecials.Count; i++)
            {
                var special = _chainReactionSpecials[i];
                if (special.Special == SpecialType.Bomb) yield return StartCoroutine(ExplodeBombRoutine(special));
                else if (special.Special == SpecialType.Rainbow) yield return StartCoroutine(ExplodeRainbowRoutine(special, _board.GetRandomType()));
            }
        }

        private IEnumerator ExplodeRainbowRoutine(BoardCell rainbow, ElementType targetType)
        {
            _allMatchedCells.Clear();
            _chainReactionSpecials.Clear();
            for (int x = 0; x < _board.Width; x++)
                for (int y = 0; y < _board.Height; y++)
                {
                    BoardCell target = _board.Cells[x, y];
                    if (target.Type == targetType || target == rainbow)
                    {
                        _allMatchedCells.Add(target);
                        if (target.Special != SpecialType.None && target != rainbow) _chainReactionSpecials.Add(target);
                    }
                }
            var clearTween = _boardView.AnimateClear(_allMatchedCells);
            yield return clearTween.WaitForCompletion();
            _board.ClearMatches(_allMatchedCells);
            if (_audio != null) _audio.PlayMatch();
            for (int i = 0; i < _allMatchedCells.Count; i++) _allMatchedCells[i].Special = SpecialType.None;
            for (int i = 0; i < _chainReactionSpecials.Count; i++)
            {
                var special = _chainReactionSpecials[i];
                if (special.Special == SpecialType.Bomb) yield return StartCoroutine(ExplodeBombRoutine(special));
                else if (special.Special == SpecialType.Rainbow) yield return StartCoroutine(ExplodeRainbowRoutine(special, _board.GetRandomType()));
            }
        }
    }
}