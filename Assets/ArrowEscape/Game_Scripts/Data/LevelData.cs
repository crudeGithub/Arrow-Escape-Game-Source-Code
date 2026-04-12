using System.Collections.Generic;
using UnityEngine;

namespace Data
{
    [CreateAssetMenu(fileName = "NewLevel", menuName = "ArrowPuzzle/LevelData")]
    public class LevelData : ScriptableObject
    {
        public Vector2Int gridDimensions = new Vector2Int(5, 5);
        public int maxMoves = 10; // Maximum moves allowed to complete this level
        public List<ArrowDefinition> arrows = new List<ArrowDefinition>();
    }

    [System.Serializable]
    public struct ArrowDefinition
    {
        public List<Vector2Int> occupiedPositions; // Index 0 is Head
        public Color arrowColor;
    }
}
