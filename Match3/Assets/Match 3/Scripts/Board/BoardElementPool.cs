using System;
using System.Collections.Generic;
using UnityEngine;


namespace Match3.Board
{
    public class BoardElementPool
    {
        private readonly BoardElementView _prefab;
        private readonly Transform _parent;
        private readonly Stack<BoardElementView> _pool = new Stack<BoardElementView>(64);

        public BoardElementPool(BoardElementView prefab, Transform parent, int prewarmCount)
        {
            _prefab = prefab;
            _parent = parent;
            Prewarm(prewarmCount);
        }

        private void Prewarm(int count)
        {
            for (int i = 0; i < count; i++)
            {
                var view = UnityEngine.Object.Instantiate(_prefab, _parent);
                view.gameObject.SetActive(false);
                _pool.Push(view);
            }
        }

        public BoardElementView Get()
        {
            var view = _pool.Count > 0 ? _pool.Pop() : UnityEngine.Object.Instantiate(_prefab, _parent);

            view.gameObject.SetActive(true);

            return view;
        }

        public void Release(BoardElementView view)
        {
            view.gameObject.SetActive(false);
            _pool.Push(view);
        }
    }
}

