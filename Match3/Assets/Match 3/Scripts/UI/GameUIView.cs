using Match3.Core;
using UnityEngine;
using TMPro;
using System;

namespace Match3.UI
{

    // Подписывается на события GameState и обновляет текст.
    // Не знает про BoardController — только про GameState.
    public class GameUIView : MonoBehaviour
    {
        [SerializeField] private TMP_Text _levelText;
        [SerializeField] private BoardController _boardController;
        [SerializeField] private TMP_Text _scoreText;
        [SerializeField] private TMP_Text _movesText;
        [SerializeField] private GameObject _gameOverPanel;
        [SerializeField] private GameObject _winPanel;


        private void Start()
        {
            var state = _boardController.State;

            // Подписываемся на события. Методы вызываются при изменении.
            state.ScoreChanged += OnScoreChanged;
            state.MovesChanged += OnMovesChanged;
            state.GameOver += OnGameOver;
            state.GameWin += OnGameWin;

            // Показываем номер текущего уровня.
            if (LevelManager.Instance != null && LevelManager.Instance.CurrentLevel != null)
            {
                _levelText.text = $"Уровень {LevelManager.Instance.CurrentLevel.LevelIndex}";
            }
            else
            {
                _levelText.text = "Уровень ?";
            }
        }

        private void OnGameWin()
        {
            if(_winPanel != null)
            {
                _winPanel.SetActive(true);
            }
        }

        private void OnDestroy()
        {
            // Обязательно отписываемся, иначе утечка памяти при перезагрузке сцены.
            if (_boardController == null || _boardController.State == null) return;
            var state = _boardController.State;
            state.ScoreChanged -= OnScoreChanged;
            state.MovesChanged -= OnMovesChanged;
            state.GameOver -= OnGameOver;
            state.GameWin -= OnGameWin;
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
            if(_gameOverPanel != null)
            {
                _gameOverPanel.SetActive(true);
            }
        } 
    }
}


