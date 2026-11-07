using Match3.Core;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;


namespace Match3.UI
{
    public class LevelSelectMenu : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private string _gameSceneName = "Game";
        [SerializeField] private Button _levelButtonPrefab;

        // Куда складывать кнопки.
        [SerializeField] private Transform _buttonsParent;


        private void Start()
        {
            Debug.Log("LevelSelectMenu: Start вызван!");
            Debug.Log($"EventSystem найден: {UnityEngine.EventSystems.EventSystem.current != null}");
            CreateLevelButtons();
        }

        private void CreateLevelButtons()
        {
            if (LevelManager.Instance == null || LevelManager.Instance.AllLevels == null)
            {
                Debug.LogError("LevelManager не найден или не настроен!");
                return;
            }

            int unlockedLevel = LevelProgress.GetUnlockedLevel();
            LevelData[] allLevels = LevelManager.Instance.AllLevels;

            for (int i = 0; i < allLevels.Length; i++)
            {
                int levelIndex = i; // Индекс 0, 1, 2...
                int levelNumber = i + 1; // Номер для отображения 1, 2, 3...

                // Открыт ли этот уровень? (если индекс меньше или равен количеству пройденных)
                int unlockedIndex = LevelProgress.GetUnlockedLevel();
                bool isUnlocked = levelIndex <= unlockedIndex;

                Button button = Instantiate(_levelButtonPrefab, _buttonsParent);

                RectTransform rect = button.GetComponent<RectTransform>();
                if (rect != null)
                {
                    float startY = 150f;
                    rect.anchoredPosition = new Vector2(0, startY - (150f * levelIndex));
                }

                TMP_Text buttonText = button.GetComponentInChildren<TMP_Text>();
                if (buttonText != null)
                    buttonText.text = $"Уровень {levelNumber}";

                button.interactable = isUnlocked;

                if (isUnlocked)
                {
                    int capturedIndex = levelIndex;
                    button.onClick.AddListener(() =>
                    {
                        Debug.Log($"Нажимаем уровень с индексом {capturedIndex}");
                        LevelManager.Instance.SetCurrentLevel(capturedIndex);
                        SceneManager.LoadScene("Game");
                    });
                }
            }
        }

        public void OnLevelSelected(int levelIndex)
        {
            Debug.Log($"Нажата кнопка уровня {levelIndex}");
            if (LevelManager.Instance != null)
            {
                LevelManager.Instance.SetCurrentLevel(levelIndex);
                SceneManager.LoadScene(_gameSceneName);
            }
        }
    }
}

