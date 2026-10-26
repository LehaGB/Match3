using UnityEngine;

namespace Match3.Core
{
    /// <summary>
    /// Правила начисления очков. Пока простая формула:
    /// 3 клетки = 10, 4 = 20, 5+ = 30. Позже расширим (комбо, бонусы).
    /// </summary>
    public static class ScoreRules 
    {
        public static int CalculateForMatch(int matchedCells)
        {
            if (matchedCells < 3) return 0;
            if (matchedCells == 3) return 10;
            if (matchedCells == 4) return 40;
            return 30;
        }


    }
}

