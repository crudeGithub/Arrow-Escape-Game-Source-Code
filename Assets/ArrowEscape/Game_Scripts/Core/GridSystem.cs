using System.Collections.Generic;
using UnityEngine;
using Core;

namespace Core
{
    public class GridSystem
    {
        private int width;
        private int height;
        private Dictionary<Vector2Int, ArrowUnit> gridOccupancy;

        public int Width => width;
        public int Height => height;

        public GridSystem(int width, int height)
        {
            this.width = width;
            this.height = height;
            gridOccupancy = new Dictionary<Vector2Int, ArrowUnit>();
        }

        public bool IsInsideGrid(Vector2Int pos)
        {
            return pos.x >= 0 && pos.x < width && pos.y >= 0 && pos.y < height;
        }

        public bool IsInsideExpandedBounds(Vector2Int pos, int margin)
        {
            return pos.x >= -margin && pos.x < width + margin && pos.y >= -margin && pos.y < height + margin;
        }

        public bool IsCellOccupied(Vector2Int pos)
        {
            return gridOccupancy.ContainsKey(pos);
        }

        public void SetOccupancy(Vector2Int pos, ArrowUnit arrow)
        {
            if (IsInsideGrid(pos))
            {
                gridOccupancy[pos] = arrow;
            }
        }

        public void ClearOccupancy(Vector2Int pos)
        {
            if (gridOccupancy.ContainsKey(pos))
            {
                gridOccupancy.Remove(pos);
            }
        }
        
        public ArrowUnit GetArrowAt(Vector2Int pos)
        {
            if (gridOccupancy.TryGetValue(pos, out ArrowUnit arrow))
            {
                return arrow;
            }
            return null;
        }
    }
}
