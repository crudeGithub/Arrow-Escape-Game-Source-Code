using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace Core
{
    public class ThemeManager : MonoBehaviour
    {
        public static ThemeManager Instance { get; private set; }

        [Header("Themes")]
        public List<ArrowTheme> allThemes = new List<ArrowTheme>();

        private const string PREF_UNLOCKED_THEMES = "UnlockedThemes";
        private const string PREF_SELECTED_THEME = "SelectedTheme";

        private HashSet<string> unlockedThemeIds = new HashSet<string>();
        private string selectedThemeId;

        public System.Action OnThemeChanged;

        void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                LoadData();
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void LoadData()
        {
            // Load unlocked themes
            string unlockedString = PlayerPrefs.GetString(PREF_UNLOCKED_THEMES, "");
            if (string.IsNullOrEmpty(unlockedString))
            {
                // First time: unlock default themes
                foreach (var theme in allThemes)
                {
                    if (theme.isUnlockedByDefault)
                    {
                        unlockedThemeIds.Add(theme.id);
                    }
                }
                SaveUnlockedThemes();
            }
            else
            {
                string[] ids = unlockedString.Split(',');
                foreach (var id in ids)
                {
                    if (!string.IsNullOrEmpty(id))
                        unlockedThemeIds.Add(id);
                }
            }

            // Load selected theme
            selectedThemeId = PlayerPrefs.GetString(PREF_SELECTED_THEME, "");
            
            // If no selected theme or selected theme is invalid, select the first unlocked one
            if (string.IsNullOrEmpty(selectedThemeId) || !IsThemeUnlocked(selectedThemeId))
            {
                var defaultTheme = allThemes.FirstOrDefault(t => t.isUnlockedByDefault);
                if (defaultTheme != null)
                {
                    selectedThemeId = defaultTheme.id;
                    SaveSelectedTheme();
                }
                else if (allThemes.Count > 0)
                {
                    // Fallback to first available if no defaults
                    selectedThemeId = allThemes[0].id;
                    unlockedThemeIds.Add(selectedThemeId);
                    SaveSelectedTheme();
                    SaveUnlockedThemes();
                }
            }
        }

        public bool IsThemeUnlocked(string id)
        {
            return unlockedThemeIds.Contains(id);
        }

        public bool IsThemeSelected(string id)
        {
            return selectedThemeId == id;
        }

        public ArrowTheme GetSelectedTheme()
        {
            return allThemes.FirstOrDefault(t => t.id == selectedThemeId);
        }

        public void SelectTheme(string id)
        {
            if (IsThemeUnlocked(id))
            {
                selectedThemeId = id;
                SaveSelectedTheme();
                OnThemeChanged?.Invoke();
                Debug.Log($"Selected theme: {id}");
            }
        }


        public void UnlockTheme(string id)
        {
            if (!unlockedThemeIds.Contains(id))
            {
                unlockedThemeIds.Add(id);
                SaveUnlockedThemes();
            }
        }

        private void SaveUnlockedThemes()
        {
            string data = string.Join(",", unlockedThemeIds);
            PlayerPrefs.SetString(PREF_UNLOCKED_THEMES, data);
            PlayerPrefs.Save();
        }

        private void SaveSelectedTheme()
        {
            PlayerPrefs.SetString(PREF_SELECTED_THEME, selectedThemeId);
            PlayerPrefs.Save();
        }
        
        [ContextMenu("Reset Themes")]
        [ContextMenu("Reset Themes")]
        public void ResetThemes()
        {
             PlayerPrefs.DeleteKey(PREF_UNLOCKED_THEMES);
             PlayerPrefs.DeleteKey(PREF_SELECTED_THEME);
             PlayerPrefs.Save();
             
             unlockedThemeIds.Clear();
             selectedThemeId = "";
             
             LoadData();
             OnThemeChanged?.Invoke();
        }
    }
}
