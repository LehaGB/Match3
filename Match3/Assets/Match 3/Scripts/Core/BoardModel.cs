using System;
using System.Collections.Generic;

namespace Match3.Core
{
    public class BoardModel
    {
        public int Width { get; }
        public int Height { get; }
        public BoardCell[,] Cells { get; }

        private readonly Random _random;

        public BoardModel(int width, int height, int? seed = null)
        {
            Width = width;
            Height = height;
            Cells = new BoardCell[width, height];
            _random = seed.HasValue ? new Random(seed.Value) : new Random();
            GenerateBoard();
        }

        private void GenerateBoard()
        {
            for (int x = 0; x < Width; x++)
            {
                for (int y = 0; y < Height; y++)
                {
                    ElementType type;
                    do
                    {
                        type = GetRandomType();
                    } while (WouldCreateMatch(x, y, type));

                    Cells[x, y] = new BoardCell(x, y, type);
                }
            }
        }

        public ElementType GetRandomType()
        {
            var values = Enum.GetValues(typeof(ElementType));
            // None = 0, поэтому начинаем с 1
            return (ElementType)values.GetValue(1 + _random.Next(values.Length - 1));
        }

        private bool WouldCreateMatch(int x, int y, ElementType type)
        {
            if (x >= 2 &&
                Cells[x - 1, y]?.Type == type &&
                Cells[x - 2, y]?.Type == type)
                return true;

            if (y >= 2 &&
                Cells[x, y - 1]?.Type == type &&
                Cells[x, y - 2]?.Type == type)
                return true;

            return false;
        }

        public void Swap(int x1, int y1, int x2, int y2)
        {
            var a = Cells[x1, y1];
            var b = Cells[x2, y2];

            ElementType tmpType = a.Type;
            a.Type = b.Type;
            b.Type = tmpType;

            SpecialType tmpSpecial = a.Special;
            a.Special = b.Special;
            b.Special = tmpSpecial;
        }

        public void ClearMatches(List<BoardCell> matches)
        {
            for (int i = 0; i < matches.Count; i++)
            {
                matches[i].Type = ElementType.None;
                // Special не трогаем, чтобы бонусы не терялись при анимации
            }
        }

        public void ApplyGravityAndCollectMoves(List<CellMove> moves)
        {
            moves.Clear();

            for (int x = 0; x < Width; x++)
            {
                int writeY = 0;
                for (int y = 0; y < Height; y++)
                {
                    if (Cells[x, y].Type != ElementType.None)
                    {
                        if (writeY != y)
                        {
                            Cells[x, writeY].Type = Cells[x, y].Type;
                            Cells[x, writeY].Special = Cells[x, y].Special; // Переносим бонус

                            Cells[x, y].Type = ElementType.None;
                            Cells[x, y].Special = SpecialType.None;

                            moves.Add(new CellMove { X = x, FromY = y, ToY = writeY });
                        }
                        writeY++;
                    }
                }
            }
        }

        public void RefillAndCollectNew(List<BoardCell> newCells)
        {
            newCells.Clear();
            for (int x = 0; x < Width; x++)
            {
                for (int y = 0; y < Height; y++)
                {
                    if (Cells[x, y].Type == ElementType.None)
                    {
                        Cells[x, y].Type = GetRandomType();
                        Cells[x, y].Special = SpecialType.None; // Новый фрукт - всегда обычный
                        newCells.Add(Cells[x, y]);
                    }
                }
            }
        }

        public void UpgradeToSpecial(BoardCell cell, SpecialType special)
        {
            cell.Special = special;
        }

        // --- НОВЫЕ МЕТОДЫ ДЛЯ ПРОВЕРКИ ХОДОВ ---

        /// <summary>
        /// Проверяет, есть ли на поле хотя бы один возможный ход (свайп, создающий тройку)
        /// </summary>
        public bool HasPossibleMoves()
        {
            //return false;
            for (int x = 0; x < Width; x++)
            {
                for (int y = 0; y < Height; y++)
                {
                    // Пробуем свайпнуть вправо
                    if (x < Width - 1)
                    {
                        Swap(x, y, x + 1, y);
                        if (HasMatchAt(x, y) || HasMatchAt(x + 1, y))
                        {
                            Swap(x, y, x + 1, y); // Возвращаем назад
                            return true;
                        }
                        Swap(x, y, x + 1, y); // Возвращаем назад
                    }

                    // Пробуем свайпнуть вверх
                    if (y < Height - 1)
                    {
                        Swap(x, y, x, y + 1);
                        if (HasMatchAt(x, y) || HasMatchAt(x, y + 1))
                        {
                            Swap(x, y, x, y + 1); // Возвращаем назад
                            return true;
                        }
                        Swap(x, y, x, y + 1); // Возвращаем назад
                    }
                }
            }
            return false; // Ни одного хода не найдено
        }

        /// <summary>
        /// Вспомогательный метод: проверяет, образует ли клетка (X, Y) линию из 3+
        /// </summary>
        private bool HasMatchAt(int x, int y)
        {
            ElementType type = Cells[x, y].Type;
            if (type == ElementType.None) return false;

            // Проверка по горизонтали
            int count = 1;
            for (int i = x - 1; i >= 0 && Cells[i, y].Type == type; i--) count++;
            for (int i = x + 1; i < Width && Cells[i, y].Type == type; i++) count++;
            if (count >= 3) return true;

            // Проверка по вертикали
            count = 1;
            for (int i = y - 1; i >= 0 && Cells[x, i].Type == type; i--) count++;
            for (int i = y + 1; i < Height && Cells[x, i].Type == type; i++) count++;
            if (count >= 3) return true;

            return false;
        }
    }
}