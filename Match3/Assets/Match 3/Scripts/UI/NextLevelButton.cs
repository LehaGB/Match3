using Match3.Core;
using UnityEngine;
using UnityEngine.SceneManagement;
using DG.Tweening;

namespace Match3.UI
{
    public class NextLevelButton : MonoBehaviour
    {
        public void LoadNextLevel()
        {
            Debug.Log("КНОПКА ДАЛЕЕ НАЖАТА!"); // <--- ВРЕМЕННЫЙ ЛОГ

            if (LevelManager.Instance != null && LevelManager.Instance.CurrentLevel != null)
            {
                int nextLevelIndex = LevelManager.Instance.CurrentLevel.LevelIndex + 1;

                // Проверяем, существует ли следующий уровень в массиве.
                if(nextLevelIndex <= LevelManager.Instance.AllLevels.Length)
                {
                    // Убиваем все анимации перед сменой сцены
                    DOTween.KillAll();

                    LevelManager.Instance.SetCurrentLevel(nextLevelIndex);

                    // Перезагружаем текущую игровую сцену.
                    SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
                }
                else
                {
                    Debug.Log("Это был последний уровень!");
                    // Если уровней больше нет - можно вернуть в меню
                    SceneManager.LoadScene("Menu");
                }
            }
            else
            {
                Debug.LogError("LevelManager не найден или CurrentLevel пуст!");
            }
        }
    }
}

