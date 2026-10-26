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
        }

        public void RefreshCells(List<BoardCell> cells)
        {
            for (int i = 0; i < cells.Count; i++)
                RefreshCell(cells[i]);
        }

        public void SetSwipeListener(ISwipeListener listener) => _swipeListener = listener;
        public void NotifySwipe(int fromX, int fromY, int toX, int toY) => _swipeListener?.OnSwipe(fromX, fromY, toX, toY);

        // --- НОВЫЕ МЕТОДЫ ДЛЯ АНИМАЦИЙ ---

        // Анимация обмена местами двух визуальных элементов
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

        // Сброс позиции элемента на координаты сетки (нужно после анимации возврата)
        public void ResetPosition(int x, int y)
        {
            _views[x, y].SetPosition(CellToWorld(x, y));
        }

        // Анимация исчезновения (уменьшение до нуля)
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


        // Сброс масштаба для конкретной клетки (после анимации исчезновения)
        public void ResetScale(BoardCell cell)
        {
            _views[cell.X, cell.Y].ResetScale();
        }

        // Сброс масштаба для списка клеток (можно использовать пачкой, если нужно)
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
    }
}

