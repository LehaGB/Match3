using System;

namespace Match3.Core
{
    /// <summary>
    /// Хранит текущее состояние игры (очки, ходы) и уведомляет подписчиков
    /// о победе или поражении.
    /// </summary>
    public class GameState
    {
        // События, на которые подписывается UI
        public event Action<int> ScoreChanged;   // Вызывается при изменении очков
        public event Action<int> MovesChanged;   // Вызывается при изменении ходов
        public event Action GameOver;            // Вызывается при поражении
        public event Action GameWin;             // Вызывается при победе

        public int Score { get; private set; }       // Текущие очки
        public int MovesLeft { get; private set; }   // Оставшиеся ходы
        public bool IsGameOver { get; private set; } // Закончилась ли игра поражением
        public bool IsGameWin { get; private set; }  // Закончилась ли игра победой

        private int _targetScore; // Цель уровня (сколько нужно набрать)

        /// <summary>
        /// Инициализирует состояние при старте уровня.
        /// </summary>
        public void Initialize(int startingMoves, int targetScore)
        {
            Score = 0;
            MovesLeft = startingMoves;
            _targetScore = targetScore;
            IsGameOver = false;
            IsGameWin = false;

            // Уведомляем UI о начальных значениях
            ScoreChanged?.Invoke(Score);
            MovesChanged?.Invoke(MovesLeft);
        }

        /// <summary>
        /// Добавляет очки и проверяет, не достигнута ли цель.
        /// </summary>
        public void AddScore(int amount)
        {
            if (IsGameOver || IsGameWin || amount <= 0) return;

            Score += amount;
            ScoreChanged?.Invoke(Score);

            // Если набрали нужное количество очков — победа
            if (Score >= _targetScore)
            {
                IsGameWin = true;
                GameWin?.Invoke();
            }
        }

        /// <summary>
        /// Тратит один ход. Если ходы закончились — поражение.
        /// </summary>
        public void ConsumeMove()
        {
            if (IsGameOver || IsGameWin) return;

            MovesLeft--;
            MovesChanged?.Invoke(MovesLeft);

            // Если ходы закончились — поражение (без перемешивания)
            if (MovesLeft <= 0)
            {
                IsGameOver = true;
                GameOver?.Invoke();
            }
        }
    }
}