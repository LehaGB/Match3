
using UnityEngine;

namespace Match3.Core
{
    /// <summary>
    /// Описывает визуальное перемещение: клетка (X,Y) перемещается в (ToY) по вертикали.
    /// X не меняется(гравитация только вертикальная).
    /// </summary>
    public struct CellMove 
    {
        public int X;
        public int FromY;
        public int ToY;
    }
}

