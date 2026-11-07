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
            if (LevelManager.Instance != null && LevelManager.Instance.CurrentLevel != null)
            {
                int nextIndex = LevelManager.Instance.CurrentLevelIndex + 1;

                if (nextIndex < LevelManager.Instance.AllLevels.Length)
                {
                    DOTween.KillAll();
                    LevelManager.Instance.SetCurrentLevel(nextIndex);
                    SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
                }
                else
                {
                    Debug.Log("Это был последний уровень!");
                    DOTween.KillAll();
                    SceneManager.LoadScene("Menu");
                }
            }
        }
    }
}

