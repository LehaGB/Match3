using System;

namespace Match3.Core
{
    public class GameState
    {
        public event Action<int> ScoreChanged;
        public event Action<int> MovesChanged;
        public event Action GameOver; // Финальное поражение
        public event Action GameWin;

        public int Score { get; private set; }
        public int MovesLeft { get; private set; }
        public bool IsGameOver { get; private set; }
        public bool IsGameWin { get; private set; }

        // Счетчик перемешиваний (даем 1 шанс по умолчанию)
        public int ShufflesLeft { get; private set; }

        private int _targetScore;

        public void Initialize(int startingMoves, int targetScore)
        {
            Score = 0;
            MovesLeft = startingMoves;
            _targetScore = targetScore;
            IsGameOver = false;
            IsGameWin = false;
            ShufflesLeft = 1; // Даем 1 перемешивание

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

            // Если ходы закончились
            if (MovesLeft <= 0)
            {
                // Проверяем, есть ли шанс на перемешивание
                if (ShufflesLeft > 0)
                {
                    ShufflesLeft--;
                    MovesLeft = 5; // Даем 5 ходов за перемешивание
                    MovesChanged?.Invoke(MovesLeft);
                    // ВАЖНО: Мы не вызываем GameOver, игра продолжается!
                }
                else
                {
                    // Шансов больше нет — окончательное поражение
                    IsGameOver = true;
                    GameOver?.Invoke();
                }
            }
        }
        public void ResetGameOver()
        {
            // Этот метод тоже больше не нужен, но оставим пустым, чтобы не ломать код
            IsGameOver = false;
        }
    }
}