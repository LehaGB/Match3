using Match3.Core;
using UnityEngine;

namespace Match3.Board
{
    /// <summary>
    /// Компонент одного визуального элемента: 
    /// SpriteRenderer (кэшируется), SetSprite, SetPosition. 
    /// Не знает про ElementType — просто показывает спрайт.
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public class BoardElementView : MonoBehaviour
    {
        private SpriteRenderer _renderer;
        public SpriteRenderer Renderer
        {
            get
            {
                if(_renderer == null)
                {
                    _renderer = GetComponent<SpriteRenderer>();
                }
                return _renderer;
            }
        }

        public void SetSprite(Sprite sprite)
        {
            Renderer.sprite = sprite;
        }

        public void SetPosition(Vector3 position)
        {
            transform.position = position;
        }


        // сброс масштаба (после анимации исчезновения).
        public void ResetScale()
        {
            transform.localScale = Vector3.one;
        }

        public void SetSpecialVisual(SpecialType special)
        {
            if (special == SpecialType.None)
            {
                transform.localScale = Vector3.one;
                transform.rotation = Quaternion.identity;
                Renderer.color = Color.white;
            }
            else if (special == SpecialType.Bomb)
            {
                transform.localScale = Vector3.one * 1.1f;
                transform.rotation = Quaternion.Euler(0, 0, 45f); // Ромбик
                Renderer.color = Color.black; // Черный цвет
            }
            else if (special == SpecialType.Rainbow)
            {
                transform.localScale = Vector3.one * 1.2f;
                transform.rotation = Quaternion.identity;
                Renderer.color = new Color(1f, 0.5f, 0f); // Оранжевый
            }
        }

    }
}

