using UnityEngine;

namespace Match3.Core
{
    public static class LevelProgress
    {
        private const string UnlockedIndexKey = "UnlockedLevelIndex";

        // Возвращает индекс последнего открытого уровня (0 — первый)
        public static int GetUnlockedLevel()
        {
            return PlayerPrefs.GetInt(UnlockedIndexKey, 0); // По умолчанию 0 (первый уровень)
        }

        // Разблокирует следующий уровень по его индексу
        public static void UnlockLevel(int index)
        {
            if (index > GetUnlockedLevel())
            {
                PlayerPrefs.SetInt(UnlockedIndexKey, index);
                PlayerPrefs.Save();
                Debug.Log($"Разблокирован уровень с индексом {index}");
            }
        }
    }
}