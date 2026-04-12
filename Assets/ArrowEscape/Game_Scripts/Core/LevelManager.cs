using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Data;
using TMPro;

namespace Core
{
    public class LevelManager : MonoBehaviour
    {
        public static LevelManager Instance { get; private set; }

        [Header("Level Configuration")]
        public List<LevelData> levels = new List<LevelData>();
        public int currentLevelIndex = 0;
        public int GridPowerUps { get; private set; }
        public int HintPowerUps { get; private set; }
        
        public bool GridUnlockedForLevel { get; private set; }

        private const string PREF_LEVEL_INDEX = "CurrentLevelIndex";
        private const string PREF_GRID_POWERUPS = "GridPowerUps";
        private const string PREF_HINT_POWERUPS = "HintPowerUps";
        

        [Header("Prefabs")]
        public GameObject arrowPrefab;
        public GameObject dotPrefab; // Prefab for the background dot
        public GridCelebration gridCelebration; // Handles dot visuals and celebration

        [Header("UI (Optional)")]
        public TextMeshProUGUI movesText;
        public TextMeshProUGUI levelText;

        public GridSystem GridSystem => gridSystem;
        private GridSystem gridSystem;
        private int movesRemaining;
        private int totalArrows;
        public List<ArrowUnit> ActiveArrows => activeArrows;
        private List<ArrowUnit> activeArrows = new List<ArrowUnit>();
        private List<GameObject> dotObjects = new List<GameObject>();
        private Coroutine hintCoroutine; // Track hint coroutine to prevent overlapping hints
        private bool isLevelLoading = false;

        void Awake()
        {
            // Singleton pattern
            if (Instance == null) Instance = this;
            else Destroy(gameObject);
        }

        void Start()
        {
            // Auto-assign UI references to UIManager if they are set here but not there
            if (UIManager.Instance != null)
            {
                if (UIManager.Instance.movesText == null) UIManager.Instance.movesText = movesText;
                if (UIManager.Instance.levelText == null) UIManager.Instance.levelText = levelText;
            }

            LoadData();

            if (levels.Count > 0)
                LoadLevel(currentLevelIndex);
            else
                Debug.LogError("No levels assigned to LevelManager!");
        }

        public bool CanMakeMove()
        {
            return movesRemaining > 0;
        }

        public bool IsGameActive { get; private set; }

        private Vector3 defaultCameraPos;
        private float defaultCameraSize;

        void LoadLevel(int levelIndex)
        {
            isLevelLoading = false; // Reset loading flag

            // Reset UI
            UIManager.Instance?.HideAll();

            ClearLevel();
            
            // Reset level specific states
            GridUnlockedForLevel = false;
            Debugging.GridVisualizer.Instance?.SetVisualizerState(false);

            if (levelIndex < 0 || levelIndex >= levels.Count)
            {
                Debug.LogError($"Invalid level index: {levelIndex}");
                return;
            }

            LevelData levelData = levels[levelIndex];
            currentLevelIndex = levelIndex;
            movesRemaining = levelData.maxMoves;
            totalArrows = levelData.arrows.Count;
            IsGameActive = true;

            // Initialize grid system
            gridSystem = new GridSystem(levelData.gridDimensions.x, levelData.gridDimensions.y);

            // Center Camera
            defaultCameraPos = new Vector3(
                levelData.gridDimensions.x / 2f - 0.5f,
                levelData.gridDimensions.y / 2f - 0.5f,
                -10);
            
            Camera.main.transform.position = defaultCameraPos;

            float screenRatio = (float)Screen.width / Screen.height;
            float targetSizeY = levelData.gridDimensions.y / 2f + 1f;
            float targetSizeX = (levelData.gridDimensions.x / 2f + 1f) / screenRatio;
            
            defaultCameraSize = Mathf.Max(targetSizeY, targetSizeX);
            Camera.main.orthographicSize = defaultCameraSize;

            // Setup Camera Controller
            var camController = Camera.main.gameObject.GetComponent<CameraController>();
            if (camController == null) camController = Camera.main.gameObject.AddComponent<CameraController>();
            camController.SetBounds(levelData.gridDimensions);

            // Spawn arrows
            foreach (var arrowDef in levelData.arrows)
                SpawnArrow(arrowDef);

            // Create background dots only at cells that were initially occupied by arrows
            CreateDots(levelData);

            UpdateUI();
            UIManager.Instance?.UpdateLevel(currentLevelIndex + 1);
            UIManager.Instance?.UpdatePowerUps(GridPowerUps, HintPowerUps);
            Debug.Log($"Level {levelIndex + 1} loaded. Moves: {movesRemaining}");
        }

        void ClearLevel()
        {
            // Destroy arrows
            foreach (var arrow in activeArrows)
                if (arrow != null) Destroy(arrow.gameObject);
            activeArrows.Clear();

            // Destroy dots
            foreach (var dot in dotObjects)
                if (dot != null) Destroy(dot);
            dotObjects.Clear();
        }

        void SpawnArrow(ArrowDefinition def)
        {
            if (arrowPrefab == null)
            {
                Debug.LogError("Arrow Prefab is missing in LevelManager!");
                return;
            }
            GameObject go = Instantiate(arrowPrefab);
            ArrowUnit unit = go.GetComponent<ArrowUnit>();
            if (unit == null) unit = go.AddComponent<ArrowUnit>();
            
            // Assign Selected Theme
            if (ThemeManager.Instance != null)
            {
                unit.currentTheme = ThemeManager.Instance.GetSelectedTheme();
            }
            
            unit.Initialize(def.occupiedPositions, def.arrowColor, gridSystem);
            unit.OnArrowMoved += OnArrowMoved;
            activeArrows.Add(unit);
        }

        // Create background dot sprites only at cells that were initially occupied by arrows.
        void CreateDots(LevelData levelData)
        {
            if (dotPrefab == null)
            {
                Debug.LogWarning("Dot Prefab not assigned; skipping dot creation.");
                return;
            }

            // Collect all unique cell positions that were initially occupied by arrows
            HashSet<Vector2Int> occupiedCells = new HashSet<Vector2Int>();
            foreach (var arrowDef in levelData.arrows)
            {
                foreach (var pos in arrowDef.occupiedPositions)
                {
                    occupiedCells.Add(pos);
                }
            }

            // Create a dot at each initially occupied cell
            foreach (var cellPos in occupiedCells)
            {
                // Position at cell centre with a slight negative Z to sit behind arrows.
                Vector3 pos = new Vector3(cellPos.x, cellPos.y, -0.5f);
                GameObject dot = Instantiate(dotPrefab, pos, Quaternion.identity);
                // Parent under LevelManager for organization; GridCelebration will animate its children.
                dot.transform.parent = this.transform;
                // Ensure the dot renders behind arrows (if using SpriteRenderer).
                var sr = dot.GetComponent<SpriteRenderer>();
                if (sr != null) sr.sortingOrder = -1;
                dotObjects.Add(dot);
            }

            Debug.Log($"Created {dotObjects.Count} background dots at initially occupied cells.");
        }



        public void OnArrowMoved()
        {
            movesRemaining--;
            UpdateUI();
            Debug.Log($"Move made! Moves remaining: {movesRemaining}");
            if (movesRemaining == 0)
                Debug.LogWarning("Out of moves! No more arrows can be moved.");

            // Cancel any previous checks and start a fresh repeated check
            CancelInvoke(nameof(CheckGameState));
            InvokeRepeating(nameof(CheckGameState), 0.5f, 0.5f);
        }

        void CheckGameState()
        {
            // 1. Check Win Condition first (this also cleans up destroyed arrows)
            if (CheckWinCondition())
            {
                CancelInvoke(nameof(CheckGameState));
                Debug.Log("Level Complete! Starting celebration...");

                // Reset Camera Pan/Zoom when winning
                var camController = Camera.main.gameObject.GetComponent<CameraController>();
                if (camController != null)
                {
                    camController.ResetCamera(defaultCameraPos, defaultCameraSize, 0.5f);
                }
                
                // Trigger Win Sequence
                if (gridCelebration != null)
                {
                    gridCelebration.Celebrate(() => OnLevelWin());
                }
                else
                {
                    OnLevelWin();
                }
                return;
            }

            // 2. If valid arrows exist, check if any are still moving.
            // If so, we postpone the State Check until they finish.
            bool isAnyArrowMoving = false;
            foreach (var arrow in activeArrows)
            {
                if (arrow != null && arrow.IsMoving)
                {
                    isAnyArrowMoving = true;
                    break;
                }
            }

            if (isAnyArrowMoving)
            {
                return; // Wait for next tick
            }

            // 3. If nothing is moving and we haven't won, check Lose Condition
            if (movesRemaining <= 0)
            {
                CancelInvoke(nameof(CheckGameState));
                Debug.Log("Out of moves! Level Failed!");
                OnLevelLose();
            }
        }

        void OnLevelWin()
        {
            IsGameActive = false;
            Debug.Log("Level Won! Showing UI.");
            
            // Coins are now awarded in UIManager when player clicks Next
            
            // Save progress for next level
            SaveProgress(currentLevelIndex + 1); 

            VFXManager.Instance?.PlayWinEffect();
            UIManager.Instance?.ShowWinUI(currentLevelIndex + 1);
        }

        void OnLevelLose()
        {
            IsGameActive = false;
            Debug.Log("Level Lost! Showing UI.");
            AudioManager.Instance?.PlayLoseSound();
            VFXManager.Instance?.PlayLoseEffect();
            UIManager.Instance?.ShowLoseUI(currentLevelIndex + 1);
        }

        bool CheckWinCondition()
        {
            // Clean up any null references first
            activeArrows.RemoveAll(a => a == null || ReferenceEquals(a, null));
            int activeCount = 0;
            foreach (var arrow in activeArrows)
            {
                if (arrow != null && arrow.gameObject != null && arrow.gameObject.activeInHierarchy)
                    activeCount++;
            }
            Debug.Log($"Active arrows remaining: {activeCount} / {totalArrows}");
            return activeCount == 0;
        }

        public void LoadNextLevel()
        {
            if (isLevelLoading) return;
            isLevelLoading = true;

            Debug.Log($"LoadNextLevel called. Current: {currentLevelIndex}, Total levels: {levels.Count}");
            int next = currentLevelIndex + 1;
            if (next < levels.Count)
            {
                LoadLevel(next);
            }
            else
            {
                Debug.Log("All levels completed! You win! Looping back to level 1.");
                LoadLevel(0);
                SaveProgress(0); // Optional: Reset to level 1 on completion or keep at max
            }
        }

        public void ReloadCurrentLevel()
        {
            Debug.Log($"Reloading current level: {currentLevelIndex + 1}");
            LoadLevel(currentLevelIndex);
        }

        void UpdateUI()
        {
            UIManager.Instance?.UpdateMoves(movesRemaining);
        }

        // UI button hooks
        public void OnRestartButtonClicked() => ReloadCurrentLevel();
        public void OnSkipLevelButtonClicked() => LoadNextLevel();

        public void ShowLegalMoves()
        {
            // Stop any existing hint coroutine first
            if (hintCoroutine != null)
            {
                StopCoroutine(hintCoroutine);
                hintCoroutine = null;
            }
            
            hintCoroutine = StartCoroutine(ShowLegalMovesRoutine());
        }

        private IEnumerator ShowLegalMovesRoutine()
        {
            List<ArrowUnit> highlightedArrows = new List<ArrowUnit>();

            foreach (var arrow in activeArrows)
            {
                // Only highlight arrows that are not moving and can move
                if (arrow != null && !arrow.IsMoving && arrow.CanMove())
                {
                    arrow.Highlight(true);
                    highlightedArrows.Add(arrow);
                }
            }

            if (highlightedArrows.Count > 0)
            {
                AudioManager.Instance?.PlayButtonSound(); // Or a specific powerup sound
            }

            yield return new WaitForSeconds(2.0f);

            foreach (var arrow in highlightedArrows)
            {
                if (arrow != null)
                {
                    arrow.Highlight(false);
                }
            }
        }

        private void LoadData()
        {
            currentLevelIndex = PlayerPrefs.GetInt(PREF_LEVEL_INDEX, 0);
            currentLevelIndex = PlayerPrefs.GetInt(PREF_LEVEL_INDEX, 0);
            GridPowerUps = PlayerPrefs.GetInt(PREF_GRID_POWERUPS, 1); // Default 1 free
            HintPowerUps = PlayerPrefs.GetInt(PREF_HINT_POWERUPS, 1); // Default 1 free
            
            // Validate index
            if (currentLevelIndex >= levels.Count && levels.Count > 0)
                currentLevelIndex = 0; // Reset if out of bounds (e.g. new levels removed)
        }

        private void SaveProgress(int nextLevelIndex)
        {
            // Only save if we are advancing
            if (nextLevelIndex > PlayerPrefs.GetInt(PREF_LEVEL_INDEX, 0))
            {
                PlayerPrefs.SetInt(PREF_LEVEL_INDEX, nextLevelIndex);
            }
            SavePowerUps();
        }
        
        private void SavePowerUps()
        {
            PlayerPrefs.SetInt(PREF_GRID_POWERUPS, GridPowerUps);
            PlayerPrefs.SetInt(PREF_HINT_POWERUPS, HintPowerUps);
            PlayerPrefs.Save();
        }

        public void AddGridPowerUps(int amount)
        {
            GridPowerUps += amount;
            SavePowerUps();
            UIManager.Instance?.UpdatePowerUps(GridPowerUps, HintPowerUps);
        }

        public void AddHintPowerUps(int amount)
        {
            HintPowerUps += amount;
            SavePowerUps();
            UIManager.Instance?.UpdatePowerUps(GridPowerUps, HintPowerUps);
        }
        
        public bool TryUseGridPowerUp()
        {
            if (GridUnlockedForLevel) return true;
            
            if (GridPowerUps > 0)
            {
                GridPowerUps--;
                GridUnlockedForLevel = true;
                SavePowerUps();
                UIManager.Instance?.UpdatePowerUps(GridPowerUps, HintPowerUps);
                return true;
            }
            return false;
        }


        public bool TryUseHintPowerUp()
        {
            if (HintPowerUps > 0)
            {
                HintPowerUps--;
                SavePowerUps();
                UIManager.Instance?.UpdatePowerUps(GridPowerUps, HintPowerUps);
                return true;
            }
            return false;
        }


        [ContextMenu("Reset Progress")]
        public void ResetProgress()
        {
            PlayerPrefs.DeleteKey(PREF_LEVEL_INDEX);
            PlayerPrefs.DeleteKey(PREF_LEVEL_INDEX);
            PlayerPrefs.DeleteKey(PREF_GRID_POWERUPS);
            PlayerPrefs.DeleteKey(PREF_HINT_POWERUPS);
            PlayerPrefs.Save();
            Debug.Log("Player progress reset!");
            
            // Reset Themes
            ThemeManager.Instance?.ResetThemes();
            
            // If game is running, update state
            if (Application.isPlaying)
            {
                currentLevelIndex = 0;
                currentLevelIndex = 0;
                GridPowerUps = 1;
                HintPowerUps = 1;
                LoadLevel(0);
                LoadLevel(0);
                UIManager.Instance?.UpdatePowerUps(1, 1);
            }
        }
    }
}
