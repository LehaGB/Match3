using UnityEngine;
using TMPro;
using DG.Tweening;
using Match3.Core;

namespace Match3.UI
{
    public class LevelGoalUI : MonoBehaviour
    {
        [SerializeField] private TMP_Text _goalText;   // Текст с целью.
        [SerializeField] private float _displayDuration = 1.5f;  // Сколько секунд висит на экране.

        private void Start()
        {
            if (LevelManager.Instance != null && LevelManager.Instance.CurrentLevel != null)
            {


                // Проверяем, есть ли LevelManager и текущий уровень.
                var level = LevelManager.Instance.CurrentLevel;

                // Формируем текст: цель по очкам и количество ходов.
                _goalText.text = $"Цель: {level.TargetScore} очков\nХодов: {level.StartingsMoves}";

                // Включаем панель и начинаем с нулевого масштаба (чтобы анимировать появление).
                gameObject.SetActive(true);
                transform.localScale = Vector3.zero;

                // Анимация: выезжает, стоит, уезжает.
                Sequence seq = DOTween.Sequence();
                seq.Append(transform.DOScale(Vector3.one, 0.3f).SetEase(Ease.OutBack));  // Появление.
                seq.AppendInterval(_displayDuration);  // Висит на экране.
                seq.Append(transform.DOScale(Vector3.one, 0.3f).SetEase(Ease.InBack));  // Исчезновение.

                seq.OnComplete(() => gameObject.SetActive(false));  // Выключаем после анимации.
            }
            else
            {
                // Если LevelManager нет — просто выключаем панель.
                gameObject.SetActive(false);
            }
        }
    }
}

