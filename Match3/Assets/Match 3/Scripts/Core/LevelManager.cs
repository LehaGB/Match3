using UnityEngine;

namespace Match3.Core
{
    public class LevelManager : MonoBehaviour
    {
        public static LevelManager Instance { get; private set; }

        [Header("All Levels")]
        public LevelData[] AllLevels;

        public LevelData CurrentLevel { get; private set; }
        public int CurrentLevelIndex { get; private set; } // 0, 1, 2...

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            // При старте загружаем сохраненный индекс (по умолчанию 0 — первый уровень)
            int savedIndex = PlayerPrefs.GetInt("SelectedLevelIndex", 0);
            SetCurrentLevel(savedIndex);
        }

        // Передаем ИНДЕКС (0, 1, 2), а не номер уровня (1, 2, 3)
        public void SetCurrentLevel(int index)
        {
            if (index >= 0 && index < AllLevels.Length)
            {
                CurrentLevelIndex = index;
                CurrentLevel = AllLevels[index];

                PlayerPrefs.SetInt("SelectedLevelIndex", index);
                PlayerPrefs.Save();
                Debug.Log($"Выбран уровень с индексом {index} (по счету: {index + 1})");
            }
        }
    }
}