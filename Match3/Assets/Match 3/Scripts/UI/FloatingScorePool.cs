using System.Collections.Generic;
using UnityEngine;

namespace Match3.UI
{
    public class FloatingScorePool : MonoBehaviour
    {
        [SerializeField] private FloatingScoreText _prefab;
        [SerializeField] private Transform _parent;

        private readonly Stack<FloatingScoreText> _pool = new Stack<FloatingScoreText>(16);

        public void Play(Vector3 worldPosition, int amount)
        {
            var item = _pool.Count > 0 ? _pool.Pop() : Instantiate(_prefab, _parent);
            item.Play(worldPosition, amount);
            _pool.Push(item);
        }
    }
}