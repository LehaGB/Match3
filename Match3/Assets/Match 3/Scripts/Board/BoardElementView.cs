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

    }
}

