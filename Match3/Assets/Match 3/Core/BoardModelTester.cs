using Match3.core;
using UnityEngine;

namespace Match3.Core
{
    public class BoardModelTester : MonoBehaviour
    {
        [SerializeField] private int width = 8;
        [SerializeField] private int height = 8;

        private void Start()
        {
            var board = new BoardModel(width, height);
            Debug.Log($"Board {width}x{height} generated. Sample: [0,0]={board.Cells[0, 0].Type}, [7,7]={board.Cells[7, 7].Type}");
        }
    }
}
