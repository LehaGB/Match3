
namespace Match3.Core
{
    /// <summary>
    /// 	Модель одной клетки: координаты X, Y и тип Type. Не MonoBehaviour, просто данные.
    /// </summary>
    public class BoardCell
    {
        public int X { get; }
        public int Y { get; }
        public ElementType Type { get; set; }

        public BoardCell(int x, int y, ElementType type)
        {
            X = x;
            Y = y;
            Type = type;
        }
    }
}

