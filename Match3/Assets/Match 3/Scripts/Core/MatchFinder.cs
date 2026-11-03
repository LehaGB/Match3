using System.Collections.Generic;

namespace Match3.Core
{
    public static class MatchFinder
    {
        // Ищем линии и складываем их в результаты
        public static void FindMatches(BoardModel board, List<MatchResult> results, List<BoardCell> allMatchedCells)
        {
            results.Clear();
            allMatchedCells.Clear();

            // 1. Проверка горизонтальных линий
            for (int y = 0; y < board.Height; y++)
            {
                int runStart = 0;
                for (int x = 1; x <= board.Width; x++)
                {
                    bool same = x < board.Width &&
                                board.Cells[x, y].Type != ElementType.None &&
                                
                                board.Cells[x, y].Type == board.Cells[runStart, y].Type;

                    if (!same)
                    {
                        int runLength = x - runStart;
                        if (runLength >= 3)
                        {
                            MatchResult match = new MatchResult { IsHorizontal = true };
                            for (int i = runStart; i < x; i++)
                            {
                                match.Cells.Add(board.Cells[i, y]);
                                // Чтобы не дублировать клетки в общем списке (они могут быть в пересечении)
                                if (!allMatchedCells.Contains(board.Cells[i, y]))
                                    allMatchedCells.Add(board.Cells[i, y]);
                            }
                            results.Add(match);
                        }
                        runStart = x;
                    }
                }
            }

            // 2. Проверка вертикальных линий
            for (int x = 0; x < board.Width; x++)
            {
                int runStart = 0;
                for (int y = 1; y <= board.Height; y++)
                {
                    bool same = y < board.Height &&
                                board.Cells[x, y].Type != ElementType.None &&
                                
                                board.Cells[x, y].Type == board.Cells[x, runStart].Type;

                    if (!same)
                    {
                        int runLength = y - runStart;
                        if (runLength >= 3)
                        {
                            MatchResult match = new MatchResult { IsHorizontal = false };
                            for (int i = runStart; i < y; i++)
                            {
                                match.Cells.Add(board.Cells[x, i]);
                                if (!allMatchedCells.Contains(board.Cells[x, i]))
                                    allMatchedCells.Add(board.Cells[x, i]);
                            }
                            results.Add(match);
                        }
                        runStart = y;
                    }
                }
            }
        }
    }
}