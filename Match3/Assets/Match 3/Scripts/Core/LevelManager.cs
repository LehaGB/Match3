using UnityEngine;

namespace Match3.Core
{
    public class LevelManager : MonoBehaviour
    {
        public static LevelManager Instance { get; private set; }

        [Header("All Levels")]
        public LevelData[] AllLevels;

        public LevelData CurrentLevel { get; private set; }

        private void Awake()
        {
            // Убираем DontDestroyOnLoad. Пусть менеджер живет только в своей сцене.
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            // При старте пытаемся загрузить уровень, который был сохранен
            int savedLevel = PlayerPrefs.GetInt("SelectedLevel", 1);
            SetCurrentLevel(savedLevel);
        }

        public void SetCurrentLevel(int levelIndex)
        {
            if (levelIndex >= 1 && levelIndex <= AllLevels.Length)
            {
                CurrentLevel = AllLevels[levelIndex - 1];
                // Сохраняем выбор, чтобы он пережил загрузку сцены
                PlayerPrefs.SetInt("SelectedLevel", levelIndex);
                PlayerPrefs.Save();
                Debug.Log($"Выбран уровень {levelIndex}");
            }
        }
    }
}