using UnityEngine;
using UnityEditor;
using Data;
using System.Collections.Generic;

namespace EditorTools
{
    public class LevelEditorWindow : EditorWindow
    {
        private LevelData currentLevelData;
        private Color currentArrowColor = Color.red;
        private List<Vector2Int> currentPaintingPath = new List<Vector2Int>();
        private bool isPainting = false;
        
        private const float CellSize = 40f;
        private const float Padding = 20f;

        [MenuItem("Window/Arrow Puzzle Level Editor")]
        public static void ShowWindow()
        {
            GetWindow<LevelEditorWindow>("Level Editor");
        }

        private float zoomScale = 1.0f;
        private Vector2 scrollPosition;

        private void OnGUI()
        {
            GUILayout.Label("Level Editor", EditorStyles.boldLabel);

            currentLevelData = (LevelData)EditorGUILayout.ObjectField("Level Data", currentLevelData, typeof(LevelData), false);

            if (currentLevelData == null)
            {
                EditorGUILayout.HelpBox("Please assign a LevelData asset to start editing.", MessageType.Info);
                if (GUILayout.Button("Create New Level Data"))
                {
                    CreateNewLevelData();
                }
                return;
            }

            DrawToolbar();
            HandleZoomInput();

            // Scroll View for the Grid
            scrollPosition = GUILayout.BeginScrollView(scrollPosition);
            
            float effectiveCellSize = CellSize * zoomScale;
            float gridWidth = currentLevelData.gridDimensions.x * effectiveCellSize;
            float gridHeight = currentLevelData.gridDimensions.y * effectiveCellSize;
            
            // Reserve space for the grid
            Rect canvasRect = GUILayoutUtility.GetRect(gridWidth + Padding * 2, gridHeight + Padding * 2);
            
            DrawGridArea(effectiveCellSize);
            HandleInput(effectiveCellSize);
            
            GUILayout.EndScrollView();
        }

        private void HandleZoomInput()
        {
            Event e = Event.current;
            // Zoom with Ctrl + Scroll
            if (e.type == EventType.ScrollWheel && e.control)
            {
                float zoomDelta = -e.delta.y * 0.05f;
                zoomScale = Mathf.Clamp(zoomScale + zoomDelta, 0.2f, 3.0f);
                e.Use();
                Repaint();
            }
        }

        private void CreateNewLevelData()
        {
            LevelData newData = ScriptableObject.CreateInstance<LevelData>();
            string path = EditorUtility.SaveFilePanelInProject("Save Level Data", "NewLevel", "asset", "Save Level Data");
            if (!string.IsNullOrEmpty(path))
            {
                AssetDatabase.CreateAsset(newData, path);
                AssetDatabase.SaveAssets();
                currentLevelData = newData;
            }
        }

        private void DrawToolbar()
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label("Grid Size:");
            currentLevelData.gridDimensions = EditorGUILayout.Vector2IntField("", currentLevelData.gridDimensions);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            int prevMoves = currentLevelData.maxMoves;
            currentLevelData.maxMoves = currentLevelData.arrows.Count + 3;
            if (prevMoves != currentLevelData.maxMoves)
            {
                EditorUtility.SetDirty(currentLevelData);
            }
            GUILayout.Label("Max Moves (Auto):");
            EditorGUILayout.LabelField(currentLevelData.maxMoves.ToString());
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Arrow Color:");
            currentArrowColor = EditorGUILayout.ColorField(currentArrowColor);
            GUILayout.EndHorizontal();
            
            GUILayout.BeginHorizontal();
            GUILayout.Label("Zoom:");
            zoomScale = EditorGUILayout.Slider(zoomScale, 0.2f, 3.0f);
            GUILayout.EndHorizontal();

            if (GUILayout.Button("Clear All Arrows"))
            {
                if (EditorUtility.DisplayDialog("Clear Level", "Are you sure you want to clear all arrows?", "Yes", "No"))
                {
                    currentLevelData.arrows.Clear();
                    isPainting = false;
                    currentPaintingPath.Clear();
                    EditorUtility.SetDirty(currentLevelData);
                }
            }
        }

        private void DrawGridArea(float cellSize)
        {
            // Draw relative to the scroll view content, starting at Padding
            Rect gridRect = new Rect(Padding, Padding, currentLevelData.gridDimensions.x * cellSize, currentLevelData.gridDimensions.y * cellSize);
            
            // Draw Background
            EditorGUI.DrawRect(gridRect, new Color(0.2f, 0.2f, 0.2f));

            // Draw Grid Lines
            for (int x = 0; x <= currentLevelData.gridDimensions.x; x++)
            {
                Rect lineRect = new Rect(gridRect.x + x * cellSize, gridRect.y, 1, gridRect.height);
                EditorGUI.DrawRect(lineRect, Color.gray);
            }
            for (int y = 0; y <= currentLevelData.gridDimensions.y; y++)
            {
                Rect lineRect = new Rect(gridRect.x, gridRect.y + y * cellSize, gridRect.width, 1);
                EditorGUI.DrawRect(lineRect, Color.gray);
            }

            // Draw Existing Arrows
            foreach (var arrow in currentLevelData.arrows)
            {
                DrawArrow(arrow, gridRect, cellSize);
            }

            // Draw Currently Painting Arrow
            if (isPainting && currentPaintingPath.Count > 0)
            {
                ArrowDefinition tempArrow = new ArrowDefinition { occupiedPositions = currentPaintingPath, arrowColor = currentArrowColor };
                DrawArrow(tempArrow, gridRect, cellSize);
            }
        }

        private void DrawArrow(ArrowDefinition arrow, Rect gridRect, float cellSize)
        {
            if (arrow.occupiedPositions == null || arrow.occupiedPositions.Count == 0) return;

            Handles.color = arrow.arrowColor;
            for (int i = 0; i < arrow.occupiedPositions.Count; i++)
            {
                Vector2Int pos = arrow.occupiedPositions[i];
                
                float guiX = gridRect.x + pos.x * cellSize;
                float guiY = gridRect.y + (currentLevelData.gridDimensions.y - 1 - pos.y) * cellSize;

                Rect cellRect = new Rect(guiX + 2, guiY + 2, cellSize - 4, cellSize - 4);
                EditorGUI.DrawRect(cellRect, arrow.arrowColor);

                if (i > 0)
                {
                    Vector2Int prevPos = arrow.occupiedPositions[i - 1];
                    float pGuiX = gridRect.x + prevPos.x * cellSize;
                    float pGuiY = gridRect.y + (currentLevelData.gridDimensions.y - 1 - prevPos.y) * cellSize;
                    
                    float minX = Mathf.Min(guiX, pGuiX) + 2;
                    float maxX = Mathf.Max(guiX + cellSize, pGuiX + cellSize) - 2;
                    float minY = Mathf.Min(guiY, pGuiY) + 2;
                    float maxY = Mathf.Max(guiY + cellSize, pGuiY + cellSize) - 2;
                    
                    EditorGUI.DrawRect(new Rect(minX, minY, maxX - minX, maxY - minY), arrow.arrowColor);
                }

            }

            // Draw Head Indicator After Body is Fully Connected
            {
                Vector2Int headPos = arrow.occupiedPositions[0];
                float guiX = gridRect.x + headPos.x * cellSize;
                float guiY = gridRect.y + (currentLevelData.gridDimensions.y - 1 - headPos.y) * cellSize;

                Vector2Int dir = Vector2Int.up; // default up
                if (arrow.occupiedPositions.Count > 1)
                {
                    dir = arrow.occupiedPositions[0] - arrow.occupiedPositions[1];
                }

                Handles.color = Color.white;
                Vector3 center = new Vector3(guiX + cellSize / 2f, guiY + cellSize / 2f, 0);
                float size = cellSize * 0.3f;
                Vector3 p1, p2, p3;

                if (dir.x > 0) // Right
                {
                    p1 = center + new Vector3(size, 0, 0);
                    p2 = center + new Vector3(-size, size, 0);
                    p3 = center + new Vector3(-size, -size, 0);
                }
                else if (dir.x < 0) // Left
                {
                    p1 = center + new Vector3(-size, 0, 0);
                    p2 = center + new Vector3(size, size, 0);
                    p3 = center + new Vector3(size, -size, 0);
                }
                else if (dir.y < 0) // Down
                {
                    p1 = center + new Vector3(0, size, 0);
                    p2 = center + new Vector3(-size, -size, 0);
                    p3 = center + new Vector3(size, -size, 0);
                }
                else // Up
                {
                    p1 = center + new Vector3(0, -size, 0);
                    p2 = center + new Vector3(-size, size, 0);
                    p3 = center + new Vector3(size, size, 0);
                }

                Handles.DrawAAConvexPolygon(new Vector3[] { p1, p2, p3 });
            }
        }

        private void HandleInput(float cellSize)
        {
            Event e = Event.current;
            Rect gridRect = new Rect(Padding, Padding, currentLevelData.gridDimensions.x * cellSize, currentLevelData.gridDimensions.y * cellSize);

            // Catch MouseUp globally so releasing mouse outside grid still saves the arrow!
            if (e.rawType == EventType.MouseUp && e.button == 0 && isPainting)
            {
                isPainting = false;
                if (currentPaintingPath.Count > 1) // Enforce minimum arrow length of 2 cells
                {
                    currentLevelData.arrows.Add(new ArrowDefinition
                    {
                        occupiedPositions = new List<Vector2Int>(currentPaintingPath),
                        arrowColor = currentArrowColor
                    });
                    EditorUtility.SetDirty(currentLevelData);
                }
                currentPaintingPath.Clear();
                Repaint();
                e.Use();
                return;
            }

            if (!gridRect.Contains(e.mousePosition)) return;

            // Calculate Grid Coordinate
            int x = Mathf.FloorToInt((e.mousePosition.x - gridRect.x) / cellSize);
            int y = Mathf.FloorToInt((e.mousePosition.y - gridRect.y) / cellSize);
            // Convert back to Bottom-Left origin
            int gridY = currentLevelData.gridDimensions.y - 1 - y;
            Vector2Int gridPos = new Vector2Int(x, gridY);

            if (e.type == EventType.MouseDown && e.button == 0)
            {
                if (IsPositionOccupied(gridPos)) return;
                // Start Painting
                isPainting = true;
                currentPaintingPath.Clear();
                currentPaintingPath.Add(gridPos);
                e.Use();
            }
            else if (e.type == EventType.MouseDrag && isPainting)
            {
                // Continue Painting
                if (currentPaintingPath.Count > 0)
                {
                    Vector2Int lastPos = currentPaintingPath[currentPaintingPath.Count - 1];
                    if (gridPos != lastPos)
                    {
                        // Check adjacency
                        if (Vector2Int.Distance(gridPos, lastPos) == 1) // Only adjacent
                        {
                            // Check self-intersection and block overlapping other arrows
                            if (!currentPaintingPath.Contains(gridPos) && !IsPositionOccupied(gridPos))
                            {
                                currentPaintingPath.Add(gridPos);
                                Repaint();
                            }
                        }
                    }
                }
                e.Use();
            }
            else if (e.type == EventType.MouseDown && e.button == 1)
            {
                // Right Click: Delete Arrow
                RemoveArrowAt(gridPos);
                e.Use();
            }
        }

        private bool IsPositionOccupied(Vector2Int pos)
        {
            foreach (var arr in currentLevelData.arrows)
            {
                if (arr.occupiedPositions != null && arr.occupiedPositions.Contains(pos))
                    return true;
            }
            return false;
        }

        private void RemoveArrowAt(Vector2Int pos)
        {
            for (int i = currentLevelData.arrows.Count - 1; i >= 0; i--)
            {
                if (currentLevelData.arrows[i].occupiedPositions.Contains(pos))
                {
                    currentLevelData.arrows.RemoveAt(i);
                    EditorUtility.SetDirty(currentLevelData);
                    Repaint();
                    return;
                }
            }
        }
    }
}
