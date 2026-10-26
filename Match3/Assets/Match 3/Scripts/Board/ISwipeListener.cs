


namespace Match3.Board
{
    /// <summary>
    /// Интерфейс «я слушаю свайпы»: OnSwipe(fromX, fromY, toX, toY). 
    /// BoardController его реализует, BoardView вызывает.
    /// </summary>
    public interface ISwipeListener
    {
        void OnSwipe(int fromX, int fromY, int toX, int toY);
    }
}

