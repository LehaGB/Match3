using DG.Tweening;
using Match3.Board;
using Match3.UI;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.GraphicsBuffer;

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
            // 1. Если LevelManager есть, берем уровень из него
            if (_levelData == null && LevelManager.Instance != null)
            {
                _levelData = LevelManager.Instance.CurrentLevel;
            }

            // 2. Если уровня всё равно нет (запуск из Game напрямую), берем первый попавшийся
            if (_levelData == null && LevelManager.Instance != null && LevelManager.Instance.AllLevels.Length > 0)
            {
                _levelData = LevelManager.Instance.AllLevels[0]; // Берем Level_1 для теста
                Debug.LogWarning("LevelData не был выбран, используем Level_1 по умолчанию.");
            }

            // 3. Если LevelManager вообще нет (нет объекта в сцене), используем жесткие настройки
            if (_levelData == null)
            {
                Debug.LogError("LevelData не назначен! Использую настройки по умолчанию (20 ходов, 1000 очков).");
                State = new GameState();
                State.Initialize(20, 1000);
                return;
            }

            State = new GameState();
            State.Initialize(_levelData.StartingsMoves, _levelData.TargetScore);
            State.GameOver += OnGameOverCheckShuffle;
            State.GameWin += OnGameWinUnlockNextLevel; // <--- Убедись, что эта строка тоже есть
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
                // 1. Сначала анимируем свайп (обмен местами)
                var specialSwapTween = _boardView.AnimateSwap(fromX, fromY, toX, toY);
                yield return specialSwapTween.WaitForCompletion();

                // 2. Активируем бонус (взрыв)
                yield return StartCoroutine(ActivateSpecialRoutine(fromX, fromY, toX, toY));

                // 3. Возвращаем визуальные объекты на их логические места
                // (так как в модели мы обмен не делали)
                _boardView.ResetPosition(fromX, fromY);
                _boardView.ResetPosition(toX, toY);

                // 4. Обновляем спрайты на случай, если что-то изменилось
                _boardView.RefreshCell(_board.Cells[fromX, fromY]);
                _boardView.RefreshCell(_board.Cells[toX, toY]);

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
            Debug.Log($"Свайп: from({fromX},{fromY}) Type={cell1.Type}, Spec={cell1.Special} | to({toX},{toY}) Type={cell2.Type}, Spec={cell2.Special}");

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

                // 3. ПРОВЕРКА: есть ли в найденных линиях уже существующие бонусы?
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
                    Debug.Log("В линии найден бонус! Активируем...");
                    yield return StartCoroutine(ActivateSpecialsInMatchRoutine());
                    continue; // Пропускаем обычное удаление, идем на следующий круг после взрыва
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
                        Debug.Log($"Подготовлен бонус: {specialToCreate} в ({cell.X}, {cell.Y})");
                    }
                }

                // 5. Анимация исчезновения (для обычных элементов)
                var clearTween = _boardView.AnimateClear(_allMatchedCells);
                yield return clearTween.WaitForCompletion();

                // 6. Очистка модели
                _board.ClearMatches(_allMatchedCells);

                // 7. Применяем новые бонусы
                for (int i = 0; i < _specialSpawnList.Count; i++)
                {
                    var info = _specialSpawnList[i];
                    BoardCell cell = _board.Cells[info.X, info.Y];

                    if (cell.Type != ElementType.None)
                    {
                        cell.Special = info.Type;
                        cell.Type = info.Type == SpecialType.Bomb ? _board.GetRandomType() : ElementType.Apple;
                        _boardView.RefreshCell(cell);
                    }
                }

                // 8. Очки и звук
                ShowScoreAndAddPoints();
                if (_audio != null) _audio.PlayMatch();

                cascade++;
                if (cascade > 0 && _cameraShake != null) _cameraShake.Shake();
            }
        }

        private IEnumerator ActivateSpecialsInMatchRoutine()
        {
            // Собираем все бонусы, которые попали в совпадение.
            List<BoardCell> specialsToActivate = new List<BoardCell>();

            for(int i = 0; i < _allMatchedCells.Count; i++)
            {
                if (_allMatchedCells[i].Special != SpecialType.None)
                {
                    specialsToActivate.Add(_allMatchedCells[i]);
                }
            }

            // Взрываем каждый бонус.
            for (int i = 0; i < specialsToActivate.Count; i++)
            {
                var special = specialsToActivate[i];
                if(special.Special == SpecialType.Bomb)
                {
                    yield return StartCoroutine(ExplodeBombRoutine(special));
                }
                else if (special.Special == SpecialType.Rainbow)
                {
                    yield return StartCoroutine(ExplodeRainbowRoutine(special, _board.GetRandomType()));
                }
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


            // ЛОГ 1: Зашли в метод
            Debug.Log($"ActivateSpecialRoutine: cell1.Special={cell1.Special}, cell2.Special={cell2.Special}");

            if (cell1.Special == SpecialType.Bomb || cell2.Special == SpecialType.Bomb)
            {
                BoardCell bomb = cell1.Special == SpecialType.Bomb ? cell1 : cell2;

                Debug.Log($"Взрываем бомбу в ({bomb.X}, {bomb.Y})");

                yield return StartCoroutine(ExplodeBombRoutine(bomb));
            }
            else if (cell1.Special == SpecialType.Rainbow || cell2.Special == SpecialType.Rainbow)
            {
                BoardCell rainbow = cell1.Special == SpecialType.Rainbow ? cell1 : cell2;
                BoardCell target = cell1.Special == SpecialType.Rainbow ? cell2 : cell1;

                // Берем тип ЦЕЛЕВОЙ клетки ДО любых изменений
                ElementType targetType = target.Type;
                Debug.Log($"АКТИВАЦИЯ РАДУГИ: цель цвета {targetType}, радуга в ({rainbow.X},{rainbow.Y})");

                yield return StartCoroutine(ExplodeRainbowRoutine(rainbow, targetType));
            }

            // После взрыва запускаем разрешение (падение, рефилл, каскады)
            Debug.Log("ActivateSpecialRoutine: запускаем ResolveMatchesRoutine");
            yield return StartCoroutine(ResolveMatchesRoutine());
            State.ConsumeMove();
        }

        private IEnumerator ExplodeBombRoutine(BoardCell bomb)
        {

            Debug.Log($"ExplodeBombRoutine начал для бомбы ({bomb.X}, {bomb.Y})");

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

                            // Если внутри есть еще бомба или радуга — добавляем в очередь на взрыв
                            if (target.Special != SpecialType.None && target != bomb)
                            {
                                _chainReactionSpecials.Add(target);
                            }
                        }
                    }
                }
            }
            Debug.Log($"ExplodeBombRoutine: собрали {_allMatchedCells.Count} клеток для взрыва");

            // Анимация исчезновения
            var clearTween = _boardView.AnimateClear(_allMatchedCells);
            yield return clearTween.WaitForCompletion();

            _board.ClearMatches(_allMatchedCells);
            if (_audio != null) _audio.PlayMatch();

            // НОВОЕ: Затираем Special у взорванных клеток, чтобы не было фантомов
            for (int i = 0; i < _allMatchedCells.Count; i++)
                _allMatchedCells[i].Special = SpecialType.None;

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
            _chainReactionSpecials.Clear();

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
                        {
                            _chainReactionSpecials.Add(target);
                        }
                    }
                }
            }
            // ЛОГ: сколько клеток нашли
            Debug.Log($"ВЗРЫВ РАДУГИ: нашли {_allMatchedCells.Count} клеток цвета {targetType} (включая радугу)");

            var clearTween = _boardView.AnimateClear(_allMatchedCells);
            yield return clearTween.WaitForCompletion();
            _board.ClearMatches(_allMatchedCells);
            if (_audio != null) _audio.PlayMatch();

            // НОВОЕ: Затираем Special у взорванных клеток
            for (int i = 0; i < _allMatchedCells.Count; i++)
                _allMatchedCells[i].Special = SpecialType.None;

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

        private void OnGameWinUnlockNextLevel()
        {
            // Разблокируем следующий уровень (текущий + 1).
            if (_levelData != null)
            {
                int nextLevelIndex = _levelData.LevelIndex + 1;
                LevelProgress.UnLockLevel(nextLevelIndex);
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