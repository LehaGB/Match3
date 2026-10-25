using UnityEngine;
using Match3.Core;

namespace Match3.Board
{
    public interface IElementSpriteProvider
    {
        Sprite GetSprite(ElementType type);
    }
}

