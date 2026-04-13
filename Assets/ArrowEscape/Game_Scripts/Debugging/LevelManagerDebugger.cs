using UnityEngine;
using Core;

namespace Debugging
{
    /// <summary>
    /// Add this to your LevelManager GameObject to verify setup
    /// </summary>
    public class LevelManagerDebugger : MonoBehaviour
    {
        private LevelManager levelManager;

        void Start()
        {
            levelManager = GetComponent<LevelManager>();
            
            if (levelManager == null)
            {
                Debug.LogError("❌ LevelManager component not found!");
                return;
            }

            Debug.Log("=== LEVEL MANAGER SETUP VERIFICATION ===");
            
            // Check levels list
            if (levelManager.levels == null || levelManager.levels.Count == 0)
            {
                Debug.LogError("❌ CRITICAL: Levels list is empty! Add LevelData assets to the Levels list.");
            }
            else
            {
                Debug.Log($"✅ Levels list has {levelManager.levels.Count} level(s)");
                
                for (int i = 0; i < levelManager.levels.Count; i++)
                {
                    if (levelManager.levels[i] == null)
                    {
                        Debug.LogError($"❌ Level {i} is NULL! Assign a LevelData asset.");
                    }
                    else
                    {
                        var level = levelManager.levels[i];
                        Debug.Log($"  Level {i + 1}: Grid={level.gridDimensions}, MaxMoves={level.maxMoves}, Arrows={level.arrows.Count}");
                        
                        if (level.maxMoves <= 0)
                        {
                            Debug.LogWarning($"⚠️ Level {i + 1} has maxMoves={level.maxMoves}. Should be > 0!");
                        }
                        
                        if (level.arrows.Count == 0)
                        {
                            Debug.LogWarning($"⚠️ Level {i + 1} has no arrows!");
                        }
                    }
                }
            }
            
            // Check arrow prefab
            if (levelManager.arrowPrefab == null)
            {
                Debug.LogError("❌ CRITICAL: Arrow Prefab is not assigned!");
            }
            else
            {
                Debug.Log($"✅ Arrow Prefab assigned: {levelManager.arrowPrefab.name}");
                
                var arrowUnit = levelManager.arrowPrefab.GetComponent<ArrowUnit>();
                if (arrowUnit == null)
                {
                    Debug.LogError("❌ Arrow Prefab doesn't have ArrowUnit component!");
                }
                else
                {
                    Debug.Log("✅ Arrow Prefab has ArrowUnit component");
                }
            }
            

            Debug.Log($"Current Level Index: {levelManager.currentLevelIndex}");
            Debug.Log("=== END VERIFICATION ===");
        }
    }
}
