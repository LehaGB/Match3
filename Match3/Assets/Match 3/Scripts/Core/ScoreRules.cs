using UnityEngine;

namespace Match3.Core
{
    /// <summary>
    /// Правила начисления очков. Пока простая формула:
    /// 3 клетки = 10, 4 = 20, 5+ = 30. Позже расширим (комбо, бонусы).
    /// </summary>
    public static class ScoreRules 
    {

        // Базовые очки за обычные совпадения
        public const int BaseMatchScore = 10;
        public const int BonusFor4Match = 25; // За линию из 4
        public const int BonusFor5Match = 50; // За линию из 5+

        public static int CalculateForMatch(int matchedCells)
        {
            if (matchedCells < 3) return 0;
            if (matchedCells == 3) return BaseMatchScore;
            if (matchedCells == 4) return BaseMatchScore * 2;
            return BonusFor5Match;
        }

        // НОВОЕ: Начисление очков за СОЗДАНИЕ бонуса (Бомбы или Радуги)
        public static int GetBonusForSpecialCreation(SpecialType type)
        {
            if (type == SpecialType.Bomb) return BonusFor4Match;
            if (type == SpecialType.Rainbow) return BonusFor5Match;
            return 0;
        }
    }
}

