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
            if(Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }


        // Вызывается, когда игрок выбирает уровень.
        public void SetCurrentLevel(int levelIndex)
        {
            if(levelIndex >= 1 && AllLevels.Length <= 1)
            {
                CurrentLevel = AllLevels[levelIndex - 1];
                Debug.Log($"Выбран уровень {levelIndex}");
            }
        }
    }
}

