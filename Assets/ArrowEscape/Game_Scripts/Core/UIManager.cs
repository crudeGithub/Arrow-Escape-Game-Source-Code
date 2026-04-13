using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

namespace Core
{
    public class UIManager : MonoBehaviour
    {
        public static UIManager Instance { get; private set; }

        [Header("In-Game UI")]
        public GameObject inGameUI;
        public TextMeshProUGUI movesText;
        public TextMeshProUGUI levelText;
        public TextMeshProUGUI coinText; // New Coin Text
        public Transform coinIconTransform; // Target for coin animation

        [Header("Panels")]
        public GameObject winPanel;
        public GameObject losePanel;

        [Header("Win/Lose Elements")]
        public TextMeshProUGUI winLevelText;
        public TextMeshProUGUI loseLevelText;
        public Image winImage;
        public Image loseImage;

        [Header("Buttons")]
        public Button winNextButton;
        public Button loseNextButton;
        public Button loseRetryButton;
        public Button gridVisualizerButton;
        public Button legalMovesButton;
        public Button skipLevelButton;
        public Button backgroundToggleButton;
        public Button shopButton;

        private bool isSequenceRunning = false;
        public bool IsSequenceRunning => isSequenceRunning;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void Start()
        {
            // Hide panels initially
            if (winPanel != null) winPanel.SetActive(false);
            if (losePanel != null) losePanel.SetActive(false);
            if (inGameUI != null) inGameUI.SetActive(true);

            // Ensure GridVisualizer exists
            if (Debugging.GridVisualizer.Instance == null)
            {
                GameObject gv = new GameObject("GridVisualizer");
                gv.AddComponent<Debugging.GridVisualizer>();
            }

            // Setup listeners
            if (winNextButton != null)
            {
                winNextButton.onClick.AddListener(OnNextLevelClicked);
                // Sound played in sequence now
            }
            if (loseNextButton != null)
            {
                // Only add NextLevel listener if it's NOT the same as skipLevelButton
                // If they are the same, we prioritize the Skip/Ad behavior
                if (skipLevelButton == null || loseNextButton != skipLevelButton)
                {
                    loseNextButton.onClick.AddListener(OnNextLevelClicked);
                    loseNextButton.onClick.AddListener(() => AudioManager.Instance?.PlayButtonSound());
                }
            }
            if (loseRetryButton != null)
            {
                loseRetryButton.onClick.AddListener(OnRetryClicked);
                loseRetryButton.onClick.AddListener(() => AudioManager.Instance?.PlayButtonSound());
            }
            if (gridVisualizerButton != null)
            {
                gridVisualizerButton.onClick.AddListener(OnGridVisualizerClicked);
                gridVisualizerButton.onClick.AddListener(() => AudioManager.Instance?.PlayButtonSound());
            }
            if (legalMovesButton != null)
            {
                legalMovesButton.onClick.AddListener(OnLegalMovesClicked);
                legalMovesButton.onClick.AddListener(() => AudioManager.Instance?.PlayButtonSound());
            }
            if (skipLevelButton != null)
            {
                skipLevelButton.onClick.AddListener(OnSkipLevelClicked);
                skipLevelButton.onClick.AddListener(() => AudioManager.Instance?.PlayButtonSound());
            }

            if (backgroundToggleButton != null)
            {
                backgroundToggleButton.onClick.AddListener(OnBackgroundToggleClicked);
                backgroundToggleButton.onClick.AddListener(() => AudioManager.Instance?.PlayButtonSound());
            }

            // Start checking for ad availability
            StartCoroutine(CheckAdAvailabilityRoutine());
            
            // Initialize Coin UI
            if (CurrencyManager.Instance != null)
            {
                UpdateCoinUI(CurrencyManager.Instance.Coins);
                CurrencyManager.Instance.OnCoinsChanged += UpdateCoinUI;
            }
        }

        private void OnDestroy()
        {
            if (CurrencyManager.Instance != null)
            {
                CurrencyManager.Instance.OnCoinsChanged -= UpdateCoinUI;
            }
        }

        private void UpdateCoinUI(int coins)
        {
            if (coinText != null)
            {
                coinText.text = coins.ToString();
                StartCoroutine(AnimatePopup(coinText.transform, 0.2f));
            }
        }

        public void UpdateMoves(int moves)
        {
            if (movesText != null)
            {
                StartCoroutine(AnimateTextChange(movesText, $"{moves}"));
            }
        }

        public void UpdateLevel(int level)
        {
            if (levelText != null)
            {
                levelText.text = $"Level {level}";
                StartCoroutine(AnimatePopup(levelText.transform, 0.2f));
            }
        }

        public void UpdatePowerUps(int gridCount, int hintCount)
        {
            if (gridVisualizerButton != null)
            {
                var text = gridVisualizerButton.GetComponentInChildren<TextMeshProUGUI>();
                if (text != null) text.text = $"({gridCount})";
            }
            
            if (legalMovesButton != null)
            {
                var text = legalMovesButton.GetComponentInChildren<TextMeshProUGUI>();
                if (text != null) text.text = $"({hintCount})";
            }
        }


        public void ShowWinUI(int level)
        {
            isSequenceRunning = false; // Reset flag when showing UI
            SetInGameUIActive(false);
            
            if (winPanel != null)
            {
                winPanel.SetActive(true);
                if (winLevelText != null) winLevelText.text = $"COMPLETED";
                if (winImage != null) StartCoroutine(AnimatePopup(winImage.transform, 0.5f));
            }
            
            if (losePanel != null) losePanel.SetActive(false);
        }

        public void ShowLoseUI(int level)
        {
            isSequenceRunning = false; // Reset flag when showing UI
            SetInGameUIActive(false);
            
            if (losePanel != null)
            {
                losePanel.SetActive(true);
                if (loseLevelText != null) loseLevelText.text = $"FAILED";
                if (loseImage != null) StartCoroutine(AnimatePopup(loseImage.transform, 0.5f));
            }
            
            if (winPanel != null) winPanel.SetActive(false);
        }

        public void HideAll()
        {
            isSequenceRunning = false;
            if (winPanel != null) winPanel.SetActive(false);
            if (losePanel != null) losePanel.SetActive(false);
            SetInGameUIActive(true);
        }

        private void SetInGameUIActive(bool active)
        {
            if (inGameUI != null)
            {
                inGameUI.SetActive(active);
            }
            else
            {
                // Fallback: hide individual elements if container is missing
                if (movesText != null) movesText.gameObject.SetActive(active);
                if (levelText != null) levelText.gameObject.SetActive(active);
                if (gridVisualizerButton != null) gridVisualizerButton.gameObject.SetActive(active);
            }
        }

        private void OnNextLevelClicked()
        {
            if (isSequenceRunning) return;
            if (shopButton != null) shopButton.interactable = false;
            StartCoroutine(NextLevelSequence());
        }

        private IEnumerator NextLevelSequence()
        {
            isSequenceRunning = true;
            if (shopButton != null) shopButton.interactable = false;

            // Play button sound
            AudioManager.Instance?.PlayButtonSound();

            // Award coins and play animation locally
            int rewardAmount = 50;
            if (CurrencyManager.Instance != null)
            {
                CurrencyManager.Instance.AddCoins(rewardAmount);
                // Trigger Animation from center of screen by default
                CoinFeedbackManager.Instance?.PlayCoinAnimation(Vector3.zero); 
            }

            // Wait for animation to finish (approx 1.5s)
            yield return new WaitForSeconds(1.5f);

            HideAll();
            LevelManager.Instance?.LoadNextLevel();
            
            // Note: isSequenceRunning will be reset in ShowWinUI/ShowLoseUI of next level or Reload
        }

        private void OnRetryClicked()
        {
            if (isSequenceRunning) return;
            isSequenceRunning = true;
            if (shopButton != null) shopButton.interactable = false;

            HideAllLines();
            HideAll();
            LevelManager.Instance?.ReloadCurrentLevel();
        }

        private void OnGridVisualizerClicked()
        {
            if (LevelManager.Instance == null) return;

            // If already unlocked, just toggle
            if (LevelManager.Instance.GridUnlockedForLevel)
            {
                Debugging.GridVisualizer.Instance?.ToggleVisualizer();
                return;
            }

            // Try to use existing power-up
            if (LevelManager.Instance.TryUseGridPowerUp())
            {
                Debugging.GridVisualizer.Instance?.SetVisualizerState(true);
            }
            else 
            {
                // Show purchase dialog with both options
                if (PurchaseDialog.Instance != null)
                {
                    PurchaseDialog.Instance.ShowDialog(
                        title: "Get Grid Visualizer",
                        description: "Choose how to get 3 Grid PowerUps",
                        coinCost: 100,
                        onCoinPurchaseCallback: () => {
                            LevelManager.Instance.AddGridPowerUps(3);
                            Debug.Log("Purchased 3 Grid PowerUps with coins!");
                        },
                        onAdPurchaseCallback: () => {
                            LevelManager.Instance.AddGridPowerUps(3);
                            Debug.Log("Earned 3 Grid PowerUps from ad!");
                        }
                    );
                }
            }
        }

        private void OnLegalMovesClicked()
        {
            if (LevelManager.Instance == null) return;

            // Try to use existing power-up
            if (LevelManager.Instance.TryUseHintPowerUp())
            {
                LevelManager.Instance.ShowLegalMoves();
            }
            else 
            {
                // Show purchase dialog with both options
                if (PurchaseDialog.Instance != null)
                {
                    PurchaseDialog.Instance.ShowDialog(
                        title: "Get Hints",
                        description: "Choose how to get 3 Hint PowerUps",
                        coinCost: 100,
                        onCoinPurchaseCallback: () => {
                            LevelManager.Instance.AddHintPowerUps(3);
                            Debug.Log("Purchased 3 Hint PowerUps with coins!");
                        },
                        onAdPurchaseCallback: () => {
                            LevelManager.Instance.AddHintPowerUps(3);
                            Debug.Log("Earned 3 Hint PowerUps from ad!");
                        }
                    );
                }
            }
        }

        private void OnSkipLevelClicked()
        {
            if (LevelManager.Instance == null || isSequenceRunning) return;

             // Check Ads or Coins
            if (AdsManager.Instance != null && AdsManager.Instance.enableAds)
            {
                isSequenceRunning = true;
                if (shopButton != null) shopButton.interactable = false;

                AdsManager.Instance.ShowRewardedAd(() => {
                    LevelManager.Instance.LoadNextLevel();
                    Debug.Log("Ad watched! Level skipped.");
                }, () => {
                    isSequenceRunning = false;
                    if (shopButton != null) shopButton.interactable = true;
                    Debug.Log("Ad failed or cancelled.");
                });
            }
            else
            {
                // Use Coins (Skip is more expensive)
                int cost = 200;
                if (CurrencyManager.Instance != null && CurrencyManager.Instance.SpendCoins(cost))
                {
                    isSequenceRunning = true;
                    if (shopButton != null) shopButton.interactable = false;

                    LevelManager.Instance.LoadNextLevel();
                    Debug.Log($"Spent {cost} coins! Level skipped.");
                }
            }
        }
        
        private void OnBackgroundToggleClicked()
        {
            CameraBackgroundToggler.Instance?.ToggleBackground();
        }
        
        private void HideAllLines()
        {
             Debugging.GridVisualizer.Instance?.SetVisualizerState(false);
        }

        private IEnumerator AnimateTextChange(TextMeshProUGUI target, string newContent)
        {
            float duration = 0.15f;
            Vector3 originalScale = Vector3.one;
            Vector3 targetScale = Vector3.one * 1.4f;

            // Scale Up
            float elapsed = 0;
            while (elapsed < duration)
            {
                target.transform.localScale = Vector3.Lerp(originalScale, targetScale, elapsed / duration);
                elapsed += Time.deltaTime;
                yield return null;
            }

            target.text = newContent;

            // Scale Down
            elapsed = 0;
            while (elapsed < duration)
            {
                target.transform.localScale = Vector3.Lerp(targetScale, originalScale, elapsed / duration);
                elapsed += Time.deltaTime;
                yield return null;
            }
            target.transform.localScale = originalScale;
        }

        private IEnumerator AnimatePopup(Transform target, float duration)
        {
            target.localScale = Vector3.zero;
            float elapsed = 0;

            while (elapsed < duration)
            {
                float t = elapsed / duration;
                // Elastic ease out
                float scale = Mathf.Sin(-13 * (t + 1) * Mathf.PI * 0.5f) * Mathf.Pow(2, -10 * t) + 1;
                
                target.localScale = Vector3.one * scale;
                elapsed += Time.deltaTime;
                yield return null;
            }
            target.localScale = Vector3.one;
        }

        private IEnumerator CheckAdAvailabilityRoutine()
        {
            var wait = new WaitForSeconds(1.0f);
            while (true)
            {
                bool adReady = AdsManager.Instance != null && AdsManager.Instance.IsRewardedAdReady();
                bool canInteract = !isSequenceRunning;

                // Update Shop Button
                if (shopButton != null)
                {
                    shopButton.interactable = canInteract;
                }
                
                // Update PowerUp buttons
                if (LevelManager.Instance != null)
                {
                    if (gridVisualizerButton != null)
                    {
                        // Enable if we have powerups OR if we can buy with coins OR if ads are available
                        bool hasCoins = CurrencyManager.Instance != null && CurrencyManager.Instance.Coins >= 100;
                        bool hasAds = AdsManager.Instance != null && AdsManager.Instance.enableAds && adReady;
                        bool canBuy = hasCoins || hasAds;

                        bool canUseGrid = LevelManager.Instance.GridPowerUps > 0 || canBuy;
                        gridVisualizerButton.interactable = canUseGrid && canInteract;
                    }

                    // Hint Button
                    if (legalMovesButton != null)
                    {
                        bool hasCoins = CurrencyManager.Instance != null && CurrencyManager.Instance.Coins >= 100;
                        bool hasAds = AdsManager.Instance != null && AdsManager.Instance.enableAds && adReady;
                        bool canBuy = hasCoins || hasAds;

                        bool canUseHint = LevelManager.Instance.HintPowerUps > 0 || canBuy;
                        legalMovesButton.interactable = canUseHint && canInteract;
                    }
                    
                    // Skip Level Button
                    if (skipLevelButton != null)
                    {
                        if (AdsManager.Instance != null && !AdsManager.Instance.enableAds)
                        {
                            skipLevelButton.interactable = canInteract && CurrencyManager.Instance != null && CurrencyManager.Instance.Coins >= 200;
                        }
                        else
                        {
                            skipLevelButton.interactable = canInteract && adReady;
                        }
                    }
                }

                yield return wait;
            }
        }
    }
}
