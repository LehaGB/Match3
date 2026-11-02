using DG.Tweening;
using Match3.Core;
using System;
using System.Collections.Generic;
using UnityEngine;


namespace Match3.Board
{
    /// <summary>
    /// Представление поля: Initialize создаёт визуал из модели, 
    /// RefreshCell/RefreshCells обновляют спрайты, 
    /// ScreenToCell/IsInside переводят экран в индексы, 
    /// NotifySwipe/SetSwipeListener — мост к вводу. Кэширует Camera в Awake.
    /// </summary>
    public class BoardView : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private BoardElementView _elementPrefab;
        [SerializeField] private Transform _elementsParent;
        [SerializeField] private ElementSpriteProvider _spriteProvider;

        [Header("Layout")]
        [SerializeField] private float _cellSize = 1f;
        [SerializeField] private Vector2 _origin = Vector2.zero;

        [Header("Animation")]
        [SerializeField] private float _swapDuration = 0.15f;
        [SerializeField] private float _clearDuration = 0.1f;

        private BoardElementView[,] _views;
        private BoardElementPool _pool;
        private ISwipeListener _swipeListener;
        private Camera _camera;

        private void Awake()
        {
            _camera = Camera.main;
        }

        public void Initialize(BoardModel model)
        {
            int total = model.Width * model.Height;
            _pool = new BoardElementPool(_elementPrefab, _elementsParent, total);
            _views = new BoardElementView[model.Width, model.Height];

            for (int x = 0; x < model.Width; x++)
            {
                for (int y = 0; y < model.Height; y++)
                {
                    var cell = model.Cells[x, y];
                    var view = _pool.Get();
                    view.SetSprite(_spriteProvider.GetSprite(cell.Type));
                    view.SetPosition(CellToWorld(x, y));
                    view.ResetScale(); // На всякий случай
                    _views[x, y] = view;
                }
            }
        }

        public void RefreshCell(BoardCell cell)
        {
            var view = _views[cell.X, cell.Y];
            view.SetSprite(_spriteProvider.GetSprite(cell.Type));
            view.SetSpecialVisual(cell.Special);
        }

        public void RefreshCells(List<BoardCell> cells)
        {
            for (int i = 0; i < cells.Count; i++)
                RefreshCell(cells[i]);
        }

        public void SetSwipeListener(ISwipeListener listener) => _swipeListener = listener;
        public void NotifySwipe(int fromX, int fromY, int toX, int toY) => _swipeListener?.OnSwipe(fromX, fromY, toX, toY);

        // --- НОВЫЕ МЕТОДЫ ДЛЯ АНИМАЦИЙ ---


        /// <summary>
        /// Анимация обмена местами двух визуальных элементов
        /// </summary>
        /// <param name="x1"></param>
        /// <param name="y1"></param>
        /// <param name="x2"></param>
        /// <param name="y2"></param>
        /// <returns>На свои места</returns>
        public Tween AnimateSwap(int x1, int y1, int x2, int y2)
        {
            var view1 = _views[x1, y1];
            var view2 = _views[x2, y2];

            Vector3 pos1 = view1.transform.position;
            Vector3 pos2 = view2.transform.position;

            Sequence seq = DOTween.Sequence();
            seq.Join(view1.transform.DOMove(pos2, _swapDuration).SetEase(Ease.OutQuad));
            seq.Join(view2.transform.DOMove(pos1, _swapDuration).SetEase(Ease.OutQuad));
            return seq;
        }

        
        /// <summary>
        /// Сброс позиции элемента на координаты сетки (нужно после анимации возврата)
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        public void ResetPosition(int x, int y)
        {
            _views[x, y].SetPosition(CellToWorld(x, y));
        }


        /// <summary>
        /// Анимация исчезновения (уменьшение до нуля)
        /// </summary>
        /// <param name="cells"></param>
        /// <returns>Null</returns>
        public Tween AnimateClear(List<BoardCell> cells)
        {
            Sequence seq = DOTween.Sequence();
            for (int i = 0; i < cells.Count; i++)
            {
                var view = _views[cells[i].X, cells[i].Y];
                seq.Join(view.transform.DOScale(Vector3.zero, _clearDuration).SetEase(Ease.InBack));
            }
            return seq;
        }

        // --- ВСПОМОГАТЕЛЬНЫЕ МЕТОДЫ ---

        public bool ScreenToCell(Vector2 screenPos, out int x, out int y)
        {
            Vector3 world = _camera.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, -_camera.transform.position.z));
            x = Mathf.RoundToInt((world.x - _origin.x) / _cellSize);
            y = Mathf.RoundToInt((world.y - _origin.y) / _cellSize);
            return IsInside(x, y);
        }

        public bool IsInside(int x, int y)
        {
            return _views != null && x >= 0 && y >= 0 && x < _views.GetLength(0) && y < _views.GetLength(1);
        }

        private Vector3 CellToWorld(int x, int y)
        {
            return new Vector3(_origin.x + x * _cellSize, _origin.y + y * _cellSize, 0f);
        }


        
        /// <summary>
        /// Сброс масштаба для конкретной клетки (после анимации исчезновения)
        /// </summary>
        /// <param name="cell"></param>
        public void ResetScale(BoardCell cell)
        {
            _views[cell.X, cell.Y].ResetScale();
        }


        /// <summary>
        /// Сброс масштаба для списка клеток (можно использовать пачкой, если нужно)
        /// </summary>
        /// <param name="cells"></param>
        public void ResetScale(List<BoardCell> cells)
        {
            for (int i = 0; i < cells.Count; i++)
                ResetScale(cells[i]);
        }

        public void SwapViews(int x1, int y1, int x2, int y2)
        {
            var temp = _views[x1, y1];
            _views[x1, y1] = _views[x2, y2];
            _views[x2, y2] = temp;
        }




        /// <summary>
        /// Анимация падения: элементы съезжают на новые позиции.
        ///  moves — список перемещений из модели.
        ///  Сначала обновляем спрайты для новых позиций, потом двигаем.
        /// </summary>
        /// <param name="moves"></param>
        /// <param name="model"></param>
        /// <returns></returns>
        public Tween AnimateFall(List<CellMove> moves, BoardModel model)
        {
            Sequence seq = DOTween.Sequence();
            for (int i = 0; i < moves.Count; i++)
            {
                var move = moves[i];
                var view = _views[move.X, move.FromY];
                Vector3 target = CellToWorld(move.X, move.ToY);

                seq.Join(view.transform.DOMove(target, 0.2f).SetEase(Ease.InQuad));
            }
            return seq;
        }



        /// <summary>
        /// Синхронизация _views после падения: элементы, которые уехали вниз,
        /// должны «переехать» в массиве _views на новые индексы.
        /// </summary>
        /// <param name="moves"></param>
        /// <param name="model"></param>
        public void ApplyFallToViews(List<CellMove> moves, BoardModel model)
        {
            for (int i = 0; i < moves.Count; i++)
            {
                var move = moves[i];
                _views[move.X, move.ToY] = _views[move.X, move.FromY];
            }
        }


        /// <summary>
        /// Анимация появления новых элементов сверху.
        /// newCells — клетки, которые заполнились в модели.
        /// </summary>
        /// <param name="newCells"></param>
        /// <returns>Возвращает Sequence.</returns>
        public Tween AnimateSpawn(List<BoardCell> newCells)
        {
            Sequence seq = DOTween.Sequence();
            for (int i = 0; i < newCells.Count; i++)
            {
                var cell = newCells[i];
                var view = _views[cell.X, cell.Y];
                Vector3 target = CellToWorld(cell.X, cell.Y);
                Vector3 start = target + Vector3.up * _cellSize * 2f;

                view.SetPosition(start);
                view.SetSprite(_spriteProvider.GetSprite(cell.Type));
                view.ResetScale();

                seq.Join(view.transform.DOMove(target, 0.2f).SetEase(Ease.OutQuad));
            }
            return seq;
        }

        public Tween AnimateFullBoardUpdate(BoardModel model)
        {
            Sequence seq = DOTween.Sequence();

            for (int x = 0; x < model.Width; x++)
            {
                for (int y = 0; y < model.Height; y++)
                {
                    var cell = model.Cells[x, y];
                    var view = _views[x, y];

                    // Если объекта нет, берем из пула
                    if (view == null)
                    {
                        view = _pool.Get();
                        _views[x, y] = view;
                    }

                    // ВАЖНО: принудительно включаем объект и сбрасываем масштаб
                    view.gameObject.SetActive(true);
                    view.ResetScale();

                    // Ставим правильный спрайт
                    view.SetSprite(_spriteProvider.GetSprite(cell.Type));

                    Vector3 targetPos = CellToWorld(x, y);

                    // Если позиция не совпадает — анимируем полет
                    if (Vector3.Distance(view.transform.position, targetPos) > 0.01f)
                    {
                        seq.Join(view.transform.DOMove(targetPos, 0.25f).SetEase(Ease.OutQuad));
                    }
                    else
                    {
                        view.SetPosition(targetPos); // На всякий случай
                    }
                }
            }
            return seq;
        }


        /// <summary>
        /// 1. Плавное падение существующих элементов
        /// </summary>
        /// <param name="moves"></param>
        /// <returns></returns>
        public Tween AnimateFall(List<CellMove> moves)
        {
            Sequence seq = DOTween.Sequence();
            for (int i = 0; i < moves.Count; i++)
            {
                var move = moves[i];
                // Берем визуальный объект, который сейчас лежит на старой позиции (FromY)
                var view = _views[move.X, move.FromY];
                Vector3 target = CellToWorld(move.X, move.ToY);

                // Анимируем падение с ускорением
                seq.Join(view.transform.DOMove(target, 0.25f).SetEase(Ease.InQuad));

                // СРАЗУ переносим ссылку в массиве, чтобы следующие шаги видели его на новом месте
                _views[move.X, move.ToY] = view;
            }
            return seq;
        }


        /// <summary>
        /// 2. Плавное появление новых элементов
        /// </summary>
        /// <param name="newCells"></param>
        /// <param name="model"></param>
        /// <returns></returns>
        public Tween AnimateSpawn(List<BoardCell> newCells, BoardModel model)
        {
            Sequence seq = DOTween.Sequence();
            for (int i = 0; i < newCells.Count; i++)
            {
                var cell = newCells[i];

                // Для новых элементов берем объект из пула
                var view = _pool.Get();
                _views[cell.X, cell.Y] = view;

                Vector3 target = CellToWorld(cell.X, cell.Y);
                Vector3 start = target + Vector3.up * _cellSize * 2f; // Стартуем выше поля

                view.SetPosition(start);
                view.SetSprite(_spriteProvider.GetSprite(cell.Type));
                view.ResetScale();

                // Анимируем полет сверху вниз
                seq.Join(view.transform.DOMove(target, 0.25f).SetEase(Ease.OutQuad));
            }
            return seq;
        }


        public Vector3 GetCellWorldPosition(BoardCell cell)
        {
            return CellToWorld(cell.X, cell.Y);
        }


        public void RefreshAll(BoardModel model)
        {
            for (int x = 0; x < model.Width; x++)
            {
                for (int y = 0; y < model.Height; y++)
                {
                    var cell = model.Cells[x, y];
                    var view = _views[x, y];
                    view.SetSprite(_spriteProvider.GetSprite(cell.Type));
                    view.SetPosition(CellToWorld(x, y));
                    view.ResetScale();
                }
            }
        }
    }
}

