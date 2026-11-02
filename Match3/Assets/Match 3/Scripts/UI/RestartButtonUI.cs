using UnityEngine;
using UnityEngine.SceneManagement;

namespace Match3.UI
{
    public class RestartButtonUI : MonoBehaviour
    {
        public void RestartLevel()
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
    }
}

