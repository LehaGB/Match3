using System;


namespace Match3.Core
{
    /// <summary>
    /// Чистое состояние игры: счёт и оставшиеся ходы.
    /// Не знает ни про Unity, ни про UI — только данные и события.
    /// </summary>
    public class GameState
    {
        // События: UI подписывается и обновляет текст при изменении.
        public event Action<int> ScoreChange;
        public event Action<int> MovesChange;
        public event Action GameOver;
        public event Action GameWin;

        public int Score { get; private set; }
        public int MovesLeft { get; private set; }
        public bool IsGameOver { get; private set; }
        public bool IsGameWin { get; private set; }

        private int _targetScore;

        // Инициализация: вызывается один раз при старте игры.
        public void Initialize(int statrtingMoves, int targetScore)
        {
            Score = 0;
            MovesLeft = statrtingMoves;
            _targetScore = targetScore;
            IsGameOver = false;
            IsGameWin = false;

            // Сообщаем подписчикам текущие значения (UI обновит текст).
            ScoreChange?.Invoke(Score);
            MovesChange?.Invoke(MovesLeft);
        }


        // Начисление очков: вызывается после успешного хода.
        public void AddScore(int amout)
        {
            if (IsGameOver || IsGameWin || amout <= 0) return;

            Score += amout;
            ScoreChange?.Invoke(Score);

            // Проверка на победу.
            if(Score >= _targetScore)
            {
                IsGameWin = true;
                GameWin?.Invoke();
            }
        }


        // Трата одного хода: вызывается после успешного хода.
        // Если ходы закончились — выставляем GameOver и уведомляем.
        public void ConsumeMove()
        {
            if (IsGameOver || IsGameWin) return;

            MovesLeft--;

            MovesChange?.Invoke(MovesLeft);

            if (MovesLeft <= 0)
            {
                IsGameOver = true;
                GameOver?.Invoke();
            }
        }
    }
}

