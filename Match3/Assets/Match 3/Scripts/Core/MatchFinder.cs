using NUnit.Framework;
using NUnit.Framework.Interfaces;
using System.Collections.Generic;
using UnityEngine;

namespace Match3.Core
{
    public static class MatchFinder
    {
        // Возвращает список клеток, входящих в совпадения 3+ в ряд.
        // Список переиспользуется — не создавай новый на каждый вызов.
        public static void FindMatches(BoardModel board, List<BoardCell> results)
        {
            results.Clear();

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
                            for (int i = runStart; i < x; i++)
                                results.Add(board.Cells[i, y]);
                        }
                        runStart = x;
                    }
                }
            }

            // Вертикальные линии
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
                            for (int i = runStart; i < y; i++)
                                results.Add(board.Cells[x, i]);
                        }
                        runStart = y;
                    }
                }
            }
        }
    }
}

