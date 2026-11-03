using UnityEngine;
using System.Collections.Generic;
using System;
//using System.Diagnostics;


namespace Match3.Core
{

    /// <summary>
    /// Модель всего поля: двумерный массив клеток, 
    /// генерация без стартовых совпадений (GenerateBoard, WouldCreateMatch), 
    /// обмен (Swap), разрешение хода (ResolveStep — очистка, гравитация, рефилл), 
    /// случайный тип (GetRandomType).
    /// </summary>
    public class BoardModel
    {
        public int Width { get; }
        public int Height { get; }

        public BoardCell[,] Cells { get; }

        private readonly System.Random _random;

        public BoardModel(int width, int height, int? seed = null)
        {
            Width = width;
            Height = height;
            Cells = new BoardCell[width, height];
            _random = seed.HasValue ? new System.Random(seed.Value) : new System.Random();
            GenerateBoard();
        }

        private void GenerateBoard()
        {
            for(int x = 0; x < Width; x++)
            {
                for(int y = 0; y < Height; y++)
                {
                    ElementType type;

                    do
                    {
                        type = GetRandomType();
                    }
                    while (WouldCreateMatch(x, y, type));

                    Cells[x, y] = new BoardCell(x, y, type);
                }
            }
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

        public ElementType GetRandomType()
        {
            // None не должен выпадать при генерации — начинаем с 1.
            var values = Enum.GetValues(typeof(ElementType));
            return (ElementType)values.GetValue(1 + _random.Next(values.Length - 1));
        }

        public void Swap(int x1, int y1, int x2, int y2)
        {
            var a = Cells[x1, y1];
            var b = Cells[x2, y2];

            ElementType tmp = a.Type;
            a.Type = b.Type;
            b.Type = tmp;
        }


        public void ResolveStep(List<BoardCell> matches, List<BoardCell> changed)
        {
            changed.Clear();

            // 1. Очистка совпадений
            for (int i = 0; i < matches.Count; i++)
            {
                matches[i].Type = ElementType.None;
                changed.Add(matches[i]);
            }

            // 2. Гравитация: сдвигаем вниз, собираем изменённые
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
                            Cells[x, y].Type = ElementType.None;
                            changed.Add(Cells[x, writeY]);
                            changed.Add(Cells[x, y]);
                        }
                        writeY++;
                    }
                }
            }

            // 3. Рефилл: заполняем пустые
            for (int x = 0; x < Width; x++)
            {
                for (int y = 0; y < Height; y++)
                {
                    if (Cells[x, y].Type == ElementType.None)
                    {
                        Cells[x, y].Type = GetRandomType();
                        changed.Add(Cells[x, y]);
                    }
                }
            }
        }

        // Возвращает список перемещений (для анимации падения).
        // Очистка совпадений уже должна быть сделана до вызова (ClearMatches).
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
                            Cells[x, writeY].Special = Cells[x, y].Special;

                            // ЛОГ: Если падает бомба, мы это увидим
                            if (Cells[x, y].Special == SpecialType.Bomb || Cells[x, y].Special == SpecialType.Rainbow)
                            {
                                Debug.Log($"ПАДЕНИЕ: Бонус {Cells[x, y].Special} упал с ({x},{y}) на ({x},{writeY})");
                            }

                            Cells[x, y].Type = ElementType.None;

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
                        Cells[x, y].Special = SpecialType.None;

                        newCells.Add(Cells[x, y]);
                    }
                }
            }
        }

        // Очищает клетки, помечая их как пустые (None).
        public void ClearMatches(List<BoardCell> matches)
        {
            for (int i = 0; i < matches.Count; i++)
            {
                matches[i].Type = ElementType.None;
            }
        }

        // Превращает клетку в бонус (бомбу или радугу)
        public void UpgradeToSpecial(BoardCell cell, SpecialType special)
        {
            cell.Special = special;
            // Мы не меняем Type, он остается того же цвета (для бомбы) или любого (для радуги)
        }
    }
}

