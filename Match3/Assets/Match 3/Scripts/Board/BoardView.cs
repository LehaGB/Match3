using Match3.Core;
using System;
using UnityEngine;


namespace Match3.Board
{
    public class BoardView : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private BoardElementView _elementPrefab;
        [SerializeField] private Transform _elementsParent;
        [SerializeField] private ElementSpriteProvider _spriteProvider;

        [Header("Layout")]
        [SerializeField] private float _cellSize = 1f;
        [SerializeField] private Vector2 _origin = Vector2.zero;

        private BoardElementView[,] _view;
        private BoardElementPool _pool;

        public void Initialize(BoardModel model)
        {
            int total = model.Width * model.Height;
            _pool = new BoardElementPool(_elementPrefab, _elementsParent, total);
            _view = new BoardElementView[model.Width, model.Height];

            for (int x = 0; x < model.Width; x++)
            {
                for (int y = 0; y < model.Height; y++)
                {
                    var cell = model.Cells[x, y];
                    var view = _pool.Get();
                    view.SetSprite(_spriteProvider.GetSprite(cell.Type));
                    view.SetPosition(CellToWorld(x, y));
                    _view[x, y] = view;
                }
            }
        }

        public void RefreshCell(BoardCell cell)
        {
            var view = _view[cell.X, cell.Y];
            view.SetSprite(_spriteProvider.GetSprite(cell.Type));
        }

        private Vector3 CellToWorld(int x, int y)
        {
            return new Vector3(_origin.x + x * _cellSize, _origin.y + y * _cellSize, 0f);
        }
    }
}

