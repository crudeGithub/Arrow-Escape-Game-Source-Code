using System.Collections.Generic;
using UnityEngine;
using Debugging;

namespace Core
{
    public class ArrowUnit : MonoBehaviour
    {
        // Event to notify when this arrow is moved (bool indicating if it was a valid move)
        public System.Action<bool> OnArrowMoved;
        
        [Header("Theme Settings")]
        [Tooltip("Assign an Arrow Theme to override visuals.")]
        public ArrowTheme currentTheme;

        [Header("Legacy Visual Settings (Used if no Theme assigned)")]
        [Tooltip("Optional: Assign your own arrow sprite. If null, a procedural arrow will be generated.")]
        public Sprite customArrowSprite;
        
        [Tooltip("Scale multiplier for custom arrow sprite (only applies if custom sprite is assigned)")]
        public float customArrowScale = 0.5f;
        
        [Tooltip("Rotation offset in degrees for custom arrow sprite (only applies if custom sprite is assigned)")]
        public float customArrowRotationOffset = 0f;
        
        [Tooltip("Use centered pivot for custom sprite (recommended: true for most sprites)")]
        public bool useCenteredPivot = true;
        
        public List<Vector2Int> currentPositions = new List<Vector2Int>(); // 0 is head
        private GridSystem gridSystem;
        private bool isMoving = false;
        public bool IsMoving => isMoving; // Public property to check if arrow is moving
        private Color arrowColor;
        private GameObject headVisual;
        private GameObject headOutlineVisual; // For sprite outline
        private List<GameObject> bodyVisuals = new List<GameObject>(); // For sprite-based body
        private List<LineRenderer> lineRenderers = new List<LineRenderer>(); // For multi-layer lines
        private Coroutine pulseCoroutine; // Track pulse coroutine separately

        // Input handling variables
        private bool isPressed = false;
        private float pressStartTime;
        private bool longPressTriggered = false;
        private bool isMouseOver = false;
        private const float LongPressDuration = 0.3f; // Reduced to 0.3s for better responsiveness

        public void Initialize(List<Vector2Int> startPositions, Color color, GridSystem grid)
        {
            currentPositions = new List<Vector2Int>(startPositions);
            arrowColor = color;
            gridSystem = grid;
            
            CreateHeadVisual();
            UpdateVisuals();
            
            // Register initial occupancy
            foreach (var pos in currentPositions)
            {
                gridSystem.SetOccupancy(pos, this);
            }
        }

        private void CreateHeadVisual()
        {
            if (headVisual != null) Destroy(headVisual);
            if (headOutlineVisual != null) Destroy(headOutlineVisual);
            
            // Check for Prefab in Theme
            if (currentTheme != null && currentTheme.headPrefab != null)
            {
                headVisual = Instantiate(currentTheme.headPrefab, transform);
                headVisual.name = "HeadVisual_Prefab";
                
                // Apply Scale
                headVisual.transform.localScale = Vector3.one * currentTheme.headScale;
                
                // If the prefab has a SpriteRenderer and we want to override color
                if (currentTheme.overrideColor)
                {
                    var srs = headVisual.GetComponentsInChildren<SpriteRenderer>();
                    foreach (var s in srs) s.color = currentTheme.themeColor;
                }
            }
            else
            {
                // Standard Sprite / Procedural Logic
                headVisual = new GameObject("HeadVisual");
                headVisual.transform.SetParent(transform);
                
                SpriteRenderer sr = headVisual.AddComponent<SpriteRenderer>();
                
                // Determine settings based on Theme or Legacy
                Sprite spriteToUse = null;
                float scaleToUse = 1.0f;
                float rotationOffset = 0f;
                bool useCentered = true;
                Color colorToUse = arrowColor;
                int sortingOrder = 2;

                if (currentTheme != null)
                {
                    if (currentTheme.headSprite != null)
                    {
                        spriteToUse = currentTheme.headSprite;
                        scaleToUse = currentTheme.headScale;
                        rotationOffset = currentTheme.headRotationOffset;
                        sortingOrder = currentTheme.headSortingOrder;
                        useCentered = true; 
                    }
                    
                    if (currentTheme.overrideColor)
                    {
                        colorToUse = currentTheme.themeColor;
                    }
                }
                else
                {
                    // Legacy fallback
                    spriteToUse = customArrowSprite;
                    scaleToUse = customArrowScale;
                    rotationOffset = customArrowRotationOffset;
                    useCentered = useCenteredPivot;
                }

                // Apply Sprite
                if (spriteToUse != null)
                {
                    if (useCentered)
                    {
                        Texture2D tex = spriteToUse.texture;
                        Rect rect = spriteToUse.rect;
                        Vector2 centeredPivot = new Vector2(0.5f, 0.5f);
                        sr.sprite = Sprite.Create(tex, rect, centeredPivot, spriteToUse.pixelsPerUnit);
                    }
                    else
                    {
                        sr.sprite = spriteToUse;
                    }
                    
                    headVisual.transform.localScale = Vector3.one * scaleToUse;
                }
                else
                {
                    // Procedural Arrow
                    sr.sprite = CreateArrowSprite();
                    headVisual.transform.localScale = Vector3.one * 1.2f;
                }

                sr.color = colorToUse;
                sr.sortingOrder = sortingOrder;

                // Create Outline if requested (Only for Sprite mode)
                if (currentTheme != null && currentTheme.showHeadOutline && spriteToUse != null)
                {
                    headOutlineVisual = new GameObject("HeadOutline");
                    headOutlineVisual.transform.SetParent(headVisual.transform); // Child of head visual to follow transform
                    headOutlineVisual.transform.localPosition = Vector3.zero;
                    headOutlineVisual.transform.localRotation = Quaternion.identity;
                    // Scale needs to be relative. If head is scaled, outline child scale multiplies.
                    // We want outline to be slightly larger.
                    headOutlineVisual.transform.localScale = Vector3.one * currentTheme.headOutlineScale;
                    
                    SpriteRenderer outlineSr = headOutlineVisual.AddComponent<SpriteRenderer>();
                    outlineSr.sprite = sr.sprite;
                    outlineSr.color = currentTheme.headOutlineColor;
                    outlineSr.sortingOrder = sortingOrder - 1; // Behind head
                }
            }
        }

        private Sprite CreateArrowSprite()
        {
            int size = 64;
            Texture2D texture = new Texture2D(size, size);
            texture.filterMode = FilterMode.Bilinear; // Smoother edges
            Color[] colors = new Color[size * size];
            for (int i = 0; i < colors.Length; i++) colors[i] = Color.clear;
            
            // Dart Shape Logic - Optimized for blending
            // Tip at x=56, y=32
            // Base Wings at x=8, y=50 and y=14 (Width 36px)
            // Back Notch at x=16, y=32
            
            Vector2 tip = new Vector2(56, 32);
            Vector2 baseTop = new Vector2(8, 50);
            Vector2 baseBot = new Vector2(8, 14);
            Vector2 notch = new Vector2(16, 32);

            for (int x = 0; x < size; x++)
            {
                for (int y = 0; y < size; y++)
                {
                    // 1. Check if inside outer triangle (BaseTop -> Tip -> BaseBot)
                    // Upper edge: (8,50) to (56,32)
                    // Lower edge: (8,14) to (56,32)
                    
                    // Slope = (32-50)/(56-8) = -18/48 = -0.375
                    // Upper: y <= -0.375(x-56) + 32 => y <= -0.375x + 53
                    // Lower: y >=  0.375(x-56) + 32 => y >=  0.375x + 11
                    
                    float slope = 18f / 48f;
                    
                    // Calculate distance from edges for anti-aliasing
                    float upperEdge = -slope * x + 53f;
                    float lowerEdge = slope * x + 11f;
                    
                    bool insideOuter = (x >= 8) && (x <= 56) &&
                                       (y <= upperEdge) &&
                                       (y >= lowerEdge);

                    if (insideOuter)
                    {
                        // 2. Check if outside inner notch triangle (BaseTop -> Notch -> BaseBot)
                        // Upper notch: (8,50) to (16,32)
                        // Lower notch: (8,14) to (16,32)
                        
                        // Slope = (32-50)/(16-8) = -18/8 = -2.25
                        // Upper: y <= -2.25(x-16) + 32 => y <= -2.25x + 68
                        // Lower: y >=  2.25(x-16) + 32 => y >=  2.25x - 4
                        
                        float notchSlope = 18f / 8f;
                        bool insideNotch = (x < 16) &&
                                           (y < -notchSlope * x + 68f) &&
                                           (y >  notchSlope * x - 4f);
                        
                        if (!insideNotch)
                        {
                            // Anti-aliasing: fade edges based on distance
                            float alpha = 1f;
                            
                            // Distance from upper edge
                            float distUpper = upperEdge - y;
                            if (distUpper < 1.5f) alpha = Mathf.Min(alpha, distUpper / 1.5f);
                            
                            // Distance from lower edge
                            float distLower = y - lowerEdge;
                            if (distLower < 1.5f) alpha = Mathf.Min(alpha, distLower / 1.5f);
                            
                            // Distance from left edge
                            float distLeft = x - 8f;
                            if (distLeft < 1.5f && !insideNotch) alpha = Mathf.Min(alpha, distLeft / 1.5f);
                            
                            colors[y * size + x] = new Color(1, 1, 1, alpha);
                        }
                    }
                }
            }
            
            texture.SetPixels(colors);
            texture.Apply();
            
            // Pivot at (8, 32) which is the base x=8, center y=32.
            // This aligns the base of the arrow with the node position.
            return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(8f/64f, 0.5f));
        }

        private void OnMouseDown()
        {
            if (LevelManager.Instance != null && !LevelManager.Instance.IsGameActive) return;
            
            isPressed = true;
            pressStartTime = Time.time;
            longPressTriggered = false;
        }

        private void OnMouseEnter()
        {
            isMouseOver = true;
        }

        private void OnMouseExit()
        {
            isMouseOver = false;
        }

        private void Update()
        {
            if (isPressed)
            {
                // Check for Long Press
                if (!longPressTriggered && Time.time - pressStartTime > LongPressDuration)
                {
                    longPressTriggered = true;
                    GridVisualizer.Instance?.ShowPreviewLine(this);
                }

                // Check for Release
                if (Input.GetMouseButtonUp(0))
                {
                    isPressed = false;
                    
                    // Hide visual if it was shown
                    if (longPressTriggered)
                    {
                        GridVisualizer.Instance?.HidePreviewLine();
                    }
                    
                    // If it was NOT a long press, AND we are still over the arrow, Trigger Move
                    if (!longPressTriggered && isMouseOver)
                    {
                        AttemptMove();
                    }
                }
            }

            // Animate Line Textures
            if (currentTheme != null && lineRenderers.Count > 0)
            {
                for (int i = 0; i < lineRenderers.Count; i++)
                {
                    if (i < currentTheme.lineLayers.Count)
                    {
                        float speed = currentTheme.lineLayers[i].scrollSpeed;
                        if (Mathf.Abs(speed) > 0.001f)
                        {
                            LineRenderer lr = lineRenderers[i];
                            if (lr != null && lr.material != null)
                            {
                                Vector2 offset = lr.material.mainTextureOffset;
                                offset.x -= speed * Time.deltaTime; // Subtract to move forward along the line usually
                                lr.material.mainTextureOffset = offset;
                            }
                        }
                    }
                }
            }
        }

        private void AttemptMove()
        {
            // Check if game is active
            if (LevelManager.Instance != null && !LevelManager.Instance.IsGameActive)
            {
                return;
            }

            // Check if moves are available
            if (LevelManager.Instance != null && !LevelManager.Instance.CanMakeMove())
            {
                Debug.Log("No moves remaining! Cannot move arrow.");
                AudioManager.Instance?.PlayBlockedSound();
                VFXManager.Instance?.PlayBlockedAnimation(this);
                return;
            }
            
            if (!isMoving)
            {
                bool isClear = CheckPathClear();
                // Notify that a move is being made (counts even if blocked)
                OnArrowMoved?.Invoke(isClear);
                
                if (isClear)
                {
                    AudioManager.Instance?.PlayMoveSound();
                    
                    // IMMEDIATELY clear all cells occupied by this arrow
                    // This allows other arrows to start moving without waiting
                    ClearAllOccupancy();
                    
                    // Now start the movement animation
                    StartCoroutine(MoveRoutine());
                }
                else
                {
                    Debug.Log("Path is blocked!");
                    AudioManager.Instance?.PlayBlockedSound();
                    VFXManager.Instance?.PlayBlockedAnimation(this);
                }
            }
        }

        private void ClearAllOccupancy()
        {
            // Clear all cells currently occupied by this arrow
            foreach (var pos in currentPositions)
            {
                if (gridSystem.IsInsideGrid(pos))
                {
                    if (gridSystem.GetArrowAt(pos) == this)
                    {
                        gridSystem.ClearOccupancy(pos);
                    }
                }
            }
        }

        public bool CanMove()
        {
            return CheckPathClear();
        }

        public void Highlight(bool enable)
        {
            if (enable)
            {
                // Stop any existing pulse first
                if (pulseCoroutine != null)
                {
                    StopCoroutine(pulseCoroutine);
                }
                pulseCoroutine = StartCoroutine(PulseRoutine());
            }
            else
            {
                // Only stop the pulse coroutine, not all coroutines!
                if (pulseCoroutine != null)
                {
                    StopCoroutine(pulseCoroutine);
                    pulseCoroutine = null;
                }
                
                // Reset scale to default
                transform.localScale = Vector3.one; 
                if (headVisual != null)
                {
                     // Reset head visual scale based on theme or legacy
                     float targetScale = 1.2f;
                     if (currentTheme != null) targetScale = currentTheme.headScale;
                     else if (customArrowSprite != null) targetScale = customArrowScale;
                     
                     headVisual.transform.localScale = Vector3.one * targetScale;
                }
            }
        }

        private System.Collections.IEnumerator PulseRoutine()
        {
            float duration = 0.5f;
            Vector3 originalScale = headVisual != null ? headVisual.transform.localScale : Vector3.one;
            Vector3 targetScale = originalScale * 1.3f;
            
            while (true)
            {
                float t = Mathf.PingPong(Time.time, duration) / duration;
                if (headVisual != null)
                {
                    headVisual.transform.localScale = Vector3.Lerp(originalScale, targetScale, t);
                }
                yield return null;
            }
        }

        private bool CheckPathClear()
        {
            if (currentPositions.Count == 0) return false;

            Vector2Int headPos = currentPositions[0];
            Vector2Int direction = GetDirection();
            Vector2Int checkPos = headPos + direction;

            // Loop until we hit the grid boundary
            while (gridSystem.IsInsideGrid(checkPos))
            {
                if (gridSystem.IsCellOccupied(checkPos))
                {
                    return false;
                }
                checkPos += direction;
            }

            // If we reached here, we hit the boundary without hitting an obstacle
            return true;
        }

        private System.Collections.IEnumerator MoveRoutine()
        {
            isMoving = true;
            while (true)
            {
                // Move one step
                bool moved = TryMove();
                if (!moved) break;
                
                // Check if destroyed (Exit condition)
                if (this == null || gameObject == null) yield break;

                yield return new WaitForSeconds(0.05f); // Speed of movement
            }
            isMoving = false;
        }

        public bool TryMove()
        {
            if (currentPositions.Count == 0) return false;

            Vector2Int headPos = currentPositions[0];
            Vector2Int direction = GetDirection();
            Vector2Int nextPos = headPos + direction;

            // 1. Check if outside grid -> Exit logic
            if (!gridSystem.IsInsideGrid(nextPos))
            {
                MoveStep(nextPos, true);
                return true;
            }

            // 2. Since we already cleared all occupancy when clicked,
            // we don't need to check for collisions during movement
            // Just move the visual representation
            MoveStep(nextPos, false);
            return true;
        }

        private Vector2Int GetDirection()
        {
            if (currentPositions.Count < 2) return Vector2Int.right; // Default
            return currentPositions[0] - currentPositions[1];
        }

        private void MoveStep(Vector2Int newHeadPos, bool isExiting)
        {
            // Remove tail from grid (no need to clear occupancy, already cleared)
            Vector2Int tailPos = currentPositions[currentPositions.Count - 1];

            // Update list
            currentPositions.Insert(0, newHeadPos);
            currentPositions.RemoveAt(currentPositions.Count - 1);

            // Don't register occupancy during movement - cells are already cleared
            
            UpdateVisuals();

            // Check for destruction (when fully off-screen)
            if (IsFullyOffScreen())
            {
                Debug.Log($"Arrow is fully off-screen, destroying...");
                AudioManager.Instance?.PlayExitSound();
                Destroy(gameObject);
            }
        }

        private bool IsFullyOffScreen()
        {
            // Check if ALL segments are outside the expanded bounds
            int margin = 2; // Distance to move before destroying (reduced from 4 for faster cleanup)
            
            foreach (var pos in currentPositions)
            {
                // If ANY part is still within the "visible" area (Grid + Margin), don't destroy
                if (gridSystem.IsInsideExpandedBounds(pos, margin))
                {
                    return false;
                }
            }
            return true;
        }

        private void UpdateVisuals()
        {
            // Determine visual type
            ArrowVisualType visualType = ArrowVisualType.LineRenderer;
            if (currentTheme != null) visualType = currentTheme.visualType;

            if (visualType == ArrowVisualType.LineRenderer)
            {
                UpdateLineVisuals();
                // Clear any sprite bodies if they exist
                ClearBodySprites();
            }
            else if (visualType == ArrowVisualType.SpriteBody)
            {
                UpdateSpriteVisuals();
                // Clear line renderers if they exist
                ClearLineRenderers();
            }
            
            UpdateColliders();
            UpdateHeadRotation();
        }

        private void UpdateLineVisuals()
        {
            // Get Layers from Theme or Default
            List<LineLayer> layers = new List<LineLayer>();
            int smoothness = 5;
            
            if (currentTheme != null)
            {
                if (currentTheme.lineLayers.Count > 0) layers.AddRange(currentTheme.lineLayers);
                smoothness = currentTheme.lineSmoothness;
            }
            
            if (layers.Count == 0)
            {
                // Default Layer
                LineLayer defaultLayer = new LineLayer();
                defaultLayer.width = 0.4f;
                defaultLayer.endWidth = -1f; // Use start width
                defaultLayer.color = arrowColor;
                defaultLayer.material = new Material(Shader.Find("Sprites/Default"));
                defaultLayer.textureMode = LineTextureMode.Stretch;
                defaultLayer.sortingOrderOffset = 0;
                layers.Add(defaultLayer);
            }

            // Ensure we have enough LineRenderers
            while (lineRenderers.Count < layers.Count)
            {
                GameObject lineObj = new GameObject($"LineLayer_{lineRenderers.Count}");
                lineObj.transform.SetParent(transform);
                LineRenderer lr = lineObj.AddComponent<LineRenderer>();
                lineRenderers.Add(lr);
            }
            
            // Remove excess
            while (lineRenderers.Count > layers.Count)
            {
                Destroy(lineRenderers[lineRenderers.Count - 1].gameObject);
                lineRenderers.RemoveAt(lineRenderers.Count - 1);
            }

            // Calculate max width to determine optimal corner radius
            float maxWidth = 0f;
            foreach (var layer in layers)
            {
                float w = Mathf.Max(layer.width, layer.endWidth >= 0 ? layer.endWidth : layer.width);
                if (w > maxWidth) maxWidth = w;
            }

            // Adjust corner radius based on width to prevent inner overlap
            // Base radius 0.35f. If width is large, we need larger radius.
            // We clamp it to 0.5f to ensure it doesn't exceed the segment half-length.
            // Using 0.6f factor provides a safety margin so InnerRadius > 0.
            float cornerRadius = Mathf.Clamp(Mathf.Max(0.35f, maxWidth * 0.6f), 0.35f, 0.5f);

            // Generate smooth path with interpolated corners
            List<Vector3> smoothPath = GenerateSmoothPath(currentPositions, smoothness, cornerRadius);

            // Update each layer
            for (int i = 0; i < layers.Count; i++)
            {
                LineRenderer lr = lineRenderers[i];
                LineLayer layer = layers[i];
                
                // Apply start and end offsets to create a trimmed path for this layer
                List<Vector3> layerPath = ApplyOffsets(smoothPath, layer.startOffset, layer.endOffset);
                
                lr.positionCount = layerPath.Count;
                lr.startWidth = layer.width;
                // If endWidth is negative, use startWidth (constant width)
                lr.endWidth = layer.endWidth >= 0 ? layer.endWidth : layer.width;
                
                // Use alignment mode to ensure corners are properly rounded
                lr.alignment = LineAlignment.TransformZ;
                
                // Scale corner and cap vertices based on line width for smooth bending at any width
                float widthFactor = Mathf.Max(layer.width, layer.endWidth >= 0 ? layer.endWidth : layer.width);
                int scaledSmoothness = Mathf.Max(smoothness, Mathf.CeilToInt(smoothness * widthFactor * 2f));
                
                // IMPORTANT: Set numCornerVertices to 0 because we are using a manually smoothed path (Bezier).
                // Adding LineRenderer corner vertices on top of a smoothed path causes artifacts (glitch lines).
                lr.numCornerVertices = 0;
                lr.numCapVertices = scaledSmoothness;
                
                if (layer.material != null) lr.material = layer.material;
                else lr.material = new Material(Shader.Find("Sprites/Default"));
                
                lr.textureMode = layer.textureMode;
                
                // Apply color - multiply with arrow color if useArrowColor is true
                Color finalColor = layer.color;
                if (layer.useArrowColor)
                {
                    finalColor = new Color(
                        layer.color.r * arrowColor.r,
                        layer.color.g * arrowColor.g,
                        layer.color.b * arrowColor.b,
                        layer.color.a * arrowColor.a
                    );
                }
                
                lr.startColor = finalColor;
                lr.endColor = finalColor;
                lr.sortingOrder = layer.sortingOrderOffset;

                // Set all positions from layer-specific path
                lr.SetPositions(layerPath.ToArray());
            }
            
            // Disable main LineRenderer if it exists (we use children now)
            LineRenderer mainLr = GetComponent<LineRenderer>();
            if (mainLr != null) mainLr.enabled = false;
        }

        private List<Vector3> GenerateSmoothPath(List<Vector2Int> positions, int cornerSmoothness, float cornerRadius)
        {
            List<Vector3> smoothPath = new List<Vector3>();
            
            if (positions.Count == 0) return smoothPath;
            if (positions.Count == 1)
            {
                smoothPath.Add(new Vector3(positions[0].x, positions[0].y, 0));
                return smoothPath;
            }

            // Add first point
            smoothPath.Add(new Vector3(positions[0].x, positions[0].y, 0));

            // Process middle segments with corner rounding
            for (int i = 1; i < positions.Count - 1; i++)
            {
                Vector2Int prev = positions[i - 1];
                Vector2Int current = positions[i];
                Vector2Int next = positions[i + 1];

                Vector2Int dirIn = current - prev;
                Vector2Int dirOut = next - current;

                // Check if this is a corner (direction change)
                if (dirIn != dirOut)
                {
                    // Calculate the points before and after the corner using the provided radius
                    Vector3 cornerPoint = new Vector3(current.x, current.y, 0);
                    Vector3 beforeCorner = cornerPoint - new Vector3(dirIn.x, dirIn.y, 0).normalized * cornerRadius;
                    Vector3 afterCorner = cornerPoint + new Vector3(dirOut.x, dirOut.y, 0).normalized * cornerRadius;

                    // Add the point before corner
                    smoothPath.Add(beforeCorner);

                    // Add interpolated points around the corner for smooth curve
                    int steps = Mathf.Max(2, cornerSmoothness);
                    for (int s = 1; s <= steps; s++)
                    {
                        float t = (float)s / (steps + 1);
                        // Use quadratic bezier curve with corner as control point
                        Vector3 p = QuadraticBezier(beforeCorner, cornerPoint, afterCorner, t);
                        smoothPath.Add(p);
                    }

                    // Add the point after corner
                    smoothPath.Add(afterCorner);
                }
                else
                {
                    // Straight line, just add the point
                    smoothPath.Add(new Vector3(current.x, current.y, 0));
                }
            }

            // Add last point
            smoothPath.Add(new Vector3(positions[positions.Count - 1].x, positions[positions.Count - 1].y, 0));

            return smoothPath;
        }

        private Vector3 QuadraticBezier(Vector3 p0, Vector3 p1, Vector3 p2, float t)
        {
            // Quadratic Bezier formula: B(t) = (1-t)²P0 + 2(1-t)tP1 + t²P2
            float u = 1 - t;
            float tt = t * t;
            float uu = u * u;
            
            Vector3 p = uu * p0;
            p += 2 * u * t * p1;
            p += tt * p2;
            
            return p;
        }

        private List<Vector3> ApplyOffsets(List<Vector3> path, float startOffset, float endOffset)
        {
            if (path.Count < 2) return new List<Vector3>(path);
            if (startOffset <= 0 && endOffset <= 0) return new List<Vector3>(path);

            // Calculate total path length
            float totalLength = 0f;
            List<float> segmentLengths = new List<float>();
            for (int i = 0; i < path.Count - 1; i++)
            {
                float segmentLength = Vector3.Distance(path[i], path[i + 1]);
                segmentLengths.Add(segmentLength);
                totalLength += segmentLength;
            }

            // Calculate actual start and end distances
            float startDist = Mathf.Clamp(startOffset, 0, totalLength * 0.9f);
            float endDist = Mathf.Clamp(endOffset, 0, totalLength * 0.9f);
            
            // If offsets would eliminate the entire path, return a minimal path
            if (startDist + endDist >= totalLength)
            {
                int midIndex = path.Count / 2;
                return new List<Vector3> { path[midIndex] };
            }

            List<Vector3> trimmedPath = new List<Vector3>();

            // Find start point
            float accumulatedDist = 0f;
            bool startFound = false;
            for (int i = 0; i < path.Count - 1; i++)
            {
                float segmentLength = segmentLengths[i];
                
                if (!startFound && accumulatedDist + segmentLength >= startDist)
                {
                    // Interpolate to find exact start point
                    float t = (startDist - accumulatedDist) / segmentLength;
                    Vector3 startPoint = Vector3.Lerp(path[i], path[i + 1], t);
                    trimmedPath.Add(startPoint);
                    startFound = true;
                    
                    // Add remaining points until we reach end offset
                    for (int j = i + 1; j < path.Count; j++)
                    {
                        float distToEnd = totalLength - accumulatedDist - segmentLength;
                        
                        // Check if we need to stop before this point
                        if (j < path.Count - 1)
                        {
                            float nextSegmentLength = segmentLengths[j];
                            float distAtNextPoint = totalLength - distToEnd + nextSegmentLength;
                            
                            if (totalLength - distAtNextPoint < endDist)
                            {
                                // Interpolate to find exact end point
                                float remainingDist = totalLength - endDist - distToEnd;
                                float tEnd = remainingDist / nextSegmentLength;
                                Vector3 endPoint = Vector3.Lerp(path[j], path[j + 1], tEnd);
                                trimmedPath.Add(endPoint);
                                return trimmedPath;
                            }
                        }
                        
                        trimmedPath.Add(path[j]);
                        
                        if (j < path.Count - 1)
                        {
                            distToEnd -= segmentLengths[j];
                        }
                    }
                    
                    return trimmedPath;
                }
                
                accumulatedDist += segmentLength;
            }

            // Fallback: return original path if something went wrong
            return new List<Vector3>(path);
        }

        private void ClearLineRenderers()
        {
            foreach (var lr in lineRenderers)
            {
                if (lr != null) Destroy(lr.gameObject);
            }
            lineRenderers.Clear();
            
            LineRenderer mainLr = GetComponent<LineRenderer>();
            if (mainLr != null) mainLr.enabled = false;
        }

        private void UpdateSpriteVisuals()
        {
            int requiredBodyCount = Mathf.Max(0, currentPositions.Count - 1);
            
            // Add missing
            while (bodyVisuals.Count < requiredBodyCount)
            {
                GameObject bodyPart = new GameObject($"Body_{bodyVisuals.Count}");
                bodyPart.transform.SetParent(transform);
                bodyPart.AddComponent<SpriteRenderer>();
                bodyVisuals.Add(bodyPart);
            }
            
            // Remove excess
            while (bodyVisuals.Count > requiredBodyCount)
            {
                Destroy(bodyVisuals[bodyVisuals.Count - 1]);
                bodyVisuals.RemoveAt(bodyVisuals.Count - 1);
            }

            // Update positions and sprites
            Sprite bodySprite = null;
            float scale = 1.0f;
            Color color = arrowColor;
            bool rotate = false;

            if (currentTheme != null)
            {
                bodySprite = currentTheme.bodySprite;
                scale = currentTheme.bodyScale;
                if (currentTheme.overrideColor) color = currentTheme.themeColor;
                rotate = currentTheme.rotateBodySprites;
            }

            for (int i = 0; i < bodyVisuals.Count; i++)
            {
                // Body index i corresponds to currentPositions index i + 1
                Vector2Int pos = currentPositions[i + 1];
                GameObject visual = bodyVisuals[i];
                SpriteRenderer sr = visual.GetComponent<SpriteRenderer>();
                
                sr.sprite = bodySprite;
                sr.color = color;
                sr.sortingOrder = 1; // Below head
                visual.transform.position = new Vector3(pos.x, pos.y, 0);
                visual.transform.localScale = Vector3.one * scale;

                if (rotate)
                {
                    // Determine rotation based on previous node (towards head)
                    // The node ahead of this one is at index i
                    Vector2Int prevPos = currentPositions[i];
                    Vector2Int dir = prevPos - pos;
                    float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
                    visual.transform.rotation = Quaternion.Euler(0, 0, angle);
                }
                else
                {
                    visual.transform.rotation = Quaternion.identity;
                }
            }
        }

        private void ClearBodySprites()
        {
            foreach (var visual in bodyVisuals)
            {
                Destroy(visual);
            }
            bodyVisuals.Clear();
        }

        private void UpdateHeadRotation()
        {
            if (headVisual == null || currentPositions.Count == 0) return;
            
            Vector2Int dir = GetDirection();
            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            
            float rotationOffset = 0f;
            if (currentTheme != null) rotationOffset = currentTheme.headRotationOffset;
            else if (customArrowSprite != null) rotationOffset = customArrowRotationOffset;
            
            angle += rotationOffset;
            
            headVisual.transform.rotation = Quaternion.Euler(0, 0, angle);
            headVisual.transform.position = new Vector3(currentPositions[0].x, currentPositions[0].y, 0);
        }

        private void UpdateColliders()
        {
            // Destroy existing colliders
            foreach (var col in GetComponents<BoxCollider2D>())
            {
                if (Application.isPlaying) Destroy(col);
                else DestroyImmediate(col);
            }

            // Add new colliders
            foreach (var pos in currentPositions)
            {
                BoxCollider2D col = gameObject.AddComponent<BoxCollider2D>();
                col.offset = new Vector2(pos.x, pos.y);
                col.size = Vector2.one;
                col.isTrigger = true; 
            }
        }


        private void OnDestroy()
        {
            Debug.Log($"Arrow OnDestroy called");
            
            if (gridSystem != null)
            {
                foreach (var pos in currentPositions)
                {
                    // Only clear if this arrow is the one occupying the cell
                    // (Prevents clearing if somehow another arrow took over, though unlikely in this logic)
                    if (gridSystem.GetArrowAt(pos) == this)
                    {
                        gridSystem.ClearOccupancy(pos);
                    }
                }
            }
        }
    }
}
