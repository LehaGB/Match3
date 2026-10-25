using Match3.Core;
using System;
using UnityEngine;


namespace Match3.Board
{
    [CreateAssetMenu(menuName = "Match3/Element Sprite Provider", fileName = "ElementSpriteProvider")]
    public class ElementSpriteProvider : ScriptableObject, IElementSpriteProvider
    {
        [Serializable]
        public struct ElementSprite
        {
            public ElementType Type;
            public Sprite Sprite;
        }

        [SerializeField] private ElementSprite[] _sprites;

        public Sprite GetSprite(ElementType type)
        {
            if (type == ElementType.None) return null;

            for (int i = 0; i < _sprites.Length; i++)
            {
                if (_sprites[i].Type == type)
                {
                    return _sprites[i].Sprite;
                }
            }
            return null;
        }
    }
}

