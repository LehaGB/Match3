using UnityEngine;

namespace Match3.Core
{
    [CreateAssetMenu(fileName = "LevelData", menuName = "Match3/Level Data")]
    public class LevelData : ScriptableObject
    {
        [Header("Board Settings")]
        public int Width = 8;
        public int Height = 8;

        [Header("Goal Settings")]
        public int StartingsMoves = 20;
        public int TargetScore = 1000;
    }
}

