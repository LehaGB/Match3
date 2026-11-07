using Match3.Core;
using TMPro;
using UnityEngine;

namespace Match3.UI
{
    public class GameUIView : MonoBehaviour
    {
        [SerializeField] private BoardController _boardController;
        [SerializeField] private TMP_Text _scoreText;
        [SerializeField] private TMP_Text _movesText;
        [SerializeField] private GameObject _winPanel;
        [SerializeField] private GameObject _losePanel;
        [SerializeField] private TMP_Text _levelText;
        [SerializeField] private GameObject _shuffleMessageText; 

        private void Start()
        {
            var state = _boardController.State;

            state.ScoreChanged += OnScoreChanged;
            state.MovesChanged += OnMovesChanged;
            state.GameOver += OnGameOver;
            state.GameWin += OnGameWin;

            OnScoreChanged(state.Score);
            OnMovesChanged(state.MovesLeft);

            // Подписываемся на перемешивание
            _boardController.OnShuffleStarted += OnShuffleStarted;

            if (LevelManager.Instance != null && LevelManager.Instance.CurrentLevel != null)
            {
                _levelText.text = $"Уровень {LevelManager.Instance.CurrentLevelIndex + 1}";
            }
            else
            {
                _levelText.text = "Уровень ?";
            }
        }

        private void OnDestroy()
        {
            if (_boardController != null && _boardController.State != null)
            {
                var state = _boardController.State;
                state.ScoreChanged -= OnScoreChanged;
                state.MovesChanged -= OnMovesChanged;
                state.GameOver -= OnGameOver;
                state.GameWin -= OnGameWin;
            }

            if (_boardController != null)
                _boardController.OnShuffleStarted -= OnShuffleStarted;
        }

        private void OnScoreChanged(int score)
        {
            _scoreText.text = $"Score: {score}";
        }

        private void OnMovesChanged(int moves)
        {
            _movesText.text = $"Moves: {moves}";
        }

        private void OnGameOver()
        {
            if (_losePanel != null) _losePanel.SetActive(true);
        }

        private void OnGameWin()
        {
            if (_winPanel != null) _winPanel.SetActive(true);
        }

        private void OnShuffleStarted()
        {
            if (_shuffleMessageText != null)
            {
                _shuffleMessageText.SetActive(true);
                Invoke(nameof(HideShuffleMessage), 1.5f);
            }
        }

        private void HideShuffleMessage()
        {
            if (_shuffleMessageText != null)
                _shuffleMessageText.SetActive(false);
        }
    }
}