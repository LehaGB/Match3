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
                int levelNumber = i + 1;
                bool isUnlocked = levelNumber <= unlockedLevel;

                // 1. Создаем кнопку
                Button button = Instantiate(_levelButtonPrefab, _buttonsParent);

                // 2. НАСТРАИВАЕМ ПОЗИЦИЮ (ставим в столбик с отступом вниз)
                RectTransform rect = button.GetComponent<RectTransform>();
                if (rect != null)
                {
                    // anchoredPosition работает относительно центра родителя (LevelMenuPanel)
                    // X = 0 (по центру), Y = -100, -200, -300...
                    // Делаем так, чтобы Уровень 1 был сверху, а остальные шли вниз
                    float startY = 150f;
                    rect.anchoredPosition = new Vector2(0, startY - (100f * (levelNumber - 1)));
                }

                // 3. Настраиваем текст
                TMP_Text buttonText = button.GetComponentInChildren<TMP_Text>();
                if (buttonText != null)
                    buttonText.text = $"Уровень {levelNumber}";

                // 4. Включаем/выключаем кнопку
                button.interactable = isUnlocked;

                // 5. Привязываем клик
                if (isUnlocked)
                {
                    int levelIndex = levelNumber;
                    button.onClick.AddListener(() =>
                    {
                        Debug.Log($"КЛИК! Выбран уровень {levelIndex}");
                        OnLevelSelected(levelIndex);
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

