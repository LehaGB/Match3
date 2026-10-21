using UnityEngine;
using System.Collections.Generic;
using System;


namespace Match3.core
{
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
            if (x >= 2 && Cells[x - 1, y]?.Type == type && Cells[x - 2, y]?.Type == type) return true;

            if (y >= 2 && Cells[x, y - 1]?.Type == type && Cells[x, y - 2]?.Type == type) return true;

            return false;
        }

        private ElementType GetRandomType()
        {

            var values = Enum.GetValues(typeof(ElementType));
            return (ElementType)values.GetValue(_random.Next(values.Length));
        }
    }
}

