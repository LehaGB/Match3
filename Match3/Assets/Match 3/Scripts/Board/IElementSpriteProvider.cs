using UnityEngine;
using Match3.Core;

namespace Match3.Board
{
    /// <summary>
    /// Интерфейс «дай спрайт по типу». 
    /// Позволяет подменить реализацию (скины, темы, Addressables) без правки отрисовки.
    /// </summary>
    public interface IElementSpriteProvider
    {
        Sprite GetSprite(ElementType type);
    }
}

