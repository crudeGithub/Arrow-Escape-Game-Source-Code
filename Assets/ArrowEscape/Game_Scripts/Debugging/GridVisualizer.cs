using UnityEngine;
using System.Collections.Generic;
using Core;

namespace Debugging
{
    public class GridVisualizer : MonoBehaviour
    {
        public static GridVisualizer Instance { get; private set; }

        [Header("Settings")]
        public Color gridLineColor = Color.black;
        public float lineWidth = 0.1f;

        private bool isVisualizerEnabled = false;
        private List<LineRenderer> linePool = new List<LineRenderer>();
        private GameObject poolContainer;
        private LineRenderer previewLine;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);

            poolContainer = new GameObject("VisualizerLinePool");
            poolContainer.transform.SetParent(transform);
            
            // Create the preview line
            GameObject previewGo = new GameObject("PreviewLine");
            previewGo.transform.SetParent(transform);
            previewLine = previewGo.AddComponent<LineRenderer>();
            previewLine.material = new Material(Shader.Find("Sprites/Default"));
            previewLine.startWidth = lineWidth;
            previewLine.endWidth = lineWidth;
            previewLine.positionCount = 2;
            previewLine.sortingOrder = -1;
            previewLine.gameObject.SetActive(false);
        }

        public void ToggleVisualizer()
        {
            SetVisualizerState(!isVisualizerEnabled);
        }

        public void SetVisualizerState(bool state)
        {
            isVisualizerEnabled = state;
            if (!isVisualizerEnabled)
            {
                HideAllLines();
            }
            Debug.Log($"Grid Visualizer State: {isVisualizerEnabled}");
        }

        public void ShowPreviewLine(ArrowUnit arrow)
        {
            if (arrow == null) return;
            UpdateLine(previewLine, arrow);
            // Make preview line slightly more visible/distinct if needed, or keep same style
            previewLine.startColor = new Color(gridLineColor.r, gridLineColor.g, gridLineColor.b, 0.5f);
            previewLine.endColor = new Color(gridLineColor.r, gridLineColor.g, gridLineColor.b, 0.5f);
        }

        public void HidePreviewLine()
        {
            if (previewLine != null)
            {
                previewLine.gameObject.SetActive(false);
            }
        }

        private void Update()
        {
            if (!isVisualizerEnabled) return;
            if (LevelManager.Instance == null || LevelManager.Instance.GridSystem == null) return;

            var arrows = LevelManager.Instance.ActiveArrows;
            int arrowCount = arrows.Count;

            // Ensure pool size
            while (linePool.Count < arrowCount)
            {
                CreateLineRenderer();
            }

            // Update lines
            for (int i = 0; i < linePool.Count; i++)
            {
                if (i < arrowCount)
                {
                    UpdateLine(linePool[i], arrows[i]);
                }
                else
                {
                    linePool[i].gameObject.SetActive(false);
                }
            }
        }

        private void CreateLineRenderer()
        {
            GameObject go = new GameObject("GridLine");
            go.transform.SetParent(poolContainer.transform);
            LineRenderer lr = go.AddComponent<LineRenderer>();
            lr.material = new Material(Shader.Find("Sprites/Default"));
            lr.startWidth = lineWidth;
            lr.endWidth = lineWidth;
            lr.positionCount = 2;
            lr.sortingOrder = -1; // Draw behind arrows
            linePool.Add(lr);
        }

        private void UpdateLine(LineRenderer lr, ArrowUnit arrow)
        {
            if (arrow == null || arrow.currentPositions.Count == 0)
            {
                lr.gameObject.SetActive(false);
                return;
            }

            lr.gameObject.SetActive(true);
            lr.startColor = gridLineColor;
            lr.endColor = gridLineColor;

            Vector2Int headPos = arrow.currentPositions[0];
            Vector2Int direction = GetArrowDirection(arrow);
            
            // Start from head
            Vector3 start = new Vector3(headPos.x, headPos.y, 0);
            
            GridSystem grid = LevelManager.Instance.GridSystem;
            
            // Calculate distance to edge
            int dist = 0;
            if (direction.x > 0) dist = grid.Width - 1 - headPos.x;
            else if (direction.x < 0) dist = headPos.x;
            else if (direction.y > 0) dist = grid.Height - 1 - headPos.y;
            else if (direction.y < 0) dist = headPos.y;

            // Draw line to the edge of the grid
            Vector3 end = start + (Vector3)(Vector2)direction * dist;
            
            // Extend significantly to go beyond the grid as requested
            end += (Vector3)(Vector2)direction * 10f; 

            lr.SetPosition(0, start);
            lr.SetPosition(1, end);
        }

        private Vector2Int GetArrowDirection(ArrowUnit arrow)
        {
            if (arrow.currentPositions.Count < 2) return Vector2Int.right;
            return arrow.currentPositions[0] - arrow.currentPositions[1];
        }

        private void HideAllLines()
        {
            foreach (var lr in linePool)
            {
                lr.gameObject.SetActive(false);
            }
        }
    }
}
