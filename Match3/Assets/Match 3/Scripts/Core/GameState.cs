using System;

namespace Match3.Core
{
    public class GameState
    {
        public event Action<int> ScoreChanged;
        public event Action<int> MovesChanged;
        public event Action GameOver;
        public event Action GameWin;

        public int Score { get; private set; }
        public int MovesLeft { get; private set; }
        public bool IsGameOver { get; private set; }
        public bool IsGameWin { get; private set; }
        public int ShufflesLeft { get; private set; }

        private int _targetScore;

        public void Initialize(int startingMoves, int targetScore)
        {
            Score = 0;
            MovesLeft = startingMoves;
            _targetScore = targetScore;
            IsGameOver = false;
            IsGameWin = false;
            ShufflesLeft = 1; // Даем 1 перемешивание по умолчанию

            ScoreChanged?.Invoke(Score);
            MovesChanged?.Invoke(MovesLeft);
        }

        public void AddScore(int amount)
        {
            if (IsGameOver || IsGameWin || amount <= 0) return;

            Score += amount;
            ScoreChanged?.Invoke(Score);

            if (Score >= _targetScore)
            {
                IsGameWin = true;
                GameWin?.Invoke();
            }
        }

        public void ConsumeMove()
        {
            if (IsGameOver || IsGameWin) return;

            MovesLeft--;
            MovesChanged?.Invoke(MovesLeft);

            if (MovesLeft <= 0)
            {
                IsGameOver = true;
                GameOver?.Invoke();
            }
        }

        public void UseShuffle()
        {
            ShufflesLeft--;
        }

        public void ResetGameOver()
        {
            IsGameOver = false;
            // Даем 5 ходов за перемешивание
            MovesLeft = 5;
            MovesChanged?.Invoke(MovesLeft); // Вот здесь мы вызываем событие
        }
    }
}