using System.Collections.Generic;


namespace Match3.Core
{
    /// <summary>
    /// // Описывает одно найденное совпадение (линию).
    /// </summary>
    public class MatchResult
    {
        public List<BoardCell> Cells = new List<BoardCell>();
        public int Length => Cells.Count;

        // Направление: true - горизонтально, false - вертикально
        public bool IsHorizontal;

    }
}

