using UnityEngine;


namespace Match3.Core
{
    public class LevelProgress : MonoBehaviour
    {
        private const string UnlockedLevelKey = "UnlockedLevel";

        // Возвращает номер последнего открытого уровня (по умолчанию 1).
        public static int GetUnlockedLevel()
        {
            return PlayerPrefs.GetInt(UnlockedLevelKey, 1);
        }


        // Разблокирует следующий уровень.
        public static void UnLockLevel(int levelIndex)
        {
            if(levelIndex > GetUnlockedLevel())
            {
                PlayerPrefs.SetInt(UnlockedLevelKey, levelIndex);
                PlayerPrefs.Save();

                Debug.Log($"Уровень {levelIndex} разблокирован!");
            }
        }
    }
}

